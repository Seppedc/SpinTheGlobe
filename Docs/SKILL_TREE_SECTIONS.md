# Skill tree information hubs

The 63 purchasable skills surround four free information hubs connected to Unlock Upgrades:

Statue Gold Bonus (`statue_mastery`) has been removed. The statue skill branch ends at Statue Gold. The Gold shop's separate More Statue Gold chance upgrade remains available. Authored tree restoration now removes graphics, labels and incoming links for skills that no longer exist in the data. Old saved ranks for removed skills follow the existing load rule and are ignored; no refunds or player-save reset were applied.

Removal changed DefaultSkillTreeFactory.cs, UI/SkillTreeView.cs, Data/SkillTree/SkillTree.asset, Scenes/DetectiveOffice.unity, Tests/EditMode/ScenePresentationTests.cs and ProgressionTests.cs (under Assets/DetectiveDesk), plus Tools/economy_snapshot.json, layout.json, Docs/GAME_DESIGN.md and this document. Confirmed the removed id is absent from the active asset, saved scene and exported snapshot. Runtime, Editor and test assemblies compile; all nine Python economy tests and the 63-skill/four-heading geometry check pass. The first Unity suite passed 172/173 checks; its outdated six-mastery expectation was changed to five. The full rerun is awaiting the editor command queue.

- Globe above: Auto Spin, Spin Limit, Less Friction and Stronger Click share a horizontal connector. Spin Pay continues above Spin Limit.
- Pins right: the pin chain continues horizontally; Faster Reload sits above the hub, with Auto Fire above Reload. The Globe section has extra height to leave room for this branch.
- Case Files left: the case unlock leads into its payout, waiting-time and stamping upgrades.
- Desk Items below: statues branch directly left, drinks directly right, and lootboxes directly down. Their later upgrades continue below each entry. These three entries use individual straight links instead of a shared lower connector; Gold-locked branches remain hidden until unlocked.

Hover or click a hub to read a short explanation. Hubs have no purchase handler, price or rank. They follow entry-skill visibility, hiding before Unlock Upgrades is bought. Drinks and statues retain their Gold gates. Costs, effects, ranks, prerequisites and saved skill ids are unchanged.

Section positions and descriptions live in SkillTree.asset and DefaultSkillTreeFactory. SkillSectionWidget handles information interactions separately from purchasable skills. The saved canvas contains the new layout; an authored layout revision also updates older canvases once without continually overwriting later editor adjustments.

Changed files for this revision (relative to Assets/DetectiveDesk unless indicated):

- Scripts/Runtime/Progression/SkillTree/SkillTreeAsset.cs and DefaultSkillTreeFactory.cs
- Scripts/Runtime/UI/SkillTreeView.cs, SkillTreeView.Sections.cs and SkillSectionWidget.cs with its .meta
- Scripts/Editor/Tools/EconomySnapshotMenu.cs
- Data/SkillTree/SkillTree.asset and Scenes/DetectiveOffice.unity
- Tests/EditMode/ProgressionTests.cs and ScenePresentationTests.cs
- Repository Tools/economy_snapshot.json, layout.json, Docs/GAME_DESIGN.md and this document

Validation: all 164 EditMode tests passed (Logs/four-hubs-editmode.xml). Tests cover hub visibility, descriptions, hover/click without spending or purchasing, restoration of purchase tooltips, canvas restoration and unchanged prerequisites. The layout checker reports 64 skills and four free headings, with no overlaps or connectors crossing nodes. Runtime, Editor and test assemblies also compiled independently. Nine Python economy tests passed. The saved scene was updated through Unity. Native preview capture produced a blank image, so a rendered visual review remains unverified. No player save was reset.

## Desk branch arrangement — 2026-11-10

Applied the annotated layout: Faster Reload and Auto Fire now extend upward from Pins. Statues sit left of Desk Items, whiskey right and boxes below, using direct straight connections. Later upgrades follow each moved entry. The Globe heading and its upgrades move up one cell to provide room for Auto Fire. Layout revision 4 updates older authored canvases once. Prices, effects, prerequisites and save ids are unchanged.

Changed DefaultSkillTreeFactory.cs, SkillTreeAsset.cs, Data/SkillTree/SkillTree.asset and Scenes/DetectiveOffice.unity under Assets/DetectiveDesk; repository Tools/economy_snapshot.json, layout.json, Docs/GAME_DESIGN.md and this file. Validation: 172/172 Unity EditMode tests passed (Logs/desk-cross-layout-editmode.xml), all nine Python economy tests passed, and the 64-skill/four-heading layout check found no overlaps or connectors through nodes. Git diff whitespace checks passed. No new rendered screenshot was captured.
