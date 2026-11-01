# DetectiveDesk — asset production pack

This is **not** a Unity project replacement. Merge the folders into your existing project using Codex; do not overwrite your existing AGENTS.md, prefabs, scripts, or already-finished `.blend` models.

## Contents
- `Asset_Tracker.xlsx` — 42 groups in a production tracker, priorities and progress.
- `asset_registry.json` — script-friendly registry of every planned asset.
- `Assets/DetectiveDesk/ArtSource/` — folders for all 42 groups.
- 21 distinct procedural Blender scripts (`build_asset.py`) for geometry-oriented groups. Each generator makes **editable starter geometry**, not a final photorealistic model.
- Existing precision `build_coffer.py` and `build_collectible_pin.py` retained as better sources; do **not** replace their finished models with starter proxies.
- `References/` folders and `Inspiration/SceneAndValidatedAssets/` — selected user-provided and previously generated project reference images, for art direction only.
- Non-mesh folders contain `IMPLEMENTATION_BRIEF.md` for Unity UI, texturing or effects work (21 groups). These should not be artificially converted to Blender meshes.
- `Tools/blender_shared.py` and `Tools/build-one.ps1` — common geometry utilities and CLI helper.

## Usage
1. Unzip in a temporary folder. Give Codex your existing Unity project and ask it to inspect diffs before copying; never overwrite assets automatically.
2. Merge `Assets/DetectiveDesk/ArtSource` and `Tools` into your existing Unity project. Merge `AGENTS_ASSET_ADDENDUM.md` with the existing AGENTS.md.
3. To build a *starter* object: `powershell -File Tools/build-one.ps1 -Slug DetectiveDesk` (replace the slug); Blender must be on PATH or pass `-Blender "C:\...\blender.exe"`.
4. Each script saves a named `.blend` and attempts FBX export in the same folder, embedding its generator text into the Blender project.
5. Inspect the result in Blender first and iteratively increase detail. Then ask Codex Unity MCP to replace only the visual mesh, preserving transform, scripts, colliders and interaction.
6. Update status and file paths in `Asset_Tracker.xlsx` as you finish the work.

## File strategy
`.blend` files are created **only after running generators on your computer**. Pre-generated .blend files are not included, because Blender was not run here. The prior pin coffer and pin models remain the authoritative source for those assets, and should be copied in from the working Unity project when migrating.

## Visual source rules
Inspiration artwork is reference imagery only. Some images are prior concepts and not yet perfectly aligned with final requirements. The `WorldMap` asset is geographically themed only: no strings or clue photos on the map itself. Do not put the former evidence crate on the desk. Maintain a perfectly front-facing player camera.

## Codex / Gemini engineering rules
The supplied technical-artist rules are incorporated in `BLENDER_ASSET_STANDARD.md`. Starters now generate a root anchored at XY=0, Z=0, embed their generator, and export a selected hierarchy with explicitly configured Unity FBX axes. Preserve child pivots for animation. The generators have been syntax-checked but **not tested in Blender or Unity here**.
