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
- **Namespaces:** the pure-logic layers use `DetectiveDesk.Core`, `.Economy`, `.Progression` and `.Save`. The MonoBehaviour feature systems share `DetectiveDesk.Gameplay`.
- **Composition root:** `GameManager` owns the Wallet, Inventory, StatSheet and SkillTreeState, adds each feature system to its own GameObject, and calls `Initialize(game)` after loading the save. Feature systems talk through `EventBus` and the GameManager, not to each other directly.
- **Data-driven:** every number a designer might tune is in `GameBalance` or `SkillTreeAsset`, never hard-coded.
- **Stats, not special cases:** every upgrade is a `StatModifier` on a `StatId`.
  - Formula: final = (base + Σadd) × max(0.05, 1 + Σpercent) × temporary buffs.
  - Per-pin stats take a pin index. A modifier with pin index −1 applies to every pin.
- **Events:** systems talk through `EventBus` (`PinFellOff`, `CurrencyChanged`, `SkillPurchased`, …), not through direct references.
- **Logic and views are separate:** `Wallet`, `StatSheet`, `SkillTreeState` and `SaveStore` are plain C# and unit-tested. MonoBehaviours only connect them to the scene.
- **First-use guidance:** `PlayerGuidanceState` stores lifetime completion ids in `SaveData.completedGuidance`; `FirstUseGuidance` drives the globe prompt and `ObjectPulseOverlay` draws the surface pass without replacing materials. Appearance lives in GameBalance. See [FIRST_USE_GUIDANCE.md](FIRST_USE_GUIDANCE.md).
- **Purchase presentation:** `PurchasePalette` defines grey/unavailable, brown/available and gold/maxed colours for skill nodes, tooltip cards and Gold shop rows. Purchase buttons opt in through `ThemedButton.SetPurchaseState`. See [PURCHASE_COLOURS.md](PURCHASE_COLOURS.md).
- **Authored presentation:** HUD, brass plates and menu layouts live under `DetectiveOffice/Scene presentation`. Runtime code binds to serialized views; the GameManager inspector offers editor previews. See [SCENE_PRESENTATION.md](SCENE_PRESENTATION.md).
- **Tree sections:** `SkillSectionData` in `SkillTreeAsset` defines free headings and shared connectors. Sections organize the view; only `SkillNodeData` represents purchasable, saved skills. See [SKILL_TREE_SECTIONS.md](SKILL_TREE_SECTIONS.md).
- **Gold shop:** `GameBalance.goldShopItems` stores the active catalog and tuning; `GoldShopCatalog.CreateDefaults` generates defaults. `GoldShopState` applies purchases and fresh-start grants. Save version 2 migrates the consolidated statue upgrades. See [GOLD_SHOP_REWORK.md](GOLD_SHOP_REWORK.md).
- **Bulk skill purchases:** `SkillTreeState.QuotePurchase` previews the affordable quantity and sum of rank prices. Its batch `TryPurchase` commits one wallet transaction and publishes the final rank once; single purchases use the same path. `SkillTreeView.Purchases.cs` owns the authored amount controls.
- **Currency uses `double`** and is shown with `NumberFormatter` (1.23K, 4.50M, … aa, ab).
- **Saves:** JSON in `Application.persistentDataPath`. Writes are atomic and keep a `.bak` file. The file is versioned and has a migration hook. Skill ranks are saved by node **id**, so a shipped id must never be renamed.

## Asset and git rules
- Move or rename assets **inside Unity**, or always move the `.meta` file together with the asset. Scene references depend on the GUID stored in the `.meta` file.
- The `.blend` files are the source of truth for meshes (see `SpinTheGlobe/AGENTS.md` and `BLENDER_ASSET_STANDARD.md`).
- Backups are git commits, not copies of scenes or prefabs inside `Assets/`.
- Large binaries: before pushing to a remote, install **Git LFS** and track `*.blend *.fbx *.png *.psd *.wav`. Some textures are over 20 MB.
