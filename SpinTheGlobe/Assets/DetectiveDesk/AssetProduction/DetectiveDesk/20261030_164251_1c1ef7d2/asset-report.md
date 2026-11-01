# DD-01 DetectiveDesk asset report

Result: Needs Review. Technical validation passed; no human approval or completion claim.

## Provenance and scope
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets/SKILL.md, registry DD-01, both matching workbook entries, README, reference brief, existing build_asset.py and shared starter implementation. Visually inspected References/SceneApproved.png. The existing script only delegated to generic starter geometry; no detailed existing model was replaced. Reference is art direction only; no downloaded content or reference pixels were used in the model.

Updated only the assigned asset-specific build_asset.py. Created a deterministic self-contained tabletop with thick beveled walnut slab, lower stepped molding, inset brass perimeter and thin brass fascia. DeskStructure owns legs/apron; they were intentionally not generated. No gameplay objects were replaced.

## Source and artifacts
Authoritative generator: Assets/DetectiveDesk/ArtSource/Environment/DetectiveDesk/build_asset.py
Editable version source: DetectiveDesk.blend in this report folder, with exact GENERATE_DetectiveDesk.py embedded and matching build_asset.py snapshot beside it.
Same-source export: DetectiveDesk.fbx. Source includes metric UV projection, seeded raster walnut grain and subtle scratch/roughness maps packed in the blend and embedded in FBX. External raster copies remain in the assigned ProductionLogs staging textures folder; promotion did not copy that subfolder. No helper dependencies beyond Blender's installed Python/NumPy.
Blender build and validation were separate isolated background processes; both fully exited before promotion. Coordinator guards passed before each build/validation and promotion requested before-import itself.

## Executed evidence
- Unity relay guidelines and AssetQueueReview.Preflight succeeded.
- Blender 5.2.3 generator ran and exported blend/FBX successfully.
- First validation caught a BOM mismatch in the source snapshot. Source reader corrected, rebuilt and validated again successfully; validator was unchanged.
- blender-validation.json: PASS, exact embedded source, 8 nonempty meshes, 2400 evaluated triangles, 3 named Principled materials, applied scales and parent hierarchy.
- Dimensions: 2.800 x 1.150 x 0.106 m in Blender XYZ. Centered XY, underside at Z=0. ROOT_DetectiveDesk at origin. No moving parts; individual trim objects remain separately named. Unity axis conversion comes from same-source FBX; no gameplay fit test was performed.
- promote.py succeeded after Blender exited.
- AssetQueueReview.Review succeeded using isolated additive scene. unity-validation.json: PASS, 8 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Helper restored original scene. No Play Mode or global Console clear.
- Visually inspected blender-preview.png and preview.png in this folder. Unity preview shows slab silhouette, edge thickness and inset brass border with no pink materials.

## Limitations and remaining review
The review helper creates new URP materials without copying image texture bindings. Thus Unity preview is flat walnut color, not the baked wood grain visible in Blender. Packed source maps exist, but grain/roughness binding to NEW review materials still requires a scoped material pass; existing production materials were untouched. Blender preview also has strong broad highlights, and the procedural grain is regular; neither preview establishes hero quality or reference parity. Fine scratch detail, edge wear, color/sheen and gameplay camera proportions require human review and likely refinement. No normal map or scratch interaction behavior was implemented. Surface is UV mapped for subsequent scratch/material authoring.

Blender reported a thumbnail-cache OpenImageIO write warning and use_nodes deprecation warnings; output saving, export and required validation completed with exit code zero. No project disk failure or Unity Console error was observed. Tracker/queue files were not edited. All subprocesses finished.
