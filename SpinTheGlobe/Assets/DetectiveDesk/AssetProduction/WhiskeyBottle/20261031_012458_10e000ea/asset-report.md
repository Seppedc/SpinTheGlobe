# WhiskeyBottle — DD-24

Outcome: asset_error. Geometry and structural import validation passed, but Unity's required isolated review screenshot is blank. Visual review is not satisfied. No human approval or completion is claimed. Queue/tracker files were not modified by this worker.

## Provenance and changes

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets/SKILL.md, registry DD-24, workbook row 29, the asset README, reference brief and existing generator before editing. References contains a written brief only; no reference images were supplied. No external resources were used. The previous generator delegated to the shared generic starter. Updated only the assigned asset's build_asset.py to a self-contained deterministic decanter generator, leaving shared helpers untouched.

Source: Assets/DetectiveDesk/ArtSource/Props/WhiskeyBottle/build_asset.py.
Canonical editable generated model for this attempt: WhiskeyBottle.blend in this report's folder. Matching external source snapshot: build_asset.py. Exact embedded Text Editor source: GENERATE_WhiskeyBottle.py. Same-source export: WhiskeyBottle.fbx. Outputs were generated and validated outside Assets, then promoted only after both Blender processes exited.

The model has a hollow clear-glass body, 24 shallow longitudinal flutes, rounded shoulder, narrow open neck, weighted base and independently named removable crystal stopper. No labels, logos, liquid, external textures or helper dependencies. Named objects: ROOT_WhiskeyBottle, Decanter_HollowFlutedBody, Stopper_RemovableCrystal. Root is centered at the base origin. Stopper extraction pivot is at Z=0.220 m in Blender. Nominal body wall is 4 mm; base thickness is 16 mm. Closed end rings use a 0.1 mm central radius rather than a single pole.

## Executed validation

- Unity relay GetUserGuidelines and required AssetQueueReview.Preflight succeeded.
- Coordinator guards passed before-blender, before-validation and before-import.
- Tools/build-one.ps1 executed local Blender 5.2.3 in an isolated background process; exited 0 after saving BLEND and FBX.
- Tools/AssetQueue/validate_blender.py executed in a second background Blender process against the saved BLEND; exited 0, rendered blender-preview.png and produced blender-validation.json.
- Blender checks: 2 nonempty meshes, 14,592 triangles, named Principled material Crystal_Clear, unit mesh scales, metric units, hierarchy, base Z=0, root at origin and exact embedded/external source equality. Measured dimensions: 0.104 x 0.104 x 0.258 m. Both generated model files are nonempty.
- Promotion completed after Blender exited. Review manifest references the same-source FBX. Job-local Materials/Crystal_Clear.mat was created through Unity MCP using URP/Lit transparency, alpha 0.22, smoothness 0.94 and no shadow casting. Existing production materials were untouched.
- Required AssetQueueReview.Review executed successfully: unity-validation.json records 2 meshes/renderers, 0 missing materials, 0 Console errors and protected_scene_unchanged=true. It saved WhiskeyBottle_Review.prefab and the isolated review scene through the prescribed helper, then restored the original active scene.
- Inspected blender-preview.png: clear hollow decanter and stopper are visible. Inspected preview.png: blank near-black background; bottle is not visible. Structural PASS does not establish visual success.
- Read Unity Console warnings/errors without clearing it: 0 errors, 7 pre-existing warnings (Input Manager deprecation, relay executable-signature collection and earlier unrelated timestamp/import warnings). No job-specific Console warning was returned.
- Read-only Unity diagnostics show renderer opaque and transparent layer masks both -1, supported URP pipeline and enabled UniversalForward material pass. These checks did not resolve the blank capture. No project renderer settings, orchestration scripts or review helpers were changed.

## Evidence and limitations

Evidence is beside this report: blender-validation.json, blender-preview.png, unity-preflight.json, unity-validation.json, protected-scene-before.txt, review_manifest.json and preview.png. The latter is an unsuccessful visual capture, retained honestly for diagnosis. Unity validation hashes the FBX and screenshot. Blender validation records source/model hashes.

Blender emitted a thumbnail-cache write warning while saving, but saved the model successfully and the separate required preview render completed. Blender also emitted use_nodes deprecation warnings for future Blender 6.0. No installation, permission escalation, worker/process inspection, Play Mode or gameplay-scene edit was performed. All invoked subprocesses finished.

Remaining work: diagnose transparent-model visibility in the prescribed isolated review capture, then obtain a visible Unity preview and human visual assessment. URP/Lit alpha glass approximates transparency and does not reproduce Cycles refraction, thickness or caustics. Transparent surface sorting, semantic stopper pivot after FBX conversion, lighting response and final scene readability still require visual review. No UV texture detailing, wear, LODs, collider, animation or gameplay integration is supplied. Asset remains unapproved; this attempt must not be treated as Complete.
