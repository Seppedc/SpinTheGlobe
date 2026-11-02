"""Deterministic map-frame generator. Metric, open aperture, four mitered rails.
Output override DETECTIVEDESK_ASSET_OUTPUT keeps queue builds outside Assets.
"""
from pathlib import Path
import os, shutil
import bpy

SLUG = 'WorldMapFrame'
WIDTH, HEIGHT = 1.60, 0.65
# Inset from outer edge, front/back depth. Shaped wooden molding with inner bevel.
PROFILE = [(0, .020), (0, -.010), (.004, -.018), (.012, -.021),
           (.017, -.018), (.023, -.018), (.034, -.007), (.036, .005),
           (.036, .020)]

def build():
    out = Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT', Path(__file__).parent)).resolve()
    out.mkdir(parents=True, exist_ok=True)
    # Only the isolated startup objects or this asset collection may be removed.
    for obj in list(bpy.data.objects):
        if obj.name in ('Cube', 'Camera', 'Light') or obj.name.startswith(SLUG+'_') or obj.name == 'ROOT_'+SLUG:
            bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    coll = bpy.data.collections.get('Collection_'+SLUG) or bpy.data.collections.new('Collection_'+SLUG)
    if coll.name not in scene.collection.children: scene.collection.children.link(coll)
    root = bpy.data.objects.new('ROOT_'+SLUG, None); coll.objects.link(root)
    root['aperture_width_m'] = WIDTH-.072
    root['aperture_height_m'] = HEIGHT-.072
    root['front_direction'] = '-Y; bottom-center pivot; Z up'
    materials=[]
    for name, color, rough in [('Mahogany',(.105,.039,.019,1),.36), ('InnerBevel',(.17,.075,.032,1),.40), ('Rebate',(.045,.020,.012,1),.57)]:
        mat=bpy.data.materials.new(SLUG+'_'+name);mat.use_nodes=True
        bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=rough
        mat.diffuse_color=color;materials.append(mat)
    # Each profile ring has identical diagonal corners, producing true 45-degree miters.
    rings=[]
    for inset, depth in PROFILE:
        x=WIDTH/2-inset;lo=inset;hi=HEIGHT-inset
        rings.append([(-x,depth,lo),(x,depth,lo),(x,depth,hi),(-x,depth,hi)])
    for side,name in enumerate(['BottomRail','RightStile','TopRail','LeftStile']):
        verts=[]
        for ring in rings: verts.extend([ring[side],ring[(side+1)%4]])
        n=len(PROFILE);faces=[]
        for j in range(n):
            k=(j+1)%n;faces.append((2*j,2*j+1,2*k+1,2*k))
        faces.extend([tuple(2*j for j in reversed(range(n))),tuple(2*j+1 for j in range(n))])
        mesh=bpy.data.meshes.new(SLUG+'_'+name);mesh.from_pydata(verts,[],faces);mesh.update()
        obj=bpy.data.objects.new(SLUG+'_'+name,mesh);coll.objects.link(obj);obj.parent=root
        for mat in materials:mesh.materials.append(mat)
        for poly in mesh.polygons:poly.material_index=1 if poly.index in (5,6) else 2 if poly.index in (7,8) else 0
        bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
        obj.select_set(False)
        bevel=obj.modifiers.new('Fine edge easing','BEVEL');bevel.width=.0004;bevel.segments=2
        obj.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    source=Path(__file__).read_text(encoding='utf-8-sig')
    text=bpy.data.texts.get('GENERATE_'+SLUG+'.py') or bpy.data.texts.new('GENERATE_'+SLUG+'.py');text.clear();text.write(source)
    dest=out/'build_asset.py'
    if dest.resolve()!=Path(__file__).resolve():dest.write_text(source,encoding='utf-8')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(SLUG+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in coll.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(out/(SLUG+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)

if __name__=='__main__':build()
