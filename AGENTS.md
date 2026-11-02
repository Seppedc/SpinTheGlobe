# Spin The Globe — rules for AI agents

Read first: `Docs/GAME_DESIGN.md` (what we build) and `Docs/ARCHITECTURE.md` (folders, assemblies, code rules).
The Unity project is `SpinTheGlobe/` (Unity 6000.4.7f1, URP). Main scene: `Assets/DetectiveDesk/Scenes/DetectiveOffice.unity`.

## Repository layout
- `ArtSource/<Category>/<Asset>/` — editable sources: the **latest** `.blend`, its `build_*.py` generator, helper scripts, source textures, `References/`, README / IMPLEMENTATION_BRIEF notes. Not imported by Unity.
- `ArtSource/_Inspiration/` — general reference images.
- `SpinTheGlobe/Assets/DetectiveDesk/Art/<Category>/<Asset>/{Models,Meshes,Materials,Textures,Prefabs}` — Unity-ready art.
- `SpinTheGlobe/Assets/DetectiveDesk/{Scripts,Tests,Data,Prefabs,Scenes}` — code, tests, ScriptableObject data, gameplay prefabs, scenes.
- `Tools/` — Blender build/export scripts. `Docs/Pipeline/BLENDER_ASSET_STANDARD.md` — modelling standard. `Docs/Production/Asset_Tracker.xlsx` — asset status list.

## Hard rules
1. **Git is the backup.** Never put backup copies of scenes, prefabs or models inside `Assets/`. Commit instead.
2. **Move/rename Unity assets with their `.meta`** (or inside the Unity Editor). Scene references are GUIDs in `.meta` files.
3. **Keep one `.blend` per asset — the latest.** Never delete the current `.blend` of an asset.
4. **Do not modify or save the open gameplay scene** unless the task asks for it. Preserve existing unsaved user edits.
5. **3D assets:** follow `.agents/skills/blender-unity-assets/SKILL.md` and `Docs/Pipeline/BLENDER_ASSET_STANDARD.md`. Run Blender through `Tools/build-blender-asset.ps1`; never claim a build that did not run.
6. **Code:** runtime code in `Scripts/Runtime/<Feature>/` (assembly `DetectiveDesk.Runtime`), editor code in `Scripts/Editor/` (`DetectiveDesk.Editor`), tests in `Tests/EditMode/`. All balance numbers live in ScriptableObjects under `Data/`. Upgrades are `StatModifier`s in `Data/SkillTree/SkillTree.asset`; skill node ids are stored in saves, never rename a shipped id. Run the EditMode tests after changes.
7. Report file paths changed, validations actually executed and known limitations.

## Protected bindings (keep working)
- **Pin coffer:** prefab `Art/Props/PinCoffer/Prefabs/DetectivePinCoffer.prefab` references 74 baked meshes in `Art/Props/PinCoffer/Meshes/`. Refresh them in place from `Art/Props/PinCoffer/Models/PinCoffer.fbx` with **Tools ▸ Detective Desk ▸ Blender Assets ▸ Refresh Pin Coffer From Blender**. Keep `Models/Pinbox_Original.blend` (existing URP materials are sub-assets of it), the 12 `PinCofferSlot` sockets, colliders, lid pivot and the 0.011 insert lift. Generator: `ArtSource/Props/PinCoffer/build_pincoffer.py` (see its README).
- **Collectible pin:** `Art/Props/CollectiblePin/Models/Pin.blend` is referenced directly by `Prefabs/Pins/CollectiblePin.prefab`; generator `ArtSource/Props/CollectiblePin/build_collectible_pin.py`. Do not replace it with the generic starter.
- **Globe:** the scene uses baked meshes in `Art/Props/MechanicalGlobe/Meshes/` (current) and `Meshes/V1`, `Meshes/Legacy` (still referenced). One `GameManager` and one `GlobeController` own rotation and payouts; forward rotation is negative Unity Y.
- Keep the PhysX physics backend, the eight country assets in `Data/Countries/` with their Natural Earth provenance, and the scene layout / front-facing camera.
- Starter `build_asset.py` generators are editable starting points, not finished hero assets.
