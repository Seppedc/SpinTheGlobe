# Spin The Globe

An active-play incremental clicker set on a noir detective's desk. Spin the globe, fire pins at the world map, close cases, and smash golden statues.

- **Engine:** Unity 6000.4.7f1 with URP.
- **Unity project:** the `SpinTheGlobe/` subfolder. Open that folder in Unity Hub.
- **Main scene:** `Assets/DetectiveDesk/Scenes/DetectiveOffice.unity`.

## Docs
- [Game design](Docs/GAME_DESIGN.md): the systems and the build order.
- [Architecture](Docs/ARCHITECTURE.md): folder layout, code principles and asset rules.
- [Agent rules](AGENTS.md) · [Blender → Unity skill](.agents/skills/blender-unity-assets/SKILL.md) · [Modelling standard](Docs/Pipeline/BLENDER_ASSET_STANDARD.md)
- [Art sources](ArtSource/README.md): latest `.blend` files, generators and reference images.

## First-time setup
1. Open `SpinTheGlobe/` in Unity Hub (6000.4.7f1).
2. Run **Tools ▸ Spin The Globe ▸ Create Default Data**. This creates `GameBalance.asset` and `SkillTree.asset`.
3. Open **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**. All tests should pass.

## Git
- Commit small, focused changes.
- Unity's `Library/`, `Temp/` and `Logs/` folders are ignored.
- Before adding a remote, set up Git LFS (see Architecture).
