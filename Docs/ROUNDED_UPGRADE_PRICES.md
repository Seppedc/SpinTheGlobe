# Rounded upgrade prices — 2026-11-07

All 64 money upgrades now round each level's final price to the nearest EUR 5.
Examples: 11 → 10, 12 → 10, 13 → 15, 26 → 25, 89 → 90.
The ordinary whole-euro price is calculated first, then rounded. Positive
prices have a EUR 5 minimum; intentionally free prices remain zero.

The rounded amount is the actual purchase cost, used by affordability checks,
tooltips and bulk purchases. Bulk totals add the rounded price of each level.
The underlying growth curves remain intact. Gold shop and lootbox prices
retain their existing pricing rules. Upgrade effects and saved ranks remain
intact.

Examples of the first five actual purchase prices:

| Upgrade | Prices (EUR) |
|---|---|
| Auto Spin | 20, 30, 40, 50, 60 |
| Spin Limit | 25, 35, 45, 55, 65 |
| Spin Pay | 50, 100, 200, 280, 390 |
| Pin 1 Strength | 15, 25, 50, 90, 160 |

`CostCurve.roundingStep` stores the rounding policy. All money skills in the
active SkillTree asset and the default factory use 5; other curves keep the
previous behavior when the field is unset or 1. The Python economy model
reads the same field from the exported snapshot.

Changed files:

- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Economy/CostCurve.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/SkillTree/SkillTreeAsset.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/EconomyTests.cs`
- `Tools/economy_model.py`
- `Tools/economy_snapshot.json`
- `Docs/ECONOMY_REBALANCE.md`
- `Docs/ECONOMY.md`
- `Docs/ROUNDED_UPGRADE_PRICES.md`

No scene was modified or saved for this price change.

Validation executed:

- Seven Python economy tests passed, plus all ten requested/boundary rounding
  examples against the simulator.
- The actual `CostCurve.cs` compiled and passed a standalone .NET harness
  covering the examples, explicit prices, exponential growth, bulk sums,
  unchanged Gold pricing, old data without a rounding step and overflow.
  Unity attribute stubs were used only for that standalone harness.
- Ten NUnit test cases were added to the Unity EditMode suite. The Unity run
  was requested, but the editor connection remained unresponsive; a full
  Unity test pass and live UI validation are not claimed.
- `git diff --check` passed.
