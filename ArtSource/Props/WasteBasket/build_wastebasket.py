import bpy, math, os
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
NAME='WasteBasket'
old=bpy.data.collections.get('ART_'+NAME)
if old:
 for obj in list(old.objects): bpy.data.objects.remove(obj,do_unlink=True)
 bpy.data.collections.remove(old)
collection=bpy.data.collections.new('ART_'+NAME)
bpy.context.scene.collection.children.link(collection)
root=bpy.data.objects.new('ROOT_WasteBasket',None); collection.objects.link(root)
def mat(name,color,metal,rough):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
 return m
iron=mat('Basket_BlackenedBronze',(.11,.075,.04),.75,.42)
brass=mat('Basket_AgedBrass',(.42,.27,.10),.8,.32)
def finish(obj,name,material):
 obj.name=name
 for c in list(obj.users_collection): c.objects.unlink(obj)
 collection.objects.link(obj); obj.parent=root
 obj.data.materials.append(material)
 bpy.context.view_layer.objects.active=obj; obj.select_set(True)
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 for p in obj.data.polygons:p.use_smooth=True
 obj.select_set(False)
 return obj
def ring(name,r,z,t,material):
 bpy.ops.mesh.primitive_torus_add(major_segments=96,minor_segments=10,location=(0,0,z),major_radius=r,minor_radius=t)
 return finish(bpy.context.object,name,material)
ring('Rolled_top_rim',.255,.64,.014,brass)
ring('Upper_binding',.252,.60,.006,brass)
ring('Rolled_foot',.185,.016,.012,brass)
ring('Lower_binding',.19,.055,.006,brass)
for n in range(1,5):
 z=n*.12; ring('Horizontal_wire_%02d'%n,.185+(.07*z/.64),z,.0028,iron)
# Crossed helical wires form a genuinely open diamond mesh.
for direction in (-1,1):
 for i in range(20):
  curve=bpy.data.curves.new('Wire','CURVE');curve.dimensions='3D';curve.bevel_depth=.004;curve.bevel_resolution=2
  spline=curve.splines.new('POLY');spline.points.add(32)
  for j,p in enumerate(spline.points):
   z=.03+.59*j/32;angle=2*math.pi*i/20+direction*(z/.64)*.55;r=.185+.07*z/.64
   p.co=(r*math.cos(angle),r*math.sin(angle),z,1)
  obj=bpy.data.objects.new('Diagonal',curve);collection.objects.link(obj)
  bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.convert(target='MESH')
  finish(bpy.context.object,'Diagonal_%s_%02d'%(direction,i),iron)
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=.185,depth=.012,location=(0,0,.016))
finish(bpy.context.object,'Solid_base',iron)
# Consolidate static lattice for one renderer per material group.
bpy.ops.object.select_all(action='DESELECT')
for obj in list(collection.objects):
 if obj.type=='MESH':obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.join();basket=bpy.context.object;basket.name='Basket_Mesh'
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
text=bpy.data.texts.get('GENERATE_'+NAME+'.py') or bpy.data.texts.new('GENERATE_'+NAME+'.py');text.clear();text.write(Path(__file__).read_text(encoding='utf-8'))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/(NAME+'.blend')))
print('VALIDATED',len(basket.data.vertices),'vertices',len(basket.data.uv_layers),'UV maps; height .654m; embedded generator')
