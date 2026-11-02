---
name: blender-unity-assets
description: Create or modify Spin The Globe / DetectiveDesk 3D assets with reproducible Blender Python, save the .blend with an embedded copy of its generator, export FBX and publish it into Unity while keeping gameplay scripts. Apply whenever an agent creates, revises, exports, or imports a Blender asset.
---

# Blender → Unity reproducible asset workflow

## Where things live
| What | Path |
|---|---|
| Editable source (latest `.blend`, `build_*.py`, helper scripts, source textures, README) | `ArtSource/<Category>/<Asset>/` (repo root, outside Unity) |
| Reference images | `ArtSource/<Category>/<Asset>/References/` · general mood: `ArtSource/_Inspiration/` |
| Unity-ready asset | `SpinTheGlobe/Assets/DetectiveDesk/Art/<Category>/<Asset>/{Models,Meshes,Materials,Textures,Prefabs}` |
| Shared materials / textures | `.../Art/Shared/Materials/<Library>/`, `.../Art/Shared/Textures/` |

Categories: `Props`, `Environment`, `Materials`, `VFX`, `Animation`, `UI`. Keep **only the latest** `.blend` per asset; history lives in git.

## Required sequence
1. **Inspect** the existing Unity prefab/scene object (Unity MCP if available). Note scale, orientation, pivot, gameplay components and which Unity asset (FBX or baked `Meshes/*.asset`) is actually referenced. Do not delete it.
2. **Source:** edit the existing generator in `ArtSource/<Category>/<Asset>/` (`build_<asset>.py`, else `build_asset.py`). New assets get a new folder with `build_<asset>.py`, `Textures/`, `References/` and a short `README.md`.
3. **Generate:** `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-blender-asset.ps1 -Asset <Asset> -ExportFbx` (Blender path in `Tools/blender-path.json`). If Blender is unavailable, report the blocker; never skip generation silently.
4. **Persist:** the generator saves `<Asset>.blend` beside itself with its own source embedded as `GENERATE_<Asset>.py` in `bpy.data.texts`, packs/relative textures, uses stable object names, edits only its own collection, and is re-runnable.
5. **Validate** the `.blend` exists with non-empty meshes; check orientation, transforms, named materials, UVs and metric scale. Render a preview when possible.
6. **Publish:** add `-Publish` (and `-PublishTextures` when maps changed) to copy the FBX into `Art/<Category>/<Asset>/Models/`. The copy overwrites in place so the `.meta` GUID and every scene reference survive. Unity imports FBX only — `.blend` files stay outside `Assets/` (exceptions: `Art/Props/CollectiblePin/Models/Pin.blend` and `Art/Props/PinCoffer/Models/Pinbox_Original.blend`, which existing materials/prefabs reference directly).
7. **Prefab:** when a replacement is requested keep the gameplay root and scripts; swap only visuals beneath a `Visuals` child. Keep pivots, colliders and animations. Assets that use baked `Meshes/*.asset` snapshots (globe, pin coffer) must be refreshed in place, never re-pointed. Confirm no pink materials or Console errors. Only save the scene/prefab the task authorises.
8. **Revise** by editing the generator and re-running; never hand-tweak an imported mesh as the authoritative source.

## Material rules
- Source images and node setups travel with the `.blend` (pack when reasonable).
- Unity appearance comes from URP/Lit `.mat` assets in `Art/.../Materials/`; do not rely on Blender-only procedural shaders.
- Distinct names for wood, velvet, brass; separate lid with hinge-origin pivot when it animates.
- Keep licence/source notes for downloaded textures; Blender meters = Unity meters.

## Test task
`Use $blender-unity-assets to rebuild WhiskeyGlass with -ExportFbx, verify its .blend embeds the generator, and report the result without publishing into Unity.`
