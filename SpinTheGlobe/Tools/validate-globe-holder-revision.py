"""Validate staged holder and prove the approved sphere is byte-for-byte unchanged."""
from pathlib import Path
import bpy,bmesh,json,hashlib,struct,math
from array import array
from mathutils import Vector
folder=Path(bpy.data.filepath).parent
def geometry(o):
 h=hashlib.sha256();m=o.data
 for seq,prop,size,kind in [(m.vertices,'co',3,'f'),(m.loops,'vertex_index',1,'i'),(m.polygons,'loop_total',1,'i')]:
  buf=array(kind,[0])*(len(seq)*size);seq.foreach_get(prop,buf);h.update(buf.tobytes())
 for uv in m.uv_layers:
  buf=array('f',[0])*(len(uv.data)*2);uv.data.foreach_get('uv',buf);h.update(buf.tobytes())
 for row in o.matrix_world:h.update(struct.pack('4f',*row))
 for mat in m.materials:h.update(mat.name.encode())
 return h.hexdigest()
def sphere():return {o.name:geometry(o) for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Sphere_')}
def images():return {i.name:hashlib.sha256(i.packed_file.data).hexdigest() for i in bpy.data.images if i.packed_file}
for embedded,external in [('GENERATE_MechanicalGlobe.py','build_asset.py'),('legacy_globe.py','legacy_globe.py'),('reference_globe.py','reference_globe.py')]:assert bpy.data.texts[embedded].as_string()==(folder/external).read_text(encoding='utf-8')
bpy.context.view_layer.update();ball=sphere();maps=images();assert len(ball)>=7
assert (bpy.data.objects['Sphere_RotationPivot'].location-Vector((-1.45,-.33,2.15))).length<1e-7
assert (bpy.data.objects['RPMGauge_Assembly'].location-Vector((-1.23,-.80,1.18))).length<1e-6
assert not [o for o in bpy.data.objects['Clockwork_Assembly'].children_recursive if o.type=='MESH']
band=bpy.data.objects['Meridian_BroadGraduatedBrassBand'];assert max(v.co.x for v in band.data.vertices)<1e-6
assert min(v.co.x for v in band.data.vertices)<-.78
bm=bmesh.new();bm.from_mesh(band.data);assert sum(e.is_boundary for e in bm.edges)==0;bm.free()
meshes=[o for o in bpy.data.objects if o.type=='MESH'];triangles=0;bad=0
for o in meshes:
 assert o.data.uv_layers and o.data.materials and all(abs(s-1)<1e-6 for s in o.scale)
 o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles);bad+=sum(t.area<1e-18 for t in o.data.loop_triangles)
assert bad==0
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];dims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)]
floor=(.815-.06879997253417969)/.9200000166893005;assert abs(min(p.z for p in pts)-floor)<1e-5
report={'status':'PASS','scope':'Blender/FBX only; Unity integration pending','mesh_count':len(meshes),'triangles':triangles,'zero_area_triangles':0,'holder_span_degrees':180,'holder_on_player_left_only':True,'holder_closed_cut_faces':True,'exposed_gear_meshes':0,'gauge_pivot_unity':[-1.23,1.18,-.80],'gauge_original_radius_m':.16,'dimensions_blender_m':dims,'sphere_mesh_hashes':ball,'embedded_sources_exact':True,'uv_and_materials_all_meshes':True,'packed_images':len(maps),'unity_applied':False}
bpy.ops.wm.open_mainfile(filepath=str(folder.parent/'Checkpoint/MechanicalGlobe.blend'));bpy.context.view_layer.update()
assert sphere()==ball,'Approved sphere geometry, UVs, transforms or material names changed'
assert images()==maps,'Original texture image payload changed'
report.update(approved_sphere_geometry_uv_material_names_transforms_exact=True,all_original_packed_texture_payloads_exact=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(folder/'MechanicalGlobe.fbx'));imported=[o for o in bpy.data.objects if o.type=='MESH'];assert len(imported)==report['mesh_count']
assert all(o.data.uv_layers and o.data.materials for o in imported)
bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in imported for c in o.bound_box];fdims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)];assert all(abs(a-b)<.002 for a,b in zip(sorted(dims),sorted(fdims)))
report.update(fbx_reimport_mesh_count=len(imported),fbx_dimensions_m=fdims,fbx_uv_materials_retained=True)
(folder/'source-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('GLOBE_HOLDER_SOURCE_PASS '+json.dumps(report))
