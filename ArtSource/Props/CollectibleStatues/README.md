# Collectible statues

Goal: Modular miniature figures/busts.

Run: `blender --background --python "ArtSource/Props/CollectibleStatues/build_asset.py"` from Unity project root.

Outputs: CollectibleStatues.blend (editable Blender source with embedded generator); CollectibleStatues.fbx (if Blender FBX exporter succeeds).

This is a structured procedural **starter**, not a reference-matched high-detail model. For final quality: refine the geometry/materials in this source script, regenerate, then check Game camera in Unity.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.
