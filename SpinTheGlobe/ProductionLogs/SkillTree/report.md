# Skill-tree inspection and checkpoint

Scene: `Assets/DetectiveDesk/Generated/DetectiveOffice.unity`. Edit Mode, 1 loaded scene; clean before and after saveAsCopy. 1579 objects, 4601 components (including transforms).

Protected art: 546 files hashed; 0 differ from prior Gameplay baseline. Physics backend ID 4072204805 matches PhysX (4072204805).

Full serialized properties, GlobalObjectIds, session IDs and object references are in scene-before.json. UI layout, CanvasScalers, all persistent callbacks, core bindings and third-figurine source mesh/material references are in inspection.json.

Current core components:
- `DetectiveDesk.DetectiveGame` ID 68692 at `DetectiveOffice`
  - money: 422
  - moneyPerMinute: 12.5
  - gold: 0
- `DetectiveDesk.Gameplay.GameManager` ID 68696 at `DetectiveOffice`
  - speedLevel: 0
  - motorLevel: 0
  - durabilityLevel: 0
  - selectedSkin: 0
- `DetectiveDesk.Gameplay.FigurinePedestal` ID 66652 at `DetectiveOffice/GameplayObjects/StatueCollectibleSlot_0`
  - tier: 0
- `DetectiveDesk.Gameplay.FigurinePedestal` ID 68826 at `DetectiveOffice/GameplayObjects/StatueCollectibleSlot_1`
  - tier: 0
- `DetectiveDesk.Gameplay.FigurinePedestal` ID 67194 at `DetectiveOffice/GameplayObjects/StatueCollectibleSlot_2`
  - tier: 0
- `DetectiveDesk.Gameplay.GameplayPanel` ID 68712 at `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls`
- `DetectiveDesk.DeskAssetPresentation` ID 68292 at `DetectiveOffice/DeskAssetIntegration`

Upgrades panel hierarchy and layout:
- `DetectiveOffice/GameUI/Upgrades panel` ID 67958; activeSelf=False
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(720, 600)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Upgrades heading` ID 68200; activeSelf=False
  - {'m_AnchorMin': '(0, 0.5)', 'm_AnchorMax': '(0, 0.5)', 'm_AnchoredPosition': '(26, 105)', 'm_SizeDelta': '(470, 50)', 'm_Pivot': '(0, 0.5)'}
  - text: 'UPGRADES'
- `DetectiveOffice/GameUI/Upgrades panel/Upgrade description` ID 67044; activeSelf=False
  - {'m_AnchorMin': '(0, 0.5)', 'm_AnchorMax': '(0, 0.5)', 'm_AnchoredPosition': '(26, 36)', 'm_SizeDelta': '(500, 45)', 'm_Pivot': '(0, 0.5)'}
  - text: 'Income upgrade: +€5/min for €100'
- `DetectiveOffice/GameUI/Upgrades panel/Button_BUY €100` ID 66780; activeSelf=False
  - {'m_AnchorMin': '(0, 0.5)', 'm_AnchorMax': '(0, 0.5)', 'm_AnchoredPosition': '(32, -45)', 'm_SizeDelta': '(220, 55)', 'm_Pivot': '(0, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Button_BUY €100/BUY €100` ID 68442; activeSelf=True
  - {'m_AnchorMin': '(0, 0.5)', 'm_AnchorMax': '(0, 0.5)', 'm_AnchoredPosition': '(10, 0)', 'm_SizeDelta': '(200, 55)', 'm_Pivot': '(0, 0.5)'}
  - text: 'BUY €100'
- `DetectiveOffice/GameUI/Upgrades panel/Button_CLOSE` ID 68342; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, -255)', 'm_SizeDelta': '(180, 55)', 'm_Pivot': '(0, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Button_CLOSE/CLOSE` ID 67334; activeSelf=True
  - {'m_AnchorMin': '(0, 0.5)', 'm_AnchorMax': '(0, 0.5)', 'm_AnchoredPosition': '(10, 0)', 'm_SizeDelta': '(160, 55)', 'm_Pivot': '(0, 0.5)'}
  - text: 'CLOSE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls` ID 68708; activeSelf=True
  - {'m_AnchorMin': '(0, 0)', 'm_AnchorMax': '(1, 1)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(0, 0)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Heading` ID 68592; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 258)', 'm_SizeDelta': '(640, 40)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'WORKSHOP / RESTORATIONS'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Summary` ID 67786; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 211)', 'm_SizeDelta': '(640, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'GLOBAL ×3.0   /   Ruby selected'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Row 0` ID 67860; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(-61, 151)', 'm_SizeDelta': '(488, 45)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'Terminal speed  L0 / €75'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE` ID 68004; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(262, 151)', 'm_SizeDelta': '(112, 42)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE/Caption` ID 66958; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(105, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'UPGRADE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Row 1` ID 67326; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(-61, 89)', 'm_SizeDelta': '(488, 45)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'Auto-motor  L0 / €100'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE` ID 66770; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(262, 89)', 'm_SizeDelta': '(112, 42)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE/Caption` ID 68486; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(105, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'UPGRADE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Row 2` ID 66636; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(-61, 27)', 'm_SizeDelta': '(488, 45)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'Pin durability  L0 / €60'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE` ID 66840; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(262, 27)', 'm_SizeDelta': '(112, 42)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE/Caption` ID 68584; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(105, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'UPGRADE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Row 3` ID 68576; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(-61, -35)', 'm_SizeDelta': '(488, 45)', 'm_Pivot': '(0.5, 0.5)'}
  - text: '1. Broken rubble ×1.0 / €120'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE` ID 67008; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(262, -35)', 'm_SizeDelta': '(112, 42)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE/Caption` ID 68062; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(105, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'RESTORE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Row 4` ID 67506; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(-61, -97)', 'm_SizeDelta': '(488, 45)', 'm_Pivot': '(0.5, 0.5)'}
  - text: '2. Broken rubble ×1.0 / €120'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE` ID 68430; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(262, -97)', 'm_SizeDelta': '(112, 42)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE/Caption` ID 66804; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(105, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'RESTORE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Row 5` ID 67814; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(-61, -159)', 'm_SizeDelta': '(488, 45)', 'm_Pivot': '(0.5, 0.5)'}
  - text: '3. Broken rubble ×1.0 / €120'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE` ID 66850; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(262, -159)', 'm_SizeDelta': '(112, 42)', 'm_Pivot': '(0.5, 0.5)'}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE/Caption` ID 68828; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, 0)', 'm_SizeDelta': '(105, 35)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'RESTORE'
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/Controls` ID 66918; activeSelf=True
  - {'m_AnchorMin': '(0.5, 0.5)', 'm_AnchorMax': '(0.5, 0.5)', 'm_AnchoredPosition': '(0, -216)', 'm_SizeDelta': '(650, 44)', 'm_Pivot': '(0.5, 0.5)'}
  - text: 'Drag left to right / choose a coffer color / pin a country\nIncome shown per minute is an estimate from rotations.'

Canvas scalers:
- `DetectiveOffice/GameUI` ID 67770: {'m_UiScaleMode': 1, 'm_ScaleFactor': '1', 'm_ReferenceResolution': '(1920, 1080)', 'm_ScreenMatchMode': 0, 'm_MatchWidthOrHeight': '0.5'}
- `DetectiveOffice/DeskAssetIntegration/CaseArchive` ID -8420: {'m_UiScaleMode': 1, 'm_ScaleFactor': '1', 'm_ReferenceResolution': '(1200, 900)', 'm_ScreenMatchMode': 1, 'm_MatchWidthOrHeight': '0.5'}
- `DetectiveOffice/DeskAssetIntegration/CaseCompletionEffect` ID -9980: {'m_UiScaleMode': 1, 'm_ScaleFactor': '1', 'm_ReferenceResolution': '(900, 600)', 'm_ScreenMatchMode': 1, 'm_MatchWidthOrHeight': '0'}

Persistent callbacks: 10 total.
- `DetectiveOffice/GameUI/Upper left HUD/Button_SET` [67854] -> `ToggleSettings`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68692, 'name': 'DetectiveOffice', 'type': 'DetectiveDesk.DetectiveGame', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1973946899-0', 'hierarchy': 'DetectiveOffice'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Settings panel/Button_CLOSE` [67316] -> `ToggleSettings`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68692, 'name': 'DetectiveOffice', 'type': 'DetectiveDesk.DetectiveGame', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1973946899-0', 'hierarchy': 'DetectiveOffice'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Button_BUY €100` [66784] -> `UpgradeIncome`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68692, 'name': 'DetectiveOffice', 'type': 'DetectiveDesk.DetectiveGame', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1973946899-0', 'hierarchy': 'DetectiveOffice'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Button_CLOSE` [68346] -> `ToggleUpgrades`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68692, 'name': 'DetectiveOffice', 'type': 'DetectiveDesk.DetectiveGame', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1973946899-0', 'hierarchy': 'DetectiveOffice'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE` [68008] -> `BuySpeed`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68712, 'name': 'Gameplay controls', 'type': 'DetectiveDesk.Gameplay.GameplayPanel', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1999109301-0', 'hierarchy': 'DetectiveOffice/GameUI/Upgrades panel/Gameplay controls'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE` [66774] -> `BuyMotor`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68712, 'name': 'Gameplay controls', 'type': 'DetectiveDesk.Gameplay.GameplayPanel', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1999109301-0', 'hierarchy': 'DetectiveOffice/GameUI/Upgrades panel/Gameplay controls'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/UPGRADE` [66844] -> `BuyDurability`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68712, 'name': 'Gameplay controls', 'type': 'DetectiveDesk.Gameplay.GameplayPanel', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1999109301-0', 'hierarchy': 'DetectiveOffice/GameUI/Upgrades panel/Gameplay controls'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE` [67012] -> `RestoreFirst`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68712, 'name': 'Gameplay controls', 'type': 'DetectiveDesk.Gameplay.GameplayPanel', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1999109301-0', 'hierarchy': 'DetectiveOffice/GameUI/Upgrades panel/Gameplay controls'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE` [68434] -> `RestoreSecond`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68712, 'name': 'Gameplay controls', 'type': 'DetectiveDesk.Gameplay.GameplayPanel', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1999109301-0', 'hierarchy': 'DetectiveOffice/GameUI/Upgrades panel/Gameplay controls'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}
- `DetectiveOffice/GameUI/Upgrades panel/Gameplay controls/RESTORE` [66854] -> `RestoreThird`; {'m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target': {'id': 68712, 'name': 'Gameplay controls', 'type': 'DetectiveDesk.Gameplay.GameplayPanel', 'assetPath': '', 'globalId': 'GlobalObjectId_V1-2-de62bb745eb8dd44c93d66a6d4b694f1-1999109301-0', 'hierarchy': 'DetectiveOffice/GameUI/Upgrades panel/Gameplay controls'}, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode': 0, 'm_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState': 2}

Existing skill-tree search hits under Assets: []. Reuse GameManager, GameplayPanel, UpgradeConfig and the existing Upgrades modal; no duplicate systems were created.

Backups: 45 files copied with matching SHA-256, plus live scene saveAsCopy and serialized UI panels. Original disk scene is preserved separately under Checkpoint/Files. All Gameplay runtime/editor scripts and their metadata were included to cover the requested manager/panel/config/runtime validation scripts and their dependencies.

Visual reference: `ProductionLogs/Gameplay/upgrades-panel.png` reused as authorized; no toggles, camera changes, Canvas changes, scene edits or original scene save.

Bridge stopped after dedicated Console read exposed an existing Unity AI NoSubscription log. No security, login, installation, model changes, reflection, Blender, agents, Play Mode or Console clear used. No tracker/queue writes.

Limitations: no fresh runtime physics probe; serialized backend ID and prior Gameplay public-API evidence used. No new screenshot. Array sizes are obtained by counting element fields, since raw dump labels ArraySize properties. Full reference and field data remains in JSON.

Actual array counts (counted serialized elements):
- DetectiveDesk.Gameplay.GameManager ID 68696: {'figurines': 3, 'pinSlots': 12}
- DetectiveDesk.Gameplay.FigurinePedestal ID 66652: {'bustRenderers': 18, 'restorationSeams': 3, 'authoredTierVisuals': 0}
- DetectiveDesk.Gameplay.FigurinePedestal ID 68826: {'bustRenderers': 19, 'restorationSeams': 3, 'authoredTierVisuals': 0}
- DetectiveDesk.Gameplay.FigurinePedestal ID 67194: {'bustRenderers': 20, 'restorationSeams': 3, 'authoredTierVisuals': 0}
- DetectiveDesk.Gameplay.GameplayPanel ID 68712: {'upgradeLabels': 3, 'figurineLabels': 3, 'upgradeButtons': 3, 'figurineButtons': 3}
- DetectiveDesk.DeskAssetPresentation ID 68292: {'evidenceTextures': 6, 'slots': 12, 'pinNames': 4, 'pinNumbers': 4, 'pinHeads': 4}

All three figurines are tier 0. Their authoredTierVisuals arrays are empty; the existing runtime part-restoration fallback remains applicable. Third figurine uses the existing CollectibleStatues FBX and StatueSlots FBX; full exact source/material paths are listed in inspection.json.

GameManager ID 68696; GameplayPanel ID 68712; legacy HUD adapter ID 68692; MainCamera ID 68048. Upgrade branches are level 0/0/0; selected skin 0; existing legacy cash 422, income estimate 12.5, gold 0. These serialized values were not changed.

GameUI CanvasScaler reference resolution is 1920x1080; Archive is 1200x900; CaseCompletionEffect is 900x600. One camera and five lights were captured with transforms and all serialized fields. No component named UIReferences exists in the live scene; actual bindings reside in DetectiveGame, GameplayPanel, GameManager and DeskAssetPresentation.

Final filesystem verification: all 45 backed-up originals remain byte-identical to their checkpoint hashes, and every backup matches. The original scene was not saved or changed. Writer released; no further Unity bridge calls pending.
