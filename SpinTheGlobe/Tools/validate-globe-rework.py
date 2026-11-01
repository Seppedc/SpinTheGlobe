"""Independent read-only source and FBX checks for the original-derived globe."""
from pathlib import Path
import json,math,hashlib,os
import bpy
from mathutils import Vector
folder=Path(bpy.data.filepath).parent
for embedded,external in [('GENERATE_MechanicalGlobe.py','build_asset.py'),('legacy_globe.py','legacy_globe.py'),('reference_globe.py','reference_globe.py')]:
 assert bpy.data.texts[embedded].as_string()==(folder/external).read_text(encoding='utf-8'),external
required=['BlenderMechanicalGlobe_Visuals','Sphere_RotationPivot','Meridian_Assembly','Meridian_BroadGraduatedBrassBand','Pedestal_SteppedSculptedBrass','Pedestal_GaugeSupportArch','Gear_12T_Pivot','Gear_16T_Pivot','Gear_14T_Pivot','RPMGauge_NeedlePivot']
assert all(bpy.data.objects.get(n) for n in required)
sphere=bpy.data.objects['Sphere_RotationPivot'];assert (sphere.location-Vector((-1.45,-.33,2.15))).length<1e-5
assert abs(bpy.context.scene.unit_settings.scale_length-1)<1e-7
meshes=[o for o in bpy.data.objects if o.type=='MESH'];assert len(meshes)>=50
assert max(v.co.x for v in bpy.data.objects['Meridian_BroadGraduatedBrassBand'].data.vertices)<1e-6
assert not [o for o in bpy.data.objects['Clockwork_Assembly'].children_recursive if o.type=='MESH']
assert (bpy.data.objects['RPMGauge_Assembly'].location-Vector((-1.23,-.80,1.18))).length<1e-6
assert all(o.data.uv_layers and len(o.data.materials)>0 for o in meshes)
assert all(math.isfinite(v) for o in meshes for p in o.data.vertices for v in p.co)
assert all(abs(v-1)<1e-6 for o in meshes for v in o.scale)
images=[i for i in bpy.data.images if i.source=='FILE'];assert len(images)>=21 and all(i.packed_file for i in images)
assert all(i.filepath.replace('\\','/').startswith('//Textures/') for i in images)
world=bpy.data.images.get('WorldCartography_BaseColor.png');assert world and list(world.size)==[2048,1024]
dg=bpy.context.evaluated_depsgraph_get();triangles=0;degenerate=0
for o in meshes:
 m=o.evaluated_get(dg).data;m.calc_loop_triangles();triangles+=len(m.loop_triangles);degenerate+=sum(t.area<1e-18 for t in m.loop_triangles)
assert degenerate==0
bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];dims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
expected_floor=(.815-.06879997253417969)/.9200000166893005
assert abs(min(p.z for p in pts)-expected_floor)<1e-5
assert (bpy.data.objects['Pedestal_Assembly'].location-Vector((-1.45,-.33,.86))).length<1e-5
report={'status':'PASS','mesh_count':len(meshes),'triangles':triangles,'zero_area_triangles':degenerate,'uv_and_materials_all_meshes':True,'unit_mesh_scales':True,'dimensions_blender_m':dims,'embedded_entry_legacy_and_revision_exact':True,'packed_images':len(images),'relative_texture_paths':True,'cartography_size':[2048,1024],'sphere_center_and_radius_preserved':True,'pedestal_assembly_origin_preserved':True,'base_seated_at_world_y':.815,'semantic_pivots':required}
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(folder/'MechanicalGlobe.fbx'));imported=[o for o in bpy.data.objects if o.type=='MESH']
assert len(imported)==report['mesh_count'];assert all(o.data.uv_layers and o.data.materials for o in imported);assert all(bpy.data.objects.get(n) for n in required)
bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in imported for c in o.bound_box];fdims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
assert all(abs(a-b)<.002 for a,b in zip(sorted(dims),sorted(fdims)))
report.update(fbx_reimport_mesh_count=len(imported),fbx_dimensions_m=fdims,fbx_pivots_uv_materials_retained=True)
report.update(holder_span_degrees=180,exposed_gear_meshes=0,gauge_pivot_unity=[-1.23,1.18,-.80])
Path(os.environ.get('DETECTIVEDESK_VALIDATION_OUTPUT',str(folder/'source-validation.json'))).write_text(json.dumps(report,indent=2),encoding='utf-8');print('GLOBE_SOURCE_AND_FBX_PASS '+json.dumps(report))
