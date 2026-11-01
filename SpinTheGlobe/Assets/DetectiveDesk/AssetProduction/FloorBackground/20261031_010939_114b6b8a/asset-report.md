# DD-09 Floor / background — asset report

Result: generated and validated; Needs Review, no human aesthetic approval.
Review date: 2026-10-31. Owner: bounded Codex asset worker.

## Provenance and changes
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets skill, registry DD-09 entry and Excel row (read via standard-library XLSX XML), README, REFERENCE_BRIEF and existing build_asset.py. References contains only the written noir/mahogany art brief; no reference images or third-party textures were supplied. The original generator delegated to a shared generic slab. Updated only this asset's build_asset.py to a self-contained deterministic generator, without changing shared helpers.

Created shallow floor substrate, fourteen staggered plank rows, recessed joints, beveled board edges, perimeter border and six subtle wood tones. No wall, desk, gameplay placement, scripts or production material edits. No moving parts are needed.

Source: Assets/DetectiveDesk/ArtSource/Environment/FloorBackground/build_asset.py.
Canonical editable generated model: FloorBackground.blend in this report's directory; external build_asset.py snapshot and exact embedded GENERATE_FloorBackground.py accompany it. FloorBackground.fbx is exported from the same source. Queue-specific staging takes precedence over the general source-folder output convention. FloorBackground.blend1 is the retained pre-encoding-fix Blender backup, not the canonical model.

## Executed validation
Unity relay guidelines loaded and AssetQueueReview.Preflight passed. Coordinator guards passed before each generation/validation invocation and before import. All Blender subprocesses exited before promotion/import. The first validator invocation caught a UTF-8 BOM mismatch; removed BOM from the generator, regenerated, and reran the unmodified validator successfully.

blender-validation.json: PASS, exact embedded/external source, 75 nonempty meshes, 8,004 evaluated triangles, eight named Principled materials, applied mesh rotation/scale, metric units, valid hierarchy. Dimensions: 5.0 x 3.1 x 0.08 meters. Bounds centered on XY; minimum Z=0. ROOT_FloorBackground at origin. Top surface Z=0.08 m; +Y rear and -Y seated camera direction in Blender. No external textures or helper dependencies.

Promotion helper passed and copied closed outputs to this isolated version folder. Unity AssetQueueReview.Review passed; unity-validation.json records 75 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true and output hashes. New URP materials and FloorBackground_Review.prefab are isolated here. Review scene: Assets/DetectiveDesk/AssetReview/AssetReview.unity, created/restored by the provided helper only.

Console inspected without clearing: zero errors; five pre-existing warnings concerning Input Manager, relay executable signature metadata, and a previous CityBackdrop manifest timestamp. No FloorBackground import errors reported. Blender logged a thumbnail-cache image-write warning and use_nodes deprecation warnings, but canonical save, FBX export, validation and preview render succeeded.

## Visual evidence and limitations
Inspected preview.png: coherent shallow rectangular floor, visible staggered board joints and perimeter border, no pink/missing materials. blender-preview.png is the executed validator render. Unity screenshot is an isolated review camera, not an in-game fit or approval test.

Materials are flat PBR colors, without grain/normal maps, scratches or photorealistic wear. Border miters and fastening details are not modeled. The preview looks relatively uniform and light under review illumination; final dark-mahogany appearance requires human review under intended lighting. Gameplay camera scale, desk clearance, floor elevation, wall junctions and composition remain untested because gameplay scenes were deliberately preserved. No runtime, collider or animation integration was requested or performed. Human visual review remains required; this report does not mark Complete. Queue/tracker files were not edited.
