# ArtSource

Editable art sources live here, outside Unity, so the Unity project only imports what the game needs.

```
ArtSource/
├── _Inspiration/                 general mood / approved scene references
└── <Category>/<Asset>/
    ├── <Asset>.blend             latest editable model (older versions are in git history)
    ├── build_<asset>.py          generator (or build_asset.py for starters) + helper scripts
    ├── Textures/                 source maps the .blend uses
    ├── References/               reference images for this asset
    └── README.md / IMPLEMENTATION_BRIEF.md
```

Categories: **Props**, **Environment**, **Materials**, **VFX**, **Animation**, **UI**.

Rebuild and push into Unity:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-blender-asset.ps1 -Asset WhiskeyGlass -ExportFbx -Publish
```
The Unity copy lands in `SpinTheGlobe/Assets/DetectiveDesk/Art/<Category>/<Asset>/Models/`.
