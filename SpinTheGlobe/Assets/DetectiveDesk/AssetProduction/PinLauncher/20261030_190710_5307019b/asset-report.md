# DD-16 Toy pin launcher — Needs Review

## Provenance and scope
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets/SKILL.md, registry DD-16 and Excel sheet2 row21 (read via ZIP/XML; openpyxl unavailable), asset README, reference brief, existing build_asset.py and its shared starter implementation. Visually inspected References/SceneApproved.png. Its mahogany, aged brass and green accents informed this design; the pictured cannon silhouette was deliberately replaced with a broad open tabletop spring toy per the explicit non-firearm requirement. No downloaded textures or external dependencies.

Only the assigned asset generator was edited. The original 498-byte script merely delegated to a shared starter; it now contains self-contained deterministic asset-specific geometry. Source: Assets/DetectiveDesk/ArtSource/Gameplay/PinLauncher/build_asset.py. Editable canonical run source: PinLauncher.blend in this output directory, with exact external build_asset.py snapshot and embedded GENERATE_PinLauncher.py. No helper dependencies. Outputs first generated and validated outside Assets in the assigned BuildStaging directory, then promoted by the supplied helper after both Blender processes exited.

## Changes
Mahogany platform, brass plinth and rivets, green enamel deck, rubber feet, open twin-rail tray, continuous nine-turn steel compression spring, independent plunger rod/pusher/pull tab, power marks and toy star badge. Six named Principled materials; isolated review helper created new URP materials and PinLauncher_Review.prefab. No existing gameplay assets or materials were edited.

## Executed validation
Unity relay guidelines and Preflight succeeded. Coordinator guards passed before generation, validation and import. Blender 5.2.3 background generation and same-source FBX export exited successfully. First validation found a UTF-8 BOM mismatch in embedded versus external source; source encoding was corrected and the generator rerun, followed by a successful fresh validation. Blender emitted a thumbnail-cache write diagnostic and future material API deprecation warnings; canonical Blend/FBX saves and preview rendering succeeded.

blender-validation.json: PASS; 28 nonempty meshes, 39,456 evaluated triangles, six assigned Principled materials, unit mesh scales, metric units, root at origin, minimum Z zero, embedded generator exactly matches external snapshot. Bounds: X ±0.09 m, Y ±0.14 m, Z 0–0.0955 m. Overall 180 × 280 × 95.5 mm. Launch direction +Y. ROOT_PinLauncher is origin; Plunger_Translation_Pivot at (0,-0.012,0.079) m supports future Y translation. Spring deformation/animation is not implemented.

Unity review: PASS; 28 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Supplied review helper created and closed the isolated additive scene and retained the gameplay snapshot. Console inspected without clearing: four existing warnings concerning deprecated Input Manager and relay executable signature collection, no errors. No Play Mode or gameplay integration performed.

## Visual evidence and limitations
Inspected both blender-preview.png and Unity preview.png. Both show a coherent open-tray tabletop toy with visible spring and colored materials, no pink rendering. Unity's opposite viewing angle occludes the rear pull tab and pusher; Blender preview clearly shows them. The front star badge partly sits underneath the tray lip and needs art review. Current surfaces are clean flat PBR colors, not textured/weathered hero finishes. Geometry is a developed procedural concept requiring human visual approval, not Complete.

Screenshots: preview.png (Unity isolated review) and blender-preview.png (saved-source Blender preview). Output hashes and scene-preservation evidence: unity-validation.json, blender-validation.json and protected-scene-before.txt. Model: PinLauncher.blend and same-source PinLauncher.fbx; standalone review prefab: PinLauncher_Review.prefab.

CollectiblePin dependency is unavailable per job: socket clearance, actual pin dimensions, retention, launch physics and gameplay fit are unverified. No placeholder collectible was fabricated. Runtime integration, animation, collision and launch behavior remain outside this job. Human approval must assess silhouette, finish, scale, pin fit and camera readability before production placement. Queue and tracker files were not changed by this worker. All launched subprocesses exited before reporting.
