# Asset Production Pack installation — 2026-10-30

Installed into `C:/Users/decra/source/Antigravity/SpinTheGlobe/SpinTheGlobe`, the Unity root containing Assets, Packages and ProjectSettings.

## Archive and merge

Inspected the actual `../DetectiveDesk_Asset_Production_Pack.zip`: 134 files, 28,123,066 uncompressed bytes. Checked entry paths for traversal, extracted into a unique temporary folder, and inspected instructions, tools, generator code, tracker schema and project destinations before installation. The archive checksum and staging path are recorded in `../Artifacts/AssetProductionPackSetup/`. All 134 destination files were new; no completed model, gameplay script, prefab or scene was overwritten.

The supplied `AGENTS_ASSET_ADDENDUM.md` remains installed as reference. Its tracking and categorized-source rules were merged into the existing AGENTS.md while retaining all prior rules. Established coffer paths and export/pivot conventions take precedence over starter defaults. The installed `BLENDER_ASSET_STANDARD.md` now states this compatibility exception explicitly; the ZIP and staged originals are unchanged.

## Installed structure

```text
AGENTS.md                         existing rules retained; tracking rules appended
AGENTS_ASSET_ADDENDUM.md
BLENDER_ASSET_STANDARD.md
Asset_Tracker.xlsx
asset_registry.json
assets.csv
README.md                        pack overview
Inspiration/SceneAndValidatedAssets/  seven supplied images
Tools/
  build-one.ps1
  blender_shared.py
  update-asset-tracker.py
Assets/DetectiveDesk/ArtSource/
  Environment/                   nine groups
  Gameplay/                      nine groups
  Props/                         ten groups
  UI/                            six groups
  Materials/                     four groups
  Effects/                       four groups
  _Validation/ProductionPackStarter/WallMolding/
    build_asset.py
    blender_shared.py
    WallMolding.blend
    WallMolding.fbx
    Materials/                   two test-only URP/Lit assets
  PinCoffer/                     existing production source; unchanged
  ExampleCoffer/                 existing workflow example; unchanged
ProductionLogs/TrackerBackups/   automatic tracker backups
Docs/ASSET_PRODUCTION_PACK_INSTALLATION.md
```

The 42 pack folders contain the supplied reference briefs/images, 21 distinct procedural starters and 21 non-mesh implementation briefs. `InstalledPackFiles.txt` in the artifact directory lists every supplied file. Unity created metadata for the new asset directories, files and isolated test materials.

## Completed assets protected

- PinCoffer: tracker points to the existing `ArtSource/PinCoffer/build_pincoffer.py`, `PinCoffer.blend`, `PinCoffer.fbx` and `Assets/DetectiveDesk/PinCoffer/DetectivePinCoffer.prefab`. The established refresh menu, normalized mesh assets, original material references and all slot/pivot bindings remain unchanged.
- CollectiblePin: opened `Assets/Objects/Pin.blend` in background Blender. Its embedded detailed `build_collectible_pin.py` exactly matches `C:/Users/decra/Downloads/DetectiveDesk_Collectible_Pin_Blender_Kit/build_collectible_pin.py`. Copied that generator unchanged to `ArtSource/Gameplay/CollectiblePin/build_collectible_pin.py` and corrected tracker bindings to the actual model and `Assets/DetectiveDesk/Prefabs/CollectiblePin.prefab`. No pin generator was executed and no pin assets were rebuilt. Its original output handling and embedded-source persistence must be adapted before a future requested rebuild.
- Both categorized `PREFER_EXISTING.md` files now identify the actual detailed sources. Generic PinCoffer and CollectiblePin starters remain as code references but explicitly refuse direct execution; `build-one.ps1` also blocks these two slugs. Both protections were executed and passed.

## Tool configuration

Blender: `C:/Program Files/Blender Foundation/Blender 5.2/blender.exe`, version 5.2.3 LTS. The new wrapper reads the existing `Tools/blender-path.json`, supports explicit executable and output-directory overrides, uses a separate factory-startup background process, propagates Python failures, checks both output files, and restores its temporary process environment variable. It refuses existing outputs unless `-Rebuild` is explicitly supplied; that branch backs up outputs first. The first-build and protected-asset paths were executed; the rebuild-backup branch was syntax-checked but not exercised. No execution-policy, registry, global PATH or system settings were changed.

`blender_shared.py` keeps the supplied starter designs and export defaults. Added output redirection for isolated tests, a scene update before bounds calculation, deterministic seeding, and exact embedding/preservation of both generator and shared helper sources. Existing `build-blender-asset.ps1`, `export-blender-fbx.py`, detailed source scripts and coffer editor tool were unchanged.

`Tools/update-asset-tracker.py` uses Python's standard library; no package installation is needed. It updates the registry, CSV and formatted Excel workbook together, preserving workbook sheets/styles/formulas and creating backups before writes. Added spreadsheet columns for executed validation, notes and evidence. Existing pack registry `status` denotes source maturity; `progress_status` corresponds to the Excel Status dropdown. Use it after future asset work:

```powershell
python .\Tools\update-asset-tracker.py --slug WallMolding --status Review --check "Actual executed checks" --notes "Remaining work"
```

PinCoffer and CollectiblePin are recorded as Review, with actual source paths and evidence rather than starter paths. WallMolding remains Not Started for production; its isolated test is recorded separately. Other asset progress was not claimed or marked complete.

## Executed starter test

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\build-one.ps1 -Slug WallMolding -OutputDirectory .\Assets\DetectiveDesk\ArtSource\_Validation\ProductionPackStarter\WallMolding
```

The actual command was run from the parent workspace with the corresponding project-prefixed output path. The following checks passed:

- Nonempty editable WallMolding.blend and FBX generated by Blender 5.2.3.
- Eight meshes, 1,504 triangles, metric units, centered root at origin and lowest evaluated point at Z=0.
- Applied mesh rotation/scale; separately named objects and named Principled materials retained.
- Embedded `GENERATE_WallMolding.py` and `GENERATE_blender_shared.py` exactly match their external sources, both preserved next to the outputs.
- FBX reimported into a fresh Blender scene: eight meshes, identical triangle count and maximum bounding-dimension discrepancy of approximately 0.000000175 m.
- Unity MCP imported both outputs in the isolated _Validation folder: eight meshes, 1,504 triangles, two test-only URP/Lit material remaps, no missing or incompatible materials. No object was instantiated in the active scene and no production asset was replaced.
- PowerShell syntax passed; all 32 Python files under ArtSource and Tools parsed successfully. Only the WallMolding starter was executed; the other starters were not claimed as runtime-tested.
- Final Unity Console query: zero errors and zero warnings. Blender validation emitted a future Blender 6.0 `Material.use_nodes` deprecation notice; current 5.2.3 generation and verification succeeded.

## Preservation and future work

SHA256 baselines confirm every pre-existing file under Assets, Packages, ProjectSettings and Tools is unchanged. Before/after serialized component snapshots of the open DetectiveOffice scene match exactly, including its pre-existing dirty state. The active scene was neither saved nor edited; no Play Mode test was run. Existing AGENTS.md content was retained with additional tracking rules.

Future asset work must read the tracker, references, existing generator and engineering standard; update the detailed generator; execute Blender; retain editable source with embedded scripts; export same-source FBX as needed; update only authorized Unity visual children; validate geometry/materials/camera/Console; then record actual progress in all tracker formats. Completed assets use their verified existing bindings. Non-mesh groups remain UI/material/texture/animation/VFX work, not Blender proxies.

Evidence and file manifests: `../Artifacts/AssetProductionPackSetup/`. This setup did not finish any new hero-quality production asset, migrate/rebuild the pin, or change gameplay objects.
