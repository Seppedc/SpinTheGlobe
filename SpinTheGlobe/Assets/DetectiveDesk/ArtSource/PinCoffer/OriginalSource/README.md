# DetectiveDesk – Precision 12-Slot Pin Coffer

This rebuild **does not use the AssetFurnace mesh**. It creates a new, editable 3D model with real topology and correct dimensions in Blender.

## Requirements
- Blender **4.2 LTS or newer** (Python included).
- No add-ons or internet access are required.

## Run
1. Extract this entire ZIP to a folder. Keep `build_coffer.py` and the three `.png` textures together.
2. Open Blender > **Scripting** > **Open** > choose `build_coffer.py`.
3. Click **Run Script**. **Warning: the script clears the currently open Blender scene.** Use a new empty Blender document and save existing work first.
4. Outputs are saved to `~/DetectivePinCofferOutput/` (on Windows typically `C:\Users\<you>\DetectivePinCofferOutput`). The Blender System Console will print the output path.
5. Open the created `.blend`; use **Material Preview** or **Rendered** shading. Studio lights/camera are in the `PREVIEW ONLY` collection and are not included in the GLB.

## Files created by Blender
- `Detective_Pin_Coffer_12Slots.blend` — editable master with original procedural material details, modeled hinge, brass hardware, exact sockets and preview lighting.
- `Detective_Pin_Coffer_12Slots.glb` — export with base color texture images for wood/velvet and PBR brass. Texture fidelity depends on Unity's glTF importer; Blender-only procedural bump is not baked into the GLB.
- `pin_slot_positions.csv` — 12 local Blender XYZ coordinates (metres), 1-indexed row-major.

## Geometry specification
- Chest external: **160 × 110 × 38 mm** (width × depth × base height).
- 12 sockets: **3 rows × 4 columns**, physically recessed 7 mm.
- Each brass rim: **16 mm outside / 11 mm inside** diameter.
- Recommended pin base: **9.9 mm max** (90% of opening), with further clearance testing in Unity.
- Lid pivots backward about the *rear* top edge at **108 degrees**.
- Coordinate system: Blender **X = left/right**, **Y = front/back** (front is −Y), **Z = up**.
- Each socket is composed of a true concave velvet cavity mesh plus a separate hollow annular brass rim. No surface spans the top opening.
- All 12 sockets are named `PinSlot_01` ... `PinSlot_12` and have matching `Brass | socket_ring_##` objects.

## Blender adjustment
Change dimensions in `build_coffer.py` at the top and regenerate. To animate the lid, rotate `LID_PIVOT | animate X rotation` about its local X axis. A setting of 0° corresponds to closed; −108° is its initial open pose.

## Unity 6 URP
- Import the GLB via a compatible glTF import package such as glTFast, or export FBX from Blender, together with texture images.
- Place the visual object under your existing `PinBox` gameplay root; retain the collider and scripts on the gameplay root.
- Map the named `PinSlot_##` nodes or use `pin_slot_positions.csv`; do not embed collectible pin models into the coffer mesh.
- Apply `Universal Render Pipeline/Lit` materials as appropriate, with smoothness/metallic settings adjusted for wood, brass and velvet.
- For best visual parity, bake Blender procedural bump/normal and roughness maps before final Unity import. The included color PNGs are an initial texture baseline, not scanned production-grade PBR maps.
- Generated glTF should retain per-part meshes and named nodes; test the output importer before replacing your existing prefab.

## Known limitations / QA
This package is **source-verified for Python syntax**, but Blender is not installed in this environment, so its runtime output and shading are **not yet visually validated** here. If Blender reports an API issue, send the console error. The result should be substantially cleaner geometrically than the AI mesh, but achieving the same appearance as a high-end cinematic render still depends on proper material baking, detailed textures and your Unity scene lighting.
