"""Deterministic JewelerHammer: brass bench mallet, metric, no external dependencies.
Art direction: supplied REFERENCE_BRIEF.md; no photographic reference supplied.
"""
from pathlib import Path
import os, math
import bpy
from mathutils import Vector
HERE=Path(__file__).resolve().parent
SLUG='JewelerHammer'

def material(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 return m

def lathe(name,profile,mat,axis='Z',center=(0,0,0),segments=64,oval=1):
 verts=[]
 for height,radius in profile:
  for i in range(segments):
   a=2*math.pi*i/segments;v=(radius*math.cos(a),radius*math.sin(a)*oval,height)
   if axis=='X': v=(height,v[0],v[1])
   verts.append(tuple(v[j]+center[j] for j in range(3)))
 faces=[]
 for j in range(len(profile)-1):
  for i in range(segments):
   a=j*segments+i;b=j*segments+(i+1)%segments
   faces.append((a,b,b+segments,a+segments))
 faces.extend([tuple(reversed(range(segments))),tuple((len(profile)-1)*segments+i for i in range(segments))])
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);obj.parent=root;mesh.materials.append(mat)
 for p in mesh.polygons: p.use_smooth=len(p.vertices)==4
 return obj

if __name__=='__main__':
 out=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',str(HERE)));out.mkdir(parents=True,exist_ok=True)
 # Only remove this asset's prior collection; unrelated scene content stays intact.
 previous=bpy.data.collections.get('Collection_'+SLUG)
 if previous:
  for obj in list(previous.objects): bpy.data.objects.remove(obj,do_unlink=True)
  bpy.data.collections.remove(previous)
 if not bpy.data.filepath:
  for name in ('Cube','Camera','Light'):
   ob=bpy.data.objects.get(name)
   if ob: bpy.data.objects.remove(ob,do_unlink=True)
 # Factory startup is isolated by the authorized build wrapper.
 collection=bpy.data.collections.new('Collection_'+SLUG);bpy.context.scene.collection.children.link(collection)
 root=bpy.data.objects.new('ROOT_'+SLUG,None);collection.objects.link(root)
 brass=material('JewelerHammer_AgedBrass',(0.46,0.285,0.085),0.82,0.3)
 face=material('JewelerHammer_PolishedBrassFaces',(0.63,0.43,0.16),0.88,0.23)
 wood=material('JewelerHammer_Walnut',(0.19,0.075,0.028),0,0.42)
 grain=material('JewelerHammer_WoodGrain',(0.12,0.043,0.014),0,0.48)
 lathe('Handle_Walnut',[(0,.0075),(.001,.009),(.006,.010),(.018,.0105),(.045,.010),(.085,.0085),(.130,.007),(.174,.0065),(.201,.0068),(.211,.0068)],wood,oval=.83)
 # Fine longitudinal grain inlays follow the tapered grip, physical geometry survives FBX.
 profile=[(.006,.010),(.018,.0105),(.045,.010),(.085,.0085),(.130,.007),(.174,.0065)]
 for i in range(16):
  a=i*2*math.pi/16;verts=[]
  for z,r in profile:
   drift=.018*math.sin(z*48+i)
   for delta in [-.006,.006]:
    angle=a+drift+delta;verts.append(((r+.000015)*math.cos(angle),(r*.83+.000015)*math.sin(angle),z))
  mesh=bpy.data.meshes.new('Grain');mesh.from_pydata(verts,[],[(j*2,j*2+1,j*2+3,j*2+2) for j in range(len(profile)-1)]);mesh.materials.append(grain)
  ob=bpy.data.objects.new('Handle_Grain_%02d'%i,mesh);collection.objects.link(ob);ob.parent=root
 lathe('Handle_BrassFerrule',[(.178,.0066),(.179,.0071),(.190,.0071),(.191,.0066)],brass,oval=.85)
 lathe('Head_BrassBody',[(-.033,.0115),(-.030,.0125),(-.023,.0125),(-.019,.0105),(-.012,.010),(.012,.010),(.019,.0105),(.023,.0125),(.030,.0125),(.033,.0115)],brass,'X',(0,0,.204))
 for sign,label in [(-1,'Left'),(1,'Right')]:
  lathe('Head_StrikingFace_'+label,[(sign*.033,.0115),(sign*.034,.011)],face,'X',(0,0,.204)) if sign==1 else lathe('Head_StrikingFace_'+label,[(-.034,.011),(-.033,.0115)],face,'X',(0,0,.204))
 # Visible end-grain tenon and restrained metal wedge at the top of the through handle.
 lathe('Handle_ExposedTenon',[(.213,.0058),(.2143,.0056)],wood,oval=.78)
 bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
 source=Path(__file__).read_text(encoding='utf-8');(out/'build_asset.py').write_text(source,encoding='utf-8')
 text=bpy.data.texts.get('GENERATE_'+SLUG+'.py') or bpy.data.texts.new('GENERATE_'+SLUG+'.py');text.clear();text.write(source)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in collection.objects: ob.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(SLUG+'.blend')))
 bpy.ops.export_scene.fbx(filepath=str(out/(SLUG+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
