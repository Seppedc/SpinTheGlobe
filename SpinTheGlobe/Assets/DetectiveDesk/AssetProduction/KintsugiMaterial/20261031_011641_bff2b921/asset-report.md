# DD-21 Kintsugi material — Needs Review

Created 2026-10-31 by the bounded queue worker. No human approval or Complete status is implied. Queue and tracker files were not edited.

## Provenance and source
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, Blender asset skill, registry entry, workbook, implementation brief and References/REFERENCE_BRIEF.md. The reference folder contains no images and the source had no existing generator. No downloaded assets or dependencies were used. This is a Shader / Texture deliverable; Blender geometry is not applicable.

Reproducible Unity authoring snapshot: `Assets/DetectiveDesk/ArtSource/Props/KintsugiMaterial/Generate_Kintsugi.cs.txt`. Execute its CommandScript through Unity_RunCommand. It uses seed 21031, 48 warped periodic Voronoi sites, black stone grain and narrow gold infill. An initial seam interpolation error was corrected and generation rerun before review; final gold mask coverage is 58,757 / 1,048,576 pixels (5.60%).

## Outputs
All deliverables below are beside this report:

- `Kintsugi.mat`: standalone Universal Render Pipeline/Lit material with metallic gold and HDR emission.
- `StoneGold_BaseColor.png`, `GoldSeams_Emission.png`, `StoneGold_MetallicSmoothness.png`: 1024-square maps. Metallic is red, smoothness alpha; packed map imports linear, color/emission maps sRGB. Repeat wrap, uncompressed, mipmaps enabled by importer defaults.
- `Kintsugi_Review.prefab`: helper-created standard sphere, for review only.
- `review_manifest.json`, `material-validation.json`, `unity-preflight.json`, `unity-validation.json`, protected scene snapshot, and `preview.png`.

## Executed validation
Unity relay guidelines and required Preflight executed successfully. Coordinator before-import writer guard returned PASS. Unity authoring compiled and ran; all three textures imported and material references assigned. Required AssetQueueReview.Review executed separately and returned PASS. It captured the 1200x900 preview, recorded material and screenshot hashes, found one review mesh/renderer, zero missing materials and zero Console errors, and verified protected_scene_unchanged=true after restoring the original scene. Tool localFixedCode added only the tool namespace/formatting; no scene API substitution occurred.

Inspected preview.png: dark charcoal stone, warm thin gold polygon seams and visible surface variation; no pink material. Dimensions and pivots are not material properties; helper sphere is Unity's unit-diameter primitive at its default centered pivot. No production geometry, transforms, gameplay scene, prefab or existing materials were changed.

## Limitations and remaining review
CollectibleStatues dependency is unavailable; statue assignment, UV scale/seams, camera readability and final lighting remain untested. No gameplay integration was attempted. Uses standard URP/Lit with baked textures, not a custom runtime crack shader. Emission is enabled but helper post-processing is disabled, so bloom/halo behavior is not validated. No normal/displacement map or animated cracks. Repeat wrapping is configured; fine grain and varying seam width have not been validated for seamless tiling across production UV boundaries. Human review should assess crack regularity, gold brightness and stone response in the intended noir lighting before approval. Blender/FBX checks are not applicable to this non-3D job.
