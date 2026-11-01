"""Independent saved-source and same-source FBX checks. Does not save Blender."""
from pathlib import Path
import bpy,bmesh,json,math
from mathutils import Vector
folder=Path(bpy.data.filepath).parent
for embedded,external in [('GENERATE_BankersLamp.py','build_asset.py'),('lamp_detail.py','lamp_detail.py')]:
 assert bpy.data.texts[embedded].as_string()==(folder/external).read_text(encoding='utf-8'),external
required=['ROOT_BankersLamp','Shade_Pivot','BulbSocket_Pivot','PullChain_Pivot','LampLightOrigin','LampLightTarget','Shade_GreenGlass','Shade_RolledBrassLip','PullChain_SwitchHousing','Socket_SupportBracket']
assert all(bpy.data.objects.get(n) for n in required)
assert bpy.data.objects['ROOT_BankersLamp'].location.length<1e-8
assert abs(bpy.context.scene.unit_settings.scale_length-1)<1e-8
assert abs(bpy.data.objects['Shade_Pivot'].rotation_euler.x-math.radians(10))<1e-6
beads=[o for o in bpy.data.objects if o.name.startswith('PullChain_Bead_')];assert len(beads)==48
bpy.context.view_layer.update();centers=[o.matrix_world.translation for o in beads]
assert max(p.x for p in centers)-min(p.x for p in centers)<1e-7
assert max(p.y for p in centers)-min(p.y for p in centers)<1e-7
meshes=[o for o in bpy.data.objects if o.type=='MESH'];assert len(meshes)>=120
assert all(o.data.uv_layers and o.data.materials for o in meshes)
assert all(abs(v-1)<1e-6 for o in meshes for v in o.scale)
assert all(math.isfinite(v) for o in meshes for p in o.data.vertices for v in p.co)
triangles=0;degenerate=0
for o in meshes:
 o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles);degenerate+=sum(t.area<1e-18 for t in o.data.loop_triangles)
assert degenerate==0
bm=bmesh.new();bm.from_mesh(bpy.data.objects['Shade_GreenGlass'].data);boundary=sum(e.is_boundary for e in bm.edges);bm.free();assert boundary==0,boundary
images=[i for i in bpy.data.images if i.source=='FILE'];assert len(images)==12 and all(i.packed_file for i in images)
assert all(i.filepath.replace('\\','/').startswith('//Textures/') for i in images)
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];dims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
assert abs(min(p.z for p in pts))<1e-7
assert .30<dims[0]<.32 and .21<dims[1]<.24 and .41<dims[2]<.43
report={'status':'PASS','mesh_count':len(meshes),'triangles':triangles,'zero_area_triangles':0,'dimensions_blender_m':dims,'embedded_entry_and_helper_exact':True,'packed_images':12,'shade_boundary_edges':boundary,'chain_beads':48,'unit_mesh_scales':True,'uv_and_materials_all_meshes':True,'semantic_pivots':required,'base_bottom_m':min(p.z for p in pts)}
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(folder/'BankersLamp.fbx'))
imported=[o for o in bpy.data.objects if o.type=='MESH'];assert len(imported)==report['mesh_count'];assert all(o.data.uv_layers and o.data.materials for o in imported);assert all(bpy.data.objects.get(n) for n in required)
bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in imported for c in o.bound_box];fdims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
assert all(abs(a-b)<.002 for a,b in zip(sorted(dims),sorted(fdims)))
report.update(fbx_reimport_mesh_count=len(imported),fbx_dimensions_m=fdims,fbx_pivots_uv_materials_retained=True)
(folder/'source-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('LAMP_SOURCE_AND_FBX_PASS '+json.dumps(report))
