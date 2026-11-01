# DetectivePinCoffer integration

Imported the real authored `Assets/Objects/Pinbox.blend` with Blender 5.2.3 LTS,
exported it to `Assets/DetectiveDesk/PinCoffer/DetectivePinCoffer.fbx`, and created
`Assets/DetectiveDesk/PinCoffer/DetectivePinCoffer.prefab`.

The Blender source file was not modified. Its seven separate wood, brass, velvet
and steel material assets already use URP/Lit and remain linked unchanged.
The 74 authored mesh objects total 19,168 triangles. Derived Unity-axis meshes
are stored under `Assets/DetectiveDesk/PinCoffer/Meshes/`. Re-exporting the FBX
alone does not regenerate those derived mesh snapshots.

## Saved placement

- World position: `(1.63, 0.815, -0.14)`.
- World rotation: `(0, 0, 0)` degrees.
- Proportional root scale: `(6.8, 6.8, 6.8)`.
- Base is exactly at the current desk top; latch faces the camera/negative Z.
- Lid is open toward the back wall/positive Z.
- Bounds clear the globe, toy launcher and case files. The coffer is right of
  the globe and its body is in front of the launcher.

The removable velvet insert, socket rings and floor guides were raised together
by 0.011 native model units (0.0748 scene units) so all socket openings are visible
from the unchanged seated Game camera. No nonuniform scaling was used.

## Hierarchy and gameplay

The existing pin-box GameObject and `DeskInteractable` component were retained;
the GameObject is now named `DetectivePinCoffer`. Its original game reference
and PinBox action are unchanged. The model is organized under `Body`, `Lid`,
`VelvetInsert` and `PinSlots`.

`PinSlot_01` through `PinSlot_12` reuse the authored socket guide positions, with
three rows and four columns. Row 1 is the back row; numbering proceeds left to
right from the player's view. Each receiver references
`Assets/DetectiveDesk/Prefabs/CollectiblePin.prefab` and starts empty.

New script: `Assets/DetectiveDesk/Scripts/Runtime/PinCofferSlot.cs`.

```csharp
PinSkin rubyPin = socket.PlacePin(0);
PinSkin sapphirePin = anotherSocket.PlacePin(1);
PinSkin releasedPin = socket.ReleasePin(newParent);
```

The prefab's native orientation and socket-compatible scale are retained during
insertion; each instance receives its own skin. `Selected` can be subscribed to
for future inventory UI. Clicking a socket routes to the original SelectPin HUD
action. Six solid body/perimeter/bottom/lid colliders remain on the gameplay
parent; twelve small trigger colliders mark the socket openings. The old broad
box collider is disabled so it cannot cover all twelve sockets. Previous visuals
remain inactive for rollback; only one active coffer interaction exists.

## Backups and validation

- Scene backup: `Assets/DetectiveDesk/Generated/DetectiveOffice_PreDetectivePinCoffer.unity`.
- Pin-box prefab snapshot: `Assets/DetectiveDesk/Prefabs/Backups/PinBox_BeforeDetectivePinCoffer.prefab`.
- Saved scene: `Assets/DetectiveDesk/Generated/DetectiveOffice.unity`.
- Screenshot: `DetectivePinCoffer_GameCamera.png` in this artifact folder.
- Runtime results: `RuntimeValidation.txt`.

All twelve openings passed nine geometric visibility samples each from the current
Game camera. All twelve trigger sight lines clear the solid interaction colliders.
Runtime tests verified independent Ruby/Sapphire insertion, release and the original
selection callback. Final Console error count: zero. The entire protected scene
component snapshot (including camera, globe, map, launcher, lamp, files, stamp and
HUD) matches its pre-edit state.

Remaining project issue: Unity reports an empty default physics scene even with
enabled colliders. Native mouse-hit testing was unavailable; global physics settings
were left unchanged. Callback, placement and geometric collider checks passed.

An early editor-only visibility check attempted direct reads of optimized mesh
buffers. It was corrected to use `MeshUtility.AcquireReadOnlyMeshData`; the resolved
diagnostics were archived in `EditorValidationDiagnostics.json` before a clean
final verification. No runtime mesh-read code or mesh import flag change was added.
