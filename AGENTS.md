# Workspace routing

The Unity project root is `SpinTheGlobe/` (containing Assets, Packages and ProjectSettings), not this parent workspace directory. Before working on Unity or 3D assets, read and follow `SpinTheGlobe/AGENTS.md`. For new or modified 3D assets, apply `SpinTheGlobe/.agents/skills/blender-unity-assets/SKILL.md`. Use the project-local Blender generator workflow, preserve existing gameplay and user edits, and do not save or modify open scenes during standalone workflow tests.

## Project structure & code (updated 2026-11-02)

- Read `Docs/GAME_DESIGN.md` (what we build) and `Docs/ARCHITECTURE.md` (folders, assemblies, code rules) before gameplay work.
- Git is the backup system: never copy scenes/prefabs into backup folders inside `Assets/`; commit instead.
- Move/rename Unity assets together with their `.meta` files. Keep art-pipeline folder paths (ArtSource, AssetProduction, Globe*, PinCoffer, …) stable.
- Older `.blend` sources that are not imported by Unity live in `Archive/BlenderSources/`. Never delete `.blend` files.

