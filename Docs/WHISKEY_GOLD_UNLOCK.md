# Permanent Whiskey Set unlock — 2026-11-08

**Whiskey Set** is a one-time purchase in the Gold shop's Unlocks section.
It costs 10 Gold and persists through saves and retirement. Its price is
configured by `GameBalance.whiskeyUnlockGoldCost`; money discounts do not
affect it.

Until purchased, the glass, whiskey bottle, rim, cap and legacy interaction
objects are hidden. The four drink skill nodes are hidden and cannot be bought,
including maxed nodes from an older save. Sip, developer fill and automatic
refill/boost behavior cannot use the locked drink. Bottomless Glass requires
Whiskey Set first. Previously purchased skill ranks remain saved.

Buying the unlock reveals the props immediately. The existing skill-tree
prerequisites still determine which drink upgrades are discovered next.
Existing Gold-shop canvases add a missing catalog row without duplication.

The main scene now has all six drink-object references assigned, with those
objects inactive for its fresh-game presentation. Inactive-aware lookup and
serialized bindings allow an unlocked save to reveal them again on startup.
The scene was clean before this targeted authoring save. No source model,
prefab, material, prop position or player save was changed.

The economy snapshot was exported from Unity. Both economy models now require
the unlock for boosts; the behavioral tool accepts a starting `shop` state for
controlled unlocked fixtures. Fresh-run simulations have no drink boost, while
the explicitly boosted sensitivity checkpoints include the unlock.

Changed paths, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Data/Balance/GameBalance.asset`
- `Data/SkillTree/SkillTree.asset`
- `Scenes/DetectiveOffice.unity`
- `Scripts/Runtime/Progression/GameBalance.cs`
- `Scripts/Runtime/Progression/GoldShop.cs`
- `Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs`
- `Scripts/Runtime/Progression/SkillTree/SkillTreeState.cs`
- `Scripts/Runtime/Core/GameManager.cs`
- `Scripts/Runtime/Whiskey/WhiskeyGlass.cs`
- `Scripts/Runtime/UI/GoldShopView.cs`
- `Scripts/Editor/Tools/EconomySnapshotMenu.cs`
- `Tests/EditMode/ProgressionTests.cs`
- `Tests/EditMode/ScenePresentationTests.cs`

Other changed paths: `Tools/economy_model.py`, `Tools/economy_playtest.py`,
`Tools/economy_snapshot.json`, `Tools/test_economy_playtest.py`,
`Docs/GAME_DESIGN.md` and this document.

Validation: 163/163 Unity EditMode tests and 9/9 Python economy tests passed.
New checks cover the data-driven price, insufficient Gold, single purchase,
save/retirement persistence, hidden old maxed skills, locked interaction,
prop reveal, missing-row restoration, authored bindings and simulated boost
eligibility. Result: `SpinTheGlobe/Logs/whiskey-gold-unlock-editmode.xml`.
The main-camera capture was visually inspected: glass and bottle are absent.
`git diff --check` passed. A full Play-mode purchase/drink animation sequence
and the resulting first-retirement pacing have not been manually remeasured.
