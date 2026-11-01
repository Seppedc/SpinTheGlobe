# Banker lamp Unity integration

PASS for the executed integration checks. The scene and reusable visual prefab are saved; the writer guard is released. Tracker files were not edited.

Canonical editable source: `Assets/DetectiveDesk/ArtSource/Props/BankersLamp/BankersLamp.blend` (SHA256 `5108231f1f31a42ed0e9ddee072a2dab05d74a873e2c5808a85743c9a017671c`). The same-source FBX is mounted under `DetectiveOffice/Environment/Generated_BankersLamp/DetectiveBankersLamp_Visual`. Neither source file was rebuilt or edited by this integration writer.

Reusable prefab: `Assets/DetectiveDesk/LampRework/DetectiveBankersLamp_Visual.prefab`. Mount under the original Z-up gameplay root at local position zero and unit scale, preserving the prefab wrapper X rotation 89.98022 degrees and imported model X rotation 270.0198 degrees. Those rotations cancel. Do not reset either independently. Source +Y faces world -Z. Original root position (4.25, .815, .5153126), rotation (270, 0, 0), and scale 3.75 are unchanged.

The prefab has 127 enabled mesh renderers / 80,000 triangles and ten reusable URP/Lit materials in `Materials/`. Three textured materials bind sRGB BaseColor, linear packed R-metal/A-smoothness, and NormalMap imports; both required PBR keywords are enabled and packed-alpha smoothness is 1. Seven remaining materials use the specified linear colors and 1-minus-roughness smoothness. Only bulb and ivory lining emit. Emissive GI flags are set so Unity retains `_EMISSION`. The hood retains green/ivory/green-edge slots; no pink, missing slots or Standard shaders were found.

The shade retains its single 10-degree tilt, with independent chain and socket pivots. Bounds min [3.67081261, 0.815000057, 0.1686225], max [4.82918739, 2.38991213, 1.01314831]; bottom Y .815000057 and height 1.574912073 m. One new spotlight is at [4.34375, 1.8499999, 0.3390625], aimed downward using the imported markers, intensity .8, range 2.3, outer/inner angles 80/55, 3200 K with temperature enabled, white color, no shadows.

Preservation checked all 1,336 original objects and 3,930 original components against stable identities, serialized fields and object references. Preintegration inspection found no intervening edits; an additional scene copy was still saved as `Assets/DetectiveDesk/LampRework/DetectiveOffice_PreIntegration_20261031_231319.unity`. Only 41 old lamp MeshRenderer enabled flags changed, plus the authorized new child. Old objects, original prefab, four existing lights, globe, cannon, camera, HUD, materials, scripts and colliders remain preserved. The original prefab file hash matches its supplied backup. Exact object/component additions and changed renderer identities are recorded in `ProductionLogs/LampRework/integration-result.json`.

`final-game-camera.png` is 1920×1080 with HUD. `unity-lamp-closeup.png` is 1080×1080 from a temporary additive-scene camera; the main camera was not repositioned. Camera target/aspect/viewport, active render target, canvases and RectTransforms were restored in finally before saving. Both images were actually inspected: the chain, pendant, polished rim, turned brass, bulb and ivory underside are readable; the green hood stays dark emerald without broad emission. Temporary review scene/camera were closed.

Dedicated Unity_ReadConsole Get returned zero warnings and zero errors after successful helper compilation and final validation. No missing scripts, missing prefab assets, missing materials or duplicate active old lamp renderers were found. Final scene is clean and in Edit Mode.

Limitations:

- Edit Mode rendering and serialization checks executed; Play Mode/runtime smoke was not run because no gameplay behavior changed.
- Opaque coated green hood with separate ivory lining; refractive glass was not requested or tested.
- Visual inspection performed on both final screenshots; human asset approval and tracker decisions remain with the parent.
- One relay-only validation command failed to compile because Newtonsoft was unavailable in the dynamic command assembly and a local variable name conflicted. It did not execute. Validation was moved into the compiled editor helper and passed; dedicated Console reports zero errors/warnings.
- No Git repository is available in this workspace; file scope is recorded through explicit output paths and serialized scene preservation evidence.

## Pending optional bulb refinement

Parent close-up inspection prompted a smoother pear bulb and warmer bulb/lining source revision. BuildReferenceV3 was executed and independently validated in Blender (127 meshes, 84352 triangles). Unity bridge hit its usage limit before this final refinement could execute. Canonical source was restored to the installed, previously verified V2 build (127 meshes,80000 triangles). V3 remains separate under ProductionLogs/LampRework/BuildReferenceV3. No new Unity or Console checks were claimed after this interruption. The source_sha256 evidence field now uses the verified FBX hash required by the queue schema; the original blend hash is retained separately.
