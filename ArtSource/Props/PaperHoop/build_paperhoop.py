import bpy, math
from pathlib import Path
HERE=Path(__file__).resolve().parent
NAME='PaperHoop'
old=bpy.data.collections.get('ART_'+NAME)
if old:
 for o in list(old.objects): bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(old)
c=bpy.data.collections.new('ART_'+NAME);bpy.context.scene.collection.children.link(c)
root=bpy.data.objects.new('ROOT_PaperHoop',None);c.objects.link(root)
def material(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=m.diffuse_color;p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 return m
wood=material('Hoop_Walnut',(.11,.045,.02),0,.65)
brass=material('Hoop_Brass',(.52,.32,.10),.8,.35)
rim=material('Hoop_RedMetal',(.44,.075,.025),.65,.4)
rope=material('Hoop_Cotton',(.8,.74,.6),0,.9)
def finish(o,name,mat):
 o.name=name
 for col in list(o.users_collection):col.objects.unlink(o)
 c.objects.link(o);o.parent=root;o.data.materials.append(mat)
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);o.select_set(False)
 return o
def cube(name,pos,size,mat,bevel=.01):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.scale=size;finish(o,name,mat)
 mod=o.modifiers.new('Soft edges','BEVEL');mod.width=bevel;mod.segments=3;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return o
def tube(name,points,width,mat):
 cv=bpy.data.curves.new(name,'CURVE');cv.dimensions='3D';cv.bevel_depth=width;cv.bevel_resolution=2
 s=cv.splines.new('POLY');s.points.add(len(points)-1)
 for p,co in zip(s.points,points):p.co=(*co,1)
 o=bpy.data.objects.new(name,cv);c.objects.link(o);o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o=bpy.context.object;finish(o,name,mat)
 return o
cube('Backboard',(0,.05,.75),(1.05,.065,.70),wood,.025)
for x in [-.51,.51]:cube('Brass_side',(x,.005,.75),(.018,.018,.67),brass,.005)
for z in [.415,1.085]:cube('Brass_edge',(0,.005,z),(1.02,.018,.018),brass,.005)
# Ivory shooting square and brass mounting plate.
for x in [-.18,.18]:cube('Target_side',(x,-.002,.71),(.012,.008,.27),rope,.002)
for z in [.58,.845]:cube('Target_edge',(0,-.002,z),(.36,.008,.012),rope,.002)
cube('Rim_mount',(0,-.04,.48),(.16,.12,.10),brass)
cube('Rim_support',(0,-.14,.48),(.07,.25,.035),rim)
bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=12,major_radius=.245,minor_radius=.016,location=(0,-.385,.49))
finish(bpy.context.object,'Scoring_Rim',rim)
# Open cotton net; crossing strands taper below the rim, with an open bottom.
for direction in [-1,1]:
 for i in range(12):
  points=[]
  for j in range(17):
   t=j/16;ang=2*math.pi*i/12+direction*t*.40;r=.24*(1-t)+.13*t
   points.append((r*math.cos(ang),-.385+r*math.sin(ang),.49-.38*t))
  tube('Net_%s_%02d'%(direction,i),points,.0035,rope)
for z,r in [(.49,.24),(.11,.13)]:
 tube('Net_binding',[(r*math.cos(i*2*math.pi/64),-.385+r*math.sin(i*2*math.pi/64),z) for i in range(65)],.004,rope)
# The bottom of the net is the lowest geometry; root remains the mounting reference at zero.
for o in list(c.objects):
 if o.type=='MESH':
  o.location.z -= .106
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
text=bpy.data.texts.new('GENERATE_'+NAME+'.py');text.write(Path(__file__).read_text(encoding='utf-8'))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/(NAME+'.blend')))

# Render a source preview after saving, keeping presentation lights outside the exported asset.
from mathutils import Vector
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=12
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
if scene.world is None:scene.world=bpy.data.worlds.new('Preview World')
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.16,.16,.16,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.4
bpy.ops.object.camera_add(location=(1.8,-3.4,1.9));cam=bpy.context.object
cam.rotation_euler=(Vector((0,-.2,.55))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=1.75;scene.camera=cam
for loc,power,size in [((1,-2,3),220,2),((-2,-1,1.6),120,2),((0,2,2),180,1.5)]:
 bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size
 light.rotation_euler=(Vector((0,0,.5))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(HERE/'References'/'BlenderPreview.png')
bpy.ops.render.render(write_still=True)
