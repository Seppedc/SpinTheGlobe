# Spin The Globe — Project Architecture

## Repository layout
```
SpinTheGlobe/                      ← git repository root
├── README.md                      ← start here
├── AGENTS.md                      ← rules for AI agents (Codex, Gemini, Claude)
├── Docs/
│   ├── GAME_DESIGN.md             ← what we're building
│   ├── ARCHITECTURE.md            ← this file
│   └── Archive/                   ← superseded specs (prototype, web version)
├── Archive/BlenderSources/        ← older .blend sources kept for future edits (not imported by Unity)
└── SpinTheGlobe/                  ← Unity project (open this folder in Unity Hub)
    ├── Assets/
    ├── Packages/  ProjectSettings/
    ├── Tools/                     ← Blender build + asset-queue pipeline scripts
    ├── Docs/                      ← asset pipeline docs
    ├── ProductionLogs/            ← pipeline evidence/staging (not shipped)
    └── asset_registry.json · asset_queue.json · Asset_Tracker.xlsx · assets.csv
```

## Unity `Assets/` layout
```
Assets/
├── DetectiveDesk/                     ← everything we own
│   ├── Scripts/
│   │   ├── Runtime/                   DetectiveDesk.Runtime.asmdef
│   │   │   ├── Core/                  EventBus, GameEvents, GameManager (orchestrator)
│   │   │   ├── Economy/               Wallet, Currency, CostCurve, NumberFormatter
│   │   │   ├── Progression/
│   │   │   │   ├── Stats/             StatId, StatModifier, StatSheet
│   │   │   │   ├── SkillTree/         SkillTreeAsset, SkillTreeState, DefaultSkillTreeFactory
│   │   │   │   └── GameBalance.cs     (+ old SkillTreeDefinition/UpgradeConfig until replaced)
│   │   │   ├── Save/                  SaveData, SaveStore
│   │   │   ├── Globe/                 GlobeController, MeridianTrigger, GlobePinMarker, TachometerUI
│   │   │   ├── Pins/                  WallMapManager, PinInstance, PinSkin, PinCofferSlot, …
│   │   │   ├── Statues/               FigurinePedestal (to be reworked)
│   │   │   ├── Interaction/           desk click/hover presentation
│   │   │   ├── UI/                    skill tree panel, HUD pieces
│   │   │   ├── Audio/                 AudioManager
│   │   │   └── Legacy/                DetectiveGame (old HUD/case adapter, being replaced)
│   │   └── Editor/                    DetectiveDesk.Editor.asmdef
│   │       ├── Tools/                 Create Default Data menu
│   │       ├── Pipeline/              asset queue review, pin coffer Blender refresh
│   │       └── Inspectors/            custom inspectors
│   ├── Tests/EditMode/                DetectiveDesk.Tests.EditMode.asmdef (NUnit)
│   ├── Data/                          ScriptableObject instances. All balancing lives here
│   │   ├── Balance/                   GameBalance.asset, (old) UpgradeConfig.asset
│   │   ├── SkillTree/                 SkillTree.asset, (old) DefaultSkillTree.asset
│   │   └── Countries/                 country data + Natural Earth source GeoJSON
│   ├── Prefabs/                       Pins/, UI/
│   ├── Scenes/                        DetectiveOffice.unity (game), Dev/AssetReview.unity
│   ├── Materials/                     shared + gameplay materials
│   │
│   │   ── Art pipeline (owned by the Blender/asset-queue workflow; keep paths stable) ──
│   ├── ArtSource/                     editable .blend + generator scripts (canonical sources)
│   ├── AssetProduction/<Asset>/<run>/ pipeline outputs: FBX, materials, textures, review prefabs
│   ├── Globe/ GlobeBlender/ GlobeRework/ GlobeHolderRevision/
│   ├── PinCoffer/ ImportedPinBox/ PinLauncherRework/ LampRework/ AssetFurnaceStamp/
│   └── Generated/                     generated materials, textures, lighting profile
├── Objects/                           Pin.blend, Pinbox.blend (referenced by URP materials, keep)
├── Settings/                          URP pipeline assets, volume profiles
└── InputSystem_Actions.inputactions
```

## Code principles
- **Assemblies:** Runtime, Editor and Tests each have their own `.asmdef`. Runtime code cannot depend on editor code. Tests only see Runtime.
- **Namespaces** follow folders: `DetectiveDesk.Core`, `.Economy`, `.Progression`, `.Save`. Older code still uses `DetectiveDesk.Gameplay` until each system is rebuilt.
- **Data-driven:** every number a designer might tune is in `GameBalance` or `SkillTreeAsset`, never hard-coded.
- **Stats, not special cases:** every upgrade is a `StatModifier` on a `StatId`.
  - Formula: final = (base + Σadd) × max(0.05, 1 + Σpercent) × temporary buffs.
  - Per-pin stats take a pin index. A modifier with pin index −1 applies to every pin.
- **Events:** systems talk through `EventBus` (`PinFellOff`, `CurrencyChanged`, `SkillPurchased`, …), not through direct references.
- **Logic and views are separate:** `Wallet`, `StatSheet`, `SkillTreeState` and `SaveStore` are plain C# and unit-tested. MonoBehaviours only connect them to the scene.
- **Currency uses `double`** and is shown with `NumberFormatter` (1.23K, 4.50M, … aa, ab).
- **Saves:** JSON in `Application.persistentDataPath`. Writes are atomic and keep a `.bak` file. The file is versioned and has a migration hook. Skill ranks are saved by node **id**, so a shipped id must never be renamed.

## Asset and git rules
- Move or rename assets **inside Unity**, or always move the `.meta` file together with the asset. Scene references depend on the GUID stored in the `.meta` file.
- The `.blend` files are the source of truth for meshes (see `SpinTheGlobe/AGENTS.md` and `BLENDER_ASSET_STANDARD.md`).
- Backups are git commits, not copies of scenes or prefabs inside `Assets/`.
- Large binaries: before pushing to a remote, install **Git LFS** and track `*.blend *.fbx *.png *.psd *.wav`. Some textures are over 20 MB.
