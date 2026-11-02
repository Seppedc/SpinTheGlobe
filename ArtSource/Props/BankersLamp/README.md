# Banker lamp — reference detail revision

Canonical editable source: `BankersLamp.blend`. Entry `build_asset.py` calls the original-derived `lamp_detail.py`; both exact Python sources are embedded in Blender. `BankersLamp.fbx` is exported from that same build, with twelve packed/external procedural PBR maps in `Textures/`. No downloaded texture dependencies.

Study `Inspiration/SceneAndValidatedAssets/SceneApproved.png` and local `References/NoirLighting.png` before revisions. The lamp is cropped in the reference, so its established base footprint and placement take precedence over invented extensions. The revised rounded emerald hood has closed ends, shell thickness, an ivory interior and continuous rolled brass lip. The banded column, concentric foot, knurled hinges, ceramic socket, bulb, supported 48-bead chain and pendant are separate named meshes.

Build from the Unity root into a fresh designated staging folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-one.ps1 -Slug BankersLamp -OutputDirectory ProductionLogs/LampRework/MyRevision
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background ProductionLogs/LampRework/MyRevision/BankersLamp.blend --python Tools/validate-lamp-rework.py
python Tools/promote-lamp-rework.py ProductionLogs/LampRework/MyRevision
```

Wait for each Blender process to finish before promotion or Unity writing. The validator reopens the saved source, compares both embedded scripts, checks geometry/UVs/maps, closed hood, pivots and vertical chain, then imports the FBX independently. Promotion preserves prior canonical outputs in ProductionLogs. Never substitute a starter for this revision.

Units are metres, Blender Z-up, root at the base bottom. Source +Y faces the player under the existing Unity root's X270 correction. Keep `Shade_Pivot` at its authored 10 degree neutral tilt, with independent `BulbSocket_Pivot` and gravity-hanging `PullChain_Pivot`. `LampLightOrigin`/`LampLightTarget` mark a downward practical light. Do not create new light gameplay or move the shared room key.

Unity presentation uses explicit reusable URP/Lit materials with separate green/ivory/edge shade slots. Emerald is an opaque coated glass approximation for stable URP rendering, not refractive transmission. Only the bulb and subdued lining emit. Use FBX for predictable import; preserve the editable .blend and exact source alongside it.

Integration output and evidence: `Assets/DetectiveDesk/LampRework/`. Existing scene root, pose, old prefab, scripts and unrelated objects must be preserved; replace only visible lamp children after backup and verification. Human visual approval remains pending until reviewed.
