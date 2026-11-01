# WallMolding — DD-04 production attempt

Result: executed Blender and isolated Unity validation PASS; Needs Review, no human visual approval. Tracker and queue files were not edited.

## Provenance and changes

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, Blender asset skill, asset_registry.json DD-04, the spreadsheet DD-04 rows, job.json, source README, reference brief, existing generator and shared starter implementation. References contains only REFERENCE_BRIEF.md, no reference images. No external assets, downloads or textures were used.

Updated only the assigned source generator at Assets/DetectiveDesk/ArtSource/Environment/WallMolding/build_asset.py. The prior script delegated to generic shared geometry; this revision is self-contained and creates profiled crown trim, baseboard, chair rail with brass fillet and four independently named mitered panel frames. No shared helpers were changed. There are no moving parts.

## Files and executed validation

This report's directory contains WallMolding.blend (canonical editable generated model), WallMolding.fbx exported from that source, build_asset.py (exact source snapshot), blender-validation.json, blender-preview.png, review_manifest.json, unity-validation.json, preview.png, isolated WallMolding_Review.prefab and new URP materials. The source is embedded exactly as GENERATE_WallMolding.py; it has no helper dependencies or external textures.

Coordinator guards passed before-blender, before-validation and before-import. Tools/build-one.ps1 executed local Blender 5.2.3 in factory-startup background mode into ProductionLogs/AssetQueue/BuildStaging/20261030_172045_0446ff27/WallMolding. The process exited successfully before the separate validate_blender.py invocation. Validation/render also exited successfully before promote.py copied outputs into Assets. Both Blender invocations are finished.

Blender validator: PASS, 8 nonempty mesh objects, 2,448 evaluated triangles, unit mesh scales, metric units, exact embedded/external generator, material assignment to named Principled materials WM_Mahogany and WM_AgedBrass. Dimensions 4.4000001 x 0.1600000 x 2.7000000 m. Bounds X +/-2.2, Y +/-0.08, Z 0..2.7 m. ROOT_WallMolding is at origin; front is -Y, up is +Z in Blender. FBX uses -Z forward/Y up and unit conversion. Static section geometry retains stable names and is parented beneath the root. No animation pivots are required.

Unity relay MCP guidelines and queue preflight executed successfully. AssetQueueReview.Review executed successfully and returned PASS: 8 meshes/renderers, zero missing materials, zero Console errors, matching FBX hash, saved isolated prefab and screenshot. protected_scene_unchanged=true. A separate Unity_GetConsoleLogs read returned zero errors and zero warnings. No Console clear, Play Mode or gameplay scene edits were used.

## Visual inspection and limitations

Inspected both blender-preview.png and preview.png. Crown, baseboard, chair rail and four open panel frames are visible; no pink or missing materials are visible. The Unity preview is darker/flatter than the Blender render and the fine crown contour is less readable at this framing. Human review of silhouette, color and seated camera legibility remains necessary. This is an initial procedural architectural trim asset, not approved hero-quality artwork.

No wood grain, baked normal/roughness textures, UV authoring, decorative carving, end returns or corner modules were created. Solid PBR colors are provisional. BackWall is unavailable, so wall fit, intersection clearance, panel placement against openings and gameplay camera composition have not been validated. Wall backing is intentionally absent; no production scene or existing prefab received this model. Architectural dimensions are design assumptions pending integration review.

Blender generation emitted a thumbnail-cache write failure for an automatic .thumbnails image, plus use_nodes deprecation warnings. Required blend, FBX and both explicit preview images were nevertheless written and validated; the optional cache thumbnail is not evidence. Unity reported no warnings or errors.

Screenshot: preview.png in this directory; Blender comparison: blender-preview.png. Remaining work is human visual review and later authorized fitting against BackWall. No Complete status or dependency satisfaction is claimed.
