# Detective Desk — Unity 6 URP Starter Prototype

A procedural 3D starter scene inspired by the agreed **straight-on, seated** noir detective-desk game design. This is a **working blockout**, not the photorealistic image converted into 3D; textures/models are simple placeholders to replace later.

## Quick start (Unity 6)

1. Open **Unity Hub** and create a new **Universal 3D (URP)** project in Unity 6. The template already configures URP.
2. Close the Unity Editor (optional, but makes file copying easier).
3. Copy the **`Assets/DetectiveDesk`** folder from this ZIP into the Unity project's `Assets/` folder. Don't copy the outer `Assets` directory into another `Assets` directory.
4. Reopen Unity and allow scripts to import. The package uses the built-in **Unity UI (uGUI)** package normally included with a Universal 3D project. If `UnityEngine.UI` cannot be found, install **Unity UI** in Package Manager.
5. Select **Tools → Detective Desk → Generate Starter Scene** from the Unity menu. Save other open scenes if prompted. The command **replaces the currently open scene** after asking to save changes.
6. Open **Assets/DetectiveDesk/Generated/DetectiveOffice.unity** if not already open, select a **16:9 Game view**, and press **Play**.
7. The generated scene lives in `Assets/DetectiveDesk/Generated/`. The generator also writes map/window PNG textures and URP materials there.

## What you get

- Locked, straight-on first-person camera; aged mahogany desk shifted forward from back wall.
- Clean, framed parchment world map with procedural **abstract placeholder continents**. **No pins, strings, photographs or overlay elements are attached to the map.** Replace the map material texture with real cartography later.
- Rain-streaked left window, warm key and cool fill lights, partly right-framing emerald banker's lamp.
- Slightly enlarged globe left of center, with clockwork detail placeholders, meridian arc, spinning sphere, amber light, and RPM gauge.
- Collectible statue display bases and tiny hammer, whiskey bottle and glass, crimson-lined colorful pin coffer, **upward-aimed toy pin launcher**, case files on the right, and far-right rubber stamp; **no evidence crate or loose pins**.
- Minimal dark-brass HUD: **Settings** at far left, **Money**, **Money/min**, **Gold**, and **UPGRADES** at far right.
- Play-mode click interaction (mouse) for globe, statues, pin box, launcher, case files and stamp.
- Functional money accumulation from income/minute, a €100 income upgrade (+€5/min), panel toggles, and a one-time stamp reward (+€50).
- Asset grouping designed to replace placeholder primitives with prefabs while keeping game logic separate.

## Controls and actions

| Action | Effect |
|---|---|
| Click globe | Displays mechanical globe status |
| Click statue | Displays collectible hint |
| Click pin coffer | Select-pin placeholder feedback |
| Click toy launcher | Pin-launch placeholder feedback |
| Click case files | Displays inspect-case hint |
| Click stamp | Marks case SOLVED once and awards €50 |
| Settings (SET) | Opens/closes settings panel |
| UPGRADES | Shows income-upgrade purchase panel |
| Esc | Closes either panel |

Money and gold are sample balances stored in memory only; **no persistence/save system** yet. Hover lighting is subtle and depends on material shader behavior.

## Project files

```text
Assets/DetectiveDesk/
├── Scripts/
│   ├── Editor/
│   │   └── DetectiveSceneBuilder.cs  # Unity editor menu and procedural scene creation
│   └── Runtime/
│       ├── DetectiveGame.cs         # Economy, HUD and basic game logic
│       └── DeskInteractable.cs      # Mouse-driven interactions
└── Generated/                      # Scene/material/texture outputs created by tool
```

## Recommended next production steps

1. **Blockout:** tweak camera FOV and positions in `CameraSetup()` and `BuildRoom()` until the in-game framing matches your reference.
2. **Globe:** replace primitive sphere/arc with a Blender or purchased FBX/GLB-style asset (prefer FBX for Unity workflow), preserving the `RotatingSphere` pivot and `DetectiveGame.globeRotor` reference.
3. **Visuals:** replace the procedural map with a licensed parchment world-map texture, use a rain shader on the window, and add URP postprocessing (bloom, color grading, ambient occlusion).
4. **Props:** swap each placeholder for a model/prefab while keeping its parent, collider, and `DeskInteractable` component.
5. **Polish:** replace UI legacy Text with TextMeshPro if desired, add audio, tooltips, outline effects, pin firing, statue inventory, cases, save data, and an actual settings menu.

## Known limitations

- **Not tested inside the Unity Editor** here; built as source for Unity 6, with possible small project-specific adjustments.
- Game is a scene prototype, not an exported executable or full .unitypackage. Copy folder into your URP project.
- Placeholder continent silhouettes are intentionally stylized and **not a geographically accurate world map**.
- The globe geometry, gears, whiskey, pins and launcher are deliberately simple generated meshes; replacing them with detailed assets will be necessary to achieve the reference image's photorealism.
- Unity's `OnMouseDown` interactions expect mouse input compatibility (active input handling set to Input Manager or Both). If you use the new Input System only, switch **Edit → Project Settings → Player → Active Input Handling** to **Both**, or migrate the pointer code.
- Material emission highlights depend on the URP material settings; the clickable status messages work independently.
- Generating again recreates the scene. Make backups of custom changes first. Asset names for generated materials/textures may be updated.
