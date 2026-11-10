# Upgrade price review — 2026-11-06

Current prices supersede the historical prices below; see [ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md). Reload timing and skill requirements are retained.

This review records the price pass before the subsequent reload-time rework.
Prices below are historical; see [RELOAD_OPENING.md](RELOAD_OPENING.md) for current
reload times and upgrade strength. The older timing simulations below describe
the earlier 1.8-second / 10% reload curve.

This pass changes two price curves, without changing upgrade strength or
resetting progress. All saved ranks and other prices are retained.

| Upgrade | Previous curve | New curve | First displayed price |
|---|---|---|---|
| Auto Spin | EUR 15 × 1.16^ownedRank | EUR 18 × 1.16^ownedRank | EUR 15 → EUR 18 |
| Faster Reload | EUR 5 × 1.14^ownedRank | EUR 6.25 × 1.14^ownedRank | EUR 5 → EUR 7 |
| Auto Fire | EUR 50,000 | EUR 50,000 | Unchanged |

The Auto Spin curve is 20% higher and Faster Reload is 25% higher before
rounding. Costs round up to whole euros. All 40 reload ranks previously cost
EUR 6,731; they now cost EUR 8,409. Their effect remains 1.8 seconds × 0.9^rank,
or about 0.027 seconds at rank 40. Growth factors and maximum ranks are unchanged.

The live game sampled during review had three pins, 24 reload ranks and nine
Auto Spin ranks, with recent spin/pin income of roughly EUR 70 per minute.
At that point, the next reload purchase changes from EUR 117 to EUR 146:
roughly 100 versus 125 seconds of that income, excluding case payouts.
The corresponding tenth Auto Spin purchase changes from EUR 58 to EUR 69.
The sampled player had not bought Auto Fire, so Faster Reload was treated as
the shooter-speed upgrade mentioned in the request.

The important distinction is availability: faster reload cannot generate more
shots when all unlocked pins are flying or attached. Strength, pin count,
rotation speed and manual firing also constrain actual earnings. A nominal
67-fold improvement in reload speed does not mean 67-fold pin income.

This is a restrained adjustment rather than an assertion that every upgrade is
balanced. Later Auto Spin ranks already become much more expensive while still
adding only five RPM. Increasing every upgrade would risk making those ranks
feel worse. The next broader balance pass should compare spending waits and
additional income at several actual stages, including case payouts and the
convenience of automation.

For context, the existing approximate simulator was run with no loot spending
and its default purchase policy, before and after these price changes:

| Sustained activity setting | First Auto Spin, old → new | Max reload, old → new |
|---|---|---|
| 50% | 9.8 → 11.9 min | 71.0 → 76.5 min |
| 100% | 5.0 → 6.1 min | 47.3 → 51.4 min |

Neither the baseline nor changed run reached retirement within the 120-minute
simulation cap. These are model outputs, not human play measurements. The
greedy spending policy, assumed manual firing, whiskey uptime and case timing
are approximations; this run does not validate the old 30-40 minute target.

Changed files:

- `SpinTheGlobe/Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `Tools/economy_snapshot.json`
- `Docs/ECONOMY.md` and this document

Validation: 90/90 EditMode tests passed (`Logs/upgrade-prices-editmode.xml`),
active tree validation reported zero errors, and exported costs matched the
applied values. The gameplay scene was not saved or modified for this pass.
No game save was reset or migrated.
