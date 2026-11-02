# PinCoffer production source

Canonical generator: `build_pincoffer.py`. Canonical editable source: `PinCoffer.blend`, with an exact generator snapshot in `GENERATE_PinCoffer.py`. This is the existing authored coffer, not ExampleCoffer or a newly designed replacement.

## Provenance

The original generator was found at `C:/Users/decra/Downloads/DetectiveDesk_Precision_12_Slot_Coffer_Blender/Detective_Coffer_Rebuild/build_coffer.py`. It exactly matched the `build_coffer.py` text embedded in the existing `Assets/DetectiveDesk/Art/Props/PinCoffer/Models/Pinbox_Original.blend`. The original kit files are in git history (removed 2026-11-02). The same textures are alongside the production generator and packed into its `.blend`. No outside texture downloads were used.

The production adaptation preserves the original dimensions, 74 named mesh objects, seven material definitions, true socket geometry and lid design. Changes are limited to scoped collections, command-line output handling, deterministic startup, relative/packed texture paths, and embedded source persistence. The original kit's optional GLB and socket CSV exports remain available.

## Rebuild and refresh

Back up the current prefab and scene before a requested production update. From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\build-blender-asset.ps1 -Asset PinCoffer -ExportFbx -Publish
```

After Unity imports the outputs, use **Tools > Detective Desk > Blender Assets > Refresh Pin Coffer From Blender**. This editor-only command validates all 74 part bindings, then refreshes changed baked mesh assets in place. It does not instantiate anything, change hierarchy, move transforms, save scenes, replace colliders, or edit gameplay scripts. Run only in Edit Mode. A deliberate topology/hierarchy change needs inspection and adaptation of the bindings; do not bypass failed validation.

## Production bindings — preserve these

- Existing gameplay prefab: `Assets/DetectiveDesk/Art/Props/PinCoffer/Prefabs/DetectivePinCoffer.prefab`.
- Canonical Unity import used for mesh refresh: `Assets/DetectiveDesk/Art/Props/PinCoffer/Models/PinCoffer.fbx`, exported from the canonical saved `.blend`.
- Both `.blend` and FBX import successfully. Both importers use baked axis conversion, meter units, no cameras/lights/animation, and remaps to the seven existing URP/Lit materials embedded in `Assets/DetectiveDesk/Art/Props/PinCoffer/Models/Pinbox_Original.blend`.
- Keep `Assets/DetectiveDesk/Art/Props/PinCoffer/Models/Pinbox_Original.blend` and its `.meta`: existing material asset GUIDs/local IDs refer to it. It is retained as a legacy material/reference dependency, not the authoring target for future updates. Existing material assets and textures are unchanged.
- Keep `Assets/DetectiveDesk/Art/Props/PinCoffer/Meshes/*.asset` and their `.meta` files. The prefab references these 74 normalized mesh snapshots, rather than directly referencing FBX submeshes. The refresh command updates contents at stable asset identities; simply re-exporting an FBX does not update the snapshots.
- Keep the existing prefab's `Body`, `Lid`, `VelvetInsert`, and `PinSlots` transforms. In particular, its existing 0.011 native-unit insert lift is a Unity assembly adjustment. It is deliberately not added to the Blender generator or altered by refresh.
- Preserve the twelve `PinCofferSlot` scripts, collectible pin prefab references, trigger and body colliders, `DeskInteractable` and game reference, lid pivot, root transform, and inactive rollback visuals. Never make a second active coffer.

## Executed validation — 2026-10-30

Original and regenerated Blender evaluated geometry matched for every mesh. Corrected Unity import matched all 74 original FBX part matrices exactly. The production refresh compared vertices, normals, indices and UVs against every existing visual mesh and reported **0 changed mesh assets**: the source was migrated without changing the appearance or asset references.

`validate_pincoffer.py` verified 12 open brass rings and recessed cavity floors in exactly 3 rows and 4 columns, each 0.007 m deep. It also verified the matching 0.160 × 0.110 m base/lid panels, 108-degree lid hinge, seven separate material definitions, all three packed/source textures, 74 meshes and 19,168 evaluated triangles. Fifty source meshes have UVs; the hand-built annular/cavity meshes use the original procedural materials and do not all have UVs. Existing Unity material overrides are preserved.

Older sources, backups and validation artifacts are in git history (before 2026-11-02).

No Play Mode, native mouse-hit or pin population test is claimed for this migration. Existing scripts and colliders are preserved. The previously reported project physics limitation remains outside this asset migration's scope.
