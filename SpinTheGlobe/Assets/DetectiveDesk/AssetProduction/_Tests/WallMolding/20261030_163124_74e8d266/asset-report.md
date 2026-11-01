# WallMolding starter smoke test — DD-04

Run: 20261030_163124_74e8d266. Result: smoke-test evidence PASS; Needs Review. This is not production completion or human approval.

## Provenance and scope
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, the blender-unity-assets skill, job.json, asset_registry.json and the WallMolding spreadsheet rows before generation. Read the existing build_asset.py, its Tools/blender_shared.py implementation and References/REFERENCE_BRIEF.md. The reference directory contains a text brief and no reference images. Brief: crown trim, baseboards and panel rails in the dark mahogany/aged brass noir setting. Existing registry maturity is generic starter tested in isolation; production progress remains Not Started. No production source, tracker, queue job, gameplay prefab, script or existing material was edited.

Executed the installed starter with Tools/build-one.ps1 -Slug WallMolding -OutputDirectory ProductionLogs/AssetQueue/BuildStaging/20261030_163124_74e8d266/WallMolding. Source snapshots build_asset.py and blender_shared.py accompany the saved WallMolding.blend and same-source WallMolding.fbx. Main source exact embedding was validated; the installed helper also embeds its snapshot as GENERATE_blender_shared.py. No new asset design was authored.

## Executed checks
Unity relay guidelines loaded; AssetQueueReview.Preflight succeeded. Coordinator guards passed before-blender, before-validation and before-import. Generation and validation ran sequentially in separate background Blender 5.2.3 processes; both exited successfully before promotion/import. validate_blender.py opened the saved blend, validated it and rendered blender-preview.png. Promotion copied closed staged files into this isolated output folder.

blender-validation.json records PASS: 8 nonempty mesh objects, 1,504 evaluated triangles, named Principled materials DD_brass and DD_woodDark, applied mesh scale, metric units (1 unit = 1 metre), exact external/embedded generator, valid FBX and root hierarchy. Overall evaluated dimensions: 4.400000 x 0.065000 x 2.335000 m. Bounds approximately (-2.2,-0.0325,0) to (2.2,0.0325,2.335). ROOT_WallMolding is at origin; base Z=0, centered X/Y. No moving part candidates. Separate horizontal/vertical meshes retain their origins under the root. No animation or gameplay-fit claim is made.

AssetQueueReview.Review compiled and executed successfully through unity_mcp. unity-validation.json records PASS: 8 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. The helper produced WallMolding_Review.prefab with new review materials and used its isolated additive AssetReview scene. A subsequent Unity_GetConsoleLogs request for errors/warnings returned zero entries, errors and warnings; Console was not cleared.

## Visual evidence and limitations
Inspected both blender-preview.png and preview.png. Unity preview visibly shows three horizontal strips and five dark uprights, with no pink/missing-material appearance. This is low-detail starter geometry, lacking finished crown profiles, convincing baseboard construction, texture detail and resolved junctions. BackWall is unavailable; alignment, intersections and seated gameplay-camera composition were not tested. No production scene integration occurred. Human aesthetic and production fit review remain required.

Screenshots: [Unity isolated preview](preview.png), [Blender preview](blender-preview.png). Machine evidence: [Blender validation](blender-validation.json), [Unity validation](unity-validation.json), [review manifest](review_manifest.json), [preflight](unity-preflight.json). Source/model files and screenshot hashes are retained in these validation files.

Generation emitted an OpenImageIO thumbnail-cache write error at a root thumbnail path; required .blend/.fbx writes and subsequent validation/render succeeded. Validation emitted Blender 6.0 deprecation notices for use_nodes. No settings or dependencies were changed to address either. Initial spreadsheet reading via openpyxl was unavailable; the existing workbook entry was read successfully using Python standard-library ZIP/XML without installation or modification.

All launched generation, validation and promotion subprocesses finished before this report. No agents, asynchronous writers, Play Mode or generic scene-editing tools were used. Tracker updates belong to the coordinator.
