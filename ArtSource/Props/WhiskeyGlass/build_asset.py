"""WhiskeyGlass: deterministic metric faceted tumbler; local reference brief only."""
from pathlib import Path
import os, math
import bpy
S='WhiskeyGlass'
def main():
 out=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',Path(__file__).parent));out.mkdir(parents=True,exist_ok=True)
 for o in list(bpy.context.scene.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
 col=bpy.data.collections.new('Collection_'+S);bpy.context.scene.collection.children.link(col)
 root=bpy.data.objects.new('ROOT_'+S,None);col.objects.link(root)
 def mat(name,c,r,t,ior):
  m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF']
  for k,v in [('Base Color',(*c,1)),('Roughness',r),('Transmission Weight',t),('IOR',ior)]:p.inputs[k].default_value=v
  m.diffuse_color=(*c,1);return m
 glass=mat('WG_HeavyCrystal',(.82,.94,.97),.075,1,1.52)
 liquid=mat('WG_AmberWhiskey',(.38,.105,.012),.14,.78,1.36)
 ice=mat('WG_ClearIce',(.72,.88,.94),.13,.86,1.31)
 def lathe(name,profile,n,material,faceted=False):
  verts=[]
  for j,(r,z) in enumerate(profile):
   for i in range(n):
    a=math.tau*i/n;rr=r
    if faceted and j in (4,5,6,13):
     local=(a+math.pi/16)%(math.tau/16)-math.pi/16;rr=r*math.cos(math.pi/16)/math.cos(local)
    verts.append((rr*math.cos(a),rr*math.sin(a),z))
  faces=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(profile)-1) for i in range(n)]
  me=bpy.data.meshes.new(name+'Mesh');me.from_pydata(verts,[],faces);me.update()
  o=bpy.data.objects.new(name,me);col.objects.link(o);o.parent=root;me.materials.append(material);return o
 # Closed section with heavy 14mm floor, hollow cavity and rolled drinking lip.
 o=lathe('CrystalRocksGlass',[(0,0),(.034,0),(.039,.001),(.041,.004),(.0415,.010),(.042,.017),(.044,.074),(.044,.090),(.0437,.092),(.0428,.093),(.0417,.0925),(.041,.091),(.041,.075),(.0385,.020),(.036,.014),(0,.014)],64,glass,True)
 bpy.context.view_layer.objects.active=o;o.select_set(True);b=o.modifiers.new('Polished facet edges','BEVEL');b.width=.00025;b.segments=2;bpy.ops.object.modifier_apply(modifier=b.name);o.select_set(False)
 lathe('AmberWhiskey',[(0,.0148),(.0355,.0148),(.0378,.0205),(.0391,.054),(.039,.055),(.0378,.0543),(0,.0543)],96,liquid)
 for index,(loc,dim,rot) in enumerate([((-.015,-.010,.060),(.026,.027,.029),(.16,.10,.28)),((.015,.010,.062),(.027,.025,.030),(-.13,.19,-.22)),((-.010,.019,.055),(.024,.022,.026),(.08,-.16,.42))]):
  bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name='IceCube_%02d'%(index+1);o.dimensions=dim;o.rotation_euler=rot
  bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
  for c in list(o.users_collection):c.objects.unlink(o)
  col.objects.link(o);o.parent=root;o.data.materials.append(ice)
  b=o.modifiers.new('Melted ice edges','BEVEL');b.width=.003;b.segments=4;bpy.ops.object.modifier_apply(modifier=b.name)
 source=Path(__file__).read_text(encoding='utf-8-sig');bpy.data.texts.new('GENERATE_'+S+'.py').write(source);(out/'build_asset.py').write_text(source,encoding='utf-8')
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(S+'.blend')))
 bpy.ops.object.select_all(action='DESELECT')
 for o in col.objects:o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(out/(S+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
if __name__=='__main__':main()
