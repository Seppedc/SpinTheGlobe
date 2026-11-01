# BankersLamp bounded production attempt

Outcome: asset_error. Requires export/import correction and renewed Unity visual review. No completion or human approval claimed.

## Provenance and changes

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets/SKILL.md, DD-25 registry and workbook entries, source README, reference brief and existing detailed build_asset.py. Inspected local References/NoirLighting.png and the preceding run's Unity preview. No downloaded resources or dependencies. The reference is art direction only.

Updated only the assigned source generator: recalculated closed shade-shell normals with bmesh and replaced parent-inverse parenting with pivot-local vertices. Preserved the existing brass base, curved stem, independently named shade pivot, opal liner/bulb and pull-chain geometry. No shared helpers or tracker/queue files edited.

## Executed evidence

- Unity relay guidelines and AssetQueueReview.Preflight completed successfully.
- Coordinator guards passed before-blender, before-validation and before-import.
- Tools/build-one.ps1 generated the staged editable BankersLamp.blend and same-source BankersLamp.fbx; Blender exited successfully before validation/import.
- Background validate_blender.py completed and rendered blender-preview.png. Exact embedded GENERATE_BankersLamp.py matches external build_asset.py. 41 meshes, 20,744 triangles, four assigned Principled materials; metric units and applied mesh scales passed. Root is at origin and lowest point Z=0.
- Dimensions: 0.276 m X, 0.1845 m Y, 0.405 m Z. Shade_Pivot is at Blender (0, -0.020, 0.361) m. Source shell has green exterior and ivory liner.
- Promotion completed after both Blender processes exited. AssetQueueReview.Review completed; unity-validation.json reports 41 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Isolated review prefab and screenshot exist.
- Read-only imported-FBX diagnosis reveals Shade_GreenGlass has 196 vertices / 1,164 indices but local scale (0.01,0.01,0.01), unlike the pivot's unit scale. Its world placement is near the base rather than the shade pivot. This is an unresolved FBX hierarchy/unit conversion defect; mesh counts alone do not validate visual correctness.
- Inspected preview.png: blank dark image. Inspected blender-preview.png: complete green-shaded lamp is visible. Therefore the helper's structural PASS is insufficient, and Unity visual validation FAILED.
- Unity Console inspection returned zero errors and four pre-existing warnings (Input Manager deprecation and relay executable certificate collection). No Console clear performed. Blender reported a thumbnail-cache write warning while successfully saving the blend; validation render succeeded.

## Files and limitations

All promoted evidence is in this report's directory: BankersLamp.blend, BankersLamp.fbx, build_asset.py, blender-validation.json, blender-preview.png, unity-preflight.json, unity-validation.json, protected-scene-before.txt, review_manifest.json, BankersLamp_Review.prefab, Materials/ and preview.png. Staging remains at ProductionLogs/AssetQueue/BuildStaging/20261030_212212_9a4d255a/BankersLamp/.

The canonical editable model for this attempt is the saved output blend with exact embedded generator. The external production generator remains Assets/DetectiveDesk/ArtSource/Props/BankersLamp/build_asset.py. No earlier generated versions were overwritten. No gameplay scenes, gameplay transforms, existing materials or prefabs were edited; the helper reports protected scene preservation.

Remaining work: correct nested mesh FBX unit/axis conversion (the exporter currently uses bake_space_transform=True), rebuild into fresh outputs, repeat Blender validation and isolated Unity review, and obtain a visible Unity screenshot. Do not accept this attempt on the structural PASS alone. Glass is a coated opaque preview material; transmitted light, chain animation, gameplay lighting and hero-quality surface wear were not implemented or tested. Human visual approval remains required after the import defect is resolved.
