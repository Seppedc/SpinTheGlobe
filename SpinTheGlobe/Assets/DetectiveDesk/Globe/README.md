# Modular mechanical globe

Prefab: `Prefabs/MechanicalGlobe_Modular.prefab`.

The existing scene instance preserves its original root transform, overall bounds,
`RotatingSphere` transform, parent BoxCollider and `DeskInteractable` component.
The scene's `DetectiveGame` still owns sphere rotation, click hints and the nearby
amber light pulse. Its speed is degrees per second; the analog dial displays
absolute degrees per second divided by six as RPM, on a 0–5 RPM scale.

## Replaceable modules

- `01_Pedestal_Module`: turned brass pedestal and molding.
- `02_Meridian_Module`: full brass meridian with twelve fasteners; ring mesh is
  centered at the sphere and tilted 30 degrees around local Y.
- `RotatingSphere/03_SphereSurface_Module`: sphere surface and cartography. Preserve
  `RotatingSphere` itself when replacing the visual mesh.
- `04_ExposedClockwork_Module`: 12-, 16- and 14-tooth gears, spokes and hubs. Preserve
  the `GearPivot_*` transforms; gear meshes rotate around local Z.
- `05_EmissiveAccents_Module`: restrained warm inlays. No additional point lights.
- `06_RPMGauge_Module`: housing, bezel, circular printed dial and live needle.
  Preserve `RPM_NeedlePivot`; local positive Y is the needle's forward direction.

Visual leaves named `SM_*_REPLACE_MESH` are intended for later Blender mesh swaps.
Replace their MeshFilter mesh and keep module/pivot transforms and script bindings.
Author gear and meridian geometry in local XY, with thickness along Z; author the
turned pedestal along local Y. Keep the original world bounds and update the
presentation component's renderer array if renderer objects are replaced.

## Runtime behavior

`DetectiveGlobeMechanism` animates gear ratios, the RPM needle and a subtle reversible
emission increase on hover. It restores resting emission after the legacy
interactable's hover events, so warm accents remain after the pointer leaves.
No outline geometry, outline shader or permanent outline is used. The scene's
sphere rotation is not duplicated. An unbound standalone prefab spins at the
configured fallback speed of 7 degrees per second. Assign `DeskInteractable.game`
when placing the prefab in another gameplay scene; external scene references are
intentionally instance overrides, not prefab dependencies.

All generated meshes are stored in `Meshes/`; shared URP/Lit materials are in
`Materials/`. The prefab uses one root collider and approximately 22,600 triangles.
No unrelated desk material assets or gameplay scripts were edited.

Cartography land outlines use the public-domain Natural Earth 1:110m land dataset.
The sphere texture and high-contrast analog dial were drawn procedurally. The dial
is lightly self-illuminated for readability in the existing noir lighting.
