# Final prestige progression — 2026-11-10

| Completed prestiges | Rank | Earnings required in this run | Result |
|---:|---|---:|---|
| 0 | Rookie | EUR 15,000 | Prestige to Detective |
| 1 | Detective | EUR 20,000 | Prestige to Inspector |
| 2 | Inspector | EUR 25,000 | Prestige to Chief Inspector |
| 3 | Chief Inspector | EUR 30,000 | Prestige to Commissioner |
| 4 | Commissioner | EUR 35,000 | Prestige to Legend of the Bureau |
| 5 | Legend of the Bureau | EUR 50,000 | Complete the game |

Each prestige grants +10 percentage points of permanent money income, capped at five. Agency Reputation remains +5 points per level with its existing 5, 10, 15... Gold prices. All other Gold shop prices, effects and skill upgrades are retained.

Requirements use total money earned in the current run, not wallet balance. Spending does not undo progress. Gold rewards keep the original EUR 15,000 reference and square-root formula, so higher retirement milestones can award more Gold: minimum rewards are 25, 28, 32, 35 and 38. Players can continue earning before retiring for a larger reward. Legend cannot prestige again and receives no additional retirement Gold.

At the final goal, GameManager marks the game complete, notifies the UI and saves the completed state. The Gold shop shows an ending with a Continue Playing button. It neither retires nor resets progress. The ending can be viewed again from the final-goal card. A saved completed game does not repeatedly open the ending on load. The existing Restart action starts a fresh game by deleting its save.

GameBalance holds the requirement increase (5000), maximum prestiges (5), final-run goal (50000), rank list and per-retirement income bonus (.10). Existing retirement counts above the new cap clamp to five when loaded. Older saves without gameCompleted default to false; an eligible final run is detected after loading. No player save was manually edited or reset.

Changed files under Assets/DetectiveDesk:

- Scripts/Runtime/Progression/GameBalance.cs and GoldShop.cs
- Scripts/Runtime/Core/GameManager.cs and Save/SaveData.cs
- Scripts/Runtime/UI/GoldShopView.cs
- Data/Balance/GameBalance.asset, Scenes/DetectiveOffice.unity and Art/UI/ScenePresentationResources.asset
- Tests/EditMode/ProgressionTests.cs, ScenePresentationTests.cs and SaveTests.cs

Repository paths: Tools/economy_model.py, Tools/economy_snapshot.json, Docs/GAME_DESIGN.md, Docs/AGENCY_PRESTIGE_MULTIPLIER.md and this document.

Validation: 177/177 Unity EditMode tests passed (Logs/final-prestige-progression-editmode.xml), including exact goal boundaries, rank transitions, Gold formula reference, no sixth prestige, +10% stacking with unchanged Agency, persisted completion and closing the ending without resetting the run. All nine Python economy tests and the 63-skill/four-heading layout check passed. All assemblies compiled. The scene was authored through Unity. These checks do not establish human run durations; no measured timing or fresh rendered screenshot is claimed.
