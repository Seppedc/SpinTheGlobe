"""Export the asset's own collection from a saved canonical .blend, without changing it.
blender -b ArtSource/Props/WhiskeyGlass/WhiskeyGlass.blend --python-exit-code 1 --python Tools/export-blender-fbx.py -- --asset WhiskeyGlass --output WhiskeyGlass.fbx
"""
import argparse
import json
import sys
from pathlib import Path
import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--asset', required=True)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--report', type=Path)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(bpy.data.filepath)
assert source.is_file(), 'Open the canonical .blend first'
collection = bpy.data.collections.get('ART_' + args.asset)
assert collection, 'Missing named asset collection'
objects = list(collection.all_objects)
meshes = [o for o in objects if o.type == 'MESH']
assert meshes and all(len(o.data.vertices) > 0 for o in meshes), 'Empty geometry'
generator = source.with_name('build_' + args.asset.lower() + '.py')
if not generator.is_file():
    generator = source.with_name('build_asset.py')
embedded = bpy.data.texts.get('GENERATE_' + args.asset + '.py')
assert generator.is_file() and embedded, 'Missing external or embedded generator'
assert embedded.as_string() == generator.read_text(encoding='utf-8'), 'Embedded generator differs from source'
assert all(len(o.data.materials) and all(m for m in o.data.materials) for o in meshes), 'Missing mesh materials'
if args.asset == 'ExampleCoffer':
    assert all(collection.objects.get('PinSlot_%02d' % i) for i in range(1, 13)), 'Missing socket marker'
    assert collection.objects.get('Lid_HingePivot'), 'Missing lid pivot'
depsgraph = bpy.context.evaluated_depsgraph_get()
triangles = 0
for obj in meshes:
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    triangles += len(mesh.loop_triangles)
    evaluated.to_mesh_clear()
report = {
    'blender_version': bpy.app.version_string,
    'source_blend': str(source),
    'generator': str(generator),
    'embedded_generator_exact_match': True,
    'mesh_count': len(meshes),
    'evaluated_triangles': triangles,
    'materials': sorted({m.name for o in meshes for m in o.data.materials}),
    'objects': [o.name for o in objects],
    'meshes_with_uvs': sum(bool(o.data.uv_layers) for o in meshes),
    'image_textures': [im.filepath for im in bpy.data.images if im.source == 'FILE'],
    'unit_scale_meters': bpy.context.scene.unit_settings.scale_length,
    'fbx': str(args.output.resolve()),
}
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:
    obj.select_set(True)
args.output.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(args.output.resolve()), use_selection=True,
    object_types={'MESH', 'EMPTY'}, apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
    global_scale=1, apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=False,
    mesh_smooth_type='FACE', add_leaf_bones=False,
    bake_anim=False, use_mesh_modifiers=True, path_mode='RELATIVE')
assert args.output.stat().st_size > 1024
if args.report:
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
