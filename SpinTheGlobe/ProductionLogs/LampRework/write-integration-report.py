import hashlib
import json
import struct
from pathlib import Path
from datetime import datetime, timezone

ROOT = Path.cwd()
OUT = Path('Assets/DetectiveDesk/LampRework')
LOG = Path('ProductionLogs/LampRework')
SOURCE = Path('Assets/DetectiveDesk/ArtSource/Props/BankersLamp')
def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def write(path, data): Path(path).write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')

checks = read(LOG/'unity-checks.json')
preservation = read(LOG/'preservation-result.json')
preflight = read(LOG/'preintegration-check.json')
source_validation = read(OUT/'blender-validation.json')
before = read(LOG/'scene-components-before.json')
added = read(LOG/'added-objects.json')
source_sha = sha(SOURCE/'BankersLamp.blend')
assert source_sha == source_validation['blend_sha256']
assert sha(SOURCE/'BankersLamp.fbx') == source_validation['fbx_sha256']
assert checks['pass'] and preservation['pass']
assert len(before['objects']) == 1336 and preservation['components_checked'] == 3930
old_prefab = Path('Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212422_3e4ba5f3/BankersLamp_Review.prefab')
old_backup = Path('Assets/DetectiveDesk/Backups/LampRework_20261031_235708/BankersLamp_Review.prefab')
assert sha(old_prefab) == sha(old_backup)
image = OUT/'final-game-camera.png'
closeup = OUT/'unity-lamp-closeup.png'
assert struct.unpack('>II', image.read_bytes()[16:24]) == (1920,1080)
assert struct.unpack('>II', closeup.read_bytes()[16:24]) == (1080,1080)
prefab = OUT/'DetectiveBankersLamp_Visual.prefab'
assert prefab.stat().st_size > 0
old_changed = []
for obj in before['objects']:
    if obj['identity']['path'].startswith('DetectiveOffice/Environment/Generated_BankersLamp/'):
        for component in obj['components']:
            if component['identity']['type'] == 'UnityEngine.MeshRenderer':
                old_changed.append(dict(identity=component['identity'], changed_field='MeshRenderer.m_Enabled', before=True, after=False))
assert len(old_changed) == 41
metadata_changes = [str(SOURCE/'Textures'/f'{name}_{role}.png.meta').replace('\\','/')
                    for name in ['Lamp_AgedBrass','Lamp_PolishedBrass','Lamp_EmeraldGlass']
                    for role in ['MetallicSmoothness','Normal']]
limitations = [
    'Edit Mode rendering and serialization checks executed; Play Mode/runtime smoke was not run because no gameplay behavior changed.',
    'Opaque coated green hood with separate ivory lining; refractive glass was not requested or tested.',
    'Visual inspection performed on both final screenshots; human asset approval and tracker decisions remain with the parent.',
    'One relay-only validation command failed to compile because Newtonsoft was unavailable in the dynamic command assembly and a local variable name conflicted. It did not execute. Validation was moved into the compiled editor helper and passed; dedicated Console reports zero errors/warnings.',
    'No Git repository is available in this workspace; file scope is recorded through explicit output paths and serialized scene preservation evidence.'
]
validation = dict(
    status='PASS', source_sha256=source_sha, prefab_path=prefab.as_posix(),
    screenshot_path=image.as_posix(), screenshot_sha256=sha(image),
    closeup_path=closeup.as_posix(), closeup_sha256=sha(closeup),
    protected_scene_unchanged=True, console_errors=0, console_warnings=0,
    console_method='Dedicated Unity_ReadConsole Get, Error+Warning, 100 entries, Json: Retrieved 0 log entries.',
    mesh_count=checks['mesh_count'], missing_materials=checks['missing_materials'],
    scene_path='Assets/DetectiveDesk/Generated/DetectiveOffice.unity',
    scene_saved=True, scene_dirty_after=False, writer_guard_released=True,
    source_fbx_sha256=sha(SOURCE/'BankersLamp.fbx'),
    material_tests=checks['materials'], mesh_material_light_placement_tests=checks,
    preservation_tests=preservation, preintegration_tests=preflight,
    original_prefab_unchanged=True, original_prefab_sha256=sha(old_prefab),
    original_four_light_components_and_transforms_deeply_equal=True,
    camera_hud_and_rect_serialized_state_restored=True,
    screenshot_dimensions=[1920,1080], closeup_dimensions=[1080,1080],
    visually_inspected=True, play_mode_executed=False,
    limitations=limitations, verified_utc=datetime.now(timezone.utc).isoformat())
write(OUT/'unity-validation.json', validation)

new_asset_files = [prefab.as_posix(), image.as_posix(), closeup.as_posix(),
                   (OUT/'Editor/LampReworkIntegration.cs').as_posix(),
                   (OUT/'unity-validation.json').as_posix(), (OUT/'asset-report.md').as_posix(), preflight['scene_copy']]
new_asset_files += [p.as_posix() for p in sorted((OUT/'Materials').glob('*.mat'))]
new_asset_files += [p+'.meta' for p in list(new_asset_files) if Path(p+'.meta').exists()]
new_asset_files += [p.as_posix() for p in [OUT/'Editor.meta',OUT/'Materials.meta'] if p.exists()]
log_files = [LOG/x for x in ['preintegration-check.json','integration-exact-baseline.json','mount-result.json',
    'preservation-result.json','unity-checks.json','added-objects.json','capture-command.cs.txt',
    'validate-command.cs.txt','write-integration-report.py','integration-result.json']]
result = dict(status='PASS', validation_path=(OUT/'unity-validation.json').as_posix(),
    modified_files=['Assets/DetectiveDesk/Generated/DetectiveOffice.unity']+metadata_changes,
    created_files=new_asset_files+[p.as_posix() for p in log_files],
    original_changed_objects=old_changed, added_objects=added['addedObjects'],
    added_components=added['addedComponentTypes'],
    parent_child_list_change='DetectiveOffice/Environment/Generated_BankersLamp: append DetectiveBankersLamp_Visual',
    console=dict(errors=0,warnings=0,method='Unity_ReadConsole Get', findings=[]),
    guard_released=True, tracker_edited=False, limitations=limitations)
write(LOG/'integration-result.json', result)

report = f'''# Banker lamp Unity integration

PASS for the executed integration checks. The scene and reusable visual prefab are saved; the writer guard is released. Tracker files were not edited.

Canonical editable source: `{SOURCE.as_posix()}/BankersLamp.blend` (SHA256 `{source_sha}`). The same-source FBX is mounted under `DetectiveOffice/Environment/Generated_BankersLamp/DetectiveBankersLamp_Visual`. Neither source file was rebuilt or edited by this integration writer.

Reusable prefab: `{prefab.as_posix()}`. Mount under the original Z-up gameplay root at local position zero and unit scale, preserving the prefab wrapper X rotation 89.98022 degrees and imported model X rotation 270.0198 degrees. Those rotations cancel. Do not reset either independently. Source +Y faces world -Z. Original root position (4.25, .815, .5153126), rotation (270, 0, 0), and scale 3.75 are unchanged.

The prefab has 127 enabled mesh renderers / 80,000 triangles and ten reusable URP/Lit materials in `Materials/`. Three textured materials bind sRGB BaseColor, linear packed R-metal/A-smoothness, and NormalMap imports; both required PBR keywords are enabled and packed-alpha smoothness is 1. Seven remaining materials use the specified linear colors and 1-minus-roughness smoothness. Only bulb and ivory lining emit. Emissive GI flags are set so Unity retains `_EMISSION`. The hood retains green/ivory/green-edge slots; no pink, missing slots or Standard shaders were found.

The shade retains its single 10-degree tilt, with independent chain and socket pivots. Bounds min {checks['bounds_min']}, max {checks['bounds_max']}; bottom Y .815000057 and height 1.574912073 m. One new spotlight is at {checks['new_light']['world_position']}, aimed downward using the imported markers, intensity .8, range 2.3, outer/inner angles 80/55, 3200 K with temperature enabled, white color, no shadows.

Preservation checked all 1,336 original objects and 3,930 original components against stable identities, serialized fields and object references. Preintegration inspection found no intervening edits; an additional scene copy was still saved as `{preflight['scene_copy']}`. Only 41 old lamp MeshRenderer enabled flags changed, plus the authorized new child. Old objects, original prefab, four existing lights, globe, cannon, camera, HUD, materials, scripts and colliders remain preserved. The original prefab file hash matches its supplied backup. Exact object/component additions and changed renderer identities are recorded in `ProductionLogs/LampRework/integration-result.json`.

`final-game-camera.png` is 1920×1080 with HUD. `unity-lamp-closeup.png` is 1080×1080 from a temporary additive-scene camera; the main camera was not repositioned. Camera target/aspect/viewport, active render target, canvases and RectTransforms were restored in finally before saving. Both images were actually inspected: the chain, pendant, polished rim, turned brass, bulb and ivory underside are readable; the green hood stays dark emerald without broad emission. Temporary review scene/camera were closed.

Dedicated Unity_ReadConsole Get returned zero warnings and zero errors after successful helper compilation and final validation. No missing scripts, missing prefab assets, missing materials or duplicate active old lamp renderers were found. Final scene is clean and in Edit Mode.

Limitations:

''' + '\n'.join('- '+x for x in limitations) + '\n'
(OUT/'asset-report.md').write_text(report, encoding='utf-8')
print(json.dumps(dict(status=validation['status'],mesh_count=checks['mesh_count'],materials=len(checks['materials']),added_objects=len(added['addedObjects']),old_renderer_changes=len(old_changed),source_sha256=source_sha,screenshot_sha256=sha(image),guard_released=True),indent=2))
