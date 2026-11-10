# Scene presentation

The main scene now contains the HUD, desk brass plates, shooter reload ring,
skill tree, Settings and Gold Shop layouts before Play mode starts.

In the hierarchy, expand `DetectiveOffice > Scene presentation`. Move and style
these objects normally. Gameplay reuses the serialized objects, updates their
values and binds their controls; it does not rebuild their positions at startup.
Keep the control names and assigned references intact because button bindings
use their hierarchy paths.

Select `DetectiveOffice` and look below the GameManager inspector for **Scene
presentation**. **Desk**, **Skill Tree**, **Settings** and **Gold Shop** preview
the corresponding layout without starting gameplay or loading a save. The skill
tree preview shows every upgrade. Save with Desk selected for the normal editor
view; runtime initialization closes menus regardless of the saved preview.
**Create missing scene layouts** fills missing top-level layouts without
duplicating existing objects. It retains existing transforms.

The tree now includes a free Globe section heading. It has no
purchase action; their positions and membership are stored separately from
skills in the tree asset. See [SKILL_TREE_SECTIONS.md](SKILL_TREE_SECTIONS.md).

Preview values represent a new run rather than the player's saved progress.
Brass plates and the reload ring have sample text/progress so they can be placed.
Pins, flying projectiles, case evidence and floating payout numbers still appear
in response to gameplay; their source models remain in their existing scene
objects/prefabs. Merely browsing the editor does not run the economy or write a
game save.

Fonts, their baked atlases, generated sprites and plate materials are persisted
in `Assets/DetectiveDesk/Art/UI/ScenePresentationResources.asset`. This avoids
missing fonts/materials after the scene or editor is reopened. The baked fonts
cover the current UI alphabet and symbols; future translations may need extra
glyphs. Runtime-created payout text retains its existing dynamic font support.

Changed paths, relative to `SpinTheGlobe/Assets/DetectiveDesk/`:

- `Scenes/DetectiveOffice.unity`
- `Art/UI/ScenePresentationResources.asset` and `.meta`
- `Scripts/Runtime/Core/GameManager.cs`
- `Scripts/Runtime/UI/{GameHUD,DeskInfoSystem,SkillTreeView,SettingsMenu,GoldShopView}.cs`
- `Scripts/Runtime/UI/SceneViewBindings.cs` and `.meta`
- `Scripts/Editor/Tools/ScenePresentationAuthoring.cs` and `.meta`
- `Scripts/Editor/Inspectors/GameManagerEditor.cs` and `.meta`
- `Scripts/Editor/DetectiveDesk.Editor.asmdef`
- `Tests/EditMode/ScenePresentationTests.cs` and `.meta`
- `Tests/EditMode/DetectiveDesk.Tests.EditMode.asmdef`

Documentation: this file and `Docs/ARCHITECTURE.md`.

Validation: 84/84 EditMode tests passed. Regression tests cover repeated layout creation, preserving
authored positions, HUD actions, restoring serialized skill node references and
avoiding duplicate button callbacks. Live checks confirmed that scene reopening
retains all 253 text/font references, runtime buttons open the three menus, no
duplicate presentation hierarchy is generated, and no Unity console errors
occurred during the Play-mode check. All original scene records were preserved.
Test evidence: `SpinTheGlobe/Logs/scene-presentation-editmode.xml`.

Skill-tree Buy 1 / Buy 5 / Buy 10 controls are authored under `Skill Tree/Backdrop/Purchase Amount`, with selection defaulting to Buy 1. Layout restoration reuses these controls and rebinds their actions. The current scene includes them before Play.

Upgrade colours follow the shared [purchase palette](PURCHASE_COLOURS.md): grey unavailable, brown affordable and gold maxed/owned. The authored tree and Gold shop were refreshed with this palette; runtime derives the current state from the player wallet and progression.
