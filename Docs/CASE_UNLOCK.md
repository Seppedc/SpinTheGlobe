# Case Files unlock — 2026-11-06

Case Files remains a separate branch to the left of the tree. A new EUR 30,
one-rank `case_unlock` skill comes first and requires the root and Pin 1.
Case Pay, Case Wait and Auto Stamp each require it directly; the later case
upgrades keep their existing chain, prices, ranks and ids. The unlock sits closest to the centre; Pay is directly left, Wait above-left,
and Auto Stamp below-left. Later case upgrades continue along those rows.
The authored scene tree includes the new node, so it is available in editor
previews as well as Play mode.

Locked cases show **Unlock Case Files** on the brass plate. The folder remains
visible, but fallen pins are ignored, stamping is disabled, and case money/gold
rewards are zero. After purchase, new pin falls supply evidence. Ordinary
evidence progress and the cooldown countdown resume. There is no bank of pin
falls received before purchasing the unlock.

The unlock is a normal saved skill rank and resets on retirement. Existing
testing saves without that rank must buy the new unlock; existing case level,
evidence and upgrade ranks are retained. No refunds or automatic grants were
added. The EUR 30 price is an initial tuning choice, not a new measured first-run
duration target.

Changed files, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Data/Balance/GameBalance.asset`
- `Data/SkillTree/SkillTree.asset`
- `Scenes/DetectiveOffice.unity`
- `Scripts/Runtime/Cases/CaseFileSystem.cs`
- `Scripts/Runtime/Progression/GameBalance.cs`
- `Scripts/Runtime/Progression/Stats/StatId.cs`
- `Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `Scripts/Runtime/UI/DeskInfoSystem.cs`
- `Scripts/Runtime/UI/SkillTreeView.cs`
- `Scripts/Runtime/UI/StatInfo.cs`
- `Tests/EditMode/ProgressionTests.cs`
- `Tests/EditMode/ScenePresentationTests.cs`
- `Tests/EditMode/CaseUnlockTests.cs` and `.meta`

Other changed paths: `Tools/economy_model.py`, `Tools/economy_snapshot.json`,
`Tools/skilltree_layout.py`, `Docs/GAME_DESIGN.md`, `Docs/ECONOMY.md` and this file.
The layout utility also updates the already-untracked `layout.json` output.

Validation: 89/89 EditMode tests passed (`Logs/case-unlock-editmode.xml`). The
active tree and authored scene each have 64 nodes; tree validation reports zero
errors, and the layout checker reports no collisions or links through nodes.
The economy model confirms zero case income/evidence while locked and positive
values after purchase. A fresh Play-mode check confirmed rejection before Pin 1,
a successful purchase afterward, correct plate text, new evidence collection,
and availability of Case Pay. Save loading/writes were disabled during that
check; the original save setting was restored afterward. No console errors
occurred during the live check.

Branch choice and layout update: Case Wait and Auto Stamp no longer require
Case Pay. All three choices connect directly to Case Files with short, straight
lines. The temporary long right-angle links were removed. Case skills now form
a compact three-way branch; the whiskey branch moved up half a grid cell to
leave room for the upper case row. Prices, ranks and gameplay dependencies are
unchanged by this visual pass.

Factory positions, the active SkillTree asset, the authored scene, SkillTreeView
link rendering, Tools/skilltree_layout.py and the economy snapshot were updated.
89/89 EditMode tests passed (Logs/compact-case-layout-editmode.xml); the layout
checker reports 64 nodes with no collisions or links through nodes. The compact
branch was also visually checked in Unity's editor preview. No game save was
reset or migrated. The normal desk preview was restored afterward.
