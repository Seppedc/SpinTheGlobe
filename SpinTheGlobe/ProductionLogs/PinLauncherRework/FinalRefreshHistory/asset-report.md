# Pin launcher visual rework

Integrated; needs human visual review. Executed Unity validation: PASS.

- Visual prefab: `Assets/DetectiveDesk/PinLauncherRework/DetectivePinLauncher_Visual.prefab`
- Canonical source: `Assets\DetectiveDesk\ArtSource\Gameplay\PinLauncher/PinLauncher.blend` and same-source `PinLauncher.fbx`.
- Eight new reusable URP/Lit materials in `Assets\DetectiveDesk\PinLauncherRework/Materials`; canonical wood/brass/steel BaseColor, MetallicSmoothness and Normal maps bound. sRGB/non-color/NormalMap settings verified.
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

- Handwheel is on the far side at the required aim direction and is partly visible/occluded in the Game camera. Full open-side silhouette is not achieved. Canonical generator revision is needed; no geometry was mirrored.
- Unity imported triangles total 73960; supplied Blender validation totals 74472. Mesh count is 168 in both. The triangle discrepancy was observed but not resolved by source regeneration.
- FirePin callback and Launcher action/game binding were tested in Play Mode. Physical mouse input, hover, and projectile mechanics were not tested or added.
- No obvious overlap with coffer, lamp, stamp or files in final Game camera. No exact mesh intersection test was performed.
- Source/embedded Blender validation is supplied parent evidence; this writer did not rerun Blender.
- Initial dynamic callback registration did not survive the Play Mode domain reload. The successful test used the opt-in precompiled Editor helper.
- Two initial writer snapshots serialized as empty objects and are invalid evidence. Preservation uses the supplied full original baseline and corrected explicit after export.
- Initial capture reported a render-target release warning. Final capture explicitly restored the camera target before disposal and completed without that warning; Console was never cleared.
- Visual approval and tracker/queue updates remain parent responsibilities.

## Evidence

- `unity-validation.json`, `review_manifest.json`
- `ProductionLogs/PinLauncherRework/integration-result.json`
- `ProductionLogs/PinLauncherRework/preservation-after.json`, `preservation-comparison.json`
- `ProductionLogs/PinLauncherRework/runtime-callback.json`
- Existing backup: `Assets/DetectiveDesk/Backups/PinLauncherRework_20261031_212158_817/`

FBX SHA-256: `a8393110c693e7bd4aa09fd5003e9f5208d79c6d03b2074ca28e3794b04044c1`

Screenshot SHA-256: `cce8d0edcc3f6850809685132e83c3cb41d15b2d2998bb529e50b346d7a843e7`
