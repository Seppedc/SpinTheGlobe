# RainyWindow — DD-07

Owner: bounded Codex queue worker. Review date: 2026-10-30. Needs human visual review; no completion or human approval asserted.

## Provenance and changes

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets skill, RainyWindow registry/workbook entries, README, reference brief, NoirLighting.png and existing build_asset.py before editing. Workbook read with Python standard-library ZIP/XML because openpyxl is unavailable; no dependency installed. Trackers and queue remain coordinator-owned.

Updated `Assets/DetectiveDesk/ArtSource/Environment/RainyWindow/build_asset.py` from its generic shared-library starter to an asset-specific, self-contained deterministic generator. The reference supplies dark timber/noir art direction only. Created layered jambs, head casing, rebates, facing beads, sill, apron and six open pane apertures. Removed the starter's glass from this asset design to honor frame/mullions-only scope. No external textures or helper dependencies. No gameplay integration or existing material edits.

## Executed evidence

Unity relay guidelines loaded and AssetQueueReview.Preflight passed. Coordinator guards passed before generation, validation and promotion/import. Local Blender 5.2.3 executed generation and exported same-source FBX in ProductionLogs staging; then exited. Initial invocation retained factory startup objects; generation was rerun with explicitly empty factory settings before validation. The `.blend1` is an earlier automatic backup, not the canonical asset or reviewed model.

Separate background Blender validation/render invocation exited successfully. `blender-validation.json` records PASS: exact embedded GENERATE_RainyWindow.py/external source match, 23 nonempty meshes, 4,324 evaluated triangles, applied scales, assigned Principled materials, metric units and root at origin. Dimensions: 1.360 x 0.305 x 1.840 m, base Z=0, X centered; depth midpoint is -0.0025 m due to the projecting sill. Front faces -Y in Blender, vertical +Z; FBX exports -Z forward/+Y up. Static joined frame has no independently moving parts; individual named components retain local center origins under ROOT_RainyWindow.

Promotion ran only after both Blender invocations exited. Canonical editable source is `RainyWindow.blend` in this output folder, with exact `build_asset.py` snapshot beside it and embedded. Authoring generator remains at the source path above; staging-first/output-folder source placement follows this queue job's explicit instructions.

AssetQueueReview.Review passed using the isolated additive review scene. `unity-validation.json`: 23 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Helper created only new review prefab/materials and remapped them to URP. Unity Console inspection found four existing warnings (Input Manager deprecation and MCP executable signature collection), no errors. No Console clearing, Play Mode or gameplay scene saves performed.

## Visual inspection and limitations

Inspected `blender-preview.png` and Unity `preview.png`: upright rectangular frame, six clear apertures, projecting sill, no pink or missing surfaces. Unity preview views the plainer rear face; front casing/bead detail is more visible in the Blender preview. Neutral review lighting reads lighter/browner than the dark noir reference. Flat PBR colors are review materials, not finished wood grain, weathering or photorealistic hero surfacing. Glass, rain shader, exterior backdrop, animation and gameplay placement are outside this frame-only job. No rain behavior was tested. Actual scene fit, preferred facing, proportions and material tone require human review.

Blender emitted a nonfatal thumbnail-cache write warning and use_nodes deprecation warnings; canonical blend/FBX and both preview images were generated successfully. No permissions were broadened.

## Files

- `RainyWindow.blend`, `RainyWindow.fbx`, `build_asset.py`: canonical source/export/snapshot.
- `blender-validation.json`, `unity-preflight.json`, `unity-validation.json`: executed checks.
- `RainyWindow_Review.prefab`, `Materials/`: isolated Unity review assets.
- `preview.png`: Unity screenshot; `blender-preview.png`: Blender comparison.
- `protected-scene-before.txt`: helper protection evidence.

All paths above are relative to this report's output folder unless explicitly stated. Remaining disposition: Needs Review, subject to coordinator tracking and human visual approval.
