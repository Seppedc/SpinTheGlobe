"""Deterministic four empty mahogany/brass display bases, no statues. Isolated Blender only."""
from pathlib import Path
import os, math, shutil
import bpy
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',HERE)).resolve()
def mat(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.use_nodes=True
 b=m.node_tree.nodes['Principled BSDF'];b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough
 return m
def lathe(name,profile,parent,material):
 n=96;v=[(r*math.cos(i*math.tau/n),r*math.sin(i*math.tau/n),z) for r,z in profile for i in range(n)]
 f=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(profile)-1) for i in range(n)]
 f += [tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))]
 m=bpy.data.meshes.new(name+'_Mesh');m.from_pydata(v,[],f);m.update()
 o=bpy.data.objects.new(name,m);col.objects.link(o);o.parent=parent;m.materials.append(material)
 for p in m.polygons:p.use_smooth=len(p.vertices)==4
 b=o.modifiers.new('Edge highlights','BEVEL');b.width=.0002;b.segments=2
 o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
def plate(name,parent,x,matl,width=.03,height=.008):
 bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=name;o.dimensions=(width,.0014,height)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=parent;o.location=(x,-.0405,.021);o.data.materials.append(matl)
 b=o.modifiers.new('Label bevel','BEVEL');b.width=.0004;b.segments=3
 o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
if __name__=='__main__':
 for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
 col=bpy.data.collections.new('Collection_StatueSlots');bpy.context.scene.collection.children.link(col)
 root=bpy.data.objects.new('ROOT_StatueSlots',None);col.objects.link(root)
 wood=mat('StatueSlots_Mahogany',(.085,.027,.016),0,.32)
 brass=mat('StatueSlots_AgedBrass',(.34,.22,.085),.82,.32)
 velvet=mat('StatueSlots_BurgundyPad',(.075,.009,.022),0,.86)
 dark=mat('StatueSlots_Recess',(.014,.009,.008),0,.62)
 for i in range(4):
  name='DisplayBase_%02d'%(i+1);p=bpy.data.objects.new(name,None);col.objects.link(p);p.parent=root;p.location=((i-1.5)*.13,0,0)
  p['purpose']='Reusable empty base, bottom-center pivot'
  lathe(name+'_TurnedMahogany',[(.044,0),(.047,.0015),(.047,.006),(.045,.008),(.042,.010),(.040,.014),(.040,.026),(.042,.029),(.044,.031),(.044,.034),(.042,.036)],p,wood)
  lathe(name+'_LowerBrassBand',[(.0451,.007),(.0451,.0083),(.043,.0102)],p,brass)
  lathe(name+'_UpperBrassRim',[(.042,.0358),(.042,.038),(.041,.039),(.036,.039),(.0355,.038),(.0355,.036)],p,brass)
  lathe(name+'_PadRecess',[(.0356,.036),(.0356,.037)],p,dark)
  lathe(name+'_EmptyDisplayPad',[(.0348,.0368),(.0348,.038),(.0343,.0385)],p,velvet)
  plate(name+'_BlankLabel',p,0,brass)
 root['dimensions']='Four bases: 94 mm diameter, 39 mm height, 130 mm spacing'
 OUT.mkdir(parents=True,exist_ok=True)
 bpy.data.texts.new('GENERATE_StatueSlots.py').write(Path(__file__).read_text(encoding='utf-8'))
 if OUT/'build_asset.py' != Path(__file__).resolve():shutil.copyfile(__file__,OUT/'build_asset.py')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'StatueSlots.blend'))
 bpy.ops.object.select_all(action='DESELECT')
 for o in col.objects:o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/'StatueSlots.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
