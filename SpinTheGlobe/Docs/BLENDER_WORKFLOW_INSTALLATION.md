# Installed Blender-first workflow — 2026-10-30

Unity project root: `C:/Users/decra/source/Antigravity/SpinTheGlobe/SpinTheGlobe`.

The requested ZIP was not found anywhere under the parent workspace. Installation used the existing extracted `../DetectiveDesk_Blender_Workflow/` folder after inspecting all six supplied files. A copy was staged at `C:/Users/decra/AppData/Local/Temp/DetectiveDeskWorkflow-2c5342fc8af64544be86d2623097dd75/DetectiveDesk_Blender_Workflow`. Archive integrity and extraction from a ZIP were therefore not tested.

## Files

```text
AGENTS.md
.agents/skills/blender-unity-assets/SKILL.md
Tools/
  build-blender-asset.ps1
  blender-path.json
  export-blender-fbx.py
Docs/
  README.md
  CODEX_SETUP_PROMPT.md
  BLENDER_WORKFLOW_INSTALLATION.md
Assets/DetectiveDesk/ArtSource/ExampleCoffer/
  build_examplecoffer.py
  ExampleCoffer.blend
  ExampleCoffer.blend1
  ExampleCoffer.fbx
  Materials/
    MAT_AgedMahogany.mat
    MAT_CrimsonVelvet.mat
    MAT_AgedBrass.mat
```

Unity also created `.meta` files for the new asset directories and files. `.blend1` is a backup of the first test generation. A new `../AGENTS.md` routes future sessions opened in the parent workspace to the nested project's instructions.

All six supplied files were installed at previously absent destinations; there were no existing destination instructions to merge. No files in the supplied extracted folder were changed. Installed instructions were extended to explicitly preserve unsaved scene edits and prevent standalone tests from saving or instantiating into the gameplay scene. The automation script now detects the Unity root, validates asset names, reads the local Blender path, starts an empty separate background Blender session, propagates Python failures, and optionally exports FBX with `-ExportFbx`. An FBX exporter and local Blender configuration were added. The supplied example generator was corrected to apply modifiers in order and avoid an empty Boolean material slot. Its updated source is embedded in the final `.blend`.

## Executed validation

- Blender executable: `C:/Program Files/Blender Foundation/Blender 5.2/blender.exe`, version 5.2.3 LTS.
- Ran `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-blender-asset.ps1 -AssetName ExampleCoffer` from the project context. A nonempty editable `.blend` was generated successfully. No system execution policies were changed.
- Opened the generated `.blend` in a second background Blender invocation. Verified that `GENERATE_ExampleCoffer.py` exactly matches the external generator; 25 nonempty meshes, 18,590 evaluated triangles, 25 meshes with UVs, 12 named socket markers, a separate lid hinge pivot, three named materials, meter units, and no external image textures were found. Materials are procedural color placeholders, not supplied PBR textures.
- Exported `ExampleCoffer.fbx` from that exact saved `.blend`, selecting only its `ART_ExampleCoffer` collection. The canonical `.blend` was not changed by export.
- Unity MCP connected to this project. Both `.blend` and FBX were imported and inspected as project assets without scene instantiation. Each has 25 meshes, 18,590 triangles, 12 markers and its lid pivot. Added three test-only URP/Lit materials and importer remaps for both models; zero missing or incompatible shader/material references. Mahogany metallic 0/smoothness .65; velvet 0/.09; brass 1/.71.
- Final Unity Console query returned zero errors and zero warnings. The initial direct `.blend` refresh/reimport emitted an importer inconsistency warning even though the model loaded. Subsequent import validation passed; FBX is retained as the fallback if this warning returns. No unsupported root cause is assumed.
- Blender emitted a `Material.use_nodes` deprecation notice for future Blender 6.0; this is not a generation failure in 5.2.3. The initial generator's modifier-order notices were resolved by its update.
- SHA256 checks confirmed all pre-existing files under Assets, Packages and ProjectSettings remained identical. Before/after in-memory snapshots confirmed every serialized component of the open DetectiveOffice scene was unchanged, including its pre-existing dirty state. The scene was not saved. Existing gameplay was not run or behaviorally retested during setup.

Machine-readable evidence is in `../Artifacts/BlenderWorkflowSetup/`: `BlenderValidation.json`, `UnityImportValidation.txt`, `ScenePreservation.txt`, and before/after scene and existing-file snapshots. No gameplay camera screenshot or visual scene replacement was performed because setup must leave the gameplay scene untouched.

## Usage

Open Codex in the Unity root (the parent workspace now routes instructions there too). New 3D asset work must update its generator, execute Blender, save editable source with an embedded generator, import through Unity MCP, preserve gameplay references and transforms, and validate geometry, materials and Console errors. Existing assets are rebuilt only for the requested task after inspection and backup.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\build-blender-asset.ps1 -AssetName ExampleCoffer
# For a requested rebuild plus same-source FBX fallback:
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\build-blender-asset.ps1 -AssetName ExampleCoffer -ExportFbx
```

The main generation path and separate FBX exporter were executed. The combined `-ExportFbx` wrapper branch was syntax-checked but was not separately executed during setup. No Blender plugin installation, registry edit, PATH change or execution-policy change is required. On another computer, update `Tools/blender-path.json` or pass `-BlenderExe` explicitly.
