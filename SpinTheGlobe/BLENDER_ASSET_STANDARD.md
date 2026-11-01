# DetectiveDesk — Blender Asset Engineering Standard (Codex)

## Existing project compatibility

These defaults govern new assets and iterative improvements. Preserve established completed asset paths, pivots, axis conversion, material identities and gameplay bindings. In particular, the detailed PinCoffer uses its existing `ART_PinCoffer` collection and exporter with `bake_space_transform=False`; do not retroactively change it to the starter conventions. The detailed collectible pin remains the authoritative model. Read its registry binding and existing generator before adapting it. Generic starter proxies must never replace either completed asset. Workflow installation and isolated tests must not modify or save the active scene.

This standard incorporates the requested headless, metrically accurate, Unity-ready workflow.

## Pipeline checklist (mandatory for each Blender-backed asset)
1. Inspect the reference and existing generator. **Edit the existing script** rather than overwrite finished models.
2. Make generator idempotent in a fresh isolated Blender process. Prefer `bmesh` for topology edits and booleans, and `bpy.data` for object/collection cleanup. Where `bpy.ops` is necessary, explicitly set selection and active object. Do not call interactive UI operators in background mode.
3. Model in **metric** scale: one Blender unit represents 1 meter. **Asset root** `ROOT_<AssetSlug>` belongs at (0,0,0), with X/Y centered and the lowest geometry point at Z=0. Retain semantic child pivots (lid hinges, globe sphere, gears) rather than applying their locations blindly.
4. Apply mesh scale and mesh rotation before export. Avoid applying the position of animation pivot objects. Organize meshes into `Collection_<AssetSlug>`; keep moving components independently named and parented beneath the root.
5. Use named Principled BSDF materials with realistic base color, metallic and roughness. **No Blender-only procedural materials** for critical Unity appearance; use baked base-color/normal/metallic-roughness maps for hero assets. Flat colors in the starter generators are previews, not final photorealism.
6. Embed the generator source as `GENERATE_<AssetSlug>.py` in the `.blend`. Save `.blend` as the editable source of truth, and export FBX with `use_selection=True`, `axis_forward='-Z'`, `axis_up='Y'`, `apply_unit_scale=True`, `bake_space_transform=True`. Validate axes and pivots after import—`bake_space_transform` can sometimes complicate animations.
7. Put references in `<asset>/References/`, maps in `<asset>/textures/`, and generated `.blend`/`.fbx` beside the generator. Save screenshots of Blender and Unity comparison for review.
8. Unity MCP imports/updates the **visual child**, preserving root scene transforms, prefabs, colliders, scripts and gameplay. Validate Game camera framing, mesh materials, sockets where applicable, and Unity Console.
9. Update `Asset_Tracker.xlsx` with source paths, status, owner, validation and notes.

## Reality check
This pack contains **21 editable procedural geometry starters**, not 21 finished hero-quality objects. Detailed pin coffer and collectible pin generators from prior work are preserved and preferred. The other 21 groups are correctly implemented in Unity UI, animation, material, texture and VFX tooling instead of meaningless 3D objects.

## Blender run commands
From project root, with Blender executable on PATH:

`powershell -File Tools/build-one.ps1 -Slug DetectiveDesk`

Or override path:

`powershell -File Tools/build-one.ps1 -Slug MechanicalGlobe -Blender 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe'`

**Note:** Some existing detailed generators have their own filenames and commands; do not replace them with the starter generator automatically.
