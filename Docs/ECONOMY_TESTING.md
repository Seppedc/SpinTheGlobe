# Testing income, upgrade value and saving time

The previous prestige estimate and EUR 400,000 proposal are exploratory and
superseded. A single averaged-income, greedy-buy strategy is insufficient for
choosing prices or predicting a human first run. The subsequent implementation is documented in [ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md).

## Better balance target

Use price / typical total income at the stage where a purchase is made.
This is the estimated time to earn its price from an empty wallet. Also measure
actual saving time, wallet leftovers, income from each source and bursts.
An upgrade's income payback (price / extra income) is a separate metric;
automation can have comfort value even without extra income for a perfect
manual player.

For progressively longer earning times within a branch, price growth must
outpace the typical growth in total income. A single exponential curve cannot
guarantee that across every possible build. Tune early/middle/late segments
against observed income, then check outlier paths. EUR 10,000 is a reasonable
late-price hypothesis, not a justified universal cap.

The first-run ordinary tree currently contains 540 levels, excluding 999-rank
mastery and Gold-locked skills. Buying 90-100% in 40 minutes requires roughly
one level every 4-5 seconds on average, before allowing for the opening.
Longer waits for most individual levels conflict with that goal. Fewer levels
or a longer first run resolve it directly; bulk buying can turn several small
levels into one meaningful purchase, but does not make each level take longer.

## Reproducible experiments

`Tools/economy_playtest.py` reads `Tools/economy_snapshot.json`, exported from
the actual Unity assets. It does not write Unity assets or load a player save.
It uses the current stat modifiers, swipe distance/speed and per-release cap,
friction, auto-spin floor, individual pin flight/busy/pass states, round-robin
shots, per-pin payout, evidence Strength requirements, case rewards and drink
boost effects on payouts AND the speed cap.

The buying styles are mixed, pin-focused, spin-focused and automation-focused.
Each is run with the same three input styles and three seeds, so spending
preferences and mouse activity can be compared separately. A selected upgrade
is saved for even if cheaper alternatives are affordable. Thirty-second
attention breaks occur every three minutes after the opening. Automation keeps
running. Choosing upgrades uses an approximate expected-rate heuristic based on each input preset's repeating swipes, friction and both boost phases; mastery is excluded, and new pins have some value for unlocking their own upgrades.
these policies are examples, not human or optimal play.

Every purchase logs time, rank, price, actual saving time, preceding 60-second
total income including cases, recurring income, and price/income from-zero
seconds. Every minute logs the wallet, total earned, RPM and owned ranks.
The report has independent build checkpoints with and without a sustained
boost. Sustained boost is a sensitivity test, not a promise of permanent uptime.

Run from the repository root:

```
python Tools/economy_playtest.py --minutes 40
python Tools/economy_playtest.py --minutes 40 --globe-endpoint 10000 --output SpinTheGlobe/Logs/economy-playtest-10k
python -m unittest discover -s Tools -p test_economy_playtest.py -v
```

The second command changes only in-memory growth curves for Auto Spin and Spin
Limit to end near EUR 10,000. All other prices and effects stay current. This
is NOT a complete proposed rebalance and does not establish a prestige price.
`--snapshot` can compare another complete candidate without changing the game.
`--step`, `--seeds`, `--inputs`, `--idle-every` and `--idle-seconds` expose assumptions.

Outputs:

- `SpinTheGlobe/Logs/economy-playtest/report.html`: interactive current-price report
- `SpinTheGlobe/Logs/economy-playtest-10k/report.html`: hypothetical EUR 10,000 endpoint report
- Each directory has `results.json` and `purchases.csv` for inspection.

## Findings from the current rules

Illustrative builds are independently constructed; they are not guaranteed
reachable within the 40-minute simulated purchase runs.

| Illustrative build | Normal total income / second | Sustained-boost total / second | Normal time to earn EUR 10,000 |
|---|---:|---:|---:|
| Pin 1 + Case Files | 0.29 | 0.38 | About 9.7 hours |
| Early mixed: 5 Spin Limit, 5 Auto Spin, 3 Spin Pay, 3 Reload | 1.42 | 2.36 | About 117 minutes |
| Mid: 4 upgraded pins, 30 Spin Limit, 25 Auto Spin, 12 Spin Pay, 15 Reload | 20.9 | 35.0 | About 8 minutes |
| Late: 12 upgraded pins, 95 Spin Limit, 90 Auto Spin, max ordinary Spin Pay, 34 Reload | 583 | 2,170 | About 17 seconds |

Thus EUR 208 would be negligible in the late build. EUR 10,000 is far more
meaningful, but can still be cheap late and prohibitively expensive earlier.
Individual popups differ from total income: this late build pays EUR 14.50 per
spin normally and EUR 39.88 boosted; each pin pass pays EUR 3 or EUR 8.25.
Multiple payouts and case rewards can make total income much higher.

Across 36 current-price runs at 40 minutes, earnings vary substantially with
both input and buying style. With the active input preset, mixed players earn
roughly EUR 15.4-15.9k, pin-focused EUR 6.9-7.4k, spin-focused EUR 8.2k and
automation-focused EUR 2.1-2.2k. These are diagnostics of these scripts, not
forecasts for actual players. The new tool deliberately does not output a
claimed time to retirement.

## Validation and remaining work

Executed 36 current-price and 36 hypothetical-price, 40-minute experiments.
Seven Python tests passed: idle income, friction step independence, saving for
an unaffordable goal, wallet reconciliation, pin busy state, case unlock and
Strength tier boundaries. Verified 92 stat values against Unity's actual
StatSheet and active assets across the four checkpoint builds. All 111 Unity
EditMode tests passed (`Logs/economy-testing-editmode.xml`); the corrupt-save
warning is expected from SaveTests. Checkpoint income differed by at most 2.6%
when changing the step from 0.05 to 0.025 seconds.

This does not validate a full Unity playthrough. Menu attention, drink filling,
stamping and random crossing timing are approximations. Loot, Gold shop and
prestige behavior are excluded. The weighted purchase planner uses its own approximate steady-income estimate
only to choose what a synthetic player wants. Credited money comes from the
event simulation. Both need calibration against real human behavior.

Next calibration should be a real fresh-save session recording purchases,
wallet and 60-second income at each checkpoint, along with swipe frequency,
manual firing and boost use. Exclude developer money grants. Match the input
presets and purchase preferences to that recording before trusting completion
times. Tune increasing wait-time bands from those measurements, then set the
prestige threshold where normal builds are near completion. Recheck all paths,
and separately validate later prestige runs against the five-hour overall goal.

Changed files: `Tools/economy_playtest.py`, `Tools/test_economy_playtest.py`,
this document and `Docs/PRESTIGE_BALANCE_REVIEW.md`. No gameplay assets, scene
or player save changed.

The simulator now supports partial bulk buying (`--quantity 1`, `5` or `10`) and records first retirement availability with ordinary levels/pins owned. New runs preserve `snapshot-used.json` alongside results for reproducibility. The earlier results in this document precede these policy corrections; use the current rebalance report for current timing.
