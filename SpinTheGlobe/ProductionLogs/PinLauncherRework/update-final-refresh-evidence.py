import json, hashlib
from pathlib import Path
from datetime import datetime, timezone

p = Path("ProductionLogs/PinLauncherRework")
out = Path("Assets/DetectiveDesk/PinLauncherRework")
src = Path("Assets/DetectiveDesk/ArtSource/Gameplay/PinLauncher")
def read(f): return json.loads(f.read_text(encoding="utf-8-sig"))
def sha(f): return hashlib.sha256(f.read_bytes()).hexdigest()
old = read(p/"FinalRefreshHistory/integration-result.json")
checks = read(p/"final-refresh-executed-checks.json")
preserve = read(p/"preservation-comparison.json")
source = read(out/"source-validation.json")
assert preserve["protected_scene_unchanged"] and not preserve["errors"]
assert source["triangles"] == 73960 and source["zero_area_triangles"] == 0
assert sha(out/"preview.png") == sha(p/"after-game-camera.png")
original = Path("Assets/DetectiveDesk/AssetProduction/PinLauncher/20261030_190710_5307019b/PinLauncher_Review.prefab")
assert sha(original) == old["original_prefab_sha256"]
factslog = checks["facts"]["structuredContent"]["data"]["executionLogs"]
extra = checks["additional"]["structuredContent"]["data"]["executionLogs"]
assert "triangles=73960 zero=0 materials=8" in factslog and "MISMATCH" not in factslog
assert "prefab authored child transform mismatches=0" in extra
assert "new visual count=1 sceneCount=1 dirty=False compiling=False updating=False playing=False" in extra
result = dict(old)
result.update(source_sha256=sha(src/"PinLauncher.fbx"), screenshot_sha256=sha(out/"preview.png"),
    screenshot_production_sha256=sha(p/"after-game-camera.png"),
    timestamp_utc=datetime.now(timezone.utc).isoformat(), console_errors=0, console_warnings=0,
    handwheel_visibility="Near-side wheel clearly visible and readable from unchanged fixed Game camera",
    final_refresh_status="PASS", prefab_child_transform_mismatches=0, source_transform_overrides_removed=0,
    scene_or_prefab_saved_this_refresh=False, zero_area_triangles=0, missing_texture_references=0,
    final_refresh_evidence=str(p/"final-refresh-executed-checks.json").replace("\\","/"))
result["facts"] = dict(old["facts"], triangles=73960, zero_area_triangles=0,
    missing_texture_references=0, authored_child_transforms_match_source=True, console_warnings=0)
result["placement"] = dict(old["placement"], handwheel={"x":2.851582,"y":1.347000,"z":0.436616},
    reduction_gear={"x":2.956229,"y":1.224500,"z":0.295959},
    scale_zero={"x":2.801873,"y":1.347000,"z":0.646711})
result["final_transforms"] = [line.removeprefix("[Log] ") for line in factslog.splitlines() if "localPosition=" in line]
result["callback_test_reused"] = True
result["resolution_history"] = [
    {"issue":old["limitations"][0], "resolution":"Canonical source moved handwheel/reduction/scale to player-facing side. Forced only canonical FBX reimport; scene and prefab authored child transforms match source without override removal. Fixed-camera final render visually inspected; near-side wheel readable."},
    {"issue":old["limitations"][1], "resolution":"Parent welded lathe poles and removed zero-area cap triangles in canonical generator. Supplied independent Blender/FBX source validation and executed Unity mesh inspection now agree: 168 meshes, exactly 73,960 triangles, zero zero-area triangles."},
    {"history":old["limitations"][5:8]},
    {"refresh_helper_note":"An initial transient relay verification command failed compilation because Mesh resolved to Unity.AI.Mesh. Corrected to UnityEngine.Mesh; rerun succeeded. No project script was changed; dedicated final Console query returned zero warnings/errors."}
]
result["limitations"] = [
    "Human aesthetic approval remains pending; parent owns tracker and queue updates.",
    "Previous Play Mode FirePin callback/action-binding PASS reused; no behavior changed. Physical mouse clicks and hover were not tested.",
    "No new projectile gameplay was added.",
    "No exact mesh intersection test was performed; final camera render shows no obvious overlap with neighboring desk props.",
    "Blender generation and embedded-source/packed-map validation are supplied parent evidence; this refresh did not run Blender."
]
result["final_refresh_actions"] = [
    "Read AGENTS.md and Unity_GetUserGuidelines; used Unity MCP relay as sole writer.",
    "Force reimported only canonical PinLauncher.fbx; retained nested instance, hierarchy, GUIDs, materials, placement and references.",
    "Verified scene and prefab authored child transforms match canonical source. Removed no overrides and inserted no objects.",
    "Verified all eight supported URP/Lit materials, nine assigned maps and importer settings; five constant-surface materials intentionally have no maps. No dangling serialized texture references.",
    "Captured 1920x1080 fixed Game camera to both requested paths; camera/canvas/RectTransform properties restored exactly.",
    "Compared complete original baseline: 1069 objects, 3167 components; only parent child-list addition and 28 Renderer.enabled changes.",
    "Dedicated Unity_ReadConsole final query returned zero warnings/errors; no global Console clear, Play Mode, Blender, tracker or material edits.",
    "Scene and prefab stayed clean, so no scene/prefab save was needed."
]
(p/"integration-result.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
validation = dict(result)
validation["scope"] = "Executed final scoped source refresh, fixed-camera capture and preservation checks; human visual approval pending"
(out/"unity-validation.json").write_text(json.dumps(validation,indent=2),encoding="utf-8")
report = f"""# Pin launcher visual rework

Final scoped Unity refresh: PASS. Human aesthetic approval pending.

- Canonical FBX reimported into the existing nested instance; no duplicate launcher, transform override removal, scene save or prefab save was needed.
- 168 meshes / 168 renderers, exactly 73,960 Unity triangles and zero zero-area triangles. Independent parent source validation agrees.
- Eight existing supported URP/Lit materials reused unchanged. Nine assigned wood/brass/steel maps load with correct import settings; five constant-surface materials intentionally need no maps. No missing material or dangling texture references.
- Handwheel is now visibly on the near side. Barrel still points up/back-left toward the map from the unchanged Game camera.
- Wrapper remains (3.05, 0.815, 0.60), yaw 146.7848 degrees, uniform scale 3.5. Nested FBX rotation remains (270.0198, 0, 0). Handwheel world position: (2.851582, 1.347000, 0.436616).
- All authored prefab/scene child transforms match the canonical FBX. Hierarchy, nested source reference, positive scales, mount, camera, colliders, scripts and existing material references retained.
- Complete original baseline comparison passed: 1,069 original objects / 3,167 components and references preserved, with only the authorized child-list addition and 28 old Renderer.enabled changes. Twelve coffer slots preserved.
- Final 1920x1080 [Game camera render](preview.png) inspected visually. Camera, canvases and affected RectTransforms restored exactly. One clean scene, Editor stopped and idle; dedicated Console query returned zero warnings/errors. Console never cleared.
- Previous FirePin callback PASS retained. No new Play Mode test or physical mouse-click test claimed.

## Resolved issues and history

- Far-side handwheel: parent corrected the canonical generator and rebuilt/promoted its source. The final Unity refresh inherited those changes without override removal; the large near-side wheel is readable in the fixed-camera render.
- Triangle mismatch: parent welded lathe poles and removed zero-area cap triangles. Blender source, FBX reimport and this executed Unity inspection now agree at 73,960 triangles, zero zero-area triangles and 168 meshes.
- Prior complete integration report, validation, preservation output and screenshot are retained in ProductionLogs/PinLauncherRework/FinalRefreshHistory/. Invalid two-byte writer snapshots were not used.
- Earlier callback-registration and capture-warning history is retained in integration-result.json. This capture restored the target before disposal and the final Console query is clean.
- A transient verification command initially failed because of a Mesh namespace ambiguity; its corrected UnityEngine.Mesh command succeeded. No project script changed.

## Remaining limits

"""+ "\n".join("- "+x for x in result["limitations"]) + f"""

## Evidence

- ProductionLogs/PinLauncherRework/integration-result.json
- ProductionLogs/PinLauncherRework/final-refresh-executed-checks.json
- ProductionLogs/PinLauncherRework/preservation-after.json and preservation-comparison.json
- ProductionLogs/PinLauncherRework/scene-components-before.json (complete original baseline)
- source-validation.json and blender-validation.json (parent-executed Blender evidence)
- unity-validation.json

Canonical source: Assets/DetectiveDesk/ArtSource/Gameplay/PinLauncher/PinLauncher.fbx

Existing prefab: Assets/DetectiveDesk/PinLauncherRework/DetectivePinLauncher_Visual.prefab

FBX SHA-256: {result["source_sha256"]}

Screenshot SHA-256 (both requested copies): {result["screenshot_sha256"]}
"""
(out/"asset-report.md").write_text(report,encoding="utf-8")
print(json.dumps({k:result[k] for k in ["final_refresh_status","source_sha256","screenshot_sha256","protected_scene_unchanged","zero_area_triangles"]},indent=2))
