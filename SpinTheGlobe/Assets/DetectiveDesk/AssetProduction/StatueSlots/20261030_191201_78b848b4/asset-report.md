# DD-19 StatueSlots asset report

Result: generated and technically validated; Needs Review, no human visual approval.
Owner: bounded Codex asset worker. Review date: 2026-10-30.

## Provenance and changes
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets skill, registry DD-19 entry, Excel DD-19 row, existing generator, README and References/REFERENCE_BRIEF.md. No reference images are supplied. Art direction is the written mahogany/aged-brass/burgundy noir brief; no downloads or external textures were used.

Updated only Assets/DetectiveDesk/ArtSource/Props/StatueSlots/build_asset.py in the production source. It previously delegated to the generic shared starter (two cylinders per base). It is now a self-contained deterministic generator with turned stepped mahogany bodies, lower brass bands, upper brass rims, recessed empty burgundy pads and blank front label plates. Four independently parented DisplayBase_01 through DisplayBase_04 groups remain empty, with no statues. No shared helper was changed.

## Output paths
All following paths are relative to this report folder, Assets/DetectiveDesk/AssetProduction/StatueSlots/20261030_191201_78b848b4/:
- StatueSlots.blend: editable canonical generated source, embeds exact GENERATE_StatueSlots.py.
- build_asset.py: external exact source snapshot; no helper dependency.
- StatueSlots.fbx: exported from the same saved Blender source.
- StatueSlots_Review.prefab and Materials/: isolated review prefab and four NEW URP materials.
- blender-validation.json, unity-preflight.json, unity-validation.json: executed checks and hashes.
- blender-preview.png: close Blender preview, visually inspected.
- preview.png: isolated Unity camera screenshot, visually inspected.
- review_manifest.json: model review binding.
Initial generation/validation used ProductionLogs/AssetQueue/BuildStaging/20261030_191201_78b848b4/StatueSlots. Promotion occurred only after both Blender processes exited.

## Executed validation
Unity relay guidelines loaded and AssetQueueReview.Preflight passed. Coordinator guard passed before-blender, before-validation and before-import. Tools/build-one.ps1 ran the edited generator in Blender 5.2.3 background, exited zero and produced nonempty blend/FBX files. The supplied validate_blender.py ran in a second background Blender process and exited zero after rendering.

Blender: 24 nonempty mesh objects, 49,056 evaluated triangles, four named Principled materials, applied mesh scale, metric units, grounded geometry, all meshes parented, ROOT_StatueSlots at origin and embedded/external generator equality PASS. Whole row bounds: 0.484 x 0.094 x 0.039 m. Each base diameter 0.094 m and height 0.039 m, center spacing 0.130 m; each DisplayBase child origin is bottom center. There are no moving components. Existing starter footprint and row spacing are retained.

AssetQueueReview.Review passed through Unity MCP. Imported 24 meshes/renderers, zero missing materials, zero Console errors, source and screenshot hashes recorded. Protected gameplay scene unchanged according to helper snapshot verification. Original active scene restored by helper; no gameplay edits, scene switching by worker, Play Mode or global Console clearing. Console inspection found four existing warnings (Input Manager deprecation and executable signature collection), plus informational shadow-atlas reduction during review. No asset import/compilation errors were reported. Historical Unity AI NoSubscription text is informational and did not block relay or review.

## Visual inspection and limitations
Blender preview shows four separate empty bases with visible brass rims, pads and blank label plates. Unity screenshot contains all four bases but the helper camera frames them very small; it confirms visibility, not detailed aesthetics or axis/fit approval. The helper was not edited or bypassed. Human review should inspect the prefab closely and confirm intended display orientation/scale before gameplay integration.

Materials use constant PBR colors, without wood grain, wear maps or fabric normal detail; this is a refined procedural proposal, not a photorealistic hero asset. Blank labels have no typography. No statue attachment compatibility, collider, LOD, UV texture workflow or gameplay integration has been tested. Triangle count is relatively high for these small props; an optimization pass may be appropriate after art approval. Blender emitted a nonfatal thumbnail-cache write error while saving; the blend, FBX and explicit preview saved and validated successfully. Blender also emitted future use_nodes deprecation warnings.

Queue, registry, CSV and workbook were not modified by this worker. No completion or human approval is claimed. All worker-launched subprocesses finished before reporting.
