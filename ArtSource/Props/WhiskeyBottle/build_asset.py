"""Deterministic unbranded fluted crystal decanter; meters, base-origin root.
Written reference: References/REFERENCE_BRIEF.md. No external dependencies.
Queue builds write exclusively to DETECTIVEDESK_ASSET_OUTPUT staging.
"""
from pathlib import Path
import math
import os
import bpy

SLUG = 'WhiskeyBottle'
HERE = Path(__file__).resolve().parent
OUT = Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT', str(HERE)))

def build():
    # A fresh background process is required; only our own named collection is removed.
    old = bpy.data.collections.get('Collection_' + SLUG)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    for obj in list(bpy.context.scene.objects):
        if obj.name in {'Cube', 'Camera', 'Light'}:
            bpy.data.objects.remove(obj, do_unlink=True)
    col = bpy.data.collections.new('Collection_' + SLUG)
    bpy.context.scene.collection.children.link(col)
    root = bpy.data.objects.new('ROOT_' + SLUG, None)
    col.objects.link(root)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    glass = bpy.data.materials.new('Crystal_Clear')
    glass.use_nodes = True
    # Keep viewport/export color opaque; transmission is authored separately.
    glass.diffuse_color = (.86, .96, .95, 1)
    bsdf = glass.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (.86, .96, .95, 1)
    bsdf.inputs['Roughness'].default_value = .065
    bsdf.inputs['IOR'].default_value = 1.46
    bsdf.inputs['Transmission Weight'].default_value = 1

    def lathe(name, profile, pivot=0):
        # Closed radial cross-section: exterior rises, returns down interior.
        n = 192
        verts = []
        for radius, z, flute in profile:
            for i in range(n):
                angle = 2 * math.pi * i / n
                r = radius + flute * (.5 + .5 * math.cos(24 * angle))
                verts.append((r * math.cos(angle), r * math.sin(angle), z - pivot))
        faces = []
        for j in range(len(profile)):
            k = (j + 1) % len(profile)
            for i in range(n):
                nxt = (i + 1) % n
                faces.append((j*n+i, j*n+nxt, k*n+nxt, k*n+i))
        mesh = bpy.data.meshes.new(name + '_Mesh')
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        col.objects.link(obj)
        obj.parent = root
        obj.location.z = pivot
        mesh.materials.append(glass)
        for poly in mesh.polygons:
            poly.use_smooth = True
        return obj

    lathe('Decanter_HollowFlutedBody', [
        (.0001,0,0),(.043,0,0),(.049,.002,0),(.052,.006,0),
        (.052,.012,0),(.051,.016,.0005),(.049,.021,.0015),
        (.049,.132,.0015),(.048,.143,.0015),(.045,.153,.001),
        (.037,.165,.0005),(.022,.180,0),(.018,.186,0),
        (.018,.211,0),(.020,.213,0),(.020,.218,0),(.019,.220,0),
        (.014,.220,0),(.0135,.217,0),(.0135,.188,0),
        (.017,.180,0),(.033,.163,0),(.042,.148,0),
        (.045,.134,0),(.045,.023,0),(.041,.016,0),(.0001,.016,0)])
    stopper = lathe('Stopper_RemovableCrystal', [
        (.0001,.199,0),(.011,.199,0),(.0128,.202,0),
        (.013,.219,0),(.018,.222,0),(.024,.225,0),
        (.026,.230,0),(.026,.248,0),(.023,.254,0),
        (.017,.258,0),(.0001,.258,0)], .220)
    stopper['pivot_description'] = 'Extraction pivot at bottle mouth, Z=0.220m'
    root['dimensions_design_m'] = '0.104 diameter x 0.258 height'
    root['description'] = 'Empty unbranded clear decanter; 4mm wall, 16mm weighted base, removable stopper'
    OUT.mkdir(parents=True, exist_ok=True)
    source = Path(__file__).read_text(encoding='utf-8')
    (OUT/'build_asset.py').write_text(source, encoding='utf-8')
    text = bpy.data.texts.get('GENERATE_' + SLUG + '.py') or bpy.data.texts.new('GENERATE_' + SLUG + '.py')
    text.clear()
    text.write(source)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(SLUG+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=str(OUT/(SLUG+'.fbx')), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        bake_space_transform=True, add_leaf_bones=False, bake_anim=False)

if __name__ == '__main__':
    build()
