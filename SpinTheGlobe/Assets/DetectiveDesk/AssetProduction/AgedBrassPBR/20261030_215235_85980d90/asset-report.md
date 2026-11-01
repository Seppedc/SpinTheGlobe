# Aged brass PBR — DD-36

Result: Needs Review. No human approval or completion claimed.

## Provenance and source
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, asset skill, registry entry, Excel tracker row, IMPLEMENTATION_BRIEF.md and References/REFERENCE_BRIEF.md. No reference images or existing generator were present. Art direction is the supplied noir brass/patina/scratch brief. All texture content is deterministic procedural authoring, seed 360; no external content or dependencies were added.

Source: Assets/DetectiveDesk/ArtSource/Materials/AgedBrassPBR/Generate_AgedBrassPBR.cs.txt. Executed through Unity MCP RunCommand. This is a non-3D material job; Blender, FBX and production geometry are not applicable.

## Deliverables
All output paths below are relative to this report:
- AgedBrassPBR.mat — new standalone URP/Lit material.
- AgedBrass_BaseColor.png — 512x512 sRGB brass, green/brown aging, bright scratches.
- AgedBrass_MetallicSmoothness.png — linear metallic R, smoothness A; oxidation lowers both.
- AgedBrass_Normal.png — linear tangent-space surface variation and scratch grooves.
- review_manifest.json and AgedBrassPBR_Review.prefab — helper-owned material sphere review.
- preview.png — inspected 1200x900 Unity review camera capture.

## Executed checks
Unity guidelines loaded and required preflight passed. Coordinator before-import guard PASS preceded creation/import. Generator compiled and executed successfully. A later read-only Unity command verified all three imported maps, dimensions, Repeat wrap, color spaces, normal importer, supported shader and material keywords (import-checks.txt). material-validation.json records generation settings. AssetQueueReview.Review executed successfully: unity-validation.json reports one preview mesh/renderer, zero missing materials, zero Console errors, protected_scene_unchanged=true, and screenshot/source hashes. Original gameplay scene restored by helper; no gameplay placement or production material was modified. No Play Mode or global asset save/Console clear was used. Trackers and queue were not edited.

## Dimensions, pivots and limitations
Material has no physical dimensions or pivot. UV tile scale is one repeat per UV unit and must be chosen for each eventual model; the helper uses a standard unit sphere centered on its default pivot. The sphere is review geometry only.

Preview inspection shows golden highlights, mottled dark aging and directional scratches. Grain reads coarse at close range, scratch direction is visibly repeated, and verdigris is subtle. This is an initial material candidate, not a finished hero material. True edge-localized wear requires model curvature or authored masks and is not demonstrated by this generic tile; sheen currently follows lighting and polished texture regions. No production mesh UV, target-scene reflection environment, platform build, or gameplay integration was tested. Human visual review remains necessary for patina balance, scratch scale, resolution, edge-wear requirements and final art direction.
