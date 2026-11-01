"""Deterministic WallMolding: 4.4m wide, 2.7m tall architectural trim.
Written noir reference brief only; no photographic matching or BackWall fit implied.
Run in a fresh factory-startup Blender process. No shared helper dependencies.
"""
from pathlib import Path
import os
import bpy

SLUG = 'WallMolding'
HERE = Path(__file__).resolve().parent

def material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bs = mat.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metallic
    bs.inputs['Roughness'].default_value = roughness
    return mat

def build():
    # Only our own previous collection is removed. Factory objects are excluded
    # from the exported selection, not destructively deleted.
    old = bpy.data.collections.get('Collection_' + SLUG)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)
    collection = bpy.data.collections.new('Collection_' + SLUG)
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new('ROOT_' + SLUG, None)
    collection.objects.link(root)
    wood = material('WM_Mahogany', (.105, .036, .019), 0, .34)
    dark = material('WM_ShadowWood', (.035, .014, .009), 0, .42)
    brass = material('WM_AgedBrass', (.34, .22, .085), .82, .36)
    objects = []

    def profile(name, length, yz, zoffset=0, mat=wood):
        # Closed profile swept along X; origins retain useful section centers.
        count = len(yz)
        vertices = [(x, y, z + zoffset) for x in (-length/2, length/2) for y,z in yz]
        faces = [tuple(reversed(range(count))), tuple(range(count, 2*count))]
        faces += [(i, (i+1)%count, (i+1)%count+count, i+count) for i in range(count)]
        mesh = bpy.data.meshes.new(name + '_Mesh')
        mesh.from_pydata(vertices, [], faces); mesh.update()
        obj = bpy.data.objects.new(name, mesh); collection.objects.link(obj)
        obj.parent = root; obj.data.materials.append(mat)
        bevel = obj.modifiers.new('Fine edge highlights', 'BEVEL')
        bevel.width = .0007; bevel.segments = 2
        objects.append(obj)
        return obj

    # Front is -Y; full depth is centered about Y=0. Crown has a stepped
    # bed, curved cove approximation, projecting fascia and ceiling lip.
    profile('Crown_Profile', 4.4, [(.08,0),(.08,.19),(-.08,.19),(-.08,.168),
        (-.065,.168),(-.065,.145),(-.045,.14),(-.027,.125),(-.012,.102),
        (0,.075),(.012,.045),(-.005,.045),(-.005,.025),(.025,.025),(.025,0)], 2.51)
    profile('Baseboard_Profile', 4.4, [(.08,0),(-.005,0),(-.005,.028),(.015,.028),
        (.015,.135),(.022,.15),(.038,.16),(.038,.185),(.08,.185)])
    profile('ChairRail_Profile', 4.4, [(.08,0),(.025,0),(.025,.012),(.005,.022),
        (.005,.044),(.025,.056),(.025,.07),(.08,.07)], .98)
    profile('ChairRail_BrassFillet', 4.4, [(.004,0),(0,0),(0,.004),(.004,.004)], 1.012, brass)
    # Four lower panel frames; each is one closed mitered rectangular ring.
    # Recessed backing deliberately omitted: BackWall owns wall surfaces.
    for index in range(4):
        cx = (index - 1.5)*1.06
        outer = [(-.47,.275),(.47,.275),(.47,.875),(-.47,.875)]
        inner = [(-.447,.298),(.447,.298),(.447,.852),(-.447,.852)]
        vertices = [(cx+x,y,z) for y in (.052,.027) for ring in (outer,inner) for x,z in ring]
        faces=[]
        for i in range(4):
            j=(i+1)%4
            faces.extend([(i,j,j+4,i+4),(i+8,i+12,j+12,j+8),
                          (i,i+8,j+8,j),(i+4,j+4,j+12,i+12)])
        mesh=bpy.data.meshes.new('Panel_%02d_Mesh'%index);mesh.from_pydata(vertices,[],faces);mesh.update()
        obj=bpy.data.objects.new('PanelRail_%02d'%index,mesh);collection.objects.link(obj)
        obj.parent=root;obj.data.materials.append(wood)
        bevel=obj.modifiers.new('Panel edge highlights','BEVEL');bevel.width=.002;bevel.segments=3
        objects.append(obj)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    # Remove only untouched factory cube, camera and light in this isolated run.
    for obj in list(scene.objects):
        if obj not in objects and obj != root and obj.name in {'Cube','Camera','Light'}:
            bpy.data.objects.remove(obj,do_unlink=True)
    out=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT', HERE)).resolve()
    out.mkdir(parents=True,exist_ok=True)
    source=Path(__file__).read_text(encoding='utf-8')
    snapshot=out/'build_asset.py'
    if snapshot.resolve()!=Path(__file__).resolve():snapshot.write_text(source,encoding='utf-8')
    text=bpy.data.texts.get('GENERATE_'+SLUG+'.py') or bpy.data.texts.new('GENERATE_'+SLUG+'.py')
    text.clear();text.write(source)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(SLUG+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root]+objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(out/(SLUG+'.fbx')),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,
        add_leaf_bones=False,bake_anim=False)

if __name__ == '__main__':
    build()
