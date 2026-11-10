# Pin Strength colours

All pins use the same Strength-based colour progression. Level 0 is slate;
levels 1-19 move through blue, teal, green, purple, red and copper shades;
level 20 is metallic gold. Each level has a distinct colour. Colour follows
the purchased Strength level, rather than slot number or remaining turns.

The same colour is applied in the coffer, during flight, on the wall map and
on the globe. Buying Strength refreshes existing coffer, airborne, map and
globe visuals immediately. Loading a save applies its saved Strength ranks
when the pins are created. The archive pin swatches and labels reflect the
Strength appearance as well.

`GameBalance.asset` stores the 21 colours and enamel/gold metallic values.
`PinSkin` uses a material property block on the enamel slot only. Shared
materials, pin meshes, needle materials and saved skill ids are preserved.
Globe marker emission follows the Strength colour. No new model or texture
was required; no gameplay scene was saved.

Changed files, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Data/Balance/GameBalance.asset`
- `Scripts/Runtime/Progression/GameBalance.cs`
- `Scripts/Runtime/Core/GameManager.cs`
- `Scripts/Runtime/Pins/PinSkin.cs`
- `Scripts/Runtime/Pins/PinBox.cs`
- `Scripts/Runtime/Pins/WallMapManager.cs`
- `Scripts/Runtime/Globe/GlobePinMarker.cs`
- `Scripts/Runtime/Interaction/DeskAssetPresentation.cs`
- `Tests/EditMode/ScenePresentationTests.cs`

Documentation changed: this file and `Docs/GAME_DESIGN.md`.

Validation: 133/133 EditMode tests passed. The two added tests check equal
Strength across different slots, all 21 distinct colours, gold at maximum,
renderer colour/metallic/emission overrides, preservation of shared material
colours and refresh of an existing coffer pin after a Strength purchase.
Results: `SpinTheGlobe/Logs/pin-strength-colours-editmode.xml`.
`git diff --check` passed.

The actual pin model was rendered and visually inspected in an isolated
Unity preview: `SpinTheGlobe/Logs/pin-strength-colours-preview.png`.
Full gameplay lighting and rapidly firing pins were not manually playtested.
Upgrade prices, Strength effects and first-retirement pacing are unchanged.
