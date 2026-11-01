# Read-only gameplay inspection

Scene: `Assets/DetectiveDesk/Generated/DetectiveOffice.unity`. Root `DetectiveOffice`. 1474 objects recorded with stable GlobalObjectIds, transforms, component serialized data, mesh/material paths and bounds. See `serialized-references.json` for authoritative live scene references; EditorJsonUtility alone emits zero instance IDs for some references.

Scene COPY: `Checkpoint/ActiveScene-copy.unity`. Original scene never saved or discarded. Dirty state false before and after. No Play mode, code wiring, scene placement, Blender, installs or gameplay script edits. Every existing Assets C# script was copied under Checkpoint/Assets; coffer and collectible pin prefabs copied. Backups are nonempty.

## Globe

`DetectiveOffice/GameplayObjects/MechanicalGlobe` is the actual globe root. Local position (0.134000063,0.06879997,0.0236), scale (0.92,0.92,0.92), yaw 0. Rotor `RotatingSphere` local position (-1.45,2.15,-0.33), scale 1, yaw 0.04716396 degrees; world center (-1.2,2.04680014,-0.28).

Geographic source formula in ArtSource/Gameplay/MechanicalGlobe/legacy_globe.py spherical(): radius*(cos(lat)*sin(lon), sin(lat), -cos(lat)*cos(lon)). Longitude 0 is front -Z; longitude +90 is +X. reference_globe.py uses radius .683 for coastline geometry; dark shell radius .6787 (actual imported mesh diameter 1.3574). Sphere meshes are centered at rotor origin after integration bake, not at the original absolute center. Marker rotor-local point is r*(cos(lat)*sin(lon),sin(lat),-cos(lat)*cos(lon)); root-local point includes center and rotor yaw. Use rotor.TransformPoint for world coordinates. Radius .683 becomes .62836 in world units.

West-to-east for this signed geographic vector requires NEGATIVE Unity Y yaw. Existing DetectiveGame.Update rotates +Y in world space at globeSpeed=7. DetectiveGlobeMechanism uses the same positive sign for its fallback and reads game.globeSpeed for gears/gauge. Both currently reference the same rotor; its fallback guard prevents current duplicate rotation, but a new controller would create duplication unless ownership is reconciled. Gear/gauge speed presentation must also be reconciled. Meridian/static brass geometry is under GlobeRework_Static; the tachometer needle pivot remains under MechanicalGlobe_Blender_Visuals/MechanicalGlobe_Blender/RPMGauge_Assembly/RPMGauge_NeedlePivot. No dedicated payout wire gameplay component exists.

## Map

Surface: `DetectiveOffice/Environment/CLEAN MAP - replace texture here`; Unity Cube mesh, 24 vertices, front face z=1.95349991. Material `Assets/DetectiveDesk/Materials/SceneIntegration/WorldMap_SceneUV.mat`; texture `Assets/DetectiveDesk/AssetProduction/WorldMap/20261030_185346_fb7b14cb/WorldMap_Parchment.png`. Source PNG 2048x900; Unity texture reports 2048x1024. Material scale (-1,-1), offset (1,1).

| World XYZ | Mesh UV | Texture UV |
|---|---|---|
| [4.11, 4.364453, 1.95349991] | [0, 1] | [1, 0] |
| [-3.01, 4.364453, 1.95349991] | [1, 1] | [0, 0] |
| [4.11, 1.235547, 1.95349991] | [0, 1] | [1, 0] |
| [-3.01, 1.235547, 1.95349991] | [1, 1] | [0, 0] |

x=-3.01+7.12*pixelX/2048; y=4.364453-3.128906*pixelY/900; z=1.95349991

INFERRED from visible graticule: lon [-180,180] -> pixels [88,1960], lat [85,-60] -> top-origin pixels [135,815]. Not verified against original texture authoring code.

Coastlines only, whitespace/title/footer included. Never map latitude/longitude across the full texture. No political country polygons located. Candidate point hotspots are feasible with explicit limitation; these are representative interior points, not country extents.

| Country | Lat, lon | Source pixel top-origin | World XYZ | Visible / obstruction |
|---|---|---|---|---|
| USA | 39.8, -98.6 | [511.28, 346.9724] | [-1.23250318, 3.15818167, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| UK | 54, -2 | [1013.6, 280.3793] | [0.5138436, 3.38969684, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| Belgium | 50.85, 4.35 | [1046.62, 295.151733] | [0.628639758, 3.33833957, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| France | 46.6, 2.2 | [1035.44, 315.082764] | [0.5897716, 3.269048, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| Brazil | -14.2, -51.9 | [754.12, 600.2138] | [-0.388254732, 2.27777219, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| Japan | 36.2, 138.25 | [1742.9, 363.855164] | [3.04930067, 3.09948778, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| Australia | -25.3, 133.8 | [1719.76, 652.269] | [2.968853, 2.09679914, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |
| Egypt | 26.8, 30.8 | [1184.16, 407.937927] | [1.10680628, 2.94623137, 1.95349991] | In viewport; collider hits 0, renderer bounds blockers 0 |

All eight candidate points are reachable from MainCamera, with no globe obstruction at those points. Ray tests include active renderers even without colliders; they are conservative bounds checks, not pixel-level depth tests. UK, Belgium and France need separate small hotspot radii to avoid overlap.

## Launcher, pins and coffer

Existing muzzle marker: `DetectiveOffice/GameplayObjects/ToyPinLauncher - aimed up at map/PinLauncher_ReworkedVisuals/PinLauncher/Turret_Yaw_Pivot/Elevation_Pivot/Recoil_Slide_Pivot/MuzzleTip`, world (2.765226,1.759845,1.03492785). Barrel runs along the imported assembly local +Y toward the marker; use actual marker position rather than the root or Muzzle_DarkBore surface. Source `Assets/DetectiveDesk/ArtSource/Gameplay/PinLauncher/PinLauncher.fbx`; visual prefab `Assets/DetectiveDesk/PinLauncherRework/DetectivePinLauncher_Visual.prefab`.

Pin prefab `Assets/DetectiveDesk/Prefabs/CollectiblePin.prefab`, geometry `Assets/Objects/Pin.blend`; generator `Assets/DetectiveDesk/ArtSource/Gameplay/CollectiblePin/build_collectible_pin.py`. Child geometry axis is +Z from foot toward enamel head; root rotation approximately X=-90 degrees maps it upward +Y. Head center child z=.0152; full head diameter about .01284. Hidden needle extends along Z. No colliders exist on this prefab. Exact unrounded child positions and mesh bounds are in asset-details.txt; preserve original orientation and twelve skin materials.

Coffer prefab `Assets/DetectiveDesk/PinCoffer/DetectivePinCoffer.prefab`, source `Assets/DetectiveDesk/ArtSource/PinCoffer/build_pincoffer.py` and canonical blend in same source directory. All twelve PinSlot_01..12 have null serialized occupant references. Preserve existing slots and their collider transforms. Exact world positions, references and serialized data are in inspection.json and scene-before.json.

## Figurines

Only StatueCollectibleSlot_0 and StatueCollectibleSlot_1 gameplay roots exist, displaying Detective and Archivist. Active base centers x=-3.55 and -2.83, z=-.3, base pivot y=.815; diameter .58. Inactive DisplayBase_03 and _04 exist inside imported multi-base groups but are not reserved gameplay slots. Third Commissioner_Assembly exists in the collectible statue review prefab, not as a third gameplay display.

Report-only candidate third base pivot (-2.85,.815,.4), footprint .6x.6, supported height range .82..1.88: clears active renderer bounds excluding supporting desk top/wear. Behind the current row; visual review and proper ground contact still needed. No object was created. Candidate (-3.55,.815,-1.15) was rejected due to hammer/glass overlap.

Collectible statues source: `Assets/DetectiveDesk/ArtSource/Props/CollectibleStatues/build_asset.py` and `CollectibleStatues.blend`; current imported prefab `Assets/DetectiveDesk/AssetProduction/CollectibleStatues/20261031_011441_91ecff05/CollectibleStatues_Review.prefab`. Contains Archivist, Commissioner and Detective assemblies. Base source: `Assets/DetectiveDesk/ArtSource/Props/StatueSlots/`; current prefab `Assets/DetectiveDesk/AssetProduction/StatueSlots/20261030_191201_78b848b4/StatueSlots_Review.prefab`.

No complete four-tier restoration set found: no authored rubble fragments or glued dark-seam tier. Existing bronze/brass statue materials do not establish a solid polished-gold tier. Kintsugi material/maps DO exist at `Assets/DetectiveDesk/AssetProduction/KintsugiMaterial/20261031_011641_bff2b921/Kintsugi.mat`; generator `Assets/DetectiveDesk/ArtSource/Props/KintsugiMaterial/Generate_Kintsugi.cs.txt`. This is a material, not a fracture/restoration mesh set; emission remains unverified per registry.

## UI and architecture

Input activeInputHandler=2 (Both); existing EventSystem uses StandaloneInputModule. Existing OnMouseDown interactions and Input.GetKeyDown(Escape) use legacy input. GameUI canvas and all inactive panel states/components are snapshotted. Settings panel has heading, detail and CLOSE; Upgrades panel has heading, description, BUY EUR100 and CLOSE. UI Button persistent calls and full reference bindings are in inspection.json. No settings sliders or separate restoration/speed/durability upgrade controls exist in the current panels.

DetectiveGame owns passive income, HUD, two panel toggles, fixed-cost income upgrade, case solving and positive globe speed. DeskInteractable routes mouse actions to it. DetectiveGlobeMechanism drives visual gears/gauge/emission. PinSkin and PinCofferSlot handle skins/socket occupancy. DeskAssetPresentation and DeskEvidenceInteraction provide scene presentation/evidence behavior. All existing scripts and exact paths are listed below; source backups are readable in Checkpoint/Assets.

- `Assets/DetectiveDesk/GlobeRework/Editor/GlobeHolderRefresh.cs`
- `Assets/DetectiveDesk/GlobeRework/Editor/GlobeIntegration.cs`
- `Assets/DetectiveDesk/GlobeRework/Editor/GlobeReview.cs`
- `Assets/DetectiveDesk/LampRework/Editor/LampReworkIntegration.cs`
- `Assets/DetectiveDesk/PinLauncherRework/Editor/PinLauncherCallbackSmoke.cs`
- `Assets/DetectiveDesk/Scripts/Editor/AssetQueueReview.cs`
- `Assets/DetectiveDesk/Scripts/Editor/PinCofferPopulationTools.cs`
- `Assets/DetectiveDesk/Scripts/Editor/PinCofferProductionPipeline.cs`
- `Assets/DetectiveDesk/Scripts/Editor/PinSkinEditor.cs`
- `Assets/DetectiveDesk/Scripts/Runtime/DeskAssetPresentation.cs`
- `Assets/DetectiveDesk/Scripts/Runtime/DeskEvidenceInteraction.cs`
- `Assets/DetectiveDesk/Scripts/Runtime/PinCofferSlot.cs`
- `Assets/DetectiveDesk/Scripts/Runtime/PinSkin.cs`
- `Assets/DetectiveDeskUnityStarter/Assets/DetectiveDesk/Scripts/Editor/DetectiveSceneBuilder.cs`
- `Assets/DetectiveDeskUnityStarter/Assets/DetectiveDesk/Scripts/Runtime/DeskInteractable.cs`
- `Assets/DetectiveDeskUnityStarter/Assets/DetectiveDesk/Scripts/Runtime/DetectiveGame.cs`
- `Assets/DetectiveDeskUnityStarter/Assets/DetectiveDesk/Scripts/Runtime/DetectiveGlobeMechanism.cs`
- `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`
- `Assets/TutorialInfo/Scripts/Readme.cs`

## Validation and limitations

Unity was not compiling and remained out of Play mode. Dedicated Console read returned zero warning/error entries initially; final Console evidence is stored separately. First helper failed because Newtonsoft was not referenced by the bridge compiler; corrected helpers compiled successfully without it. No project scripts were added. Capture before-game.png uses MainCamera at 1600x1000 with targetTexture/RenderTexture.active restored in finally; overlay UI is not rendered by Camera.Render. Canvas properties were never modified. Original scene dirty flag remained false after final inspection. Backups are nonempty; JSON parsed successfully. No runtime gameplay behavior was tested. Unity writer released after this inspection.