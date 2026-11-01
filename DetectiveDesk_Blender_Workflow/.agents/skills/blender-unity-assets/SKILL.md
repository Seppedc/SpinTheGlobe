---
name: blender-unity-assets
description: Create or modify DetectiveDesk 3D assets with reproducible Blender Python, save the .blend with an embedded copy of its generator, and import or update the asset in Unity through MCP while retaining gameplay scripts. Apply whenever Codex creates, revises, exports, or imports a Blender asset.
---

# Blender → Unity reproducible asset workflow

**Required sequence:**

1. **Inspect** the existing Unity scene/prefab using Unity MCP. Identify scale, orientation, pivot, gameplay components, and any currently used source model. Do not delete it.
2. **Source layout:** create `Assets/DetectiveDesk/ArtSource/<Name>/build_<name>.py` plus any texture files. Keep each asset self-contained.
3. **Generate:** run local Blender in background with `Tools/build-blender-asset.ps1 -AssetName <Name>`; if Blender is not installed or path unavailable, report the blocker and ask for location. Don't silently skip generation.
4. **Persist:** generator must save `<Name>.blend` in the same asset folder, with original generator source embedded in a `bpy.data.texts` block; should pack external textures and use relative paths. Use stable names, edit only its own collection, and make it re-runnable.
5. **Validate** `.blend` exists and Blender has generated mesh objects. Check geometry orientation, transforms, named materials, UVs where applicable, and scale. Take a Blender render/preview when possible.
6. **Unity import:** prefer Unity's `.blend` importer (Blender installed on Unity machine), keeping materials as Unity URP/Lit material assets where necessary. Unity may convert `.blend` internally and Blender procedural shaders do not always transfer. If `.blend` imports unreliably, use an FBX exported from the same `.blend` and do NOT swap to a separately generated model.
7. **Prefab** keep gameplay root and scripts; swap only visuals beneath a `Visuals` child. Recalculate existing colliders only if needed; keep pivots/animations. Confirm no pink materials or errors. Save prefab and scene.
8. **Revise:** edit generator, rerun, inspect regenerated `.blend`, Unity reimport, validate. Never manually tweak an imported mesh as the authoritative source, since regeneration loses those edits. If manual Blender changes are necessary, update the source script or a documented postprocessing script.

## Material rules

- `Source` images and material nodes travel with the `.blend` (pack assets in Blender if reasonable).
- URP/PBR authoring for Unity should happen as separate `.mat` assets; don't assume a rich Cycles node tree transfers unchanged.
- Keep distinct names for wood, velvet, brass; separate lid with hinge-origin pivot if it will animate.
- Preserve reproducibility, license/source notes for downloaded textures, and consistent mm-to-m conversion (Blender meters, Unity meters).

## Test task

Ask Codex: `Use $blender-unity-assets to rebuild ExampleCoffer, verify that its .blend embeds the script, then import it via Unity MCP without moving existing gameplay objects.`
