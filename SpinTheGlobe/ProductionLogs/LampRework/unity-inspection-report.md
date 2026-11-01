Banker lamp inspection and backup

Active scene: Assets/DetectiveDesk/Generated/DetectiveOffice.unity. Edit Mode; dirty False before and after. Original active scene never saved.
Root: DetectiveOffice/Environment/Generated_BankersLamp
Source prefab: Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212422_3e4ba5f3/BankersLamp_Review.prefab
Scene copy: Assets/DetectiveDesk/Backups/LampRework_20261031_235708/DetectiveOffice.unity
Prefab copy: Assets/DetectiveDesk/Backups/LampRework_20261031_235708/BankersLamp_Review.prefab

World/local position (4.25, 0.815, 0.5153126); Euler (270, 0, 0); local scale (3.75, 3.75, 3.75).
Combined renderer bounds min (3.7325, 0.815, 0.2040626), max (4.7675, 2.33375, 0.8959376), size (1.035, 1.51875, 0.691875).
Desk slab renderer top Y = 0.815; lamp felt bottom Y = 0.815. Root pivot sits at desk surface/base bottom.
Source prefab origin is (0,0,0), scale (1,1,1), X rotation 270.0198 degrees; scene instance X rotation is 270 degrees. Preserve the existing axis correction and instance scale.
43 lamp transforms, 41 mesh renderers. No lamp colliders, scripts, Light components or Animator components. Components are Transform, MeshFilter, MeshRenderer only.
Imported mesh source: Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212422_3e4ba5f3/BankersLamp.fbx
Canonical detailed generator: Assets/DetectiveDesk/ArtSource/Props/BankersLamp/build_asset.py (read only, not executed).

Lighting conclusion: No Light component exists under Generated_BankersLamp or at its modeled bulb. Warm banker's lamp is a separate scene point/key light, elsewhere, not a physical lamp bulb light. Shared-light role inferred from position and range; no serialized gameplay reference to this light was found.
Bulb_OpalTube world bounds center (4.25, 2.09, 0.6090625); geometry/material only. Warm key is displaced (-2.92, -0.069, -1.3410625) from that bulb center.
All four scene lights inspected, including inactive components (all four are active and enabled):

- DetectiveOffice/Environment/Warm banker's lamp: Point, world/local position (1.33, 2.021, -0.732); Euler (0, 0, 0); RGB [1, 1, 1]; intensity 3.5; range 5.5; temperature 3200K, useTemperature=True; shadows Soft strength 0.45; bake Realtime; renderMode Auto; mask -1; distance from lamp pivot 3.396561. Spot angles 30/21.80208 are serialized but not operative for point lights.
- DetectiveOffice/Environment/Amber fill: Point, world/local position (-1, 3.5, -0.9); Euler (0, 0, 0); RGB [0.854902, 0.7607843, 0.6392157]; intensity 4; range 11; temperature 6570K, useTemperature=False; shadows None strength 1; bake Realtime; renderMode Auto; mask -1; distance from lamp pivot 6.064226. Spot angles 30/21.80208 are serialized but not operative for point lights.
- DetectiveOffice/Environment/Cool window fill: Point, world/local position (-4.65, 2.55, 1.1); Euler (0, 0, 0); RGB [0.3372549, 0.5019608, 0.7450981]; intensity 0.9; range 5.5; temperature 6570K, useTemperature=False; shadows None strength 1; bake Realtime; renderMode Auto; mask -1; distance from lamp pivot 9.086369. Spot angles 30/21.80208 are serialized but not operative for point lights.
- DetectiveOffice/Environment/Globe inner warm glow: Point, world/local position (-1.2, 2.1, -0.2); Euler (0, 0, 0); RGB [1, 0.5607843, 0.2]; intensity 0.45; range 1.8; temperature 6570K, useTemperature=False; shadows None strength 1; bake Realtime; renderMode Auto; mask -1; distance from lamp pivot 5.644945. Spot angles 30/21.80208 are serialized but not operative for point lights.

Serialized gameplay references: DetectiveOffice / DetectiveDesk.DetectiveGame.globeLight -> DetectiveOffice/Environment/Globe inner warm glow. No external gameplay component references to lamp objects/components or other light components found. Native transform hierarchy references are retained in JSON. Runtime lookups were not exercised.

Materials (preserved existing overrides):
- Assets/DetectiveDesk/AssetProduction/AgedBrassPBR/20261030_215235_85980d90/AgedBrassPBR.mat
- Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212422_3e4ba5f3/Materials/Lamp_EmeraldGlass.mat
- Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212422_3e4ba5f3/Materials/Lamp_Felt.mat
- Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212422_3e4ba5f3/Materials/Lamp_OpalLiner.mat

Camera: DetectiveOffice/MainCamera; position (0, 2.38, -5.8); Euler (4.985991,0,0); perspective vertical FOV 52; near/far 0.03/100; full viewport; scale (1,1,1).
before-game-camera.png: 839 x 521, rendered from existing Main Camera. Camera target/viewport/aspect and active RT restored in finally. Canvas and RectTransform serialized state guarded/restored in finally. Screen Space Overlay UI is not included by Camera.Render.

Reference review: SceneApproved.png and NoirLighting.png viewed. Current hood reads as a regular half-cylinder with prominent side cut/end and narrow curved stem. References show a fuller, shaped green hood, luminous ivory underside, thin continuous lip, turned brass stem collars and tiered base, long fine bead chain with pendant. These are observations for parent-supervised future work only; no creation performed.

Per-object pivots and renderer bounds (all exact source data and mesh-local bounds are in unity-inspection.json):
- DetectiveOffice/Environment/Generated_BankersLamp: local pivot (4.25, 0.815, 0.5153126), Euler (270, 0, 0), scale (3.75, 3.75, 3.75); no renderer
- DetectiveOffice/Environment/Generated_BankersLamp/Base_Felt: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.96125, 0.815, 0.2265626), max (4.53875, 0.82625, 0.8040626)
- DetectiveOffice/Environment/Generated_BankersLamp/Base_SpunBrass: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.93875, 0.82625, 0.2040626), max (4.56125, 1.085, 0.8265626)
- DetectiveOffice/Environment/Generated_BankersLamp/Bulb_OpalTube: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.95, 2.05625, 0.5753125), max (4.55, 2.12375, 0.6428125)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_00: local pivot (-0.082, -0.035, 0.326), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 2.0315, 0.6405625), max (3.9485, 2.0435, 0.6525624)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_01: local pivot (-0.082, -0.035, 0.3214), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 2.01425, 0.6405625), max (3.9485, 2.02625, 0.6525624)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_02: local pivot (-0.082, -0.035, 0.3168), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.997, 0.6405625), max (3.9485, 2.009, 0.6525624)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_03: local pivot (-0.082, -0.035, 0.3122), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.97975, 0.6405625), max (3.9485, 1.99175, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_04: local pivot (-0.082, -0.035, 0.3076), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.9625, 0.6405625), max (3.9485, 1.9745, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_05: local pivot (-0.082, -0.035, 0.303), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.94525, 0.6405625), max (3.9485, 1.95725, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_06: local pivot (-0.082, -0.035, 0.2984), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.928, 0.6405625), max (3.9485, 1.94, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_07: local pivot (-0.082, -0.035, 0.2938), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.91075, 0.6405625), max (3.9485, 1.92275, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_08: local pivot (-0.082, -0.035, 0.2892), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.8935, 0.6405625), max (3.9485, 1.9055, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_09: local pivot (-0.082, -0.035, 0.2846), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.87625, 0.6405625), max (3.9485, 1.88825, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_10: local pivot (-0.082, -0.035, 0.28), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.859, 0.6405625), max (3.9485, 1.871, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_11: local pivot (-0.082, -0.035, 0.2754), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.84175, 0.6405625), max (3.9485, 1.85375, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_12: local pivot (-0.082, -0.035, 0.2708), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.8245, 0.6405625), max (3.9485, 1.8365, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_13: local pivot (-0.082, -0.035, 0.2662), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.80725, 0.6405625), max (3.9485, 1.81925, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_14: local pivot (-0.082, -0.035, 0.2616), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.79, 0.6405625), max (3.9485, 1.802, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_15: local pivot (-0.082, -0.035, 0.257), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.77275, 0.6405625), max (3.9485, 1.78475, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_16: local pivot (-0.082, -0.035, 0.2524), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.7555, 0.6405625), max (3.9485, 1.7675, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_17: local pivot (-0.082, -0.035, 0.2478), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.73825, 0.6405625), max (3.9485, 1.75025, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_18: local pivot (-0.082, -0.035, 0.2432), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.721, 0.6405625), max (3.9485, 1.733, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_19: local pivot (-0.082, -0.035, 0.2386), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.70375, 0.6405625), max (3.9485, 1.71575, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_20: local pivot (-0.082, -0.035, 0.234), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.6865, 0.6405625), max (3.9485, 1.6985, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_21: local pivot (-0.082, -0.035, 0.2294), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.66925, 0.6405625), max (3.9485, 1.68125, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_22: local pivot (-0.082, -0.035, 0.2248), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.652, 0.6405625), max (3.9485, 1.664, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_23: local pivot (-0.082, -0.035, 0.2202), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.63475, 0.6405625), max (3.9485, 1.64675, 0.6525625)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_24: local pivot (-0.082, -0.035, 0.2156), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.6175, 0.6405626), max (3.9485, 1.6295, 0.6525626)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Bead_25: local pivot (-0.082, -0.035, 0.211), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.9365, 1.60025, 0.6405626), max (3.9485, 1.61225, 0.6525626)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Core: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.939875, 1.58375, 0.6439375), max (3.945125, 2.04875, 0.6491876)
- DetectiveOffice/Environment/Generated_BankersLamp/PullChain_Finial: local pivot (-0.082, -0.035, 0.201), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.92375, 1.55, 0.6278126), max (3.96125, 1.5875, 0.6653126)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_Axle: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.75125, 2.151875, 0.5734375), max (4.74875, 2.185625, 0.6071875)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_EndRim_-0.13: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (4.731875, 2.044816, 0.3146904), max (4.743125, 2.331875, 0.8884345)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_EndRim_0.13: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.756875, 2.044816, 0.3146904), max (3.768125, 2.331875, 0.8884345)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_Pivot: local pivot (0, -0.02, 0.361), Euler (0, 0, 0), scale (1, 1, 1); no renderer
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_Pivot/Shade_GreenGlass: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.77, 2.045, 0.3128124), max (4.73, 2.33375, 0.8903126)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_PivotCap_-0.13: local pivot (0.13, -0.02, 0.361), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (4.7075, 2.13875, 0.5603124), max (4.7675, 2.19875, 0.6203125)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_PivotCap_0.13: local pivot (-0.13, -0.02, 0.361), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.7325, 2.13875, 0.5603124), max (3.7925, 2.19875, 0.6203125)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_Rim_-0.1: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.76625, 2.039375, 0.8846875), max (4.73375, 2.050625, 0.8959376)
- DetectiveOffice/Environment/Generated_BankersLamp/Shade_Rim_0.054: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (3.76625, 2.039375, 0.3071874), max (4.73375, 2.050625, 0.3184375)
- DetectiveOffice/Environment/Generated_BankersLamp/Stem_Collar: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (4.17875, 1.05875, 0.4440625), max (4.32125, 1.17125, 0.5865626)
- DetectiveOffice/Environment/Generated_BankersLamp/Stem_CurvedBrass: local pivot (0, 0, 0), Euler (0, 0, 0), scale (1, 1, 1); world bounds min (4.225625, 1.141171, 0.3972377), max (4.274375, 2.215614, 0.5910572)

Preservation: 1,336 object records / 3,930 components, before versus after inspection deeply equal, including serialized component data, stable GlobalObjectIds, asset paths, object references, materials, transforms and bounds. Includes globe, cannon, cameras and UI. Dedicated Unity_ReadConsole Get returned 0 warning/error entries. JSON files parse successfully. Scene and prefab backup files exist and are nonempty. No Play Mode, model/script/tracker edits, light moves, installations, permissions/security changes or Blender processes started. Unity writer file handle released after each scoped inspection.

Limitations:
- Edit Mode only; runtime name/string lookups and runtime-created lights not evaluated.
- Main camera render excludes Screen Space Overlay UI; no canvas mode was changed.
- No Blender execution or model changes.
- Two relay command compilation attempts failed locally before successful command; no project script compilation errors reported by dedicated Console.
