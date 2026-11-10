# Skill tree information hubs

The 64 purchasable skills surround four free information hubs connected to Unlock Upgrades:

- Globe above: Auto Spin, Spin Limit, Less Friction and Stronger Click share a horizontal connector. Spin Pay continues above Spin Limit.
- Pins right: the pin chain continues horizontally; Faster Reload and Auto Fire descend from the hub.
- Case Files left: the case unlock leads into its payout, waiting-time and stamping upgrades.
- Desk Items below: the lootbox chain sits directly underneath the hub, giving fresh runs a straight downward connection. Gold-locked drinks and statues branch to the left once unlocked, with their upgrades continuing downward.

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
