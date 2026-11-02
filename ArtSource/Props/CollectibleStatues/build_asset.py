"""Deterministic noir miniature bust set; metric, standalone and queue-staged.
Original three-shape starter developed into three modular cast-metal characters.
No external textures or shared helper dependency. Front faces Blender -Y.
"""
from pathlib import Path
import os, math, shutil
import bpy
from mathutils import Vector
SLUG='CollectibleStatues'
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',str(HERE))).resolve()

def material(name,color,metal,rough):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m

def finish(o,name,mat,parent,scale=None):
    o.name=name
    if scale:o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    o.data.materials.append(mat)
    for c in list(o.users_collection):c.objects.unlink(o)
    collection.objects.link(o);o.parent=parent
    for p in o.data.polygons:p.use_smooth=True
    return o

def ell(name,at,dim,mat,parent):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,radius=1,location=at)
    return finish(bpy.context.object,name,mat,parent,tuple(v/2 for v in dim))

def cyl(name,at,r,d,mat,parent):
    bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=r,depth=d,location=at)
    o=finish(bpy.context.object,name,mat,parent)
    b=o.modifiers.new('Cast edge radius','BEVEL');b.width=min(.0008,d/5);b.segments=3
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return o

def slab(name,at,dim,mat,parent,angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.rotation_euler[1]=angle
    o=finish(o,name,mat,parent,dim);b=o.modifiers.new('Soft edges','BEVEL');b.width=.001;b.segments=3
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return o

def empty(name,at,parent=None):
    o=bpy.data.objects.new(name,None);collection.objects.link(o);o.location=at;o.parent=parent;return o

if __name__=='__main__':
    if not bpy.data.filepath:
        for name in ('Cube','Camera','Light'):
            o=bpy.data.objects.get(name)
            if o:bpy.data.objects.remove(o,do_unlink=True)
    # Only our collection is removed on rerun; startup camera/light are harmless and excluded.
    old=bpy.data.collections.get('Collection_'+SLUG)
    if old:
        for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new('Collection_'+SLUG);bpy.context.scene.collection.children.link(collection)
    bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
    root=empty('ROOT_'+SLUG,(0,0,0))
    bronze=material('Statues_AgedBronze',(.23,.12,.045),.85,.36)
    edge=material('Statues_PolishedBrass',(.49,.29,.10),.88,.27)
    dark=material('Statues_DarkPatina',(.027,.049,.043),.72,.5)
    base=material('Statues_BurgundyPlinth',(.065,.012,.022),.12,.4)
    for idx,(label,x) in enumerate([('Detective',-.105),('Archivist',0),('Commissioner',.105)]):
        assembly=empty(label+'_Assembly',(x,0,0),root)
        # Local coordinates below are relative to the base-centered assembly origin.
        cyl(label+'_Plinth',(0,0,.005),.036,.010,base,assembly)
        cyl(label+'_FootRim',(0,0,.0015),.037,.003,edge,assembly)
        cyl(label+'_UpperRim',(0,0,.011),.034,.002,edge,assembly)
        cyl(label+'_Pedestal',(0,0,.020),.021,.016,bronze,assembly)
        cyl(label+'_BustSeat',(0,0,.029),.028,.002,edge,assembly)
        bust=empty(label+'_BustModule',(0,0,0),assembly)
        ell(label+'_Shoulders',(0,0,.046),(.065,.032,.038),bronze,bust)
        slab(label+'_BustCut',(0,0,.033),(.046,.026,.007),bronze,bust)
        cyl(label+'_Neck',(0,0,.069),.009,.017,bronze,bust)
        ell(label+'_Head',(0,-.001,.091),(.031,.029,.041),bronze,bust)
        ell(label+'_Nose',(0,-.016,.091),(.007,.010,.012),bronze,bust)
        ell(label+'_Chin',(0,-.007,.076),(.019,.017,.010),bronze,bust)
        for side in [-1,1]:
            ell(label+'_Ear_'+str(side),(side*.016,0,.09),(.006,.008,.013),bronze,bust)
            slab(label+'_Brow_'+str(side),(side*.007,-.014,.099),(.011,.003,.003),dark,bust,side*.12)
            slab(label+'_Lapel_'+str(side),(side*.010,-.016,.052),(.010,.004,.030),edge,bust,side*.32)
        slab(label+'_Tie',(0,-.018,.048),(.005,.003,.020),dark,bust)
        for z in [.037,.043]:ell(label+'_Button_'+str(z),(0,-.017,z),(.003,.002,.003),edge,bust)
        if idx==0:
            ell(label+'_FedoraBrim',(0,0,.108),(.052,.044,.005),dark,bust)
            ell(label+'_FedoraCrown',(0,.001,.116),(.033,.029,.023),bronze,bust)
            cyl(label+'_HatBand',(0,0,.109),.017,.004,dark,bust)
        elif idx==1:
            ell(label+'_SweptHair',(0,.003,.109),(.034,.029,.014),dark,bust)
            for side in [-1,1]:
                bpy.ops.mesh.primitive_torus_add(major_radius=.006,minor_radius=.0008,major_segments=32,minor_segments=8,location=(side*.007,-.016,.095),rotation=(math.pi/2,0,0))
                finish(bpy.context.object,label+'_Spectacles_'+str(side),edge,bust)
            slab(label+'_GlassesBridge',(0,-.017,.095),(.005,.0015,.0015),edge,bust)
        else:
            ell(label+'_UniformCap',(0,.002,.111),(.039,.034,.012),dark,bust)
            ell(label+'_CapVisor',(0,-.014,.108),(.033,.023,.004),bronze,bust)
            ell(label+'_CapBadge',(0,-.016,.113),(.006,.002,.007),edge,bust)
            for side in [-1,1]:slab(label+'_Epaulette_'+str(side),(side*.024,0,.059),(.015,.022,.004),edge,bust)
    OUT.mkdir(parents=True,exist_ok=True)
    source=Path(__file__).read_text(encoding='utf-8');text=bpy.data.texts.get('GENERATE_'+SLUG+'.py') or bpy.data.texts.new('GENERATE_'+SLUG+'.py');text.clear();text.write(source)
    if OUT!=HERE:shutil.copyfile(__file__,OUT/'build_asset.py')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(SLUG+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for o in collection.objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/(SLUG+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=False)
