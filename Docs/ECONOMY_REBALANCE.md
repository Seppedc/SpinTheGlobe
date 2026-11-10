# Economy price pass and bulk skill purchases - 2026-11-06

Latest retirement update (2026-11-07): the requirement is now EUR 15,000
earned per run, down from EUR 550,000. The same gold formula awards 25 gold
at the new threshold and 50 gold at EUR 60,000. Existing run earnings count;
no save reset or wallet payment is required. No new timing estimate is claimed.

Files changed for this threshold update: `Scripts/Runtime/Progression/GameBalance.cs`
and `Data/Balance/GameBalance.asset` under `SpinTheGlobe/Assets/DetectiveDesk/`,
`Tools/economy_snapshot.json`, `Docs/GAME_DESIGN.md`, `Docs/ECONOMY.md` and this
document. No scene was modified. Validation confirmed all three data sources
use 15,000; seven Python economy tests and `git diff --check` passed. The
EditMode test request was blocked by the unresponsive Unity connection.

Implemented first balance pass. No player save was reset. Existing ranks and
all shipped skill ids remain intact. All 64 nodes retain their titles, effects,
prerequisites, positions and unlock requirements. Pin Strength/Pay rank limits were subsequently increased to 20. Balance fields
other than the retirement threshold are unchanged, including starting money,
spin/pin/case payouts, drag behavior, speed limits and reload timing.

## Current price curves

A later general reduction applies 5% off all money upgrade curves before
whole-euro and nearest-EUR-5 rounding. Earlier formulas and example prices
below predate that reduction unless explicitly stated otherwise. Small prices
can remain unchanged after rounding. Gold prices and lootbox prices are unchanged.

All money upgrade prices now round to the nearest EUR 5 **after** evaluating
these underlying curves (positive minimum EUR 5). Tables below describe the
curves before this final rounding. See [ROUNDED_UPGRADE_PRICES.md](ROUNDED_UPGRADE_PRICES.md)
for actual opening prices and the implementation/validation report.

Money prices round up to whole euros. Auto Spin and Spin Limit rise by EUR 10
per level for the first five purchases, then use an accelerating price ramp
stored as explicit per-level prices in the active SkillTree.asset.

| Purchase level | Auto Spin | Spin Limit |
|---|---:|---:|
| 1 | 22 | 24 |
| 2 | 32 | 34 |
| 3 | 42 | 44 |
| 5 | 62 | 64 |
| 6 | 73 | 77 |
| 10 | 125 | 139 |
| 20 | 550 | 649 |
| 50 | 9,625 | 11,539 |
| 80 | 43,000 | 51,589 |
| Final (100 / 101) | 86,750 | 107,385 |

Auto Spin retains 100 ranks of +5 RPM and Spin Limit 101 ranks of +7.5 RPM.
Later absolute price increases grow; time between purchases still depends on
the player's income and build.

| Upgrade | Current base / opening | Later growth |
|---|---|---|
| Spin Pay | 50, 100, 200 | x1.4 from level 3; final 327,980 |
| Double Spin Pay | 5,000 | One purchase |
| Faster Reload | 10, 15, 23, 34, 51, 76 | ceil(76 x 1.15^(ownedRank - 5)); final 8,802 |
| Auto Fire | 350 | One purchase; existing Reload prerequisite retained |
| Case Pay | 60 | x1.5 |
| Case Wait | 60 | x1.5 |
| Auto Stamp | 150 | x1.5 |
| Less Evidence | 500 | x2 |
| Case Gold | 500 | x1.6; existing Gold-shop prerequisite retained |
| Cheaper Boxes | 200 | x1.7 |
| Better Loot | 350 | x1.7 |
| Longer Boost | 70 | x1.45 |
| Drink Wait | 85 | x1.45 |
| Stronger Boost | 300 | x1.8 |

Pin 1 costs 10. Pins 2-12 cost 1.5 x ceil(15 x 1.7^(pinNumber - 1)),
then round to the nearest EUR 5. Pin 12 costs 7,710. Individual upgrade prices are independent of the pin unlock price:

- Strength base: 15 x 1.5^(pinNumber - 1); x1.8 price growth through level 10, then x1.25 for levels 11-20.
- Pay base: 15 x 1.5^(pinNumber - 1); x1.6 through level 10, then x1.15 for levels 11-20.

Pin 12's final Strength rank costs 2,396,889; its final Pay rank remains 360,706.
This gives later pin improvements meaningful prices without the old million-
and billion-euro costs. All 40 Reload ranks cost 67,117 together. Statues,
manual input upgrades and optional mastery prices are unchanged.

## Bulk purchases

Buy 1 / Buy 5 / Buy 10 controls are authored at the bottom-right of the tree.
The default is Buy 1. Selecting an amount buys UP TO that many currently
purchasable levels, stopping at funds, max rank or the permanent speed cap.
Single-rank unlocks still buy once. Prerequisites and Gold-shop locks apply.
The hover card previews the actual count, total price and resulting stat.
An unaffordable first level still shows its ordinary single-level price.

SkillTreeState quotes the sum without changing state, then commits one wallet
transaction and one final-rank notification. This avoids repeated sounds and
UI notifications for each level in the batch. Save format and skill ids do not
change. Existing scenes without the new controls have a builder fallback; the
main scene already contains the controls for editor visibility.

## Retirement and measured simulation results

Retirement becomes available at 15,000 total money earned this run. It does
not charge an additional wallet fee. The existing gold formula now gives 25
gold at 15,000 and 50 at 60,000. Players can continue instead of retiring.
Ordinary skills including the expanded 20-level pin tracks cost 19,034,935 to fully purchase, excluding mastery and
Gold-shop-gated skills. There are 840 ordinary ranks across 55 nodes. The added pin ranks provide longer-term progression; the earlier near-complete-first-retirement estimate predates this extension.

The latest experiment used four spending styles, two input presets, three
seeds each, 30-second attention breaks every three minutes, and bulk buying
up to ten affordable levels. Every style unlocked all 12 pins before retirement.
These are historical synthetic results from before the later +20% globe / -25% pin-price adjustment, not current human playtest timings:

| Spending style | Mouse input | Retirement available | Ordinary ranks bought |
|---|---|---:|---:|
| mixed | active | 40.9-41.1 min | 517-517/540 |
| mixed | steady | 48.9-49.0 min | 522-522/540 |
| pins | active | 69.9-70.9 min | 517-518/540 |
| pins | steady | 72.9-75.2 min | 521-522/540 |
| spin | active | 42.9-42.9 min | 518-518/540 |
| spin | steady | 50.1-50.2 min | 521-522/540 |
| automation | active | 54.1-54.8 min | 517-517/540 |
| automation | steady | 62.9-63.0 min | 522-522/540 |

The active mixed/spin results are close to, but slightly above, the earlier
30-40 minute target. Pin-first and automation-first policies take materially
longer. Their behavior and input assumptions need real-player calibration;
this pass does not establish equal completion times across strategies or the
five-hour overall progression goal. The simulator excludes loot spending,
Gold shop purchases and prestige resets. Its timeline continues after first
retirement availability so continued earning can be inspected.

The planner now considers the input preset's actual repeating-swipe behavior,
friction and both boost phases rather than assuming a fixed speed fraction.
It saves for chosen purchases, values convenience and the future upgrades a
new pin unlocks, and excludes optional mastery from first-run purchasing.
The credited earnings still come from individual simulated payout events.

Report: `SpinTheGlobe/Logs/economy-rebalanced-final/report.html`.
Data: `results.json`, `purchases.csv`, and immutable `snapshot-used.json` in the
same directory. UI preview: `SpinTheGlobe/Logs/economy-bulk-preview.png`.

## Validation

- 116/116 Unity EditMode tests passed: `Logs/economy-bulk-editmode.xml`.
- 7/7 Python simulator tests passed.
- 24 final 90-minute synthetic runs executed against the exported active data.
- Layout checker: 64 skills, one free heading, no overlaps or links through nodes.
- Native Unity canvas render inspected; controls fit the footer. Temporary render
  objects were removed and the scene restored to Desk view.
- Compared all non-price node fields against the pre-change data, and all balance
  fields except retirement threshold: no changes.
- Final diff whitespace check passed. The test runner's corrupt-save warning is
  an expected SaveTests fixture; result XML reports all tests passed.

New tests cover cumulative price totals, buying fewer levels when funds run
out, stopping at permanent motor caps/max rank, retaining levels in saves,
rejecting locked or zero-quantity purchases, final-rank notification count,
authored-control reuse/rebinding, and tooltip/transaction agreement.

No real-time fresh-save human session has been timed. The next useful test is
playing a new run without developer money grants and comparing purchases and
60-second income to the report. Existing player progress was not reset.

## Changed paths in this implementation

Relative to SpinTheGlobe/Assets/DetectiveDesk/:

- Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs
- Scripts/Runtime/Progression/SkillTree/SkillTreeState.cs
- Scripts/Runtime/Progression/GameBalance.cs
- Scripts/Runtime/Core/GameManager.cs
- Scripts/Runtime/UI/SkillTreeView.cs
- Scripts/Runtime/UI/SkillTreeView.Purchases.cs and .meta
- Data/SkillTree/SkillTree.asset
- Data/Balance/GameBalance.asset
- Scenes/DetectiveOffice.unity
- Art/UI/ScenePresentationResources.asset
- Tests/EditMode/ProgressionTests.cs
- Tests/EditMode/ScenePresentationTests.cs

Other paths: Tools/economy_playtest.py, Tools/economy_snapshot.json,
Docs/ECONOMY.md, Docs/GAME_DESIGN.md, Docs/ARCHITECTURE.md,
Docs/SCENE_PRESENTATION.md, Docs/ECONOMY_TESTING.md,
Docs/RELOAD_OPENING.md, Docs/SHOOTER_UPGRADES.md,
Docs/UPGRADE_PRICE_REVIEW.md and this document.


## Follow-up: slightly dearer spin upgrades, cheaper additional pins

Auto Spin and Spin Limit prices increased by 20%, rounded up. First purchases
now cost EUR 22 and EUR 24; both final purchases cost EUR 12,000. Pin 1 stays
EUR 10. Pin 2 onward have a 25% lower base price: the next three pins cost
EUR 26, 44 and 74; Pin 12 costs EUR 5,141. Their Strength and Pay curves are
independent and retained. Effects, payouts, maximum speed and the retirement
threshold are retained. No scene or player save was edited in this follow-up.

Changed: DefaultSkillTreeFactory.cs, active Data/SkillTree/SkillTree.asset,
Tools/economy_snapshot.json, Docs/ECONOMY.md and this document.

Validation: all 13 selected asset curves agree with the exported snapshot;
other skill fields were compared and retained; all seven Python simulator
tests passed; the layout checker passed. Unity's editor connection was
unresponsive, so an EditMode test request could not execute. The previous
128-test result predates this price-only adjustment and is not a new pass.
The timing table above has not been rerun for these prices.


## Follow-up: Pin Pay pricing and clearer Strength text

All 12 Pin Pay upgrades now use the same base price and 1.6 growth as the
corresponding Strength upgrade. Pin 1's first five purchases cost EUR 15, 24,
39, 62 and 99 in either branch. Both tracks now have 20 ranks following the subsequent extension below.
Both price schedules grow by 1.6 through rank 10, then by 1.15 for ranks 11-20. Strength prices were not
changed in this pass. Effects, pin unlock prices and retirement threshold are
retained; the earlier retirement-timing table does not reflect this change.

The Strength tooltip stat label is now simply `Strength`, replacing
`Payouts per pin`. Its description is `Lasts one extra turn.` Pin-numbered
Strength titles and saved skill ids are retained. Pay still describes earnings.

Changed files: Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs,
Scripts/Runtime/UI/StatInfo.cs and Data/SkillTree/SkillTree.asset under the
DetectiveDesk asset folder; Tools/economy_snapshot.json and this document.

Validation: verified both curves for all 12 pins agree between the active asset
and snapshot, all descriptions updated, rank limits retained and unrelated
fields unchanged. Seven Python simulator tests and the layout checker passed.
Unity's editor connection remained unresponsive, so the EditMode test request
could not execute; no new Unity test pass is claimed. No scene or save reset.


## Follow-up: cheaper Case Wait and 20-level pin tracks

Case Wait prices are reduced 20% before whole-euro rounding: base EUR 75 to
EUR 60, retaining 1.5 price growth and the same cooldown reduction. Its first
five purchases cost EUR 60, 90, 135, 203 and 304.

All 12 pins now support 20 Strength levels and 20 Pay levels. Existing earned
levels remain owned. Per-level effects stay +1 pass for Strength and +50%
additive Pay. Fully upgraded values are therefore 21 passes and 11x base Pay
without other modifiers. Pin unlock count remains 12; this extends upgrade
levels rather than adding pin slots.

Existing Strength prices for levels 1-5 and Pay prices for levels 1-10 are
preserved. Both tracks share 20 explicit prices. For a pin's existing base B,
prices are ceil(B x 1.6^r) for zero-based ranks r=0..9 and
ceil(B x 1.6^9 x 1.15^(r-9)) for r=10..19. This gives the new levels gentler
price increases. Pin 1's level 20 costs EUR 4,171. The expanded tracks change
long-term completion and income potential, so earlier retirement timings and
completion percentages are historical, not verified for these new limits.

Changed files: DefaultSkillTreeFactory.cs, Data/SkillTree/SkillTree.asset and
Tests/EditMode/ProgressionTests.cs under the DetectiveDesk asset folder;
Tools/economy_snapshot.json, Docs/GAME_DESIGN.md, Docs/ECONOMY.md and this file.

Validation: checked all 24 20-level tracks and explicit price lists against the
active asset, strict price progression, preserved early prices, matching
Strength/Pay curves, and the Case Wait base price. Seven Python simulator tests
and the layout checker passed. Added a Unity regression test for 20-level
limits, higher-rank save restoration/clamping and resulting stat values.
Unity's connection initially delayed the test request, then recovered.
129/129 EditMode tests passed (`Logs/case-wait-pin-levels-editmode.xml`).
An additional read inside Unity confirmed the imported asset has Case Wait at
EUR 60, both pin tracks at 20 levels, and Pin 1's level-20 price at EUR 4,171.
No new completion-time estimate is claimed.
No scene or player save was edited or reset.


## Follow-up: steeper Spin Pay prices

Spin Pay still starts at EUR 30, with the same 25 ranks and +25% additive spin
earnings per rank. Its price growth is now 1.4 instead of 1.25: each subsequent
level costs 40% more before rounding. The first five levels cost EUR 30, 42, 59,
83 and 116. Level 25 costs EUR 96,426, compared with EUR 6,353 previously.
Double Spin Pay, pin payouts and all other upgrade curves are retained.

Changed files: Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs
and Data/SkillTree/SkillTree.asset in the DetectiveDesk asset folder;
Tools/economy_snapshot.json and this document. No scene or player save reset.

Validation: active asset and exported snapshot agree for Spin Pay's base and
growth; its modifier and all unrelated skill fields were checked and retained.
Seven Python simulator tests and the layout checker passed. Unity test result: 131/131 passed, 0 failed (`Logs/spin-pay-growth-editmode.xml`).
First-retirement pacing has not been rerun for this price change; the previous
timing tables are historical.


## Follow-up: double additional-pin unlock prices

Pin 2 onward now cost exactly twice their previous displayed prices. Pin 1
stays EUR 10. Pins 2, 3 and 4 now cost EUR 52, 88 and 148 respectively;
Pin 12 costs EUR 10,282. Factory defaults use 2 x ceil(15 x 1.7^(pinNumber - 1))
for pins after the first, preserving the exact whole-euro doubling. Strength,
Pay, reload, payouts, pin count, limits and all other upgrade fields are retained.

Changed: DefaultSkillTreeFactory.cs and Data/SkillTree/SkillTree.asset under
the DetectiveDesk asset folder, Tools/economy_snapshot.json and this document.
Validation: checked all 11 prices against the active asset and snapshot; Pin 1
and unrelated fields retained. Unity readback confirmed Pin 1 at 10, Pin 2 at 52
and Pin 12 at 10,282. 131/131 EditMode tests passed
(`Logs/double-pin-unlocks-editmode.xml`), along with seven Python simulator tests
and the layout checker. The SaveTests corrupt-file warning is an expected
fixture. No scene or player save was edited/reset. No new retirement-time
estimate is claimed for this price adjustment.


## Follow-up: accelerating Auto Spin and Spin Limit prices

The first five displayed prices are retained exactly. Beyond those, the
absolute price increases grow much faster. For purchase level L > 5, t = L - 5:

- Auto Spin: ceil(24 + 5t + 0.1t^3).
- Spin Limit: ceil(28 + 6t + 0.12t^3).

Explicit whole-euro prices remove ambiguity in the opening and match the active
asset. Level 20 costs EUR 437 / 523; level 50 costs EUR 9,362 / 11,233. Final
prices are EUR 86,237 / 106,773. Both curves are strictly increasing after the
preserved opening. Auto Spin keeps 100 ranks and +5 RPM per level; Spin Limit
keeps 101 ranks and +7.5 RPM per level. The maximum RPM, payouts, prerequisites,
pin prices, retirement threshold and existing owned levels are retained.

Changed: DefaultSkillTreeFactory.cs and Data/SkillTree/SkillTree.asset under the
DetectiveDesk asset folder; Tools/economy_snapshot.json, Docs/ECONOMY.md and this
document. No scene or player save was edited/reset.

Validation: verified all 201 explicit prices agree between the active asset
and snapshot, the first five prices are preserved, and per-level RPM effects
are unchanged. Compared unrelated node fields during the edit. Unity readback
confirmed level 20 and 50 prices for both skills. 131/131 EditMode tests passed
(`Logs/accelerating-spin-prices-editmode.xml`), as did seven Python simulator
tests and the layout checker. The corrupt-save warning is the expected SaveTests
fixture. No new first-retirement timing is claimed for this steeper price pass.


## Follow-up: skill-tree access costs EUR 10

Unlock Upgrades now costs EUR 10 instead of EUR 20. Only the root price changes;
its one-rank limit, dependencies and all other upgrades are retained. Existing
ownership is preserved. The opening hint reads the price from the active node.

Changed files: DefaultSkillTreeFactory.cs and Data/SkillTree/SkillTree.asset in
the DetectiveDesk asset folder; Tools/economy_snapshot.json,
Tools/test_economy_playtest.py, Docs/ECONOMY.md and this document. The existing
simulator saving test now uses the actual root price instead of hard-coded
19/20 euro balances.

Validation: Unity readback confirmed EUR 10, and the saved active asset and
snapshot agree. All unrelated skill fields were checked and retained. Seven
Python simulator tests and the layout checker passed. The EditMode test request
was skipped because Unity was in Play mode; no new Unity test pass is claimed.
No scene or player save was reset or manually edited.

## Follow-up: stronger opening price steps (2026-11-07)

Spin Pay starts at EUR 50, 100, 200, then rises by 40% per level. Auto Spin
starts at EUR 22, 32, 42, 52, 62; Spin Limit at EUR 24, 34, 44, 54, 64.
After these five levels, with t = purchase level - 5:

- Auto Spin: ceil(62 + 10t + 0.1t^3).
- Spin Limit: ceil(64 + 12t + 0.12t^3).

Changed files: the SkillTree.asset cost curves, DefaultSkillTreeFactory.cs,
Tools/economy_snapshot.json, Docs/ECONOMY.md and this document. Effects, rank
limits and existing save ranks are retained. No scene was saved. This is a
price pass; first-retirement timing has not been remeasured.

Validation: Unity readback confirmed the three opening price sequences;
131/131 EditMode tests and 7/7 Python economy tests passed. EditMode evidence:
`SpinTheGlobe/Logs/opening-prices-editmode.xml`. The motor-cap test in
`SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/ProgressionTests.cs` retains
its cap assertions; its obsolete cheap-price-ratio assertion was removed.
`git diff --check` passed.

## Follow-up: steeper Pin Strength price growth (2026-11-07)

All 12 Strength tracks retain their starting prices, 20 levels and +1 turn
per level. Growth increases from 60% to 80% through level 10 and from 15%
to 25% for levels 11-20. Pin 1 starts at EUR 15, 27, 49, 88, 158.
Pin Pay prices are unchanged. Existing save ranks remain owned.

Changed files: SkillTree.asset, DefaultSkillTreeFactory.cs,
Tools/economy_snapshot.json, Docs/GAME_DESIGN.md and this document.
First-retirement pacing has not been remeasured.

Validation: all 12 active Strength price curves matched the default factory
in Unity. The data update verified unchanged starting prices, increased later
prices and no changes outside Strength costs. 131/131 EditMode tests passed
(`SpinTheGlobe/Logs/strength-prices-editmode.xml`); 7/7 Python economy tests
and `git diff --check` passed. No gameplay scene was saved.

## Follow-up: cheaper new pins

Pin unlocks 2-12 are 25% cheaper before the final EUR 5 rounding. Pin 1
remains EUR 10. Strength, Pay, prerequisites and other effects are unchanged.
No player save was reset.

| Unlock | Previous actual price | New actual price |
|---|---:|---:|
| Pin 2 | 50 | 40 |
| Pin 3 | 90 | 65 |
| Pin 4 | 150 | 110 |
| Pin 5 | 250 | 190 |
| Pin 6 | 425 | 320 |
| Pin 7 | 725 | 545 |
| Pin 8 | 1,230 | 925 |
| Pin 9 | 2,095 | 1,570 |
| Pin 10 | 3,560 | 2,670 |
| Pin 11 | 6,050 | 4,535 |
| Pin 12 | 10,280 | 7,710 |

Changed files: `Data/SkillTree/SkillTree.asset` and
`Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs` under
`SpinTheGlobe/Assets/DetectiveDesk/`, `Tools/economy_snapshot.json`,
and `Docs/ECONOMY_REBALANCE.md`. No scene was modified.

Validation: all 12 authored unlock prices match the compiled factory;
149/149 Unity EditMode tests and 8/8 Python economy tests passed.
Evidence: `SpinTheGlobe/Logs/cheaper-pin-unlocks-editmode.xml`.
`git diff --check` passed. Human pacing still needs playtesting.

## Follow-up: 5% cheaper money upgrades

All 64 money upgrade curves have been reduced by 5%, before normal rounding.
This includes new pins, per-pin Strength/Pay, spin, reload, cases, drink, loot
upgrades, Gold-gated money skills and optional mastery. It does not discount
Gold purchases or buying lootboxes. Starting resources, payouts, rank limits,
prerequisites, upgrade effects and saved ranks are unchanged.

Across all money ranks, 6,827 prices became cheaper and 40 stayed
the same after nearest-EUR-5 rounding; none increased.

| Example | Before | After |
|---|---:|---:|
| Auto Spin | 20 | 20 |
| Spin Limit | 25 | 25 |
| Spin Pay level 2 | 100 | 95 |
| Pin 1 | 10 | 10 |
| Pin 2 | 40 | 40 |
| Pin 3 | 65 | 65 |
| Pin 12 | 7,710 | 7,325 |
| Case Files | 30 | 30 |

Changed files: `Data/SkillTree/SkillTree.asset` and
`Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs` under
`SpinTheGlobe/Assets/DetectiveDesk/`, `Tools/economy_snapshot.json` and
`Docs/ECONOMY_REBALANCE.md`. No scene or player save was modified.

Validation: all 6,867 authored rank prices match the compiled factory.
149/149 Unity EditMode tests and 8/8 Python economy tests passed;
`git diff --check` passed. Evidence:
`SpinTheGlobe/Logs/five-percent-cheaper-editmode.xml`.
No new human first-run timing measurement was performed.

## Follow-up: 10% cheaper shooter upgrades

Auto Fire and all 40 Faster Reload levels receive an additional 10% price
reduction after the general 5% reduction, before final EUR 5 rounding.
Auto Fire changes from EUR 335 to EUR 300. The first six Reload prices become
EUR 10, 15, 20, 30, 45, 65 after rounding; the final level changes from
EUR 8,360 to EUR 7,525. Effects, levels, prerequisites and other prices are
unchanged. No player save was reset.

Changed files: `Data/SkillTree/SkillTree.asset` and
`Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs` under
`SpinTheGlobe/Assets/DetectiveDesk/`, `Tools/economy_snapshot.json` and
`Docs/ECONOMY_REBALANCE.md`. No scene was changed.

Validation: Unity readback confirmed all 41 shooter rank prices match the
compiled factory. Eight Python economy tests and `git diff --check` passed.
The EditMode test request was skipped because the editor was in Play mode;
the running game was not stopped. Human pacing has not been remeasured.
