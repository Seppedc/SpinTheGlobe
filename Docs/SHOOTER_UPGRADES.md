# Shooter branch placement

Current prices supersede the historical prices below; see [ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md). Reload timing and skill requirements are retained.

Faster Reload and Auto Fire form a short vertical branch below-right of Unlock
Upgrades, at grid cells `(1, -1)` and `(1, -2)`. The visible links run from the
root to Faster Reload, then straight down to Auto Fire. There is no long line
back to the pin row.

Faster Reload requires both the root and Pin 1. It becomes visible after the
root is bought and stays locked until Pin 1 is owned. Auto Fire still requires
one rank of Faster Reload. Prices, upgrade effects, ranks and saved ids are
unchanged. Existing game progress is not reset or migrated.

Only these two scene nodes were repositioned. Other authored node positions and
the scene's existing unsaved edits were retained when saving the updated scene.
The original editor menu visibility and save setting were restored after the
live check.

Changed paths:

- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/UI/SkillTreeView.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset`
- `SpinTheGlobe/Assets/DetectiveDesk/Scenes/DetectiveOffice.unity`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/ProgressionTests.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/ScenePresentationTests.cs`
- `Tools/skilltree_layout.py`, `Tools/economy_snapshot.json`, and generated `layout.json`
- `Docs/GAME_DESIGN.md` and this document

Validation: 90/90 EditMode tests passed (`Logs/shooter-branch-editmode.xml`).
The layout checker reports 64 nodes with no overlaps or links through nodes.
The active scene has one connector per shooter upgrade and tree validation
reports zero errors. A fresh Play-mode check confirmed that Faster Reload is
locked after the root, rejects a purchase before Pin 1, and can be purchased
after Pin 1, followed by Auto Fire. Save loading/writes were disabled throughout
that check. Selective layout synchronization also has regression coverage for
retaining custom positions on unrelated pin and globe nodes.
