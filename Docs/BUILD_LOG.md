# Overnight build log — 2026-11-02

Queue: steps 2–9 from `Docs/GAME_DESIGN.md`. Each finished step is committed separately in git.

| Step | Status | Notes |
|---|---|---|
| 2 Globe rework (click impulse, stats, wallet, save, HUD) | written | `GlobeController`, `GameManager`, `DetectiveGame` |
| 3 Pins (random landing, land/city, cooldown, 12 pins, per-pin stats) | written | `WallMapManager`, `PinInstance`, `PinLanding`, `LandMask`, `WorldCities`, `PinBox` |
| 4 Skill tree screen | written | `SkillTreeView`, `SkillTreePanZoom` |
| 5 Case file | written | `CaseFileSystem` |
| 6 Whiskey glass | written | `WhiskeyGlass` |
| 7 Lootbox + inventory + paper ball | written | `LootboxSystem`, `Inventory`, `PaperBallThrower` |
| 8 Statues | written | `FigurinePedestal`, `StatueHammer` |
| 9 Polish | partly | see "Not done yet" |

## Decisions taken while you slept
- **Systems build themselves.** `GameManager` adds every system to its own GameObject at start. The HUD and the skill tree are built from code, so the scene didn't need new prefabs.
- **The old upgrade panel is retired.** The UPGRADES button and the `T` key now open the new skill tree. The old 39-node tree, `UpgradeConfig` and the old panel scripts were removed. Leftover "missing script" components in the scene can be cleaned with **Tools ▸ Spin The Globe ▸ Remove Missing Scripts In Open Scene**.
- **Firing pins:** press **Space**, click the launcher or the pin box, or use the HUD *Fire pin* button. Pin *n* always uses skin *n* from the coffer.
- **Land and sea detection** uses a 1° land map generated from the Natural Earth data already in the project. The city bonus uses 60 world cities.
- **Statues** reuse the existing bust parts: they show up progressively as pieces are added, then tint to gold as they fill.
- **The waste bin** is a placeholder cylinder next to the desk until a real asset exists.
- **Gold** is earned and shown, but nothing spends it yet. Its use is still to be decided.
- **Saving** writes `spintheglobe_save.json` to Application.persistentDataPath, autosaving every 30 s and on quit. **Tools ▸ Spin The Globe ▸ Delete Save File** starts over.

## Not done yet
- A closed pin box that opens with the first pin. The lid's hinge needs checking in the scene first.
- A holder structure for the payout line. This needs a model.
- Balancing pass. All numbers are in `Data/Balance/GameBalance.asset` and `Data/SkillTree/SkillTree.asset`.

## How to test
1. Open `Scenes/DetectiveOffice.unity`, then press Play.
2. Drag or click the globe → money goes up. At €25, press `T` and unlock the root, then Pin 1.
3. Press Space to fire a pin. It pays each time it passes the line and falls off when its strength runs out → evidence goes to the case file → press the stamp when the case is full.
4. Click the whiskey glass several times → boost.
5. Buy a lootbox (HUD) → items. Statue pieces build the statues → gold fill → click the statue to smash it.
