# DD-20 CollectibleStatues — bounded production report

Result: generated, imported and validated; Needs Review. No human visual approval or Complete status claimed.

## Provenance and source changes
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets skill, registry entry, workbook XML, source README, reference brief, existing generator and previous attempt report/Blender preview. References contains only REFERENCE_BRIEF.md, no images. Art direction is noir, aged brass and burgundy. No external resources used.

Preserved the existing asset-specific Detective, Archivist and Commissioner bust generator and all 72 semantic meshes, modular assemblies and four material names. Changed only its FBX export: bake_space_transform=False and apply_scale_options=FBX_SCALE_ALL. This fixes the prior nested hierarchy's centimeter scaling and sideways geometry without replacing the detailed source with a starter. No shared helpers, gameplay, queue or tracker files edited.

Source: Assets/DetectiveDesk/ArtSource/Props/CollectibleStatues/build_asset.py.
Canonical editable version: CollectibleStatues.blend in this report folder, beside exact external build_asset.py snapshot and same-source CollectibleStatues.fbx. Embedded GENERATE_CollectibleStatues.py matches the external snapshot exactly. No helper dependencies or textures.

## Executed validation
Unity relay guidelines and required Preflight succeeded. Coordinator guards passed before generation, before validation and before import. Tools/build-one.ps1 executed isolated Blender 5.2 in the assigned staging folder; process exited successfully before the second Blender invocation. validate_blender.py executed against the saved blend, rendered blender-preview.png and exited successfully before promotion. Promotion and isolated AssetQueueReview.Review succeeded.

blender-validation.json: PASS; 72 nonempty meshes, 60,832 evaluated triangles, applied mesh scales, metric units, root at origin, all meshes parented, assigned named Principled materials. Materials: Statues_AgedBronze, Statues_PolishedBrass, Statues_DarkPatina, Statues_BurgundyPlinth.

Blender bounds: 0.284 x 0.074 x 0.1275 m; minimum Z=0. Unity read-only prefab inspection confirms upright bounds approximately 0.284 x 0.1275 x 0.074 m (tool's Vector3 display rounds to 0.28,0.13,0.07), center approximately (0,0.06375,0), root scale (1,1,1). Thus the previous 100-fold shrink and incorrect vertical axis are resolved. Individual base diameter 0.074 m; assembly pivots remain base-centered at X=-0.105,0,+0.105 m. Bust modules remain independently named with base-centered pivots. No moving animation parts.

unity-validation.json: PASS, 72 renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Helper created only isolated review prefab/materials/scene. Read Console without clearing: five pre-existing warnings (Input Manager deprecation, relay signature collection and unrelated CityBackdrop timestamp mismatch), no errors. Blender emitted use_nodes deprecation warnings and a thumbnail-cache write warning; blend save, FBX export and validation render all succeeded.

## Visual evidence and limitations
Inspected both blender-preview.png and preview.png in this folder. Blender shows the three front silhouettes and facial/accessory details; Unity preview is now clearly illuminated and upright, but the helper camera views the backs. Canonical Blender front is -Y, imported front is +Z; review-camera front-facing presentation is not established. Do not interpret the rear-view screenshot as a front aesthetic approval. No helper or camera edits made.

These remain stylized procedural concept busts with simplified facial geometry and flat PBR materials, not finished hero sculpts or reference-matched photorealism. StatueSlots dependency is unavailable; socket fit and gameplay integration were not tested. Human review must assess both front details and rear silhouette, metal roughness, scale and slot fit before approval.

Evidence: CollectibleStatues.blend, CollectibleStatues.fbx, build_asset.py, blender-validation.json, blender-preview.png, unity-validation.json, preview.png, review_manifest.json, promotion.json (if emitted), CollectibleStatues_Review.prefab and Materials/ in this version folder. Staging: ProductionLogs/AssetQueue/BuildStaging/20261031_011441_91ecff05/CollectibleStatues.

All invoked subprocesses finished. No Play Mode, active gameplay edits, scene switching by worker authoring commands, global saves or production integration.
