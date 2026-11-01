# BankersLamp DD-25 production report

Result: asset_error. Human visual review is pending; this asset is not Complete.

## Provenance and changes
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, the blender-unity-assets skill, registry DD-25, spreadsheet entries, README, reference brief and existing generator. Workbook was read through standard-library ZIP/XML because openpyxl is unavailable; no packages installed. Inspected References/NoirLighting.png as art direction only. The original build_asset.py delegated to a generic shared starter. Replaced that starter implementation within the assigned source file with deterministic asset-specific geometry; no shared helpers changed.

Source: Assets/DetectiveDesk/ArtSource/Props/BankersLamp/build_asset.py.
Editable output: BankersLamp.blend beside exact external build_asset.py and same-source BankersLamp.fbx in this report's directory. GENERATE_BankersLamp.py embeds the exact source. No external helper dependencies or textures.

Geometry includes spun stepped base, felt foot, collar, curved brass stem, arched double-surface green shade with pale liner, rim rails, axle/caps, opal tube and individually named beaded pull-chain. Root is ROOT_BankersLamp at origin. Shade_Pivot is a separate axle pivot at (0,-0.02,0.361) meters. Chain beads are independent meshes, without rigging or animation.

## Executed validation
Unity relay guidelines and AssetQueueReview.Preflight succeeded. Coordinator guards passed before both generation attempts, both validation invocations and promotion. Background Blender generation and final validation/render exited successfully before promotion/import. Initial source embedding check failed due to a UTF-8 BOM; corrected source decoding and rebuilt, then exact-source validation passed.

blender-validation.json: PASS; 41 meshes, 20,744 triangles, four named Principled materials, applied mesh scale, metric units, base Z=0, root at origin, nonempty geometry and material assignments. Bounds: 0.276 x 0.1845 x 0.405 meters. Same-source FBX exported. Blender preview inspected: shade present.

AssetQueueReview.Review executed successfully, producing BankersLamp_Review.prefab, new URP materials, isolated review scene and preview.png. unity-validation.json reports PASS, 41 meshes/renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Console inspection found four existing warnings (Input Manager deprecation and relay executable signature collection); no errors. Console was not cleared.

## Visual failure and limitations
Inspected preview.png: the green shade surface is absent in Unity while brass rims, stem, base, bulb and chain remain visible. Blender preview includes the shade. Automated import checks therefore do not establish visual correctness. The nested shade pivot / FBX transform or surface import needs investigation before acceptance. Existing promoted outputs were preserved rather than overwriting a generated version. This is not an acceptable finished lamp despite structural validation PASS.

Materials use flat PBR values and coat, without baked wear maps or physically transmitting glass. Brass appears substantially lighter in Unity. No runtime lighting, interaction, animation, gameplay integration or Game camera acceptance was tested. A nonfatal Blender thumbnail-cache write message and Blender API deprecation warnings occurred; canonical blend, FBX and requested rendered preview were successfully written.

Screenshots: preview.png (Unity, missing shade), blender-preview.png (Blender, shade present). Evidence: blender-validation.json, unity-preflight.json, unity-validation.json, protected-scene-before.txt and review_manifest.json in this directory. All subprocesses exited. Queue/tracker files were not edited. Only assigned generator and isolated output/staging assets were written by this worker; review assets were created through the authorized helper. Requires source/export correction and another isolated visual review before human approval.
