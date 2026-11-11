# Agency Reputation and prestige income — 2026-11-10

Every completed retirement grants +10 percentage points of permanent global money income, including the first retirement, capped at five resets. Agency Reputation grants another +5 points per level. These are additive contributions to IncomeMultiplier, not separate compounding multipliers:

`Global money multiplier = 1 + 0.10 × min(retirements, 5) + 0.05 × Agency levels`

Examples: no retirement or Agency levels is ×1.00; one retirement is ×1.10; one retirement plus one Agency level is ×1.15; one retirement plus two Agency levels is ×1.20. Spin Pay, per-pin Pay and temporary drink bonuses remain separate. This multiplier affects money earnings that already use IncomeMultiplier, not prestige Gold or statue Gold.

Agency keeps its 50-level limit. Prices are 5, 10, 15, ... 250 Gold, stored as explicit per-level prices in GameBalance.goldShopItems. The per-retirement bonus is stored as prestigeIncomeBonusPerRetirement in GameBalance. Rising retirement requirements and the final goal are documented in [FINAL_PRESTIGE_PROGRESSION.md](FINAL_PRESTIGE_PROGRESSION.md).

The old detective-rank milestone income bonuses are no longer applied. Detective rank titles and retirement milestones remain. The serialized incomeBonus field is retained as a hidden legacy field; active bonuses derive from the actual retirement count. Existing Agency levels and prestige counts are preserved; their effects follow the new rules. No player save was reset and no refunds were added.

The HUD top bar now has a Global ×1.00-style indicator that updates with the current permanent IncomeMultiplier. The shop retirement card shows the total multiplier plus Prestige and Agency percentage contributions. The retirement confirmation shows the additional permanent bonus and the next run's multiplier. The promotion message uses the same calculation, and the offline model uses the same per-retirement bonus.

The HUD indicator and its divider are hidden while the global multiplier is exactly ×1.00. They appear when a bonus applies and hide again if the value returns to ×1.00; the horizontal layout leaves no empty slot. The shop's explanatory retirement breakdown remains visible. This visibility update changes Scripts/Runtime/UI/GameHUD.cs, Tests/EditMode/ScenePresentationTests.cs and Scenes/DetectiveOffice.unity under Assets/DetectiveDesk, plus this document. Regression coverage checks the hidden, visible and returned-to-neutral states. Results: Logs/hide-neutral-multiplier-editmode.xml.

Changed paths under Assets/DetectiveDesk:

- Scripts/Runtime/Progression/GoldShop.cs, GameBalance.cs and DetectiveRanks.cs
- Scripts/Runtime/Core/GameManager.cs
- Scripts/Runtime/UI/GameHUD.cs and GoldShopView.cs
- Data/Balance/GameBalance.asset, Scenes/DetectiveOffice.unity and Art/UI/ScenePresentationResources.asset
- Tests/EditMode/ProgressionTests.cs and ScenePresentationTests.cs

Repository paths: Tools/economy_model.py, Tools/economy_snapshot.json, Docs/GAME_DESIGN.md, Docs/GOLD_SHOP_REWORK.md and this document.

Unity validation completed: **175/175 EditMode tests passed** (Logs/agency-prestige-multiplier-final-editmode.xml). Coverage includes additive stacking at multiple retirement counts, 25-Gold spending choices, all Agency price levels, save round trips, exclusion of the old rank bonuses, HUD updates without duplicate elements, and retirement-card/confirmation next-run values. The first run had one outdated fixture that set a base price without clearing the new explicit prices; the fixture now replaces its complete cost curve, and the full suite passes. Unity had temporarily stopped servicing commands before the result was available.

Executed validation: runtime, Editor and test assemblies compile; all nine Python economy tests pass; the 63-skill/four-heading layout check passes; an independent export check verifies all 50 Agency prices, five additive multiplier builds, the saved HUD element, retained 25-Gold prestige scale/retirement threshold and unchanged other Gold shop prices. Git diff whitespace checks pass. The saved UI was authored through Unity. No player save was edited or reset. No fresh rendered screenshot or measured human second-run timing is claimed.
