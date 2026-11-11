# Lootbox opening

Buying a lootbox opens a brass-framed modal with a horizontal reward reel. It slows to the actual reward over 4.5 seconds. Skip (or Space/Escape) reveals the result; Continue closes it. Double Box displays both rewards in the result text.

Rewards and payment still commit once at purchase. The animation never rolls or grants another reward. Further purchases and other menus are blocked until the result is dismissed. The world continues earning underneath the popup. Completion is checked after closing the popup to avoid overlapping the ending screen.

## Files
- `Scripts/Runtime/UI/LootboxOpeningView.cs`: authored layout, presentation, skip and reveal.
- `Scripts/Runtime/Lootbox/LootboxSystem.cs`: purchase guard and presentation hook; decorative rolls use an independent RNG with the existing eligible weighted table.
- `Scripts/Runtime/Core/GameManager.cs`: view lifecycle, input blocking and deferred ending display.
- `Scripts/Runtime/UI/{GameHUD,GoldShopView,SkillTreeView,SettingsMenu}.cs`: modal interaction guards.
- `Scripts/Runtime/Progression/GameBalance.cs` and `Data/Balance/GameBalance.asset`: `lootboxOpeningSeconds`.
- `Scripts/Editor/Tools/ScenePresentationAuthoring.cs` and `Scripts/Editor/Inspectors/GameManagerEditor.cs`: scene layout and Lootbox Opening preview.
- `Scenes/DetectiveOffice.unity` and `Art/UI/ScenePresentationResources.asset`: saved popup and persistent visual resources.
- `Tests/EditMode/LootboxOpeningTests.cs`: payment, duplicate purchase, skip, timed reveal, Double Box and locked statue eligibility.

Paths above are relative to `SpinTheGlobe/Assets/DetectiveDesk/`.

To preview without buying anything, select GameManager and choose **Lootbox Opening** in its scene presentation controls. Choose **Desk** to restore the normal view. Rewards are credited immediately, so the wallet may update before the reel finishes. Save timing remains the game's existing autosave lifecycle.

Loot eligibility: ice cubes have zero weight until Unlock Whiskey is owned. Statue pieces have zero weight until an unlocked, incomplete, available statue can accept them. Each unlock applies immediately and independently to purchases, Double Box rewards and the decorative reel; eligible rewards retain their relative weights.
