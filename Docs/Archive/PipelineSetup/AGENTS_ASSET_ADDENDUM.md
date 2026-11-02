# Add to EXISTING AGENTS.md (merge, do not replace)

## Asset pack tracking
- Read `Asset_Tracker.xlsx` and `asset_registry.json` before asset work.
- Use `Assets/DetectiveDesk/ArtSource/<Category>/<Slug>/` as the canonical source home.
- Keep one maintained generator `build_asset.py`, its `.blend`, textures, and `References/` together. Where a `build_coffer.py` or `build_collectible_pin.py` already exists, preserve and reuse that finished generator rather than reducing quality with the generic starter.
- For each change: edit generator > execute in Blender > verify `.blend` includes the generator > import/update Unity visual child via MCP > verify camera & console > update tracker status.
- The scripts in this kit are STRUCTURED STARTERS, not photorealistic finished assets; model detail and PBR maps require iteration.
- For Unity UI, textures and VFX groups, use their `IMPLEMENTATION_BRIEF.md`, not Blender models.
- Do not alter unrelated scene transforms, scripts, collision volumes or prefab references.
- Preserve backups and avoid destructive overwrites. Add asset-specific image references to `References/` when approved.

- Follow `BLENDER_ASSET_STANDARD.md` for headless bpy/bmesh safety, metric asset roots, Principled BSDF, embedded source, selective FBX export, and Unity visual-only replacement.
