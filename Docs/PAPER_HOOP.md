# Paper basketball hoop

Paper balls are the no-currency lootbox outcome. They can be used in a small aim-and-throw side game; neither a hit nor a miss grants money or gold.

- Click the paper-ball HUD button to enter aim mode.
- Aim at the hoop opening and click to throw. One ball is consumed at release.
- Right-click, Escape, or click the paper-ball button again to cancel before throwing, keeping the ball.
- A successful landing increments the **SCORED** counter above the hoop once. A miss leaves it unchanged.
- The lifetime count is saved in `SaveData.paperShotsScored` and survives retirement. Older saves start at zero; deleting the save resets it.
- `PaperShotScored` publishes the lifetime total for a future achievement system. No achievements or rewards are added yet.

The trajectory is a deterministic aiming arc with a horizontal landing test at the rim. It is not a swipe-power or physics-bounce simulation. The current radius, flight duration, arc height and result timing live in GameBalance.

## Changed files

- `ArtSource/Props/PaperHoop/`: editable Blender model, generator, FBX and reference notes. Walnut backboard, brass frame, red metal rim and open cotton net.
- `SpinTheGlobe/Assets/DetectiveDesk/Art/Props/PaperHoop/`: model, URP materials and prefab.
- `Scenes/DetectiveOffice.unity`: the hoop replaces the wastebasket in the right-hand corner, with a saved score plate and aim instructions.
- `Scripts/Runtime/PaperBall/PaperBallThrower.cs`: aiming, hit/miss, no-currency scoring, counter and save hooks.
- `Scripts/Runtime/Core/{GameManager,GameEvents}.cs` and `Scripts/Runtime/Save/SaveData.cs`: saved lifetime score, retention through prestige and score event.
- `Scripts/Runtime/Progression/GameBalance.cs` and `Data/Balance/GameBalance.asset`: remove paper payout, add toss tuning.
- `Scripts/Runtime/Lootbox/LootboxSystem.cs`: paper reward description and purchase blocking while throwing.
- `Scripts/Runtime/UI/{GameHUD,GoldShopView,SkillTreeView,SettingsMenu}.cs`: interaction guards and aim cancellation.
- `Scripts/Editor/Tools/ScenePresentationAuthoring.cs`: saved aim instructions and world-space score canvas.
- `Tests/EditMode/{PaperBallThrowerTests,SaveTests}.cs`: hits, misses, cancellation, duplicate prevention, old saves and score persistence.
- `Tools/economy_model.py` and `Tools/economy_snapshot.json`: no paper-ball income in simulation.
- `Docs/Production/Asset_Tracker.xlsx`: hoop asset entry.

Unity paths above are relative to `SpinTheGlobe/Assets/DetectiveDesk/`.

The old wastebasket source and prefab remain available as editable assets but are no longer placed in the gameplay scene. The paper-ball model is reused.

Validation: Blender 5.2.3 built and exported the hoop through the required wrapper (11,796 triangles, 38 meshes with UVs and exact embedded generator match). Blender and Unity previews are in the asset References folder. 190 Unity EditMode tests and 9 Python economy tests passed. The hoop is a clickable aim-point game; manual swipe strength and physics rim rebounds are not implemented.
