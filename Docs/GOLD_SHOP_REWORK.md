# Gold shop revision — 2026-11-10

The shop has 15 items in three sections. Prices below are initial tuning, not a validated 45-minute second-run balance.

| Section | Item | Initial price | Effect |
|---|---|---:|---|
| Fresh Start | Keep the Skill Tree | 10 | Start with the tree unlocked |
| Fresh Start | Keep Pin 1 | 30 | Start with Pin 1 |
| Fresh Start | Keep Case Files | 30 | Start with Case Files unlocked |
| Fresh Start | Start Auto Spin | 20 | Start with Auto Spin level 1 |
| Fresh Start | Start Spin Limit | 25 | Start with Spin Limit level 1 |
| Unlocks | Unlock Statues | 10 | Unlock every pedestal and statue loot |
| Unlocks | Unlock Whiskey | 5 | Unlock the drink set and its skill branch |
| Unlocks | More Statue Gold | 20 | Ten levels, +10 percentage points of double-Gold chance per level; each level costs 5 more Gold (20 through 65) |
| Unlocks | Press Coverage | 30 | Keep existing case Gold chance and skill unlock |
| Permanent | Agency Reputation | 5 | +5% global money income per level; prices increase by 5 Gold each level |
| Permanent | Steel Tips | 50 | Keep +1 Strength for every pin |
| Permanent | Twin Barrel | 50 | Keep 20% double-shot chance; no Steel Tips prerequisite |
| Permanent | Double Box | 50 | Keep 15% double-reward chance |
| Permanent | Bottomless Glass | 50 | Keep automatic drink refill |
| Permanent | Fast Casting | 50, 75 | Two levels, each removes one required statue piece |

Every Fresh Start purchase requires only Keep the Skill Tree; the other four can be bought in any order. Case Files can be unlocked before owning Pin 1 (evidence still needs pins). Buying a perk immediately grants its skill in the current run and keeps that starting skill for future retirements. Grants preserve higher existing ranks and other skills, charge no money and trigger the normal skill/stat updates. Loading an existing save also applies any missing grants from already-owned Gold purchases. Seed Money and Golden Meridian are removed.

All statues now start at 1 Gold, without a pedestal-index multiplier or individual metal-tier upgrades. Existing money-tree statue payout upgrades still multiply the reward. More Statue Gold rolls once when a ready, unlocked statue shatters; maximum level guarantees double Gold. Incomplete or already-smashed statues cannot pay again. Fast Casting retains the existing minimum of two pieces per statue.

The active catalog, costs, rank limits and stat modifiers are serialized in GameBalance.asset under goldShopItems. GoldShopCatalog provides generation defaults. The shop UI rebuilds stale authored rows, headings and ordering when the catalog changes. The gameplay scene and its UI resources were updated through Unity.

Save version 2 merges old pedestal unlocks into the original unlock_statue_1 id, converts old per-statue tier ranks into shared bonus-chance ranks (capped at 10), and converts the old one-level Fast Casting into two levels to retain its two-piece reduction. Removed Seed Money and Golden Meridian ownership is discarded without refunds. Wallet values and skill-tree ranks are preserved. New saves do not repeat the conversion. No actual player save was reset for this task.

Changed files:

- Runtime Progression/GoldShop.cs, GameBalance.cs and Stats/StatId.cs
- Runtime Core/GameManager.cs and GameplayMath.cs
- Runtime Statues/FigurinePedestal.cs and UI/GoldShopView.cs
- Runtime Save/SaveData.cs and SaveStore.cs
- Editor Tools/EconomySnapshotMenu.cs
- Data/Balance/GameBalance.asset, Scenes/DetectiveOffice.unity and Art/UI/ScenePresentationResources.asset
- Tests/EditMode/ProgressionTests.cs, SaveTests.cs and ScenePresentationTests.cs
- Repository Tools/economy_model.py, Tools/economy_snapshot.json and documentation

Validation: 170/170 Unity EditMode tests passed (Logs/gold-shop-rework-editmode.xml), along with all nine Python economy tests and the 64-skill/four-heading layout check. Git diff whitespace checks passed. Automated checks cover shared unlocks, payout roll boundaries, actual statue shattering/resetting, Fast Casting levels, fresh-start skill grants, authored catalog tuning, save conversion and saved shop order. The statue integration fixture initially omitted its stat-source binding; it was corrected and the full suite rerun successfully. These checks do not establish human prestige-run timings. Retirement requirement growth and repeated-run economy calibration remain separate work.

## Immediate unlock fix

Fresh Start perks now apply when bought and when an existing save loads. SkillTreeState.GrantMinimum grants only missing ranks without charging money or reducing owned ranks. GameManager routes these grants through the same stat notifications and first-pin introduction state used by normal purchases. Existing saved Gold purchases are repaired automatically on the next game load.

Changed paths for this fix: Scripts/Runtime/Core/GameManager.cs, Scripts/Runtime/Progression/SkillTree/SkillTreeState.cs, Scripts/Runtime/Progression/GoldShop.cs, Tests/EditMode/ScenePresentationTests.cs, Data/Balance/GameBalance.asset and Scenes/DetectiveOffice.unity (relative to Assets/DetectiveDesk); repository Tools/economy_snapshot.json and this document. Updated descriptions explain that unlocks apply now and in future runs.

Validation: 171/171 EditMode tests passed (Logs/immediate-gold-unlocks-editmode.xml). The regression test buys all five perks, checks immediate ranks and stats without spending money, and verifies that subsequent Gold purchases preserve higher ranks. Git diff whitespace checks passed. No player save was manually edited or reset.

## Independent Fresh Start choices

All four Fresh Start options now require only Keep the Skill Tree. They retain their display order and prices but can be bought in any order. Case Files is granted even without owning Pin 1; pins are still needed to supply evidence. Updated GoldShop.cs, ProgressionTests.cs, ScenePresentationTests.cs, GameBalance.asset, DetectiveOffice.unity, Tools/economy_model.py, Tools/economy_snapshot.json and this document.

Validation: 171/171 EditMode tests passed (Logs/independent-fresh-starts-editmode.xml), including purchases in a different order and immediate Case Files ownership before Pin 1. All nine Python economy tests, the layout check and Git diff whitespace checks passed.

## Price update

The table above reflects the requested prices: statues 10, whiskey 5, Press Coverage 30; More Statue Gold costs 20 through 65 in steps of 5; Steel Tips, Double Box, Twin Barrel and Bottomless Glass each cost 50; Fast Casting costs 50 then 75. Twin Barrel has no Steel Tips prerequisite. Other prerequisites, rank limits and effects are retained.

Changed GoldShop.cs, GameBalance.cs, Data/Balance/GameBalance.asset, Scenes/DetectiveOffice.unity and Tests/EditMode/ProgressionTests.cs under Assets/DetectiveDesk, plus Tools/economy_snapshot.json, Docs/GAME_DESIGN.md and this document. Validation: 173/173 Unity EditMode tests passed (Logs/gold-shop-prices-editmode.xml), including linear statue prices, both Fast Casting prices, the 5-Gold whiskey purchase and Twin Barrel before Steel Tips. All nine Python economy tests, layout checks and Git diff whitespace checks passed. No player save was reset; repeated-run timing has not been recalibrated for these cheaper prices.
