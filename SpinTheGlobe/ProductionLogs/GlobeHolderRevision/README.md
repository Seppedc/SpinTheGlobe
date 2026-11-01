## Current status: installed and verified in Unity

The earlier staged-only state below is historical. Unity integration has now completed; the scene is saved and clean, runtime/preservation checks passed and the current Console read returned zero warnings/errors. Current report and Game screenshots: Assets/DetectiveDesk/GlobeHolderRevision/. See integration-result.json for executed final evidence. The earlier initial refresh failure/review rejection was resolved through a different, channel-preserving exact-buffer notification; no sandbox bypass or rejected mesh-clear operation was used.

# Globe holder revision — staged, not applied to Unity

User approved the current world sphere and material colors, requested removal of the right holder half and exposed gears, and marked a lower/inward RPM location against the base. The annotated image takes precedence over the older overall reference for the gauge position.

Executed: copied the actual existing detailed generators, source blend/FBX, current saved scene, six modular prefabs, materials/native meshes and tracker into `Checkpoint/`. Updated a copy of the established `reference_globe.py`, ran installed Blender 5.2.3 in an isolated process, saved editable `Source/MechanicalGlobe.blend` with all three exact Python sources embedded, exported `Source/MechanicalGlobe.fbx`, generated two preview renders, reopened and independently validated both formats. No live globe asset, model, prefab, material, gameplay target or scene was changed.

The 180-degree left brass band has closed end faces, matching half-length polished edges and graduations. Pole bearings/finial and the original pedestal remain. Exposed gear meshes are absent; empty semantic gear pivots remain for later gameplay compatibility. Gauge center moves in original globe-parent Unity coordinates from (-.49,1.43,-.75) to (-1.23,1.18,-.80), delta (-.74,-.25,-.05), retaining its .16m radius and original needle hierarchy. The compact support meets the base. This placement was actually inspected in the Blender render and approximates the user's blue circle.

Independent validation: 51 meshes, 235110 triangles, zero zero-area triangles, UVs/materials on every mesh, 24 packed images. All thirteen sphere meshes have exactly matching vertex/topology/UV/material-name/world-matrix hashes against the original saved blend. All original packed texture PNG payloads also match exactly. Source sphere and pedestal pivots, root scale and desk-height fit are retained. FBX reimport retained mesh count, UVs, material assignments and dimensions. `Source/source-validation.json` contains proof. `Source/blender-preview.png` and `blender-threequarter.png` are Blender previews; their studio lighting is not the Unity Game appearance.

Unity import, native-mesh refresh, scene placement, current Console and runtime checks were NOT executed for this revision. The installed CLI Unity bridge returned an explicit usage-limit error during the preceding lamp refinement. No retry/bypass, overnight queue or parallel writer was started. Live source files remain untouched; the existing integrated globe is still active. See `handoff.json` for source/checkpoint paths and hashes.

## Resume when Unity MCP is available

1. Read project AGENTS/Blender skill and this report; inspect current scene/prefabs/dependencies through MCP, protect any user edits, take a fresh scene/prefab backup. Revalidate staging if the original globe changed.
2. Review the staged Blender image with the user's annotation. Preserve the currently approved sphere native meshes and all nine URP material assets byte-for-byte. No sphere rebuild/recolor or lighting/camera change is needed.
3. Merge the staged `reference_globe.py` back into the established canonical source, then promote this independently validated same-source blend/FBX after Blender exits. Do not promote until the visual refresh is ready. Do not use the generic starter or run the initial `GlobeIntegration.Build()` on an already integrated globe.
4. Refresh the existing Static module's holder/support/dial native meshes by stable object name and existing mesh GUID, using the verified FBX import rotation composition. Keep removed native mesh assets for rollback, disable obsolete right labels/gear renderers, and do not duplicate modules. Keep original gear target transforms/controllers alive but without exposed visuals.
5. Move the existing RPM needle gameplay target by the authorized gauge delta in globe-root coordinates, preserving its identity, references, original rotation/calibration and animation axis; inspect actual parenting before converting the delta. Refresh the existing Needle module relative to that new target pose. The source's relocated RPMGauge_Assembly and child needle are authoritative. Do not leave the needle rotating about its former center.
6. Rebind hover feedback only as needed to the active remaining visual renderers. Preserve globe interaction, colliders, root/RotatingSphere pose, speed and script references, camera/HUD, lamp, cannon and every unrelated object. Run meaningful sphere/needle runtime checks and preservation comparison with only authorized holder/gauge changes allowed.
7. Save authorized prefab/scene changes, capture Game view, inspect materials/Console, refresh current evidence and synchronize queue/Excel/registry. Do not claim staged Blender validation as Unity success or approve the whole asset automatically.

Revalidation command from Unity root:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background ProductionLogs/GlobeHolderRevision/Source/MechanicalGlobe.blend --python-exit-code 1 --python Tools/validate-globe-holder-revision.py
```
