# Spin The Globe - Economy and pacing

Less Friction replaces Stronger Drag: ten levels reduce the speed-loss rate
by 15% each. Existing owned levels carry over. See [LESS_FRICTION_UPGRADE.md](LESS_FRICTION_UPGRADE.md).

Money upgrade prices round to the nearest EUR 5 after evaluating their curves.
See [ROUNDED_UPGRADE_PRICES.md](ROUNDED_UPGRADE_PRICES.md) for current examples.

Updated 2026-11-06. Current price curves and bulk-buy behavior are documented in [ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md). The player assets are the source of truth:
`SpinTheGlobe/Assets/DetectiveDesk/Data/Balance/GameBalance.asset` and
`SpinTheGlobe/Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset`.
The tree currently has 64 nodes; more ranks do not add more nodes.

## Current targets

- A slower opening with a visible improvement from each small purchase.
- First retirement becomes available after about 30-40 minutes of active play.
- About 5-6 hours to reach the top detective rank (20 retirements), under the
  simulated purchase strategy. This is a pacing target, not a guaranteed minimum
  or a finished-game timer. Mastery and Reputation can extend progression.
- Reruns speed up through permanent upgrades rather than repeating the opening.

## Opening and globe progression

| Setting | Current value |
|---|---|
| Starting money / gold | EUR 0 / 0 |
| Base globe payout | EUR 1 per payout-line crossing |
| Payout ticks per revolution | 1 |
| Starting spin limit | 15 RPM (90 degrees/s) |
| Auto Spin | 100 ranks, +5 RPM each, explicit prices from EUR 22 to EUR 86,750 (opening rises by EUR 10, then accelerates) |
| Spin Limit | 101 ranks, +7.5 RPM each, explicit prices from EUR 24 to EUR 107,385 (opening rises by EUR 10, then accelerates) |
| Fully upgraded spin limit | 772.5 RPM |
| Fully upgraded Auto Spin | 500 RPM, capped by Spin Limit |
| Unlock Upgrades | EUR 10 |
| First pin | EUR 10 |

The speed limit leaves headroom for dragging instead of being a speed that clicks
can easily hold. Auto Spin still tops out at 500 RPM, so active dragging can
outperform the motor throughout the run. Stable node ids and owned rank counts
are retained. Whiskey can temporarily raise the unboosted limit further.

## Manual input feel

The corrected opening cap is 15 RPM: half of the earlier 30 RPM value.
Dragging is one press-to-release gesture. Only release adds a boost; movement
while held never accumulates impulses. Net forward displacement determines the
distance contribution, weighted by average pointer speed.

- Click impulse remains 3 degrees/s (0.5 RPM initially).
- Drag acceleration remains 0.25 degrees/s per reference pixel, weighted by
  net pointer speed / 900 reference pixels/s, capped at a 1.5 speed multiplier.
- Net forward displacement must reach 32 reference pixels. Its distance
  contribution caps at 200. Reference pixels are normalized to a 1920-wide
  viewport, so smaller/larger game windows have equivalent relative gestures.
- A single release adds at most half the current speed limit, including Drag
  Power. A 200-reference-pixel swipe over 0.2 seconds adds up to 7.5 RPM at the
  opening cap; the same distance over two seconds adds about 0.9 RPM.
- Backtracking does not charge power. Returning to the starting position after
  a large movement gives neither a drag boost nor a click. The unconditional
  minimum drag top-up is removed. Small clicks retain their normal impulse.
- See [GLOBE_SWIPE_INPUT.md](GLOBE_SWIPE_INPUT.md) for cancellation behavior and
  the changed-file list.
- Stronger Drag and manual scaling (max speed / 135 degrees/s, minimum x1)
  still apply, so upgraded quick flicks can become stronger later in a run.
- Friction stays at 0.9875 per 60 Hz step. It now decays only the extra speed
  above Auto Spin: about 47% of that extra speed remains after one second.
  With the motor off, the globe still slows to rest. The motor maintains its
  own baseline without consuming manual boosts. For example, at a 5 RPM motor
  speed, a 0.5 RPM click leaves about 0.414 RPM extra after 0.25 seconds
  (previously it had already disappeared).
- Automated tests compare equal distances at different speeds, check that an
  ordinary quick swipe does not instantly max the opening globe, compare quick
  flicks with rapid clicking, and check response at 30/60/144 FPS.
- Mouse feel still needs player testing. The economy model assumes a sustained
  speed fraction and does not claim to simulate every physical gesture.

## Income and spending

- Globe payout: EUR 1 x spin earnings x all income.
- Pin payout: EUR 0.50 x that pin's payout x all income.
- Landing location does not affect payout: sea, land and cities pay the same.
- Case Files costs EUR 30 once per run and requires Pin 1 and the root. Before purchase, cases do not collect evidence or pay money/gold. The existing case upgrade chain now starts behind this unlock; all prior upgrade prices are retained.
- Case money: max(EUR 5 x (1 + completed cases x 0.02) x all income,
  1.5 seconds of recent globe/pin income) x case reward multiplier. The base and
  income-linked payouts are half the preceding setup (one eighth of the original
  EUR 40 / 12-second settings). Base growth is EUR 0.10 per completed case before
  upgrades.
- Evidence requirements repeat five times: cases 1-5 need 3, 6-10 need 4,
  11-15 need 5, 16-20 need 6, continuing without the old 40-evidence cap.
  Evidence-reduction upgrades still apply, with a minimum of one clue.
- Strength requirements: cases 1-15 accept any pin, 16-45 need Strength 1+,
  46-90 need Strength 2+, 91-150 need Strength 3+, 151-225 need Strength 4+,
  and 226 onward need Strength 5+. Cases have no designed final level;
  evidence counts continue to grow while Strength stays achievable.
- The case plate reads `Evidence: collected/required` while a case is open, and
  restores the `NEXT CASE` countdown during cooldown. It no longer includes case
  number or money while open. Evidence received while
  full or on cooldown is still lost. Press Coverage gold rewards are unchanged.
- Pin durability adds passes; later case eligibility checks the pin's own
  durability skill rank, not the Steel Tips global pass bonus.
- Lootboxes start at EUR 150, with a 1.12 price multiplier per box opened.
  Weights: statue piece 30, money 25, ice 20, gold 15, paper ball 10. Pieces are
  excluded unless an unlocked, incomplete statue has room. Money gives a 30-80%
  refund or, with 25% jackpot chance, 2-4 times the purchase price.
- Paper balls currently pay EUR 25 x spin earnings x all income when used.
- Completed statues fill with gold from revolutions; smashing pays gold.

All stat modifiers use:
`(base + sum(add)) x max(0.05, 1 + sum(percent)) x product(multiply^rank)`.
Gold shop and rank modifiers enter the same stat calculation; percentage income
bonuses add together before multiplicative bonuses and temporary whiskey buffs.

## Shooter

Faster Reload has 40 ranks, each multiplying reload by 0.88 (12% less reload time).
Price: first six ranks EUR 10, 15, 23, 34, 51, 76; later prices ceil(76 x 1.15^(ownedRank - 5)). All 40 ranks total EUR 67,117; final rank EUR 8,802. Auto Fire costs EUR 350.
Reload: 4.5 s initially, 3.960 s at rank 1, 2.375 s at rank 5,
1.253 s at rank 10, 0.661 s at rank 15, 0.097 s at rank 30 and 0.027 s at rank 40.
At the starting 15 RPM limit, a one-pass pin returns within about 4.32 seconds,
so base reload now limits early firing and the first purchases have a clearer
benefit. See [RELOAD_OPENING.md](RELOAD_OPENING.md) for the firing comparison.
Pins fire in rotation, including Twin Barrel's second pin. Fractional reload time
carries across frames; waiting for pins cannot store a burst of overdue shots.

`python Tools/shooter_sim.py` models flight, first crossings and frame timing.
At 772.5 RPM, maximum reload, any landing location and one-pass pins, three pins sustain
about 8.18 shots/s and twelve sustain 31.77 shots/s. Corresponding pin income is
EUR 4.08/s and EUR 15.84/s, before upgrades. Globe-only income is EUR 12.88/s.
These are numerical estimates, not measured rendering/physics performance.

Current price pass: late globe speed upgrades use the accelerating curves; pin unlock prices grow
by 1.7 per pin, with independent Strength and Pay curves. See
[ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md) for exact curves and measurements.
The older [UPGRADE_PRICE_REVIEW.md](UPGRADE_PRICE_REVIEW.md) is historical.
Bulk Buy 1 / 5 / 10 controls buy up to the selected amount, respecting funds,
maximum ranks, prerequisites and permanent speed caps. The tooltip shows the
actual quantity, total price and resulting stat.

## Retirement and permanent progression

Retirement unlocks at EUR 15,000 earned this run; this is not a wallet fee. Reward:
`floor(25 x sqrt(run earnings / 15,000))`.
EUR 15,000 earns 25 retirement gold; EUR 60,000 earns 50. Lootbox rewards and other
money credits count toward run earnings. Starting cash does not.

Retirement resets money, normal skills, pins, cases, whiskey, lootbox prices and
consumables. Gold, Gold shop ranks, statues, settings and lamp state persist.
Existing saves keep their progress; this balance change does not delete saves.

Gold shop purchases unlock statues and Press Coverage, improve income and give
new runs a head start. Detective ranks are defined in GameBalance: Rookie at 0,
Detective at 1, Senior Detective at 3, Inspector at 5, Chief Inspector at 8,
Commissioner at 12 and Legend of the Bureau at 20 retirements.
Rank income bonuses are not cumulative across titles.

## Reproducible pacing checks

1. Export current data through editor automation: `DetectiveDesk.Editor.EconomySnapshotMenu.Export()`.
2. Run `python Tools/economy_sim.py` for the first run.
3. Run `python Tools/prestige_sim.py --runs 20` for repeated runs.
4. Compare `--activity .5`, `--activity .75` and `--activity 1`.

The export includes resolved base stats (including code fallbacks), actual
skill data, Gold shop requirements/prices/modifiers, seed money, and rank rules.
`Tools/economy_snapshot.json` is the generated input; re-export after tuning.
`Tools/economy_model.py` is shared by both simulations, replacing the old duplicated
price lists, obsolete gold skill nodes and assumed pre-unlock statue income.

Reference first-run results (2-second steps, seed 7, activity .75):

| Milestone | Estimated time |
|---|---:|
| Unlock Upgrades | 1.7 min |
| First pin | 2.5 min |
| Auto Spin first purchase | 3.3 min |
| Third pin | 10.7 min |
| Auto Fire | 16.5 min |
| Sixth pin | 16.6 min |
| First retirement available | 66.1 min |

Earlier repeated-run tables and gesture reports predate this speed-sensitive
input pass. Re-run the current snapshot and playtest before treating them as
current pacing measurements. The five-to-six-hour rank path remains a target. The smaller case rewards currently
project a 66-minute first retirement, beyond the earlier 30-40-minute target;
retirement pricing has not been retuned as part of this case change.

Assumptions and limits:

- Hand spinning sustains the chosen fraction of max speed, with a modest benefit
  from drag upgrades. Manual firing is limited to three shots/s at 70% uptime.
- Pin lifetime/availability and whiskey uptime are averages;
  extra hand speed during whiskey is omitted. Stamp animations and manual
  response are approximated; no evidence overflow is banked.
- Lootboxes are bought at most every three seconds when they cost at most 5% of
  cash. Pieces build unlocked statues, which are smashed when full. Ice effects
  are approximated through whiskey uptime. Paper balls are redeemed immediately.
- Money upgrades use an affordable, greedy income-per-cost strategy. Gold is
  spent cheapest-first with real prerequisites, between runs. A player can choose
  a faster route, buy gold upgrades mid-run or retire later for more gold.
- The 20-run result describes the rank path, not all finite upgrades maxed or a
  story ending. It must not be presented as a guaranteed five-hour completion time.
- The first actual payout depends on initial globe alignment; continuous income
  in the model omits this short startup delay.

## Files and validation for this pacing pass

Runtime/data: `GameBalance.cs`, `DefaultSkillTreeFactory.cs`, `GoldShop.cs` (comment),
`Data/Balance/GameBalance.asset`, `Data/SkillTree/SkillTree.asset`.
Editor: new `Scripts/Editor/Tools/EconomySnapshotMenu.cs` and its meta file.
Tests: `Tests/EditMode/ProgressionTests.cs` now checks the 15 -> 772.5 RPM curve.
`GlobeInputTests.cs` covers repeated clicks versus swipes and momentum retention.
Tools: `economy_model.py`, `economy_snapshot.json`, `economy_sim.py`, `prestige_sim.py`.
Documentation: this file.

55/55 EditMode tests passed, including shooter timing, drag dominance and speed headroom.
Imported tree validation returned zero errors. The current first-run pacing and shooter checks were executed;
older repeated-run results predate the pointer-speed change. The gameplay scene was not saved.
Input pass also changed `Core/GameplayMath.cs`, `Globe/GlobeController.cs`,
`Tests/EditMode/GlobeInputTests.cs` and its meta file, and `Tools/shooter_sim.py`.

Motor-relative friction validation: `Core/GameplayMath.cs` now integrates the
motor baseline plus decaying excess exactly. Added checks in
`Tests/EditMode/GlobeInputTests.cs` cover equal boost retention at different motor
speeds, angular travel at 30/60/144 FPS, the floor and cap, and unpowered rest.
Evidence: `SpinTheGlobe/Logs/auto-spin-momentum-editmode.xml`. No balance asset,
prices or save data were changed by this fix.

## Case progression update (2026-11-05)

`Cases/CaseProgression.cs` holds the shared runtime rules. Tunable group lengths,
Strength-tier lengths, rewards and evidence increments live in GameBalance and
its asset. The old every-case +2 evidence curve and 40-evidence cap are removed.
Save fields and existing case progress are retained; excess evidence from an old
save is clamped to the current requirement when the case system initializes.

Changed Unity paths, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Scripts/Runtime/Cases/CaseProgression.cs` and its generated `.meta` (new).
- `Scripts/Runtime/Cases/CaseFileSystem.cs`.
- `Scripts/Runtime/Progression/GameBalance.cs` and `Data/Balance/GameBalance.asset`.
- `Scripts/Runtime/UI/DeskInfoSystem.cs`.
- `Tests/EditMode/CaseProgressionTests.cs` and its generated `.meta` (new).
- `Tests/EditMode/SaveTests.cs` (late case progress persistence).

Also changed `Tools/economy_model.py`, regenerated `Tools/economy_snapshot.json`,
updated `Docs/GAME_DESIGN.md` and this document. Validation: 78/78 EditMode tests
passed, plus the first-run economy simulation. Prior test counts in this document
refer to their individual earlier changes. The gameplay scene was not saved.

Auto Spin price update (2026-11-05): its base cost increased from EUR 10 to EUR 15
(+50% before normal whole-euro rounding). Growth remains 1.16 per owned rank;
speed gains and rank limits are unchanged. Earlier pacing tables predate this
price change. Changed paths: `Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`,
`Data/SkillTree/SkillTree.asset` (Unity paths relative to `Assets/DetectiveDesk/`),
`Tools/economy_snapshot.json` and this document.

Pin payout update (2026-11-05): base payout is EUR 1 per pass. Percentage-based
Pin Pay upgrades scale from that value: levels 0/1/2/10 pay EUR 1/1.50/2/6 at sea
before global bonuses. The location bonuses described in older notes are now removed. Mastery, whiskey and permanent income modifiers continue
to multiply the new base. Earlier full-run timing estimates predate this change.
Changed paths: `Scripts/Runtime/Progression/GameBalance.cs` and
`Data/Balance/GameBalance.asset` (under Unity `Assets/DetectiveDesk/`),
`Tools/economy_snapshot.json` and this document. Validation: imported-asset
upgrade/landing checks, the focused shooter simulation and 78/78 EditMode tests
passed. No scene files were changed.

Further reward reduction (2026-11-05): pin base payout is now EUR 0.50.
Pin Pay levels 0/1/2/10 pay EUR 0.50/0.75/1/3 at sea before other modifiers.
Case base reward is EUR 5, growing EUR 0.10 per completed case; the income-linked
minimum is now 1.5 seconds. Both case branches and their upgrade-scaled rewards
are halved for a fixed income rate. Lower pin earnings can also reduce the
income rate that drives later case rewards. Prior timing estimates are outdated.
Changed paths: `Scripts/Runtime/Progression/GameBalance.cs`,
`Data/Balance/GameBalance.asset`, `Tests/EditMode/CaseProgressionTests.cs`
(under Unity `Assets/DetectiveDesk/`), `Tools/economy_snapshot.json`,
`Docs/GAME_DESIGN.md` and this document. Imported-asset scaling checks and all
78 EditMode tests passed. Evidence: `SpinTheGlobe/Logs/half-pin-case-rewards-editmode.xml`.

Location-bonus removal (2026-11-05): sea, land and city proximity no longer
affect pin payouts. The two location-chance skills (`pin_land_chance` and
`pin_city_chance`) are removed; their ids are not reused. Old saved ranks are
ignored by the existing loader. No refund or migration logic is added, as this
is a testing build that will be reset manually. The active tree has 63 nodes.

Pin Pay, All Pin Pay, whiskey and permanent income upgrades still
apply. Base pin income remains EUR 0.50. Geographic placement, map/globe
mirroring and country provenance remain intact. Prior pacing estimates predate
this change. The economy model no longer applies a landing multiplier.

Changed paths: `Scripts/Runtime/Pins/WallMapManager.cs`, `PinLanding.cs`,
`WorldCities.cs`, `Scripts/Runtime/Progression/GameBalance.cs`,
`Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`,
`Data/SkillTree/SkillTree.asset`, `Tests/EditMode/ProgressionTests.cs`
(relative to Unity `Assets/DetectiveDesk/`), `Tools/economy_model.py`,
`Tools/economy_snapshot.json`, `Tools/skilltree_layout.py`, `Tools/shooter_sim.py`,
`Docs/GAME_DESIGN.md` and this document. Validation: 79/79 EditMode tests passed;
tree validation found zero errors; a read-only payout calculation with temporary
runtime pins returned identical amounts at sea, on land and near Tokyo. The
focused shooter simulation ran. No gameplay scene was saved or progress reset.

Spin-only pay upgrades (2026-11-05): Spin Pay, Double Spin Pay and Spin Bonus
now affect spin income only. Pins use their own Pay upgrades and global income
bonuses. Whiskey still temporarily boosts pin Pay. Skill ids, ranks and prices
are preserved. Changed runtime files: Core/GameManager.cs, Pins/WallMapManager.cs,
Whiskey/WhiskeyGlass.cs, UI/StatInfo.cs, Progression/Stats/StatId.cs and
Progression/SkillTree/DefaultSkillTreeFactory.cs; active Data/SkillTree/SkillTree.asset,
Tests/EditMode/ProgressionTests.cs and Tools/economy_model.py/economy_snapshot.json
were updated alongside this document and Docs/GAME_DESIGN.md.
Validation: 80/80 EditMode tests passed (Logs/spin-only-pay-editmode.xml),
tree validation found no errors, Python model compiled and git diff --check
passed. The gameplay scene was not saved or modified. Pacing projections have
not been recalibrated after this income reduction.

Current pin progression: each of the 12 pins has 20 Strength and 20 Pay levels. Existing early prices are retained; levels 11-20 use 15% price growth. Case Wait now starts at EUR 60 (20% lower base). See ECONOMY_REBALANCE.md for the new limits and validation; earlier run timings predate these changes.
