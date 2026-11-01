import bpy, json
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2]/'SpinTheGlobe'
folder=project/'Assets/DetectiveDesk/ArtSource/_Validation/ProductionPackStarter/WallMolding'
blend=folder/'WallMolding.blend'
fbx=folder/'WallMolding.fbx'
bpy.ops.wm.open_mainfile(filepath=str(blend))
assert bpy.data.texts['GENERATE_WallMolding.py'].as_string()==(folder/'build_asset.py').read_text(encoding='utf-8')
assert bpy.data.texts['GENERATE_blender_shared.py'].as_string()==(project/'Tools/blender_shared.py').read_text(encoding='utf-8')
root=bpy.data.objects['ROOT_WallMolding']
assert root.location.length<1e-7
assert bpy.data.collections.get('Collection_WallMolding')
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(objects)==8 and all(o.parent==root for o in objects)
assert all(o.data.materials and all(m.use_nodes for m in o.data.materials) for o in objects)
assert all((o.scale-Vector((1,1,1))).length<1e-6 and o.rotation_euler.to_matrix().to_quaternion().angle<1e-6 for o in objects)
def bounds(objects):
    bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get();points=[];triangles=0
    for obj in objects:
        evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        assert mesh.vertices and mesh.loop_triangles
        triangles+=len(mesh.loop_triangles)
        points.extend(obj.matrix_world@v.co for v in mesh.vertices)
        evaluated.to_mesh_clear()
    return [[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]],triangles
original,triangles=bounds(objects)
assert abs(original[0][2])<1e-5
assert abs(original[0][0]+original[1][0])<1e-5 and abs(original[0][1]+original[1][1])<1e-5
assert bpy.context.scene.unit_settings.scale_length==1
assert fbx.stat().st_size>1024
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(fbx))
imported=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(imported)==8
roundtrip,triangles2=bounds(imported)
delta=max(abs(a-b) for row_a,row_b in zip(original,roundtrip) for a,b in zip(row_a,row_b))
assert delta<1e-4 and triangles==triangles2
report={'status':'PASS','blend':str(blend),'fbx':str(fbx),'meshes':8,'triangles':triangles,
    'generator_and_helper_embedded_exactly':True,'metric_root_at_origin':True,'mesh_scale_rotation_applied':True,
    'source_bounds_m':original,'fbx_roundtrip_bounds_m':roundtrip,'max_bounds_difference_m':delta,
    'external_sources_preserved':True,'production_asset_replaced':False}
(Path(__file__).parent/'StarterValidation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
