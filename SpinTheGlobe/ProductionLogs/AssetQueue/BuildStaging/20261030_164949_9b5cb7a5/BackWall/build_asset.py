"""BackWall: three straight architectural bays with recessed mahogany panels.
Meters; front faces negative Y. No room corners or gameplay integration.
Self-contained deterministic source; output redirected by queue build wrapper.
"""
from pathlib import Path
import os
import bpy

HERE = Path(__file__).resolve().parent

def build():
    out = Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT', str(HERE)))
    out.mkdir(parents=True, exist_ok=True)
    # Fresh background scene only; remove our own collection on subsequent runs.
    old = bpy.data.collections.get('Collection_BackWall')
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    for obj in list(bpy.context.scene.objects):
        if obj.name in {'Cube', 'Camera', 'Light'}:
            bpy.data.objects.remove(obj, do_unlink=True)
    collection = bpy.data.collections.new('Collection_BackWall')
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new('ROOT_BackWall', None)
    collection.objects.link(root)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1

    def material(name, color, roughness):
        m = bpy.data.materials.new(name)
        m.diffuse_color = (*color, 1)
        m.use_nodes = True
        p = m.node_tree.nodes.get('Principled BSDF')
        p.inputs['Base Color'].default_value = (*color, 1)
        p.inputs['Roughness'].default_value = roughness
        return m

    wood = material('BackWall_Mahogany_Frame', (.105, .032, .019), .34)
    inset = material('BackWall_Mahogany_Inset', (.155, .052, .028), .42)
    plaster = material('BackWall_Warm_Umber_Plaster', (.19, .155, .108), .9)
    shadow = material('BackWall_Recess_Shadow', (.032, .015, .011), .65)

    def box(name, center, size, mat, bevel=.003):
        bpy.ops.mesh.primitive_cube_add(size=1, location=center)
        obj = bpy.context.object
        obj.name = name
        for c in list(obj.users_collection):
            c.objects.unlink(obj)
        collection.objects.link(obj)
        obj.dimensions = size
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        obj.parent = root
        obj.data.materials.append(mat)
        if bevel:
            b = obj.modifiers.new('Soft joinery edges', 'BEVEL')
            b.width = bevel
            b.segments = 3
            n = obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
        return obj

    # Three independently named 2m bays; overall 6 x .24 x 3m.
    for bay, x in enumerate((-2., 0., 2.), 1):
        prefix = f'Bay_{bay:02d}'
        box(prefix+'_Wall', (x, .04, 1.5), (2, .16, 3), plaster)
        box(prefix+'_WainscotBacking', (x, -.055, .60), (2, .03, 1.2), shadow)
        for panel, dx in enumerate((-.5, .5), 1):
            px = x+dx
            box(prefix+f'_Inset_{panel}', (px, -.078, .60), (.82, .025, .88), inset)
            for side in (-1, 1):
                box(prefix+f'_Panel_{panel}_BeadV_{side}', (px+side*.426, -.100, .60), (.018, .025, .92), wood, .004)
                box(prefix+f'_Panel_{panel}_BeadH_{side}', (px, -.100, .60+side*.451), (.87, .025, .018), wood, .004)
        for j, dx in enumerate((-.96, 0., .96)):
            box(prefix+f'_Stile_{j}', (x+dx, -.09, .61), (.08, .06, 1.02), wood)
        box(prefix+'_LowerRail', (x, -.09, .12), (2, .06, .08), wood)
        box(prefix+'_UpperRail', (x, -.09, 1.10), (2, .06, .10), wood)
    bpy.context.view_layer.update()
    source = Path(__file__).read_text(encoding='utf-8')
    text = bpy.data.texts.get('GENERATE_BackWall.py') or bpy.data.texts.new('GENERATE_BackWall.py')
    text.clear()
    text.write(source)
    (out/'build_asset.py').write_text(source, encoding='utf-8')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'BackWall.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=str(out/'BackWall.fbx'), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        bake_space_transform=True, add_leaf_bones=False, bake_anim=False)

if __name__ == '__main__':
    build()
