# Slower starting reload — 2026-11-06

Current prices supersede the historical prices below; see [ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md). Reload timing and skill requirements are retained.

The starting reload is now 4.5 seconds instead of 1.8 seconds. Faster Reload
cuts that time by 12% per rank instead of 10%. The curve remains multiplicative
with 40 ranks; prices, pin requirements, payouts and firing controls are unchanged.

| Owned reload ranks | Previous cooldown | Current cooldown |
|---|---|---|
| 0 | 1.800 s | 4.500 s |
| 1 | 1.620 s | 3.960 s |
| 5 | 1.063 s | 2.375 s |
| 10 | 0.628 s | 1.253 s |
| 15 | 0.371 s | 0.661 s |
| 30 | 0.076 s | 0.097 s |
| 40 | 0.0266 s | 0.0271 s |

At the initial 15 RPM limit, one turn takes four seconds. Including flight,
a one-pass pin returns within roughly 4.32 seconds, before the new base reload
finishes. This makes reload an early constraint when actively spinning. A pin
can still be the constraint at lower RPM or after adding Strength; this does
not guarantee that every reload rank increases earnings with every setup.

The seeded ready-pin firing simulation at 15 RPM gives these shots per minute
for a single one-pass pin:

| Ranks | Previous | Current |
|---|---|---|
| 0 | 23.4 | 13.4 |
| 1 | 24.2 | 15.2 |
| 5 | 25.9 | 21.3 |
| 10 | 26.6 | 25.3 |

The first upgrade therefore improves firing by about 13% in this setup,
compared with about 3% previously. The opening is deliberately slower overall;
the maximum firing capability is approximately retained. These figures assume
firing whenever a pin is ready, without player hesitation, and are simulation
outputs rather than measured human play.

The first displayed upgrade costs EUR 10, with 50% price growth per rank (EUR 10 x 1.5^rank, rounded up). Saved ranks are retained and
use the new timing curve after loading. No refunds, save reset or migration
were added. The economy model reads the updated exported stats automatically.

Changed files:

- `SpinTheGlobe/Assets/DetectiveDesk/Data/Balance/GameBalance.asset`
- `SpinTheGlobe/Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/GameBalance.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/PinShooterTests.cs`
- `Tools/economy_snapshot.json` and `Tools/shooter_sim.py`
- `Docs/ECONOMY.md`, `Docs/GAME_DESIGN.md`, `Docs/UPGRADE_PRICE_REVIEW.md` and this file

Validation: 90/90 EditMode tests passed (`Logs/slower-reload-opening-editmode.xml`).
The frame-rate checks now use the actual factory/balance curve, and the old
15-rank compatibility assertion was replaced by an opening bottleneck check
and an approximately unchanged fully upgraded cooldown range. Ready-pin firing
was simulated for one to three pins at 15 RPM, and for three/twelve pins at
maximum speed. All twelve pins remain useful; the existing 30/60/144 FPS checks
pass. Active-tree validation reported zero errors. The gameplay scene and game
save were not modified or reset.

Later price adjustment: factory defaults, active SkillTree.asset and economy_snapshot.json updated; Docs/ECONOMY.md reflects the new curve. Validation: 111/111 EditMode tests passed (`Logs/reload-price-editmode.xml`). The scene and player save were unchanged.

Steeper price progression: updated factory defaults, active SkillTree.asset, exported economy snapshot, and existing PinShooterTests price assertion (the previous cheaper-than-old-curve requirement is obsolete). Prices start 10, 15, 23, 34, 51. The 40-rank total is EUR 221,146,644; this makes late reload ranks long-term purchases beyond the first retirement threshold. No full pacing simulation was run for this price-only change.

Validation for steeper pricing: 111/111 EditMode tests passed (`Logs/reload-growth-editmode.xml`). Scene and saves were unchanged.
