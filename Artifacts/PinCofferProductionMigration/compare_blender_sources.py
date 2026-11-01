import bpy, json
from pathlib import Path
from mathutils import Matrix
root=Path(__file__).resolve().parents[2]
def snapshot(path, rotate=False):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    deps=bpy.context.evaluated_depsgraph_get()
    out={}
    rotation=Matrix.Rotation(3.141592653589793,4,'Z') if rotate else Matrix.Identity(4)
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        evaluated=obj.evaluated_get(deps); mesh=evaluated.to_mesh()
        points=[tuple(round(c,7) for c in rotation @ obj.matrix_world @ v.co) for v in mesh.vertices]
        mesh.calc_loop_triangles()
        out[obj.name]={'points':sorted(points),'triangles':len(mesh.loop_triangles)}
        evaluated.to_mesh_clear()
    return out
old=snapshot(root/'SpinTheGlobe/Assets/Objects/Pinbox.blend')
new=snapshot(root/'SpinTheGlobe/Assets/DetectiveDesk/ArtSource/PinCoffer/PinCoffer.blend')
differences=[]
for name, a in old.items():
    b=new.get(name)
    if b!=a:
        max_delta=max((max(abs(x-y) for x,y in zip(p,q)) for p,q in zip(a['points'],b['points'])),default=0) if b else None
        differences.append({'name':name,'max_sorted_point_delta':max_delta,'old_vertices':len(a['points']),'new_vertices':len(b['points']) if b else None})
report={'old_meshes':len(old),'new_meshes':len(new),'orientation_adapter':'none in Blender; preserve Unity importer settings','differences':differences}
(Path(__file__).parent/'BlenderSourceComparison.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
