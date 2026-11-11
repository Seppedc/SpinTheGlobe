import bpy, random
from pathlib import Path
HERE=Path(__file__).resolve().parent
old=bpy.data.collections.get('ART_PaperBall')
if old:
 for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(old)
c=bpy.data.collections.new('ART_PaperBall');bpy.context.scene.collection.children.link(c)
r=bpy.data.objects.new('ROOT_PaperBall',None);c.objects.link(r)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,radius=.06,location=(0,0,.065))
o=bpy.context.object;o.name='Crumpled_Paper';o.parent=r
for other in list(o.users_collection):other.objects.unlink(o)
c.objects.link(o)
rng=random.Random(127)
for v in o.data.vertices:v.co*=rng.uniform(.80,1.08)
m=bpy.data.materials.new('Paper_WarmIvory');m.diffuse_color=(.76,.70,.57,1);m.use_nodes=True
p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=m.diffuse_color;p.inputs['Roughness'].default_value=.95
o.data.materials.append(m)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
text=bpy.data.texts.new('GENERATE_PaperBall.py');text.write(Path(__file__).read_text(encoding='utf-8'))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'PaperBall.blend'))
