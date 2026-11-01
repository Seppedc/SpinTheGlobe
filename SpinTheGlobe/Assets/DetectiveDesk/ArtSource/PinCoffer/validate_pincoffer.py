"""Run in background Blender after opening PinCoffer.blend. Does not save or edit the model."""
import bpy, json, sys, argparse
from pathlib import Path
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--report',type=Path,required=True)
parser.add_argument('--preview',type=Path)
opt=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
path=Path(bpy.data.filepath)
source=path.with_name('build_pincoffer.py')
assert bpy.data.texts['GENERATE_PinCoffer.py'].as_string()==source.read_text(encoding='utf-8')
collection=bpy.data.collections['ART_PinCoffer']
objects=list(collection.all_objects)
meshes=[o for o in objects if o.type=='MESH']
slots=sorted([o for o in objects if o.name.startswith('PinSlot_')],key=lambda o:o['slot_index'])
cavities=[o for o in meshes if o.name.startswith('Velvet | cavity_')]
rings=[o for o in meshes if o.name.startswith('Brass | socket_ring_')]
assert len(slots)==len(cavities)==len(rings)==12
assert len({round(o.location.x,7) for o in slots})==4
assert len({round(o.location.y,7) for o in slots})==3
hits=[]
for i,slot in enumerate(slots,1):
    assert slot['slot_index']==i and slot['row']==(i-1)//4 and slot['column']==(i-1)%4
    center=slot.matrix_world.translation
    origin=Vector((center.x,center.y,.06))
    cavity=bpy.data.objects['Velvet | cavity_%02d'%i]
    ring=bpy.data.objects['Brass | socket_ring_%02d'%i]
    def cast(obj):
        inverse=obj.matrix_world.inverted()
        hit,point,normal,index=obj.ray_cast(inverse@origin,inverse.to_3x3()@Vector((0,0,-1)))
        return hit,obj.matrix_world@point
    hit,point=cast(cavity)
    assert hit and abs(point.z-.016)<1e-6, 'Socket center must hit recessed floor, not top'
    assert not cast(ring)[0], 'Brass ring must have an open center'
    assert .023-point.z>.0069
    hits.append({'slot':i,'row':slot['row']+1,'column':slot['column']+1,'center':list(center),'recess_depth':.023-point.z})
pivot=bpy.data.objects['LID_PIVOT | animate X rotation']
lid=bpy.data.objects['Lid | solid wooden panel']
base=bpy.data.objects['Chest | thick solid wooden bottom']
assert lid.parent==pivot and abs(pivot['open_angle_degrees']-108)<1e-6
def xy_size(obj):
    return [max(v.co[a] for v in obj.data.vertices)-min(v.co[a] for v in obj.data.vertices) for a in (0,1)]
assert all(abs(a-b)<1e-6 for a,b in zip(xy_size(lid),xy_size(base)))
images=[i for i in bpy.data.images if i.source=='FILE' and i.filepath]
assert len(images)==3 and all(i.packed_file for i in images)
assert all(path.with_name(Path(i.filepath.removeprefix('//')).name).is_file() for i in images)
deps=bpy.context.evaluated_depsgraph_get()
triangles=0
for obj in meshes:
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    assert mesh.vertices and mesh.loop_triangles
    triangles+=len(mesh.loop_triangles);evaluated.to_mesh_clear()
report={'status':'PASS','embedded_source_exact':True,'mesh_count':len(meshes),'evaluated_triangles':triangles,
    'rows':3,'columns':4,'recessed_socket_checks':hits,'lid_matches_base':True,'lid_hinge_degrees':108,
    'materials':sorted({m.name for o in meshes for m in o.data.materials}),
    'packed_images':[i.filepath for i in images]}
opt.report.parent.mkdir(parents=True,exist_ok=True)
opt.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
if opt.preview:
    # Render-only settings; the canonical .blend remains unchanged.
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
    for light in bpy.data.collections['PREVIEW_PinCoffer'].objects:
        if light.type=='LIGHT':light.data.energy*=.03
    scene.camera.location=(.20,-.26,.30)
    scene.camera.rotation_euler=(Vector((0,0,.055))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=900;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(opt.preview.resolve())
    bpy.ops.render.render(write_still=True)
