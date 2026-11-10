# First-use globe guidance

## Recurring ready-case stamp cue (2026-11-08)

The complete stamp uses the same white surface pulse whenever a case is
claimable: Case Files is unlocked, enough evidence has been collected, the
case is not cooling down and stamping has not started. The cue stops as soon
as a manual or automatic stamp begins. It returns for every new completed
case; this is not a once-per-player tutorial and has no saved completion flag.

`CaseFileSystem.CanClaim` is shared by the manual claim action and the cue.
FirstUseGuidance resolves the stamp from `CaseFileSystem.StampTransform` and
uses the existing GameBalance overlay material, pulse period and opacity.
As with the other prompts, it is hidden behind menus and when unfocused.

Changed files: `Scripts/Runtime/Cases/CaseFileSystem.cs`,
`Scripts/Runtime/Guidance/FirstUseGuidance.cs` and
`Tests/EditMode/CaseUnlockTests.cs`, relative to
`SpinTheGlobe/Assets/DetectiveDesk/`; plus this document and
`Docs/GAME_DESIGN.md`. No scene, model, prefab, balance asset or save changed.

Validation: 158/158 EditMode tests passed, including locked/full/cooldown
readiness and repeated completion across different cases.
Result: `SpinTheGlobe/Logs/ready-case-stamp-overlay-editmode.xml`.
The stamp's model hierarchy was inspected and `git diff --check` passed.
The new cue still needs a manual Play-mode visual check.

## Pin introduction (2026-11-08)

Buying Pin 1 for the first time leaves the coffer closed until the player
closes the skill tree and can see the desk. Its existing opening animation
then runs. Hiding the desk again, or switching to another application, pauses
this first opening so it cannot be missed. Other background gameplay continues.

After the opening completes, the entire `PinLauncher` model pulses with the
same white overlay, period and opacity as the globe introduction. The pulse
stops after the first successful pin launch, including Auto Fire. Failed shots,
reloading and clicks that cannot launch do not complete it. Pulses are hidden
behind menus and while the application is unfocused.

Lifetime completion uses the existing saved guidance-id list:
`pin_intro_started`, `pin_box_opened` and `pin_fire`. The start marker is recorded
when the first pin is purchased, preserving an unfinished introduction if the
player saves before closing the tree. These ids survive retirement. Older
saves that already own Pin 1 or have retired skip the pin introduction; old
saves without pins still receive it when they purchase their first pin.

The shooter target is optionally assignable in FirstUseGuidance and otherwise
resolves from the launcher's recoil pivot to its complete `PinLauncher` model.
No scene, source model, prefab, balance asset or player save was modified.

Changed paths, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Scripts/Runtime/Guidance/PlayerGuidanceState.cs`
- `Scripts/Runtime/Guidance/FirstUseGuidance.cs`
- `Scripts/Runtime/Core/GameManager.cs`
- `Scripts/Runtime/Pins/PinBox.cs`
- `Scripts/Runtime/Pins/WallMapManager.cs`
- `Tests/EditMode/FirstUseGuidanceTests.cs`
- `Tests/EditMode/ScenePresentationTests.cs`

Documentation changed: this file and `Docs/GAME_DESIGN.md`.
Validation: 156/156 EditMode tests passed; result
`SpinTheGlobe/Logs/pin-introduction-editmode.xml`. New checks cover hidden and
visible lid animation, pause/resume, prompt order, failed firing, whole-model
target resolution, saved pending introductions, returning players and
retirement persistence. `git diff --check` passed. The actual shooter hierarchy
was inspected; a manual Play-mode visual check of the new prompt is still due.

## Original globe implementation

A fresh player sees a smooth white surface pulse over the rotating globe sphere.
The full visible surface is covered, including the shell and continents. Its
stand, map and other desk props are excluded. The cycle lasts 2.8 seconds and
peaks at 75% white opacity. The cosine fade begins and ends transparent, rather
than abruptly flashing. The prompt pauses while a menu blocks world input.

The first valid click or released drag that adds spin completes `globe_spin`.
Cancelled, reverse and too-short drag gestures do not dismiss the prompt; motor
rotation does not count as player input. Completion is included in ordinary
save writes and retirement saves, so it survives reopening and prestige.
Quitting before using the globe leaves the prompt available next time.
Existing saves with money earned, purchased skills or retirement progress are
recognized as returning players and skip the globe introduction.

`SaveData.completedGuidance` is an additive list of stable ids; the save version
stays 1 because existing fields keep their meaning. Unknown completed prompt ids
are retained for future feature guidance. The current feature only prompts the
globe; no folder, shooter or other object prompts were added yet.

`ObjectPulseOverlay` reuses existing mesh references and current renderer
transforms. Its transparent URP unlit pass uses normal depth occlusion and no
shadow casting. It does not replace source materials, touch their property
blocks, create collision geometry or modify the globe model. No per-frame
materials are instantiated. The material is referenced by the serialized
GameBalance asset so the shader is included in builds.

The main scene has a FirstUseGuidance component with the rotating sphere target
assigned. Material, cycle duration and opacity are in GameBalance. The effect
is conditional runtime guidance; the editor globe remains normally shaded.
Native-camera previews were rendered for review with temporary cameras that
were removed afterward:

- `SpinTheGlobe/Logs/globe-guidance-normal.png`
- `SpinTheGlobe/Logs/globe-guidance-peak.png`

Validation actually executed:

- The custom shader imported and compiled without errors.
- Both native Unity camera renders were visually inspected. The peak render
  covers the sphere, respects its silhouette, and leaves other objects alone.
- 128/128 EditMode tests passed: `Logs/first-use-guidance-editmode.xml`.
- Tests cover new/untouched saves, existing-save compatibility, completion
  persistence through JSON/file saves and retirement state, duplicate completion,
  pulse phases, valid click/drag input and ineffective/cancelled gestures.
- The corrupt-save warning from SaveTests is an expected test fixture.
- No real player save was deleted, reset or manually edited. No Blender assets,
  price curves or gameplay effects changed.

A full real-time new-player session was not recorded; validation combines actual
rendered surface passes with input/state/persistence tests.

Changed paths, relative to SpinTheGlobe/Assets/DetectiveDesk/:

- Scripts/Runtime/Guidance/PlayerGuidanceState.cs and .meta
- Scripts/Runtime/Guidance/ObjectPulseOverlay.cs and .meta
- Scripts/Runtime/Guidance/FirstUseGuidance.cs and .meta, plus Guidance.meta
- Scripts/Runtime/Core/GameManager.cs
- Scripts/Runtime/Globe/GlobeController.cs
- Scripts/Runtime/Progression/GameBalance.cs
- Scripts/Runtime/Save/SaveData.cs
- Art/UI/Shaders/WhiteGuidanceOverlay.shader and .meta, plus Shaders.meta
- Art/UI/Materials/WhiteGuidanceOverlay.mat and .meta, plus Materials.meta
- Data/Balance/GameBalance.asset
- Scenes/DetectiveOffice.unity
- Tests/EditMode/FirstUseGuidanceTests.cs and .meta
- Tests/EditMode/SaveTests.cs

Other changes: Tools/economy_snapshot.json, Docs/GAME_DESIGN.md,
Docs/ARCHITECTURE.md and this document.
