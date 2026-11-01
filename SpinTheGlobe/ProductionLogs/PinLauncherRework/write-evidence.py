import json, hashlib, math, re
from pathlib import Path
from datetime import datetime, timezone
p=Path("ProductionLogs/PinLauncherRework")
out=Path("Assets/DetectiveDesk/PinLauncherRework")
source=Path("Assets/DetectiveDesk/ArtSource/Gameplay/PinLauncher")
def read(path):return json.loads(Path(path).read_text(encoding="utf-8-sig"))
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
facts=read(p/"final-unity-facts.json")
preserve=read(p/"preservation-comparison.json")
placement=read(p/"placement.json")
runtime=read(p/"runtime-callback.json")
before=read(p/"scene-components-before.json")["objects"]
slots=[o["path"] for o in before if re.search(r"/DetectivePinCoffer/PinSlots/PinSlot_\d\d$",o["path"])]
assert len(slots)==12 and preserve["protected_scene_unchanged"]
assert not facts["is_compiling"] and not facts["is_updating"] and not facts["is_playing"] and not facts["scene_dirty"]
original=Path("Assets/DetectiveDesk/AssetProduction/PinLauncher/20261030_190710_5307019b/PinLauncher_Review.prefab")
backup=Path("Assets/DetectiveDesk/Backups/PinLauncherRework_20261031_212158_817/PinLauncher_Review.prefab")
original_preserved=sha(original)==sha(backup)
assert original_preserved
limitations=[
 "Handwheel is on the far side at the required aim direction and is partly visible/occluded in the Game camera. Full open-side silhouette is not achieved. Canonical generator revision is needed; no geometry was mirrored.",
 "Unity imported triangles total 73960; supplied Blender validation totals 74472. Mesh count is 168 in both. The triangle discrepancy was observed but not resolved by source regeneration.",
 "FirePin callback and Launcher action/game binding were tested in Play Mode. Physical mouse input, hover, and projectile mechanics were not tested or added.",
 "No obvious overlap with coffer, lamp, stamp or files in final Game camera. No exact mesh intersection test was performed.",
 "Source/embedded Blender validation is supplied parent evidence; this writer did not rerun Blender.",
 "Initial dynamic callback registration did not survive the Play Mode domain reload. The successful test used the opt-in precompiled Editor helper.",
 "Two initial writer snapshots serialized as empty objects and are invalid evidence. Preservation uses the supplied full original baseline and corrected explicit after export.",
 "Initial capture reported a render-target release warning. Final capture explicitly restored the camera target before disposal and completed without that warning; Console was never cleared.",
 "Visual approval and tracker/queue updates remain parent responsibilities."
]
common=dict(source_sha256=sha(source/"PinLauncher.fbx"),prefab_path=str(out/"DetectivePinLauncher_Visual.prefab").replace("\\","/"),screenshot_path=str(out/"preview.png").replace("\\","/"),screenshot_sha256=sha(out/"preview.png"),protected_scene_unchanged=True,console_errors=0,mesh_count=168,renderer_count=168,missing_materials=0)
validation=dict(common,status="PASS",scope="Executed Unity integration checks; visual approval pending",timestamp_utc=datetime.now(timezone.utc).isoformat(),facts=facts,original_objects_checked=1069,original_components_checked=3167,old_renderer_changes=28,new_visual_children=1,pin_slots_preserved=slots,original_review_prefab_unchanged=original_preserved,callback_test=runtime,limitations=limitations)
(out/"unity-validation.json").write_text(json.dumps(validation,indent=2))
manifest=dict(common,status="NEEDS_REVIEW",validation_status="PASS",visual_approval=False,source_path=str(source/"PinLauncher.blend").replace("\\","/"),scene_path=facts["scene_path"],materials_path=str(out/"Materials").replace("\\","/"),limitations=limitations)
(out/"review_manifest.json").write_text(json.dumps(manifest,indent=2))
aim=placement["aimDirection"];horizontal=math.hypot(aim["x"],aim["z"])
integration=dict(validation,integration_status="INTEGRATED_NEEDS_VISUAL_REVIEW",placement=placement,horizontal_aim={"x":aim["x"]/horizontal,"y":0,"z":aim["z"]/horizontal},height_world=placement["boundsMax"]["y"]-placement["boundsMin"]["y"],desk_surface_y=.815,handwheel_visibility="Partial; far-side placement prevents full open-side presentation",preserved_references="All original component object-reference identities match baseline, except one appended Transform child reference on the launcher root.",preservation_evidence=str(p/"preservation-comparison.json"),after_snapshot=str(p/"preservation-after.json"),scene_saved_only=facts["scene_path"],original_prefab_sha256=sha(original),screenshot_production_path=str(p/"after-game-camera.png"),screenshot_production_sha256=sha(p/"after-game-camera.png"))
(p/"integration-result.json").write_text(json.dumps(integration,indent=2))
report=f"""# Pin launcher visual rework

Integrated; needs human visual review. Executed Unity validation: PASS.

- Visual prefab: `{common['prefab_path']}`
- Canonical source: `{source}/PinLauncher.blend` and same-source `PinLauncher.fbx`.
- Eight new reusable URP/Lit materials in `{out}/Materials`; canonical wood/brass/steel BaseColor, MetallicSmoothness and Normal maps bound. sRGB/non-color/NormalMap settings verified.
- 168 nonempty meshes, 168 renderers, 73,960 imported triangles, no missing materials. Supplied Blender total: 74,472 triangles.
- Wrapper position (3.05, 0.815, 0.60), uniform scale 3.5, world yaw 146.7848 degrees. Height 1.007678. Lowest geometry y=0.815.
- Muzzle-to-breech aim measured; horizontal aim approximately (-0.5478, 0, 0.8366). Upward elevation approximately 37 degrees.
- FBX child retains imported rotation (270.0198, 0, 0); bakeAxisConversion=False validated upright in a public temporary additive scene, then restored original active scene and closed temporary scene.
- Original gameplay root, scripts, colliders, old child transforms, incoming references, original review prefab and unrelated scene state preserved. Old visual objects retained with 28 previously enabled Renderers disabled.
- Compared 1,069 original objects / 3,167 original components and all original serialized object references. Only appended launcher child reference and old Renderer.enabled changes allowed. Twelve coffer gameplay pin slots preserved.
- FirePin callback exercised in Play Mode through a helper compiled beforehand; expected hint verified. No physical mouse-click test.
- Final 1920x1080 Game camera: [preview.png](preview.png). Camera/canvas/RectTransform values restored in finally and checked exactly. Final scene clean; one scene open; Editor stopped, idle, no Console errors or warnings returned. Console never cleared.
- Saved only authorized DetectiveOffice scene and new assets. Original prefab matches its backup byte-for-byte. No trackers, queue, gameplay scripts, original materials, security settings, or unrelated assets edited.

## Review limitations

"""+"\n".join("- "+s for s in limitations)+f"""

## Evidence

- `unity-validation.json`, `review_manifest.json`
- `ProductionLogs/PinLauncherRework/integration-result.json`
- `ProductionLogs/PinLauncherRework/preservation-after.json`, `preservation-comparison.json`
- `ProductionLogs/PinLauncherRework/runtime-callback.json`
- Existing backup: `Assets/DetectiveDesk/Backups/PinLauncherRework_20261031_212158_817/`

FBX SHA-256: `{common['source_sha256']}`

Screenshot SHA-256: `{common['screenshot_sha256']}`
"""
(out/"asset-report.md").write_text(report,encoding="utf-8")
print(json.dumps({"validation":"PASS","review":"NEEDS_REVIEW","original_prefab_unchanged":original_preserved,"slots":len(slots),**common},indent=2))
