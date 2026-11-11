# Shared pin upgrade prices — 2026-11-10

Every pin has 10 Strength levels and 20 Pay levels. Pins 2–12 now use Pin 1's respective upgrade price curves, including explicit prices and rounding. Strength and Pay remain separate curves; their effects are unchanged. Pin 1's retained levels cost the same as before. New-pin purchase prices are unchanged and were verified in Unity before and after the change.

Existing Strength ranks above 10 clamp to 10 when loaded, using the existing skill rank loading rule. Pay ranks still cap at 20. Strength therefore provides up to 11 passes before other bonuses. The existing colour mapping automatically spans the available palette and reaches metallic gold at Strength level 10. No player save was manually edited or reset, and no refunds were added.

Changed files under Assets/DetectiveDesk: Scripts/Runtime/Progression/SkillTree/DefaultSkillTreeFactory.cs, Data/SkillTree/SkillTree.asset, Tests/EditMode/ProgressionTests.cs and ScenePresentationTests.cs. Repository changes: Tools/economy_snapshot.json, Docs/GAME_DESIGN.md and this document. No scene layout change was needed.

Validation: 173/173 EditMode tests passed (Logs/shared-pin-prices-final-editmode.xml). Coverage includes every Strength and Pay price across all 12 pins, unchanged default new-pin prices, older saved ranks clamping correctly and distinct Strength colours reaching gold at the new cap. Initial colour checks assumed 20 levels; they were updated to use the authored maximum. All nine Python economy tests, the 63-skill/four-heading layout check and Git diff whitespace checks pass. These checks do not establish revised human retirement timings; cheaper late-pin upgrades can increase income sooner.
