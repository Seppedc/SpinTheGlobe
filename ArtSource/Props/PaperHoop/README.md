# Paper basketball hoop

Original modeled walnut backboard, brass trim, red metal ring and open cotton net. No downloaded textures. Source is PaperHoop.blend with embedded GENERATE_PaperHoop.py matching build_paperhoop.py.

Rebuild: `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-blender-asset.ps1 -Asset PaperHoop -ExportFbx -Publish`

Unity uses the FBX under Art/Props/PaperHoop, explicit URP materials and a scene-linked rim center. Gameplay and score UI stay on GameManager. See Docs/PAPER_HOOP.md.
