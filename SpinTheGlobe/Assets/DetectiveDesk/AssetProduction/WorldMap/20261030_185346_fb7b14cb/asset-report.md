# WorldMap — DD-05

Result: isolated texture/material deliverable validated; needs human visual review. No completion or human approval claimed.

## Provenance and changes

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, the Blender skill, WorldMap registry entry, workbook row DD-05, IMPLEMENTATION_BRIEF.md and References/REFERENCE_BRIEF.md. The reference folder contains no reference images or generator. This is a Texture / Unity job; no Blender mesh or placeholder was generated.

Inspected the existing project texture `Assets/DetectiveDesk/Generated/Refined/ParchmentWorldMap.png` visually. Reused its clean parchment artwork without altering the original. Its earlier authoring provenance and geographic dataset are not established by this job. No downloads, generated imagery or hidden photograph layers were introduced.

Created `WorldMap_Parchment.png` as a byte-identical isolated copy through Unity AssetDatabase, plus a new `WorldMap_Parchment.mat` using supported Universal Render Pipeline/Lit, white tint, metallic 0 and smoothness 0.08. Configured only the copied texture: sRGB, clamp, bilinear, mipmaps, uncompressed, maximum size 2048. No existing material, gameplay object, source generator, scene, prefab or tracker was edited by the worker.

## Executed validation

- Unity relay guidelines loaded and required preflight executed successfully.
- Coordinator before-import writer guard returned PASS.
- PNG source dimensions checked from file header: 2048 × 900. Unity imports at 2048 × 1024 under its current non-power-of-two scaling. Original file bytes and aspect information remain intact.
- Source and output PNG byte equality verified. SHA256: `5636ddce20ef37a8996e88d81ff32ef710ba74be91522b1b68769ede96331072`.
- New texture loaded successfully in Unity; URP/Lit shader availability and support checked.
- Required AssetQueueReview.Review executed successfully. `unity-validation.json` records PASS, one review mesh/renderer, zero missing materials, zero Console errors and protected_scene_unchanged=true.
- Read Unity Console after review: zero warning/error entries. Console was not cleared.
- Opened and visually inspected `preview.png`: visible parchment, coastlines, geographic grid, border and atlas typography; no clues, strings or pins.

## Dimensions, pivots and limitations

This deliverable is a texture/material, with no physical dimensions, moving parts or semantic model pivot. The helper's `WorldMap_Review.prefab` is an inspection artifact using a centered unit quad, not a production model. The helper saves its authorized isolated AssetReview scene and restores the original scene.

The review helper maps the wide image onto a square quad and uses angled lighting, so `preview.png` distorts the original aspect ratio and appears brighter than the source. Use the PNG's 2048:900 aspect ratio for future authorized integration; consider disabling non-power-of-two resizing on the copied importer during that integration. No helper modifications were made. The texture contains atlas title/footer text and no country labels or political borders. Antarctica is absent; geographic accuracy and final art direction still require human review. Reuse preserves the existing artwork rather than creating a newly authored map.

## Evidence and remaining review

Evidence alongside this report: `unity-preflight.json`, `protected-scene-before.txt`, `review_manifest.json`, `unity-validation.json`, `preview.png`, copied PNG, new production material, helper preview material and review prefab. Screenshot: `preview.png`.

Human review remains required for parchment tone, coastlines, typography, geographic coverage and target display size. Gameplay integration and scene placement are outside this job. No Blender validation is applicable to this non-3D deliverable. All invoked subprocesses finished.
