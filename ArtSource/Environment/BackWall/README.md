# Back wall

Goal: Straight wall segments, inset wood panels, no angled room.

Run: `blender --background --python "ArtSource/Environment/BackWall/build_asset.py"` from Unity project root.

Outputs: BackWall.blend (editable Blender source with embedded generator); BackWall.fbx (if Blender FBX exporter succeeds).

This is a structured procedural **starter**, not a reference-matched high-detail model. For final quality: refine the geometry/materials in this source script, regenerate, then check Game camera in Unity.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.
