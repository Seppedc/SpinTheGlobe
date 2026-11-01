# AtmosphereFX — DD-39

Owner: bounded Codex queue worker. Review date: 2026-10-31. Needs Review; no human approval or completion asserted.

## Provenance and deliverables

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets skill, registry entry, workbook row DD-39 (ZIP/XML), queue/dependency entries, IMPLEMENTATION_BRIEF.md and References/REFERENCE_BRIEF.md before authoring. This source folder contains no reference images or existing generator. Text art direction requests cool rainy-window atmosphere with warm noir interior accents. No downloaded content or dependencies installed.

RainyWindow dependency was verified in Assets/DetectiveDesk/AssetProduction/RainyWindow/20261030_185807_f932ec71: inspected report, manifest, validation and files; Unity loaded its existing review prefab with 23 renderers. Its planned registry target being absent does not block this effect. Dependency was not modified or instantiated.

Created reproducible editor command source at Assets/DetectiveDesk/ArtSource/Effects/AtmosphereFX/AuthorAtmosphereFX.cs.txt. It authors only this run's standalone outputs. AtmosphereFX.prefab has Rain_ExteriorWindow, Motes_Interior and Haze_WindowWash particle systems. Three new URP/Particles/Unlit transparent materials use original procedural alpha-mask PNGs. No mesh, Blender source or FBX is appropriate for this Unity effect deliverable. No existing runtime script/material/prefab modified.

## Executed validation

Unity relay guidelines loaded; required Preflight passed. Coordinator before-import writer guard passed before authoring/import. Public NewScene(EmptyScene, Additive)/SetActiveScene path used, with original scene and dirty state restored and temporary scene closed in finally. Reviewed localFixedCode: namespace wrapping only; no unsupported scene API substitution. No Play Mode or save-all used.

An initial worker-authored minimum-count assertion incorrectly required 100 live particles; simulation yielded 99. Corrected assertion to detect empty simulation and reran successfully, reusing new material assets. Dedicated Console inspection after the failure returned no errors; final inspection after review returned no errors or warnings. No Console clearing occurred.

Eight-second editor ParticleSystem.Simulate pass produced 99 live particles with fixed seeds 3901–3903. effect-validation.json and simulation-preview.png record this executed check. Inspected simulation-preview.png: visible vertical cool rain, warm scattered motes, and blue haze patches without pink surfaces. Actual imported masks confirmed 64 x 64 each through Unity, recorded in import-inspection.txt.

Final required AssetQueueReview.Review executed successfully. unity-validation.json records PASS, 3 renderers, 0 mesh filters (correct for particles), 0 missing materials, 0 Console errors, protected_scene_unchanged=true, prefab/screenshot hashes. Helper saved AtmosphereFX_Review.prefab and isolated AssetReview scene, then restored original gameplay scene. Trackers/queue remain coordinator-owned and were not edited.

## Dimensions and pivots

Root is origin, identity rotation, unit scale; Unity units are metres, +Y up. Named emitter transforms preserve independent adjustment. Rain position (0,1.9,0.18), local box (1.15,0.05,0.12), rotated +90 degrees about X so emission travels downward, speed 3.2 m/s and lifetime 0.55 s. Motes position (0,0.95,-0.5), box (1.25,1.25,0.65), 5/s, lifetime 7 s with gentle noise. Haze position (0,0.8,0.3), box (1.05,0.85,0.18), 1/s, lifetime 9 s, size up to 0.85 m. Root intended near window base; positive Z is exterior, negative Z interior. These are emitter dimensions, not a fixed mesh bounding box. All systems loop, prewarm, play on awake and use local simulation; maxParticles is 1000 each.

## Limitations and visual review

Inspected required preview.png: it is blank because the unmodified review helper renders immediately without advancing ParticleSystem simulation. This screenshot proves neither dynamic visibility nor aesthetics. Use inspected simulation-preview.png for actual effect appearance; it is a separate eight-second editor capture. Do not replace the helper's hashed screenshot with this supplemental image.

Haze is low-opacity billboard particles, not volumetric fog; circular patches remain visible in isolation. No collisions, glass streak shader, splashes, scene occlusion/soft intersection tuning, runtime performance, build shader stripping, or temporal motion quality were validated. Rain system is sized relative to the dependency's roughly 1.36 m wide frame but has not been mounted into gameplay or visually reviewed together with that frame. Gameplay integration, camera fit, emission density and haze subtlety require human review. New review prefab uses helper isolation layer 31; use AtmosphereFX.prefab for later authorized integration. No human approval is implied by successful structural checks.

All report-relative evidence and deliverables reside in Assets/DetectiveDesk/AssetProduction/AtmosphereFX/20261031_020108_ba47aaf2. All subprocesses completed; no background workers were launched.
