# DetectiveDesk — Blender asset workflow

This kit is a **workflow scaffold**, not an automatically installed Unity plugin. Copy the root `AGENTS.md`, `.agents/skills`, `Tools`, and `Assets/DetectiveDesk/ArtSource` into your existing Unity project (merge folders; don't overwrite your existing Game scene or gameplay files).

## One-time setup
1. Install Blender and confirm the `blender` command is on your PATH, or pass the full executable path to the PowerShell script.
2. Make sure Codex opens the **Unity project root**, where `AGENTS.md` resides. Start a new Codex session, so project instructions load.
3. Ensure Unity Editor and Unity MCP are running.

## Create example model
In the VS Code terminal at Unity project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\build-blender-asset.ps1 -AssetName ExampleCoffer
```

If Blender is not on PATH:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\build-blender-asset.ps1 -AssetName ExampleCoffer -BlenderExe "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe"
```

Update the Blender path to the installed version. The generator saves:
- `Assets/DetectiveDesk/ArtSource/ExampleCoffer/build_examplecoffer.py` — reproducible source
- `Assets/DetectiveDesk/ArtSource/ExampleCoffer/ExampleCoffer.blend` — editable result plus embedded Text Editor script

To view its embedded script in Blender, open the `.blend`, switch to Scripting, choose `GENERATE_ExampleCoffer.py` in the Text Editor. **Edit the external generator first** and regenerate; embedded text is the snapshot saved at generation time.

Unity may import `.blend` when Blender is installed; it converts the file internally. Newer Unity documentation recommends FBX for the most predictable model import. Materials often require URP setup. For a repeatable FBX fallback, use Blender's FBX export from the saved `.blend` while leaving the source file in the project.

## Modify an asset later
Tell Codex: `Use the blender-unity-assets skill. Update the ExampleCoffer source generator to do X, regenerate and save its .blend, reimport to Unity via MCP, preserve my scripts and GameObject transforms, validate.`

## Asset rules
- Keep changes to model appearance in Blender; Unity holds scene transforms, colliders, logic, and material overrides.
- Do not regenerate other assets or the whole game when changing one asset.
- For cinematic wood and velvet, use separately sourced, licensed PBR textures; procedural preview materials are not equivalent to final game shaders.
