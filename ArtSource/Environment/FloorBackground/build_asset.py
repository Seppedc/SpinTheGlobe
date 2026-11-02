"""FloorBackground: deterministic shallow mahogany room floor, meters.
Art direction: References/REFERENCE_BRIEF.md. No external imagery or textures.
Run in isolated factory-startup Blender; output is controlled by queue staging env.
"""
from pathlib import Path
import os
import shutil
import bpy

SLUG = 'FloorBackground'
HERE = Path(__file__).resolve().parent
OUT = Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT', str(HERE))).resolve()

def material(name, color, roughness):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bs = mat.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = roughness
    return mat

def build():
    # Delete only our own collection. Factory startup objects are omitted from export.
    old = bpy.data.collections.get('Collection_' + SLUG)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    collection = bpy.data.collections.new('Collection_' + SLUG)
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new('ROOT_' + SLUG, None)
    collection.objects.link(root)
    root['description'] = 'Shallow room floor; +Y rear, -Y seated camera; no wall or desk geometry'
    root['floor_surface_m'] = 0.08
    dark = material('Floor_RecessedSeams', (.018, .011, .009), .82)
    border = material('Floor_MahoganyBorder', (.067, .026, .014), .46)
    woods = [material('Floor_Mahogany_%02d' % i, (.080+i*.004, .032+i*.0018, .018+i*.001), .48+i*.014) for i in range(6)]
    def box(name, loc, size, mat, bevel=0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
        obj = bpy.context.object
        obj.name = name
        obj.dimensions = size
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        for col in list(obj.users_collection):
            col.objects.unlink(obj)
        collection.objects.link(obj)
        obj.parent = root
        obj.data.materials.append(mat)
        if bevel:
            mod = obj.modifiers.new('WornEdge', 'BEVEL')
            mod.width = bevel
            mod.segments = 2
            obj.modifiers.new('CornerNormals', 'WEIGHTED_NORMAL')
        return obj
    box('Floor_ShadowSubstrate', (0,0,.025), (5,3.1,.05), dark)
    for y in [-1.475,1.475]:
        box('Border_' + ('Front' if y < 0 else 'Rear'), (0,y,.065), (5,.15,.03), border,.002)
    for x in [-2.425,2.425]:
        box('Border_' + ('Left' if x < 0 else 'Right'), (x,0,.065), (.15,2.8,.03), border,.002)
    # Fourteen rows of physically separated boards. Staggered end joints avoid grid tiling.
    for row in range(14):
        y = -1.4 + (row+.5)*.2
        cuts = [-2.35]
        x = -2.35 + (.42,.81,1.12)[row%3]
        while x < 2.35:
            cuts.append(x)
            x += 1.18
        cuts.append(2.35)
        for part,(a,b) in enumerate(zip(cuts,cuts[1:])):
            box('Board_R%02d_P%02d' % (row,part), ((a+b)/2,y,.065), (b-a-.003,.197,.03), woods[(row*3+part*5)%6],.001)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    # Remove factory camera/cube/light only in this explicitly empty build process.
    for obj in list(scene.objects):
        if obj.name not in collection.objects:
            bpy.data.objects.remove(obj, do_unlink=True)
    OUT.mkdir(parents=True,exist_ok=True)
    source = Path(__file__).read_text(encoding='utf-8')
    text = bpy.data.texts.get('GENERATE_'+SLUG+'.py') or bpy.data.texts.new('GENERATE_'+SLUG+'.py')
    text.clear()
    text.write(source)
    if OUT / 'build_asset.py' != Path(__file__).resolve():
        shutil.copyfile(__file__, OUT / 'build_asset.py')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / (SLUG+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=str(OUT / (SLUG+'.fbx')), use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,
        add_leaf_bones=False,bake_anim=False)

if __name__ == '__main__':
    build()
