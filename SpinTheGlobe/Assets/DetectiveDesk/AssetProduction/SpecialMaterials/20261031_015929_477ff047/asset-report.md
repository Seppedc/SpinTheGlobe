# DD-38 Specialty materials — Needs Review

Run: 20261031_015929_477ff047. Owner: bounded Codex asset worker. Review date: 2026-10-31. No human approval or completion claimed. Queue and tracker files were not modified.

## Provenance and implementation

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, the Blender skill, DD-38 registry/queue and workbook entry, IMPLEMENTATION_BRIEF.md and References/REFERENCE_BRIEF.md. The reference directory contains only its written brief; no reference images or existing generator were present. No dependencies are declared. No downloads or external content were used.

This is a Unity material deliverable, so no Blender placeholder, Blender execution or physical-model validation applies. Created five new URP/Lit materials and deterministic 512x512 RGBA base-color maps in this output directory. The executed Unity authoring command is preserved at Assets/DetectiveDesk/ArtSource/Materials/SpecialMaterials/AuthorSpecialMaterials.cs.txt. It is a source snapshot for a fresh output, not an overwrite-safe rebuild command.

| Material | Appearance | Smoothness | Surface |
|---|---|---|---|
| Parchment | Warm ochre mottling | 0.12 | Opaque |
| Enamel | Deep banker green | 0.87 | Opaque |
| Crystal | Pale cool blue | 0.97 | Alpha blend, alpha 0.28 |
| Whiskey | Amber brown | 0.94 | Alpha blend, alpha 0.72 |
| Stone | Cool gray mottling | 0.22 | Opaque |

All use metallic 0, sRGB Repeat base-color textures and mipmaps. Texture alpha drives the two transparent materials. The remaining surfaces have opaque alpha. No existing production materials were edited.

## Executed validation

- Unity relay guidelines and AssetQueueReview.Preflight succeeded.
- Coordinator before-import writer guard returned PASS before authoring.
- Unity command compiled and executed successfully. It checked supported Universal Render Pipeline/Lit and actual imported dimensions of all five maps: 512x512 each.
- Standalone MaterialSwatches.prefab authored in a new empty additive scene using public APIs. Original active scene and its dirty state restored in finally; dirty state equality checked. Tool localFixedCode inspected; no internal scene APIs were substituted.
- MaterialSwatches.prefab contains five named preview-only built-in spheres, five renderers and no colliders or runtime scripts. Each sphere uses its corresponding new material. These are display swatches, not production geometry.
- AssetQueueReview.Review executed successfully after authoring. unity-validation.json records PASS, five meshes/renderers, zero missing materials, zero Console errors, and protected_scene_unchanged=true, plus source and screenshot SHA256 values.
- Dedicated Unity_ReadConsole before and after Review returned zero Error/Warning entries. Console was not cleared.
- Inspected preview.png (1200x900): all five swatches visible, no pink/missing surfaces. Left-to-right order: parchment, enamel, crystal, whiskey, stone. Distinct roughness/highlight and color differences are visible.
- Confirmed output materials, textures, manifest, prefabs, screenshot and validation files exist and are nonempty. All subprocesses finished.

## Dimensions and pivots

Materials have no physical dimensions or pivots. Preview spheres use Unity's built-in unit sphere at scale one, centered local pivots, x positions -2.5, -1.25, 0, 1.25, 2.5 meters. The display root is at the origin; these values describe the preview only. No gameplay placement or camera was changed. Review camera and lighting were created by the required isolated helper.

## Limitations and remaining review

Needs human visual review. These are initial authored material variants, not finished hero surfaces. Parchment and stone currently use broad color variation without normal, height or roughness maps; close-up fibers, pores and convincing fine relief remain future work. Large-scale noise is periodic, but the fine noise is not proven seamless; tiling seams require inspection. Crystal and whiskey use standard URP alpha transparency: no refraction, absorption, thickness, caustics, liquid volume, meniscus or physical fluid behavior was implemented or validated. Transparent appearance and sorting require review on intended production meshes with their background and lighting; the sphere preview alone does not establish optical fidelity. No gameplay integration was performed. No global asset save, Play Mode, gameplay scene save, or tracker changes were used.

Evidence: review_manifest.json, unity-preflight.json, unity-validation.json, protected-scene-before.txt, MaterialSwatches.prefab, SpecialMaterials_Review.prefab and [preview.png](preview.png), all beside this report. The helper owns the separate isolated Assets/DetectiveDesk/AssetReview/AssetReview.unity review scene.
