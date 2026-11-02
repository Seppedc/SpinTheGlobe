# Detective pin launcher — reference rework

Reference: `Inspiration/SceneAndValidatedAssets/SceneApproved.png`. The original spring-tray starter was reworked into the reference's miniature brass cannon, retaining the existing collectible-pin interaction.

Canonical source: `PinLauncher.blend`, with `build_asset.py` and `pinlauncher_detail.py` alongside it. The original build entry point calls the detailed asset-local helper. Both exact sources are embedded as `GENERATE_PinLauncher.py` and `pinlauncher_detail.py` in the saved file. All twelve exported PBR maps are also packed, with relative image paths.

The model has 168 independently named meshes, 73,960 triangles, zero zero-area triangles, UVs on every mesh and eight named materials. Moving pivots include yaw, elevation, recoil, plunger and handwheel; MuzzleTip/BreechOrigin define aim. Blender is Z-up with +Y forward; retain the FBX import rotation. The handwheel is authored on -X to face the existing seated Game camera at its mapward yaw.

From project root, build into a fresh staging directory outside Assets:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-one.ps1 -Slug PinLauncher -OutputDirectory ProductionLogs/PinLauncherRework/YourNewBuild
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background ProductionLogs/PinLauncherRework/YourNewBuild/PinLauncher.blend --python-exit-code 1 --python Tools/validate-pinlauncher-rework.py
python Tools/promote-pinlauncher-rework.py ProductionLogs/PinLauncherRework/YourNewBuild
```

Wait for each Blender process to exit before promotion/import. Promotion preserves Unity metadata identities and checkpoints prior blend/FBX files. Review the new render and validation before refreshing Unity. Keep source and helper changes together; never hand-edit imported geometry as the authoritative model.

Unity visual prefab: `Assets/DetectiveDesk/PinLauncherRework/DetectivePinLauncher_Visual.prefab`. Scene instance: `DetectiveOffice/GameplayObjects/ToyPinLauncher - aimed up at map/PinLauncher_ReworkedVisuals`. Original gameplay root, collider, scripts, camera and unrelated scene components are preserved. The old Generated_PinLauncher renderers remain disabled for rollback; do not enable both shooters.

Final report and screenshots: `Assets/DetectiveDesk/PinLauncherRework/`. Backups and full preservation/runtime callback evidence: `ProductionLogs/PinLauncherRework/`. Original scene/prefab backup: `Assets/DetectiveDesk/Backups/PinLauncherRework_20261031_212158_817/`.

The existing FirePin callback passed a Play Mode test. No new projectile mechanics were added; mouse input/hover and performance were not exhaustively tested. Original procedural PBR maps are not scanned surfaces. Human visual approval is pending.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.
