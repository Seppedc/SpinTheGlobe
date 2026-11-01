# DetectiveDesk Blender globe — completed integration

Blender 5.2.3 LTS was found at
`C:/Program Files/Blender Foundation/Blender 5.2/blender.exe` and executed successfully.
The model was built, visually reviewed with a Blender render, exported as FBX,
imported into Unity 6 URP, verified in a separate preview scene, and integrated.

## Files

- `build_mechanical_globe.py`: standalone source builder with embedded Natural Earth
  geographic polygons. Uses Blender's Python, bpy, bmesh, mathutils and stdlib only.
- `Export/MechanicalGlobe_Blender.blend`: editable Blender model.
- `Export/MechanicalGlobe_Blender.fbx`: exported model, 155,186 triangles, 81 mesh objects.
- `Export/Textures/`: 18 base-color, tangent normal and packed metallic/smoothness PNGs.
- `Export/UnityImportManifest.json`: Blender export dimensions, named pivots and materials.
- `Blender_ModelPreview.png`: Blender asset-review render.
- `BlenderGlobe_GameCamera.png`: final scene screenshot including the existing HUD.
- `UnityRuntimeValidation.txt`: real-frame Play Mode validation result.
- `inspection_and_status.json`: completion report.

Unity assets:

```text
Assets/DetectiveDesk/GlobeBlender/
  MechanicalGlobe_Blender.fbx
  Textures/
  Materials/                         # Six reusable URP/Lit materials
  NativeMeshes/                      # Imported geometry with Unity-native axes/pivots
  Prefabs/MechanicalGlobe_Blender_Visuals.prefab
  Prefabs/MechanicalGlobe_Blender.prefab
```

The original `Assets/DetectiveDesk/Globe/Prefabs/MechanicalGlobe_Modular.prefab`
remains intact. A scene backup was saved at
`Assets/DetectiveDesk/Generated/DetectiveOffice_PreBlenderSwap.unity`.

## What changed

Only the globe's visible geometry and its presentation component's mesh references
were replaced. The existing MechanicalGlobe parent, root transform, original
RotatingSphere transform, BoxCollider, DeskInteractable.game link, DetectiveGame
fields, camera, HUD, lighting and unrelated desk objects were preserved.

Six old visual modules are retained inactive inside the new gameplay prefab for
rollback. The new prefab is connected in the saved DetectiveOffice scene. It
retains the original procedural prefab as its base; the original asset was not overwritten.

The model contains a dark metallic shell, geographic metal continent plates,
brass meridian rings and graduation marks, layered 12-/16-/14-tooth clockwork,
turned pedestal, amber inlays and a separate round 0–5 RPM dial.
Geographic data is public-domain Natural Earth 1:110m land; this is stylized
engraved cartography, not terrain or survey-grade coastlines.

## Import details and future edits

FBX materials were explicitly remapped to URP/Lit. Base colors use sRGB; normal
maps use Normal Map import; packed maps use linear R=metallic, A=smoothness.
No Built-in Standard shader is used by active globe materials. The six material
sets are DarkShell, AntiqueBrass, EngravedLand, AmberAccent, GaugeFace and GaugeNeedle.
Amber and gauge emission are restrained for the existing noir Volume.

The FBX's Blender axis rotations were baked into separate Unity-native mesh assets,
with identity sphere/gear axes and the analog needle pivot normalized for the
existing controller. The native meshes preserve UVs, normals and named modular
parts. **Future FBX edits do not automatically rebuild these NativeMeshes assets**;
repeat that normalization/rebinding step before accepting a revised visual prefab.
Do not reset the existing gameplay parent's transform or substitute a second rotor.

The model is an initial detailed procedural version: about 155k triangles and
81 mesh renderers. It has no LOD chain or atlased/combined renderer optimization.
Keep animation pivots separate when optimizing for lower-end hardware.

## Validation completed

- Checked matching approximate bounds, original parent location/scale and collider.
- Checked sphere, gear and needle pivots after Unity axis normalization.
- Checked six active materials use supported URP/Lit with no pink shader errors.
- Real-frame test confirmed original 7 degrees/second sphere speed, moving layered
  gears, 1.17 RPM needle, click-to-existing-HUD behavior and reversible hover emission.
- Verified all unrelated camera/HUD/lighting/desk component state remained unchanged.
- Final Console check returned zero errors. Temporary validation code was removed.

## Re-run Blender

From the project workspace in PowerShell:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python 'Artifacts/BlenderGlobe/build_mechanical_globe.py' -- --output 'Artifacts/BlenderGlobe/Export'
```

The builder creates a separate collection and exports only its generated mesh and
empty objects. Review the new output before importing; keep the original scene
backup and inactive rollback modules until the revised model passes validation.
