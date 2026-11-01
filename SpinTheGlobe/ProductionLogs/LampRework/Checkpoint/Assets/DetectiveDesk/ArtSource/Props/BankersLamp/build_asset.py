"""Deterministic banker lamp; local NoirLighting.png is art direction only."""
from pathlib import Path
import os, math
import bpy, bmesh
from mathutils import Vector
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',str(HERE)))

def mat(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m

def finish(o,name,m):
 o.name=name;o.parent=root
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.data.materials.append(m)
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 for p in o.data.polygons:p.use_smooth=True
 o.select_set(False);return o

def lathe(name,profile,m):
 n=96;v=[(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for r,z in profile for i in range(n)];f=[]
 for j in range(len(profile)-1):
  for i in range(n):
   a=j*n+i;b=j*n+(i+1)%n;f.append((a,b,b+n,a+n))
 f.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))]);mesh=bpy.data.meshes.new(name);mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new(name,mesh);col.objects.link(o);return finish(o,name,m)

def tube(name,points,r,m):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=4;s=c.splines.new('POLY');s.points.add(len(points)-1)
 for p,co in zip(s.points,points):p.co=(*co,1)
 o=bpy.data.objects.new(name,c);col.objects.link(o);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,name,m)

def ball(name,co,r,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=r,location=co);return finish(bpy.context.object,name,m)

if __name__=='__main__':
 for o in list(bpy.data.objects):
  if o.name in {'Cube','Camera','Light'}:bpy.data.objects.remove(o,do_unlink=True)
 old=bpy.data.collections.get('Collection_BankersLamp')
 if old:
  for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.data.collections.remove(old)
 col=bpy.data.collections.new('Collection_BankersLamp');bpy.context.scene.collection.children.link(col);root=bpy.data.objects.new('ROOT_BankersLamp',None);col.objects.link(root)
 bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
 brass=mat('Lamp_AgedBrass',(.34,.205,.065),.82,.26);green=mat('Lamp_EmeraldGlass',(.009,.16,.048),.08,.16);green.node_tree.nodes['Principled BSDF'].inputs['Coat Weight'].default_value=.5
 ivory=mat('Lamp_OpalLiner',(.78,.73,.53),0,.25);dark=mat('Lamp_Felt',(.018,.025,.019),0,.85)
 lathe('Base_Felt',[(.077,0),(.077,.003)],dark)
 lathe('Base_SpunBrass',[(.077,.003),(.083,.007),(.083,.012),(.079,.016),(.073,.018),(.071,.022),(.064,.025),(.06,.03),(.045,.035),(.027,.048),(.02,.065),(.017,.072)],brass)
 lathe('Stem_Collar',[(.018,.065),(.019,.069),(.019,.075),(.012,.08),(.011,.095)],brass)
 pts=[(0,.025*(1-math.cos(t*math.pi/2)),.087+.238*t) for t in [i/40 for i in range(41)]]
 pts += [(0,.025-.045*(1-math.cos(t*math.pi/2)),.325+.042*math.sin(t*math.pi/2)) for t in [i/24 for i in range(1,25)]]
 tube('Stem_CurvedBrass',pts,.0065,brass);tube('Shade_Axle',[(-.133,-.02,.361),(.133,-.02,.361)],.0045,brass)
 pivot=bpy.data.objects.new('Shade_Pivot',None);col.objects.link(pivot);pivot.parent=root;pivot.location=(0,-.02,.361);bpy.context.view_layer.update()
 v=[];f=[];n=48;k=n+1
 for r in [.077,.0735]:
  for x in [-.128,.128]:
   for i in range(n+1):
    a=math.pi*i/n;v.append((x,-.023+r*math.cos(a),.328+r*math.sin(a)))
 for layer in range(2):
  for i in range(n):
   a=layer*2*k+i;f.append((a,a+1,a+k+1,a+k) if layer==0 else (a+k,a+k+1,a+1,a))
 for side in range(2):
  for i in range(n):
   a=side*k+i;f.append((a,a+2*k,a+2*k+1,a+1))
 for i in [0,n]:f.append((i,i+k,i+3*k,i+2*k))
 mesh=bpy.data.meshes.new('ShadeShell');mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new('Shade_GreenGlass',mesh);col.objects.link(o);finish(o,o.name,green);o.data.materials.append(ivory)
 for p in o.data.polygons:
  if n<=p.index<2*n:p.material_index=1
 # Closed shell normals must face outward for Unity's single-sided URP shader.
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
 # Use explicit pivot-local vertices; FBX does not reliably retain parent inverses.
 for vertex in mesh.vertices:vertex.co-=pivot.location
 o.parent=pivot;o.location=(0,0,0)
 for y in [-.1,.054]:tube('Shade_Rim_'+str(y),[(-.129,y,.328),(.129,y,.328)],.0015,brass)
 for x in [-.13,.13]:
  tube('Shade_EndRim_'+str(x),[(x,-.023+.075*math.cos(i*math.pi/48),.328+.075*math.sin(i*math.pi/48)) for i in range(49)],.0015,brass);ball('Shade_PivotCap_'+str(x),(x,-.02,.361),.008,brass)
 tube('Bulb_OpalTube',[(-.08,-.025,.34),(.08,-.025,.34)],.009,ivory)
 tube('PullChain_Core',[(.082,-.035,.329),(.082,-.035,.205)],.0007,brass)
 for i in range(26):ball('PullChain_Bead_%02d'%i,(.082,-.035,.326-i*.0046),.0016,brass)
 ball('PullChain_Finial',(.082,-.035,.201),.005,brass)
 OUT.mkdir(parents=True,exist_ok=True);source=Path(__file__).read_text(encoding='utf-8-sig');(OUT/'build_asset.py').write_text(source,encoding='utf-8');text=bpy.data.texts.new('GENERATE_BankersLamp.py');text.write(source)
 bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BankersLamp.blend'));bpy.ops.object.select_all(action='DESELECT')
 for o in col.objects:o.select_set(True)
 # Keep meter conversion in FBX global units, not per-object transforms.
 # Disabling experimental space baking preserves the nested shade pivot.
 bpy.context.view_layer.objects.active=root;bpy.ops.export_scene.fbx(filepath=str(OUT/'BankersLamp.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=False,add_leaf_bones=False,bake_anim=False)
