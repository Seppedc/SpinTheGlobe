# DD-35 Dark mahogany PBR

Produced 2026-10-30 by bounded queue worker. Needs human visual review; no approval or completion claimed. Queue and tracker files were not edited.

## Provenance and changes

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, blender-unity-assets skill, registry and Excel DD-35 entry, IMPLEMENTATION_BRIEF.md and References/REFERENCE_BRIEF.md. Reference folder contains text only, no images. No pre-existing generator or material existed in the assigned source directory. No external assets were used.

Added deterministic Unity command source at `Assets/DetectiveDesk/ArtSource/Materials/MahoganyPBR/GenerateMahogany.cs.txt`. Execute its contents through Unity_RunCommand for a fresh version output directory; it refuses an existing material. Authored four 1024x1024 repeatable maps: sRGB basecolor, linear roughness, tangent normal, and linear metallic/smoothness (RGB zero metallic, alpha 1-roughness). Periodic grain, ribbon variation and pores provide dark reddish mahogany direction. URP/Lit material uses basecolor, normal and packed metallic/smoothness. Standalone roughness map is supplied for reuse. All generated assets reside beside this report, without replacing production materials.

## Executed checks

- Unity relay guidelines and AssetQueueReview.Preflight succeeded; coordinator before-blender guard PASS before texture generation. No Blender was required or launched for this non-mesh job.
- Source command compiled and executed in Unity. Four PNG maps imported synchronously with repeat wrapping, mipmaps and uncompressed 1024 resolution; normal map importer and linear/sRGB settings assigned explicitly. Roughness values computed during generation: 0.3526 to 0.5459981; metallic zero. See material-validation.json.
- AssetQueueReview.Review executed successfully, saved the isolated material sphere prefab and review scene, rendered preview.png, recorded hashes and restored the original scene. unity-validation.json reports PASS, protected_scene_unchanged true, one review mesh/renderer, zero missing materials and zero Console errors.
- Unity Console read after review returned zero warning/error entries; Console was not cleared.
- Inspected preview.png: reddish brown directional grain, visible specular response, no pink material. Preview and required evidence files exist and are nonempty.

## Dimensions, pivots and limits

Material has no geometry dimensions or semantic pivots. UV repeat is one tile; real-world texel scale must be chosen during later authorized integration. Only helper-owned sphere geometry was used for review. No gameplay geometry or transforms were changed.

This is an authored procedural material candidate, not scan-derived hero wood. Grain regularity is visible; sphere UV poles distort the grain. No end-grain, measured physical scale, anisotropic shader, or gameplay lighting evaluation is supplied. Roughness PNG is converted to smoothness in the packed alpha for URP. No Blender, FBX or mesh validation applies. Human review should judge grain naturalness, sheen and color under intended lighting before integration. No active gameplay assets were assigned this material.

Screenshot: [preview.png](preview.png). Review evidence: unity-preflight.json, material-validation.json, unity-validation.json and review_manifest.json. Review prefab: MahoganyPBR_Review.prefab; material: MahoganyPBR.mat.
