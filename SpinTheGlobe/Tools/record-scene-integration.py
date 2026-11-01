"""Record executed visual integration evidence; never promote to human-approved Complete."""
from pathlib import Path
import importlib.util, json, shutil, datetime
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('dd_queue', ROOT/'Tools/AssetQueue/queue.py')
q=importlib.util.module_from_spec(spec);spec.loader.exec_module(q)
report=ROOT/'ProductionLogs/SceneIntegration/IntegrationReport.md'
manifest=json.loads((report.parent/'GeneratedAssetManifest.json').read_text())
deferred={'MechanicalAnim','InteractionAnim'}
applied=[x for x in manifest if x['slug'] not in deferred]
text='''# DetectiveOffice generated asset integration

Scene: `Assets/DetectiveDesk/Generated/DetectiveOffice.unity`

Backup: `Assets/DetectiveDesk/Backups/SceneIntegration_20261031_013748/`
contains the original scene and copies of DetectivePinCoffer and CollectiblePin prefabs.

Generated visuals were instantiated under the existing environment, decorations or
interaction roots. Original placeholder renderers were disabled, not deleted.
New visual colliders are disabled; original interaction colliders remain unchanged.
Detailed globe, coffer, pin and imported SOLVED stamp geometry were preserved.
No gameplay source script or original production prefab was edited.

Desk top and structure, paneled back wall, molding, map/frame, window frame,
floor, toy launcher, files, hammer, glass, bottle, banker lamp and two statue
variants with two display bases are now placed in the existing scene.
Other bust/base variants remain available in their source prefabs, without adding
unrequested gameplay slots. Existing rainy glass/effect is retained. City backdrop
material is assigned behind it, so its visibility is limited by the existing glass.

Materials: mahogany and aged brass on suitable generated surfaces; burgundy
velvet on the existing coffer; Kintsugi on the archivist bust. FabricLeatherPBR's
additional swatches remain reusable assets. UI feedback ColorTint values were
copied to the four existing Buttons; layout, text, callbacks and panels remain.

Scene-only material variants:
- `Assets/DetectiveDesk/Materials/SceneIntegration/WorldMap_SceneUV.mat`
  corrects the original cube face's reversed UVs without changing the texture asset.
- `Assets/DetectiveDesk/Materials/SceneIntegration/Mahogany_DeskScene.mat`
  warms and darkens the generated PBR texture, reduces smoothness/normal strength.

Scene instance fitting corrected imported root axes for lamp and busts, and the
old baked display bases. Window height and desk depth use instance scale overrides
to fit the existing room. No source geometry was rebuilt or Blender files modified.

Executed validation:
- Unity MCP integration commands compiled and executed successfully.
- Game-camera before/after captures inspected, including an orientation correction pass.
- Zero missing materials or unsupported/error shaders on enabled scene renderers.
- Serialized audit: original Camera and all 50 original colliders unchanged.
- Camera transform and 12 PinSlot transforms verified unchanged in memory.
- The only changed original MonoBehaviour records are four Unity UI Buttons
  (ColorTint feedback); original gameplay MonoBehaviour records remain unchanged.
- Entered Play Mode, observed no Console errors/warnings, then returned to Edit Mode.
  This is a smoke test, not an exhaustive mouse-interaction test.
- Scene saved; original asset prefabs retained, new model instances keep prefab links.

Before: `../Artifacts/DetectiveDesk/BeforeAssetIntegration.png` (relative to project parent).
After: `../Artifacts/DetectiveDesk/AfterAssetIntegration.png` (relative to project parent).

Deferred: MechanicalAnim and InteractionAnim were not wired into live gameplay.
Their demo rig paths do not directly match all live interaction targets. Existing
globe animation/gameplay stays active; pickup/lid clips should be adapted and
trigger-tested separately rather than automatically animating static props.
No animation boards, swatch galleries or duplicate coffer were added.
Assets remain Needs Review; this integration does not constitute human visual approval.

## Source output directories used
'''
for x in applied:text+=f"- {x['slug']}: `{x['output']}`\n"
report.write_text(text,encoding='utf-8')
with q.Lock():
 state=q.read(q.QUEUE)
 if state.get('active_worker'):raise SystemExit('Queue worker active: do not update tracker')
 checkpoint=report.parent/'TrackerCheckpoint'
 checkpoint.mkdir(exist_ok=True)
 for name in ['asset_queue.json','Asset_Tracker.xlsx','asset_registry.json','Asset_Tracker.csv']:
  p=ROOT/name
  if p.exists() and not (checkpoint/name).exists():shutil.copy2(p,checkpoint/name)
 for j in state['assets']:
  if j['slug'] in {x['slug'] for x in applied}:
   j['scene_integration']={'scene':'Assets/DetectiveDesk/Generated/DetectiveOffice.unity','report':report.relative_to(ROOT).as_posix(),'screenshot':'../Artifacts/DetectiveDesk/AfterAssetIntegration.png','at':q.now()}
   note=' Applied to DetectiveOffice scene for visual review; original gameplay/camera/colliders preserved. See ProductionLogs/SceneIntegration/IntegrationReport.md. Animation wiring is separate.'
   if note.strip() not in j['notes']:j['notes']+=note
   j['last_updated']=q.now()
   j['history'].append({'at':q.now(),'status':j['status'],'event':'scene_visual_integration','report':report.relative_to(ROOT).as_posix()})
 q.save(state)
 print(f'Recorded {len(applied)} generated asset groups; queue/Excel/registry/CSV synchronized; statuses preserved.')
