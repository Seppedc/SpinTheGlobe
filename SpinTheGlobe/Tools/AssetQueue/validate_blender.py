"""Validate an already saved queue Blender output, export same-source FBX, render without resaving."""
import argparse, hashlib, json, math, sys
from pathlib import Path
import bpy
from mathutils import Vector

PROJECT=Path(__file__).resolve().parents[2]
def safe(path):
    p=(PROJECT/path).resolve()
    if not p.is_relative_to(PROJECT):raise ValueError('Outside project')
    return p
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def rel(path):return Path(path).resolve().relative_to(PROJECT).as_posix()
parser=argparse.ArgumentParser();parser.add_argument('--job',required=True)
opt=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
job=json.loads(safe(opt.job).read_text(encoding='utf-8-sig'));folder=safe(job['build_output_path']);slug=job['slug']
blend=Path(bpy.data.filepath).resolve()
assert blend.is_file() and blend.is_relative_to(folder),'Open the job output .blend first'
embedded=bpy.data.texts.get('GENERATE_'+slug+'.py')
assert embedded,'Missing embedded generator'
candidates=[p for p in folder.glob('*.py') if p.read_text(encoding='utf-8-sig')==embedded.as_string()]
assert candidates,'External source must match embedded source exactly'
generator=candidates[0]
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert meshes and all(o.data.vertices and o.data.polygons for o in meshes),'Empty geometry'
assert len({o.name for o in meshes})==len(meshes)
assert all(o.data.materials and all(m and m.use_nodes and m.node_tree.nodes.get('Principled BSDF') for m in o.data.materials) for o in meshes),'Missing named Principled material'
assert all((o.scale-Vector((1,1,1))).length<1e-5 for o in meshes),'Unapplied mesh scale'
assert bpy.context.scene.unit_settings.system=='METRIC' and abs(bpy.context.scene.unit_settings.scale_length-1)<1e-8
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();points=[];triangles=0
for obj in meshes:
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    triangles+=len(mesh.loop_triangles);points.extend(obj.matrix_world@v.co for v in mesh.vertices);evaluated.to_mesh_clear()
minimum=Vector(tuple(min(p[i] for p in points) for i in range(3)));maximum=Vector(tuple(max(p[i] for p in points) for i in range(3)))
dimensions=maximum-minimum
assert min(dimensions)>0 and max(dimensions)<100,'Implausible or degenerate asset dimensions'
assert abs(minimum.z)<1e-4,'Asset base must be at zero'
root=bpy.data.objects.get('ROOT_'+slug)
assert root and root.location.length<1e-6,'Missing asset root at origin'
assert all(o.parent for o in meshes),'Missing asset hierarchy'
for image in bpy.data.images:
    if image.source=='FILE' and image.filepath:
        assert image.packed_file or Path(bpy.path.abspath(image.filepath)).is_file(),'Missing texture '+image.filepath
fbx=folder/(slug+'.fbx')
if not fbx.exists():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root]+meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
assert fbx.stat().st_size>1024
report={'status':'PASS','blend':rel(blend),'blend_sha256':sha(blend),'fbx':rel(fbx),'fbx_sha256':sha(fbx),
        'generator':rel(generator),'generator_sha256':sha(generator),'embedded_source_exact':True,
        'mesh_count':len(meshes),'triangles':triangles,'dimensions_m':list(dimensions),
        'root_at_origin':True,'minimum_m':list(minimum),'maximum_m':list(maximum),
        'materials':sorted({m.name for o in meshes for m in o.data.materials}),
        'moving_part_candidates':[o.name for o in bpy.context.scene.objects if any(n in o.name.lower() for n in ['pivot','lid','sphere','gear'])],
        'expected_dimensions_review':'Actual dimensions recorded; asset-specific fit remains reviewable, no visual approval implied.'}
(folder/'blender-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(folder/'review_manifest.json').write_text(json.dumps({'kind':'model','asset_paths':[job['output_path']+'/'+fbx.name],'prefab_path':job['output_path']+'/'+slug+'_Review.prefab'},indent=2))
# Temporary render setup only. Do not save it into the canonical source.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
scene.render.resolution_x=900;scene.render.resolution_y=700;scene.render.resolution_percentage=100
center=(minimum+maximum)*.5;span=max(dimensions)
for obj in list(scene.objects):
    if obj.type in {'CAMERA','LIGHT'}:bpy.data.objects.remove(obj,do_unlink=True)
camera_data=bpy.data.cameras.new('QUEUE_PREVIEW');camera=bpy.data.objects.new('QUEUE_PREVIEW',camera_data);scene.collection.objects.link(camera)
camera.location=center+Vector((1,-1.6,1))*span
camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=span*1.5;scene.camera=camera
world=bpy.data.worlds.new('QUEUE_PREVIEW');world.use_nodes=True;world.node_tree.nodes['Background'].inputs['Strength'].default_value=.15;scene.world=world
for index,offset in enumerate([(1,-1,2),(-1,.5,1)]):
    data=bpy.data.lights.new('QUEUE_LIGHT_'+str(index),'AREA');data.energy=80*span*span;data.size=span*2
    light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset)*span
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(folder/'blender-preview.png');bpy.ops.render.render(write_still=True)
print('Blender output validation PASS',json.dumps(report))
