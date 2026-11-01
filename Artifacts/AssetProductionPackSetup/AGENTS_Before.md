# DetectiveDesk — persistent rules for Codex

## Blender-first asset pipeline (mandatory)

When creating or modifying a 3D asset, ALWAYS follow `.agents/skills/blender-unity-assets/SKILL.md`.

1. Create or update one deterministic Blender Python generator for the asset.
2. Execute that generator with local Blender; never claim success if it has not run.
3. Save the editable `.blend` inside `Assets/DetectiveDesk/ArtSource/<AssetName>/` with generator and textures next to it.
4. Embed a copy of the generator in the `.blend` Text Editor as `GENERATE_<AssetName>.py` (and pack external image textures when possible).
5. Prefer placing the `.blend` directly in the Unity project and letting Unity import it. If incompatible, produce a `.fbx` *from the same Blender source* and use the FBX instead; never maintain an unrelated replacement mesh.
6. Use Unity MCP to inspect and replace *only visual children*, preserving prefabs, collider, scripts, animations, gameplay, transform and scene camera. Backup first.
7. Re-run the generator to rebuild from updated parameters rather than starting from scratch. Preserve stable object names and pivots.
8. Verify expected output files exist, nonempty mesh, material assignments, and no Unity import/Console errors. Take Game camera screenshot for visual comparison.
9. Report file paths, changed objects, successful validations, and limitations.

Do not destructively wipe Blender scenes unrelated to the target asset. Do not regenerate game visuals without updating the Blender source script and `.blend` together. The source `.blend` is the canonical editable model.

## Existing project protection

Inspect and merge existing files; never overwrite unrelated assets or scripts. Preserve gameplay roots, prefab references, colliders, animations, transforms and material overrides. For workflow setup or standalone import tests, do not instantiate assets in, save, or otherwise modify the open gameplay scene. Prefab replacement and scene saves apply only when requested for that asset. Keep existing unsaved user edits intact.

Blender is configured locally in `Tools/blender-path.json`; use `Tools/build-blender-asset.ps1 -AssetName <Name>`. The wrapper starts a separate empty background Blender process, preserving interactive Blender sessions. Use process-scoped `powershell -ExecutionPolicy Bypass -File ...` when needed; never change system execution policy. Review an existing output before an explicitly requested rebuild.

## Existing production asset bindings

For pin coffer changes, read `Assets/DetectiveDesk/ArtSource/PinCoffer/README.md` first. Update its original-derived `build_pincoffer.py`, rebuild with `-AssetName PinCoffer -ExportFbx`, then use the scoped **Tools > Detective Desk > Blender Assets > Refresh Pin Coffer From Blender** editor command. Preserve the existing prefab and normalized Mesh asset identities, insert lift, all twelve gameplay slot positions, colliders and existing material references. The old `Assets/Objects/Pinbox.blend` must remain because the current URP materials reference its sub-assets; do not treat it or ExampleCoffer as the production generator.
