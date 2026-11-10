# Less Friction replaces Stronger Drag

The Globe branch now offers **Less Friction**, with ten levels. Each level
multiplies the exponential speed-loss rate by 0.85 (15% slower decay). It no
longer increases swipe power. Normal dragging, click impulses and maximum
RPM rules are retained.

| Level | Speed-loss rate relative to level 0 | Time for excess speed to halve |
|---|---:|---:|
| 0 | 100% | 0.92 seconds |
| 1 | 85% | 1.08 seconds |
| 5 | 44.4% | 2.07 seconds |
| 10 | 19.7% | 4.66 seconds |

These times use the current `frictionAt60Hz` value. Without a motor, they apply
to total speed; with Auto Spin, they apply only to speed above its maintained
floor. Friction remains positive at level 10, so this is not free automation.
The controller still integrates decay independently of frame rate.

The node keeps the shipped `globe_drag` id and its position/prerequisite.
Existing Stronger Drag levels become the same number of Less Friction levels.
Existing prices are retained, extending the same growth curve to ten levels:
base EUR 12, growth 1.8, rounded to the nearest EUR 5. The tooltip shows
`Speed loss`, for example 100% → 85% for the first level.

The new stat `GlobeSpeedDecay = 107` has base 1. Existing stat ids, including
the legacy drag-power stat, are retained. Its base lives in GameBalance;
its upgrade modifier lives in SkillTree. The exported economy snapshot and
behavioral simulator use the same decay multiplier for both purchase planning
and credited rotation events.

Changed paths, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Data/Balance/GameBalance.asset`
- `Data/SkillTree/SkillTree.asset`
- `Scripts/Runtime/Progression/GameBalance.cs`
- `Scripts/Runtime/Progression/Stats/StatId.cs`
- `Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `Scripts/Runtime/Core/GameplayMath.cs`
- `Scripts/Runtime/Globe/GlobeController.cs`
- `Scripts/Runtime/UI/StatInfo.cs`
- `Tests/EditMode/GlobeInputTests.cs`
- `Tests/EditMode/ProgressionTests.cs`

Also changed: `Tools/economy_playtest.py`, `Tools/economy_snapshot.json`,
`Tools/test_economy_playtest.py`, `Docs/GAME_DESIGN.md`, `Docs/ECONOMY.md`
and this document. No gameplay scene or player save was modified.

The authored scene's node label is refreshed from the skill data when the
tree binds at runtime; scene layout does not need rebuilding.

Validation: 149/149 Unity EditMode tests and 8/8 Python economy tests passed.
The added tests cover slower decay, motor-floor preservation, no hidden
auto-rotation, frame-rate independence at 30/60/144 FPS, saved-rank carryover
and unchanged swipe power. Bulk-buy tests now cover the ten-level maximum.
Unity confirmed the compiled new stat and effective friction calculation.
Result: `SpinTheGlobe/Logs/less-friction-editmode.xml`.
`git diff --check` passed. Manual feel and first-retirement pacing have not
been remeasured for this upgrade.
