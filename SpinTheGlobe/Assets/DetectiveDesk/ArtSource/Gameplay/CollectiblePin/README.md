# Collectible pin

Goal: Re-use existing pin generator; separate enamel and brass.

Run: `blender --background --python "Assets/DetectiveDesk/ArtSource/Gameplay/CollectiblePin/build_asset.py"` from Unity project root.

Outputs: CollectiblePin.blend (editable Blender source with embedded generator); CollectiblePin.fbx (if Blender FBX exporter succeeds).

This is a structured procedural **starter**, not a reference-matched high-detail model. For final quality: refine the geometry/materials in this source script, regenerate, then check Game camera in Unity.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.
