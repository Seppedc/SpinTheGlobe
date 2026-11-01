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
