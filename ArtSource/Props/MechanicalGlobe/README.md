# Mechanical globe — reference rework

This source reuses the original detailed generator found at `../Artifacts/BlenderGlobe/build_mechanical_globe.py`, preserved here as `legacy_globe.py`. The original external generator/model and old Unity assets remain intact. `build_asset.py` now routes to `reference_globe.py`; the generic starter is backed up under `ProductionLogs/GlobeRework/Checkpoint`.

Reference: `Inspiration/SceneAndValidatedAssets/SceneApproved.png`. The rework provides clearer high-contrast cartography, vector-derived raised coastlines, a broad graduated brass left semicircle meridian, pole bearings/finial, a wider stepped pedestal, and a curved RPM-gauge support. The pedestal's lower molding reaches the desk without moving the established sphere/root/gear/gauge pivots.

Canonical files: `MechanicalGlobe.blend`, `MechanicalGlobe.fbx`, `Textures/`, `build_asset.py`, `legacy_globe.py` and `reference_globe.py`. All three exact Python sources are embedded in Blender, and all 24 used image maps are packed with relative paths. The 2048x1024 world texture and geographic plates derive from the original embedded Natural Earth land data. Current source geometry: 51 meshes, 235,110 triangles, zero zero-area triangles, all meshes with UVs/materials.

Build and validate in a NEW staging folder outside Assets, then promote only after both Blender processes exit:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-one.ps1 -Slug MechanicalGlobe -OutputDirectory ProductionLogs/GlobeRework/YourNewBuild
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background ProductionLogs/GlobeRework/YourNewBuild/MechanicalGlobe.blend --python-exit-code 1 --python Tools/validate-globe-rework.py
python Tools/promote-globe-rework.py ProductionLogs/GlobeRework/YourNewBuild
```

Unity uses NEW native meshes and six modular visual prefabs under `Assets/DetectiveDesk/GlobeRework/`: Static, Sphere, three retained gear branches (all exposed gear renderers disabled), Needle. These preserve the original `RotatingSphere`, three driven gear pivots, RPM needle, gameplay-root prefab, colliders, controller and incoming references. New sphere/gear/needle visuals attach beneath their original targets; no second rotation controller exists. Old renderers remain disabled for rollback. Original assets under `Assets/DetectiveDesk/GlobeBlender/` must not be overwritten.

FBX edits require a reviewed refresh of these native mesh/module assets. Reimporting FBX alone does not rebuild them. The scoped Editor `GlobeIntegration.Build()` is INITIAL authoring only and refuses a scene already containing the new modules; do not create duplicate visual children. Future revisions must preserve existing native mesh/material/prefab GUIDs, reapply the verified import-axis conversion, and retain original gameplay targets. Inspect the helper and existing bindings before refreshing.

Nine new URP/Lit materials preserve the PBR maps. DarkShell/EngravedLand deliberately suppress specular/environment reflections and use the same map with low neutral emission (.06) for Game-camera readability. Their smoothness multiplier is .6. Brass/coastlines retain reflections. This reviewed Unity preset is also recorded in the scoped Editor helper so it can be reproduced for a fresh import. Do not brighten global lighting or replace the actual cartography with a scene-reference image.

Runtime observation passed for sphere speed, all three gear ratios, RPM needle/calibration, existing InspectGlobe callback and HUD. Camera, new pin cannon, coffer slots and unrelated original scene components passed preservation checks. Physical mouse input and exhaustive performance tests were not run. Human visual approval remains pending.

Evidence and screenshots: `Assets/DetectiveDesk/GlobeRework/`. Original scene and three prefab copies: `Assets/DetectiveDesk/Backups/GlobeRework_20261031_225517/`. Full source/runtime/preservation history: `ProductionLogs/GlobeRework/`.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.

## Applied holder revision

The requested holder revision is installed in DetectiveOffice. Only the player-left 180-degree holder remains; exposed gear meshes are absent from source and old integrated gear renderers remain disabled for rollback. The original-size RPM gauge center is now (-1.23,1.18,-.80) in globe-root Unity coordinates, with a compact base support. Its original gameplay needle target moved once by (-.74,-.25,-.05); identity, calibration and bindings remain intact. Sphere geometry, all thirteen sphere native assets, nine URP materials and eight runtime scripts are byte-identical to the approved prior revision.

The existing Static and three gear prefabs were updated; existing native mesh GUIDs are preserved. GlobeHolderRefresh.cs is a scoped single-use integration helper; inspect current state before any future refresh and never apply the gauge delta twice. A Unity rendering-buffer issue after CopySerialized was resolved by notifying exact existing vertex/index data without clearing or reconstructing the mesh, with all buffers/channels verified unchanged. Future mesh updates must verify actual Game rendering, not only CPU bounds.

Current executed source, runtime, preservation and Console evidence and final screenshots: Assets/DetectiveDesk/GlobeHolderRevision/. Current Console read: zero warnings/errors. Sphere rotation, original needle calibration and InspectGlobe HUD callback passed runtime checks. All 4324 pre-existing components passed preservation except the expressly authorized renderer flags, needle position and hover feedback list. Prior source, prefab, native asset and scene checkpoints remain under ProductionLogs/GlobeHolderRevision/. The whole revised asset remains Needs Review; the user approved only the sphere/colors before this holder revision.
