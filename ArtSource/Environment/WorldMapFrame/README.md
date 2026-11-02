# Map frame

Goal: Rectangular wooden rim with beveled inner trim.

Run: `blender --background --python "ArtSource/Environment/WorldMapFrame/build_asset.py"` from Unity project root.

Outputs: WorldMapFrame.blend (editable Blender source with embedded generator); WorldMapFrame.fbx (if Blender FBX exporter succeeds).

This is a structured procedural **starter**, not a reference-matched high-detail model. For final quality: refine the geometry/materials in this source script, regenerate, then check Game camera in Unity.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.
