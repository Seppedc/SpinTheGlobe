# DD-25 Banker lamp — 2026-10-30

Result: technical validation passed; Needs Review, pending human visual approval. No tracker or queue changes made by this worker.

## Provenance and change
Inspected the existing detailed build_asset.py, README, reference brief, local NoirLighting.png, registry DD-25 and workbook rows before editing. The local image supplies art direction only; no downloaded assets or image geometry were used. Retained the spun brass base, curved stem, double-surface emerald shade and opal liner, rim hardware, bulb, beaded pull-chain and existing semantic names.

Only the assigned production generator was changed: FBX now uses apply_scale_options='FBX_SCALE_ALL' and bake_space_transform=False. This keeps unit conversion global and preserves nested pivot transforms. Geometry and material source definitions remain intact. No shared helper, existing prefab, material, runtime script or gameplay scene was edited.

## Files
Source: Assets/DetectiveDesk/ArtSource/Props/BankersLamp/build_asset.py.
This report's folder contains BankersLamp.blend (canonical editable generated model), BankersLamp.fbx exported from the same saved source, build_asset.py (exact snapshot), blender-validation.json, blender-preview.png, review_manifest.json, promotion.json if emitted by the helper, unity-preflight.json, unity-validation.json, import-inspection.txt, BankersLamp_Review.prefab, new Materials/, and preview.png.
Build staging: ProductionLogs/AssetQueue/BuildStaging/20261030_212422_3e4ba5f3/BankersLamp. Both isolated background Blender processes exited before promotion/import. Coordinator guards passed before-blender, before-validation and before-import.

## Executed checks
Blender 5.2.3 generation and provided validation/render completed with exit code zero. Exact embedded GENERATE_BankersLamp.py matches the external snapshot. Validation recorded 41 nonempty meshes, 20,744 triangles, applied mesh scales, four named Principled materials, metric units, root at origin and minimum Z=0. Dimensions: 0.276 x 0.1845 x 0.405 metres (Blender X/Y/Z).

Shade_Pivot remains an independent empty at Blender (0, -0.020, 0.361); Shade_GreenGlass uses pivot-local vertices. Imported Unity pivot and shade scales are both (1,1,1), replacing the previous 0.01 nested shade error. Read-only inspection records shade world bounds center (0,0.3665,0.023), extents (0.128,0.0385,0.077) metres, confirming correct vertical placement and width. See import-inspection.txt.

The prescribed Unity preflight and isolated AssetQueueReview.Review both executed successfully. Unity evidence records 41 renderers/meshes, zero missing materials, zero Console errors and protected_scene_unchanged=true. The helper created only isolated review assets, remapped new materials to URP/Lit, captured preview.png and restored the original gameplay scene. No Play Mode or global Console clear was used.

## Visual inspection and limitations
Inspected both blender-preview.png and preview.png. The Unity image is populated and shows the full green shade correctly above the brass base, with ivory underside and visible pull-chain; no pink material or displaced miniature shade is visible. Blender preview shows the same overall model.

This remains a procedural review asset, not a human-approved hero asset. Green glass is represented by an opaque coated green surface with an ivory lining, without validated glass transmission. Brass uses uniform material parameters rather than baked patina maps. Unity remapping changes the apparent brightness/gloss compared with Blender. The shade shell alone is parented under Shade_Pivot; rim hardware is separately named under the root, so a future complete shade animation needs coordinated parenting. No animation, switch behavior, emitted illumination or gameplay integration was executed or approved. Human review must assess silhouette, material finish and lighting fit against the noir reference.

Blender logged a nonfatal thumbnail cache write failure outside the generated model path and API deprecation warnings; saved model, FBX, source matching and rendered evidence all passed. No permission escalation or installation was attempted. The editable blend is intentionally in the isolated output directory under the queue-specific staging/promotion instructions, not written into a live production source binding.
