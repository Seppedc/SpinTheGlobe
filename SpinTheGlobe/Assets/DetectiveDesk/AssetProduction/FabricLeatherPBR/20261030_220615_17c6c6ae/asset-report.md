# FabricLeatherPBR — DD-37

Owner: Codex bounded queue worker. Review date: 2026-10-30. Needs Review; no human visual approval or completion claimed. Queue/tracker files unchanged.

## Provenance and source

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, Blender asset skill, registry DD-37, workbook row 42, IMPLEMENTATION_BRIEF.md and References/REFERENCE_BRIEF.md. Reference folder contains written noir art direction only, no images. No existing generator or detailed material source existed. New reproducible Unity authoring source: `Assets/DetectiveDesk/ArtSource/Materials/FabricLeatherPBR/build_materials.cs.txt`. Executed through Unity MCP RunCommand; localFixedCode only wrapped/formatted the command, retaining the public additive-scene APIs. No Blender invocation is applicable to this texture/material job.

## Deliverables

All deliverables below are relative to this report's folder. Four new URP/Lit materials: `Leather_Oxblood.mat`, `Leather_Espresso.mat`, `Velvet_Burgundy.mat`, `Velvet_BottleGreen.mat`. Six original procedural 512x512 PNGs: Leather/Velvet `_Albedo`, `_Normal`, `_MetallicSmoothness`. Leather uses periodic jittered cellular grain; velvet uses periodic fine weave. RGB metallic is zero; alpha stores smoothness. Albedo is sRGB; packed data is linear; normal maps use Unity normal-map import. Repeat wrap, mipmaps, uncompressed textures. No downloaded content or dependencies.

`SwatchSet.prefab` is a standalone four-sphere material comparison, with colliders removed. `review_manifest.json` feeds the required helper. `FabricLeatherPBR_Review.prefab` and `preview.png` were produced by AssetQueueReview in its isolated additive review scene.

## Executed validation

Unity guidelines loaded, preflight PASS, coordinator before-import guard PASS. Authoring compilation/execution PASS. Only new materials were saved individually; no SaveAssets or gameplay save. Temporary authoring scene restored/closed in finally. Required AssetQueueReview.Review executed successfully and restored the original scene.

`unity-validation.json`: PASS, four mesh filters/renderers, zero missing materials, zero Console errors, protected scene snapshot unchanged; source and screenshot SHA256 recorded. `material-validation.json` records executed assertions for all four supported URP/Lit shaders, all twelve texture assignments/resolutions, texture keywords and six texture import settings. Console inspection returned zero errors and four pre-existing warnings (Input Manager deprecation and relay executable signature collection warnings); Console was not cleared.

## Dimensions, pivots and visual assessment

These are UV materials without physical dimensions or gameplay pivots. Review-only spheres have diameter 1 Unity unit; swatch root is at origin, centers x=±0.6 and y=±0.65. Maps repeat three times across sphere UVs. Final target mesh UV density must determine production scale; no production objects were changed.

Inspected the actual 1200x900 `preview.png`: top row oxblood/espresso leather, bottom row burgundy/bottle-green velvet. Four visible swatches with softer matte velvet response and visible subtle leather highlight texture; no pink materials. Fine weave is subtle at this framing.

## Limitations and remaining review

URP/Lit velvet is a rough dielectric approximation, without dedicated cloth sheen, anisotropy or fiber scattering. No custom shader behavior is claimed. Maps are procedural, not measured scans; no seams, stitching, wear masks or hero surface approval. Close-up texture density, tiling, color and grazing-angle response need human review on intended meshes under final lighting. No gameplay integration, scene placement, existing material edits or Play Mode occurred. The helper's saved AssetReview scene is the only review-scene output outside this job folder, as authorized. All subprocesses completed.
