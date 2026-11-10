# Release-based globe swipes — 2026-11-06

The player presses left click on the visible globe, drags forward, then releases
to apply one boost. Holding the button never adds drag velocity. The globe's
existing momentum and Auto Spin continue normally while a gesture is held.

Power uses net horizontal displacement from press to release, not the sum of
forward movements. Repeatedly moving back and forth cannot charge power. A
gesture ending at its starting point supplies neither a drag boost nor a click
if it previously moved outside the click tolerance. A released gesture can be
consumed only once.

The current balance uses:

- A minimum 32 reference pixels of net forward travel for a drag boost.
- Up to 200 reference pixels of distance contribution. Longer drags cannot
  increase the distance contribution further.
- Reference width 1920, with distance normalized to camera viewport width, so
  equivalent relative swipes work consistently in different window sizes.
- Average net pointer speed weighting, retaining the existing 900 reference
  pixels/second reference and 1.5 speed multiplier limit. Holding longer does
  not charge extra power.
- At most 50% of the current speed limit added by a single release, including
  Stronger Drag upgrades. A single swipe from rest cannot instantly max RPM.

Small click jitter still uses the authored click tolerance (normally eight
physical pixels) and gives the existing click impulse. Movements above that
tolerance but below the swipe minimum give no impulse. Reverse and vertical-only
drags give no drag boost. Stronger Drag increases qualifying swipe power up to
the per-gesture cap; it does not remove that cap.

The old unconditional minimum drag boost (`dragOverClick`) was removed. A tiny
drag therefore cannot get a large boost simply by being classified as a drag.
At the initial 15 RPM cap, a 200-reference-pixel swipe over 0.2 seconds gives up
to 7.5 RPM, while taking two seconds gives about 0.9 RPM. All boosts remain
subject to the overall globe speed limit and subsequent friction.

Opening a menu, losing focus, pausing/disabling the component or changing
viewport width cancels a held gesture. Releasing over UI also cancels it. A new
gesture must start on the globe, respecting the existing UI and visible-ball
hit tests. The opening hint now says to drag and release.

Changed files, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Scripts/Runtime/Core/GlobeDragGesture.cs` and `.meta`
- `Scripts/Runtime/Core/GameplayMath.cs`
- `Scripts/Runtime/Core/GameManager.cs`
- `Scripts/Runtime/Globe/GlobeController.cs`
- `Scripts/Runtime/Progression/GameBalance.cs`
- `Data/Balance/GameBalance.asset`
- `Tests/EditMode/GlobeDragGestureTests.cs` and `.meta`
- `Tests/EditMode/GlobeInputTests.cs`
- `Tests/EditMode/ProgressionTests.cs`

Other paths: `Tools/economy_snapshot.json`, `Docs/GAME_DESIGN.md`,
`Docs/ECONOMY.md` and this document.

No gameplay scene save, save reset, refund, upgrade price change or income
retuning is part of this input change. Saves retain their ranks. The economy
model assumes sustained activity and does not simulate actual swipe gestures;
mouse feel and resulting human income still need player testing.

Regression coverage includes net-distance/backtracking exploits, tiny fast
drags, no velocity while held, single release consumption, click classification,
cancellation/invalid input, per-gesture caps, equal-distance swipe speeds,
30/60/144 FPS input and 960/1920/3840 viewport widths. The existing motor,
friction and payout-travel checks are also retained.

Validation: 106/106 EditMode tests passed; evidence is in
`SpinTheGlobe/Logs/release-drag-editmode.xml`. Active balance fields were exported
with the new thresholds and cap; project scripts compiled without errors.
