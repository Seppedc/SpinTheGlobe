# Direct globe upgrade choices — 2026-11-06

Spin Limit, Stronger Drag and Stronger Click now require only Unlock Upgrades.
They can be purchased independently, without buying Auto Spin or one another.
Auto Spin retains its root requirement and its existing speed cap.
Spin Pay continues to require Spin Limit, followed by the existing pay chain.

These purchase rules still apply after the visual reorganization. The upgrades
now branch from a free Globe heading above the root, using shared connectors.
See [SKILL_TREE_SECTIONS.md](SKILL_TREE_SECTIONS.md) for the current layout.

Prices, effects, rank counts and saved ids are unchanged. No save reset,
refund or migration was added. Existing scene edits were retained when saving
the updated layout. Play mode was temporarily stopped for the changes/checks.

Changed paths:

- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset`
- `SpinTheGlobe/Assets/DetectiveDesk/Scenes/DetectiveOffice.unity`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/ProgressionTests.cs`
- `Tools/skilltree_layout.py`, `Tools/economy_snapshot.json` and generated `layout.json`
- `Docs/GAME_DESIGN.md` and this document

Validation: 107/107 EditMode tests passed
(`Logs/manual-globe-root-editmode.xml`). The new regression test purchases all
three upgrades after buying only the root and verifies that Auto Spin remains
unowned and the motor remains off. Active-tree validation reports zero errors;
the layout checker reports 64 nodes with no overlaps or links through nodes.
