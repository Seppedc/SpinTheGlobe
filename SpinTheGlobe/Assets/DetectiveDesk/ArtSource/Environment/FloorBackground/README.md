# Floor / background

Goal: Small amount of room depth beneath and behind desk.

Run: `blender --background --python "Assets/DetectiveDesk/ArtSource/Environment/FloorBackground/build_asset.py"` from Unity project root.

Outputs: FloorBackground.blend (editable Blender source with embedded generator); FloorBackground.fbx (if Blender FBX exporter succeeds).

This is a structured procedural **starter**, not a reference-matched high-detail model. For final quality: refine the geometry/materials in this source script, regenerate, then check Game camera in Unity.

See References/ for inspiration, and do not modify existing Unity prefabs or their scripts without a backup.
