"""Deterministic CaseFiles model: metric folder, independent sheets and formed steel clip.
No external imagery. Art direction: local References/REFERENCE_BRIEF.md.
"""
from pathlib import Path
import os, math
import bpy
from mathutils import Vector
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',str(HERE)))

def build():
    OUT.mkdir(parents=True,exist_ok=True)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    collection=bpy.data.collections.new('Collection_CaseFiles');scene.collection.children.link(collection)
    root=bpy.data.objects.new('ROOT_CaseFiles',None);collection.objects.link(root)
    def mat(name,color,rough=.7,metal=0):
        m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
        p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
        return m
    kraft=mat('Folder_AgedManila',(.48,.31,.135));paper=mat('Paper_WarmIvory',(.82,.77,.63));ink=mat('Ink_Charcoal',(.045,.038,.031));red=mat('Ink_Oxblood',(.25,.025,.025));steel=mat('Clip_BrushedSteel',(.34,.37,.39),.28,.85)
    def finish(obj,name,material):
        obj.name=name
        for c in list(obj.users_collection):c.objects.unlink(obj)
        collection.objects.link(obj);obj.parent=root;obj.data.materials.append(material)
        return obj
    def box(name,loc,size,material,bevel=0):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);obj=bpy.context.object;obj.dimensions=size
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        finish(obj,name,material)
        if bevel:
            mod=obj.modifiers.new('Soft paper edges','BEVEL');mod.width=bevel;mod.segments=2
            obj.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
        return obj
    box('Folder_BackCover',(0,0,.0006),(.245,.325,.0012),kraft,.0004)
    box('Folder_IndexTab',(.078,.168,.0006),(.065,.018,.0012),kraft,.001)
    # Six individually editable leaves; no combined paper block.
    for i in range(6):
        box('Paper_Layer_%02d'%(i+1),((i%3-1)*.0007,-.001+i*.0004,.0015+i*.00045),(.218,.294,.0003),paper,.0001)
    # Cover rests below the exposed upper document margin.
    box('Folder_FrontCover',(0,-.028,.0052),(.245,.269,.0012),kraft,.0004)
    box('Folder_SpineFold',(0,-.1618,.0028),(.245,.0016,.0044),kraft,.0003)
    for y in [-.150,-.146]:box('Scored_Fold_'+str(y),(0,y,.00584),(.232,.00035,.00007),ink)
    def text(name,body,loc,size,material):
        curve=bpy.data.curves.new(name,'FONT');curve.body=body;curve.size=size;curve.extrude=.000008
        obj=bpy.data.objects.new(name,curve);collection.objects.link(obj);obj.location=loc;obj.parent=root;curve.materials.append(material)
        bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.convert(target='MESH');obj.select_set(False)
    bpy.ops.object.select_all(action='DESELECT')
    text('Cover_CaseNumber','CASE / 017',(-.096,.021,.00588),.015,ink)
    text('Cover_Heading','INVESTIGATION FILE',(-.095,.001,.00588),.007,ink)
    text('Cover_Confidential','CONFIDENTIAL',(-.095,-.105,.00588),.009,red)
    text('Tab_Index','017',(.060,.165,.00125),.006,ink)
    text('Sheet_Header','DETECTIVE BUREAU',(-.092,.130,.004),.007,ink)
    text('Sheet_Subtitle','EVIDENCE / INTAKE RECORD',(-.092,.119,.004),.004,ink)
    for i in range(4):box('Document_Rule_%02d'%i,(-.018,.110-i*.004,.00402),(.15,.00025,.00004),ink)
    for i in range(3):
        y=-.034-i*.018
        box('Cover_FormLine_%02d'%i,(.012,y,.00588),(.175,.0003,.00005),ink)
    # Formed oblong wire, radius 0.65mm. Separate semantic clip object.
    points=[]
    for cx,cy,r,a0,a1 in [(0,.014,.004,0,180),(0,-.014,.004,180,360),(0,.010,.0026,0,180),(0,-.010,.0026,180,310)]:
        for j in range(17):
            a=math.radians(a0+(a1-a0)*j/16);points.append((cx+r*math.cos(a),cy+r*math.sin(a),0))
    curve=bpy.data.curves.new('PaperClip_Path','CURVE');curve.dimensions='3D';curve.bevel_depth=.00065;curve.bevel_resolution=4
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for p,co in zip(spline.points,points):p.co=(*co,1)
    obj=bpy.data.objects.new('PaperClip_Steel',curve);collection.objects.link(obj);obj.location=(.082,.102,.0067);obj.parent=root;curve.materials.append(steel)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH')
    # Exact source snapshot, independent from shared helpers.
    source=Path(__file__).read_text(encoding='utf-8');(OUT/'build_asset.py').write_text(source,encoding='utf-8')
    block=bpy.data.texts.new('GENERATE_CaseFiles.py');block.write(source)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CaseFiles.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/'CaseFiles.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
if __name__=='__main__':build()
