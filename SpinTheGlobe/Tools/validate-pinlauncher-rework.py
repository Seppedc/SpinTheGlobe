"""Read-only headless Blender source/FBX checks, scoped to this revision's staging."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
folder=Path(bpy.data.filepath).parent
source=(folder/'build_asset.py').read_text(encoding='utf-8')
detail=(folder/'pinlauncher_detail.py').read_text(encoding='utf-8')
assert bpy.data.texts['GENERATE_PinLauncher.py'].as_string()==source
assert bpy.data.texts['pinlauncher_detail.py'].as_string()==detail
required=['ROOT_PinLauncher','Turret_Yaw_Pivot','Elevation_Pivot','Recoil_Slide_Pivot','Handwheel_Rotation_Pivot','MuzzleTip','BreechOrigin','Plunger_Translation_Pivot']
assert all(bpy.data.objects.get(n) for n in required)
root=bpy.data.objects['ROOT_PinLauncher']
assert root.location.length<1e-7 and all(abs(s-1)<1e-7 for s in root.scale)
assert abs(bpy.context.scene.unit_settings.scale_length-1)<1e-7
meshes=[o for o in bpy.data.objects if o.type=='MESH']
assert len(meshes)>=150
assert all(o.data.uv_layers and len(o.data.materials)>0 for o in meshes)
assert all(abs(v-1)<1e-5 for o in meshes for v in o.scale)
assert all(math.isfinite(v) for o in meshes for p in o.data.vertices for v in p.co)
images=[i for i in bpy.data.images if i.name.startswith('PL_')]
assert len(images)==12 and all(i.packed_file for i in images)
assert len(list((folder/'textures').glob('*.png')))==12
bpy.context.view_layer.update()
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
dims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
assert abs(min(p.z for p in pts))<1e-6
assert all(.15<d<.40 for d in dims)
assert abs(math.degrees(bpy.data.objects['Elevation_Pivot'].rotation_euler.x)-37)<.01
report={'status':'PASS','source_mesh_count':len(meshes),'dimensions_m':dims,'uv_and_materials_all_meshes':True,'unit_mesh_scales':True,'root_at_base_origin':True,'embedded_entry_and_helper_exact':True,'packed_exported_maps':len(images),'semantic_pivots':required}
dg=bpy.context.evaluated_depsgraph_get();triangle_count=0;degenerate=0
for o in meshes:
 m=o.evaluated_get(dg).data;m.calc_loop_triangles();triangle_count+=len(m.loop_triangles);degenerate+=sum(t.area<1e-18 for t in m.loop_triangles)
assert degenerate==0,degenerate
report.update(triangles=triangle_count,zero_area_triangles=degenerate)
# Reimport same-source FBX in this disposable process only; canonical file is never resaved.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(folder/'PinLauncher.fbx'))
imported=[o for o in bpy.data.objects if o.type=='MESH']
assert len(imported)==report['source_mesh_count']
assert all(bpy.data.objects.get(n) for n in required)
assert all(o.data.materials and o.data.uv_layers for o in imported)
bpy.context.view_layer.update()
pts=[o.matrix_world@Vector(c) for o in imported for c in o.bound_box]
fbxdims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
assert all(abs(a-b)<.005 for a,b in zip(sorted(dims),sorted(fbxdims))), (dims,fbxdims)
report.update(fbx_reimport_mesh_count=len(imported),fbx_dimensions_m=fbxdims,fbx_semantic_pivots_retained=True)
(folder/'source-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('PIN_LAUNCHER_SOURCE_AND_FBX_VALIDATED '+json.dumps(report))
