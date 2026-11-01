"""DD02 deterministic carved mahogany understructure; written reference brief only."""
from pathlib import Path
import bpy,os,math
OUT=Path(os.environ['DETECTIVEDESK_ASSET_OUTPUT']);OUT.mkdir(parents=True,exist_ok=True)
for o in list(bpy.data.objects):
 if o.name in {'Cube','Camera','Light'} or o.name.startswith('DD02_'):bpy.data.objects.remove(o,do_unlink=True)
s=bpy.context.scene;s.unit_settings.system='METRIC';s.unit_settings.scale_length=1
col=bpy.data.collections.new('Collection_DeskStructure');s.collection.children.link(col)
root=bpy.data.objects.new('ROOT_DeskStructure',None);col.objects.link(root)
def mat(name,c):
 m=bpy.data.materials.new(name);m.use_nodes=True;b=m.node_tree.nodes['Principled BSDF'];b.inputs['Base Color'].default_value=(*c,1);b.inputs['Roughness'].default_value=.38;return m
wood=mat('DD02_Mahogany',(.105,.032,.016));dark=mat('DD02_Recess',(.04,.012,.006));edge=mat('DD02_CarvedWood',(.15,.05,.023))
def finish(o,name,m):
 o.name='DD02_'+name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=root;o.data.materials.append(m)
 bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 return o
def box(n,p,d,m=wood):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=d;finish(o,n,m)
 b=o.modifiers.new('Rounded joinery','BEVEL');b.width=min(.004,min(d)/4);b.segments=3;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return o
def leg(n,x,y):
 profile=[(0,.065),(.015,.077),(.035,.079),(.055,.067),(.07,.048),(.10,.043),(.14,.052),(.18,.056),(.22,.044),(.26,.037),(.34,.044),(.42,.056),(.47,.062),(.49,.062),(.50,.069),(.52,.069)]
 v=[(x+r*math.cos(i*math.tau/48),y+r*math.sin(i*math.tau/48),z) for z,r in profile for i in range(48)]
 f=[(j*48+i,j*48+(i+1)%48,(j+1)*48+(i+1)%48,(j+1)*48+i) for j in range(len(profile)-1) for i in range(48)]
 f.extend([tuple(reversed(range(48))),tuple((len(profile)-1)*48+i for i in range(48))]);me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(n,me);col.objects.link(o);finish(o,n,wood)
 for p in me.polygons:p.use_smooth=len(p.vertices)==4
 box(n+'_Capital',(x,y,.615),(.15,.15,.19))
 for z in [.535,.70]:box(n+'_Collar_'+str(z),(x,y,z),(.163,.163,.018),edge)
 for a in [-1,0,1]:box(n+'_Flute_'+str(a),(x+a*.033,y+math.copysign(.075,y),.615),(.008,.003,.10),dark)
def relief(n,pts):
 cu=bpy.data.curves.new(n,'CURVE');cu.dimensions='3D';cu.bevel_depth=.003;cu.bevel_resolution=3;sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,co in zip(sp.points,pts):p.co=(*co,1)
 o=bpy.data.objects.new(n,cu);col.objects.link(o);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');finish(bpy.context.object,n,edge)
for i,x in enumerate([-1.25,1.25]):
 for j,y in enumerate([-.44,.44]):leg(f'Leg_{i}_{j}_FootAndShaft',x,y)
for side in [-1,1]:
 y=side*.44;box('Apron_'+str(side),(0,y,.615),(2.5,.095,.19),dark)
 for z in [.529,.701]:box('ApronRail_'+str((side,z)),(0,y+side*.048,z),(2.5,.018,.019),edge)
 for j,x in enumerate([-.81,0,.81]):
  box(f'RaisedPanel_{side}_{j}',(x,y+side*.049,.615),(.735,.019,.126))
  for z in [.559,.671]:box(f'PanelRail_{side}_{j}_{z}',(x,y+side*.062,z),(.71,.012,.009),edge)
  for mirror in [-1,1]:
   pts=[]
   for i in range(81):
    t=i/80;a=t*math.tau*1.25;r=.031*(1-t)+.004;pts.append((x+mirror*(.09+t*.14+r*math.cos(a)),y+side*.064,.615+r*math.sin(a)))
   relief(f'CarvedScroll_{side}_{j}_{mirror}',pts)
  relief(f'Diamond_{side}_{j}',[(x,y+side*.064,.64),(x+.025,y+side*.064,.615),(x,y+side*.064,.59),(x-.025,y+side*.064,.615),(x,y+side*.064,.64)])
for side in [-1,1]:
 x=side*1.25;box('EndApron_'+str(side),(x,0,.615),(.095,.88,.19),dark);box('EndPanel_'+str(side),(x+side*.052,0,.615),(.018,.70,.125))
 for z in [.53,.70]:box('EndRail_'+str((side,z)),(x+side*.049,0,z),(.018,.88,.019),edge)
source=Path(__file__).read_text(encoding='utf-8-sig');bpy.data.texts.new('GENERATE_DeskStructure.py').write(source);(OUT/'build_asset.py').write_text(source,encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DeskStructure.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in col.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'DeskStructure.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
