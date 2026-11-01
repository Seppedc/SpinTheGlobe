# DD-20 CollectibleStatues asset report

Outcome: asset_error. Blender geometry validation and Unity structural review passed, but Unity visual/metric validation failed. Human review is pending; this is not Complete.

## Provenance and changes
Read AGENTS.md, BLENDER_ASSET_STANDARD.md, the Blender skill, registry entry, workbook XML, README, reference brief, existing generator and shared helper. References contains no images, only REFERENCE_BRIEF.md. No downloaded resources or textures were used.

Updated only the assigned source build_asset.py, developing the original three-shape starter into three stylized cast-metal busts: Detective (fedora), Archivist (spectacles), Commissioner (cap/epaulettes). Each has a separate base-centered Assembly and BustModule with individually named meshes. Four named Principled materials: aged bronze, polished brass, dark patina and burgundy plinth. This remains procedural concept geometry with simplified faces, not a finished hero sculpt. No shared helper edits, gameplay integration, tracker or queue edits.

## Executed pipeline and evidence
Unity relay guidelines and required Preflight executed successfully. Coordinator guards passed before each Blender build/validation and before promotion. Isolated Blender 5.2 generation and same-source FBX export exited successfully. Validation rendered blender-preview.png and wrote blender-validation.json: PASS, 72 nonempty meshes, 60,832 evaluated triangles, four materials, applied mesh scales, exact external/embedded GENERATE_CollectibleStatues.py match, metric units, parented geometry, root at origin, lowest point Z=0. No external helper dependency.

Initial internal failures were repaired: UTF-8 BOM source mismatch, then default factory cube contaminating bounds. Final source removes only factory defaults when running an unsaved startup file and its own collection on rerun. Blender emitted a thumbnail-cache write warning and use_nodes deprecation warning; final preview rendering succeeded. Previous failed-build .blend1 backup is retained, not authoritative.

Final canonical editable source for this queue version: CollectibleStatues.blend beside build_asset.py in this output folder; original editable generator: Assets/DetectiveDesk/ArtSource/Props/CollectibleStatues/build_asset.py. Built/validated in ProductionLogs/AssetQueue/BuildStaging/20261031_011147_2ee7463d/CollectibleStatues before promotion. FBX exported from this saved source.

Unity Review executed and produced CollectibleStatues_Review.prefab, new Materials/, preview.png and unity-validation.json. Helper reports 72 renderers, zero missing materials, zero Console errors, protected_scene_unchanged=true. Console inspected without clearing: five earlier warnings (Input Manager deprecation, relay signature collection and unrelated CityBackdrop import timestamp); no errors.

## Dimensions, pivots and failed visual check
Blender set bounds: 0.284 x 0.074 x 0.1275 metres. Individual base diameter 0.074 m; assembly pivots at base centers X=-0.105,0,+0.105 m, Z=0. BustModule origins share each assembly base; modules are separately named, not animated. Front is Blender -Y. StatueSlots dependency unavailable: no socket fit tested.

Inspected blender-preview.png: all three busts are visible with distinct silhouettes. Inspected required Unity preview.png: almost black with tiny highlights, unsuitable for visual approval. Additional read-only prefab bounds inspection returned center (0,0,0.000637), extents (0.105325,0.000370,0.000637), root scale (1,1,1). The nested FBX hierarchy therefore has incorrect imported scale/orientation relative to the Blender dimensions. Structural PASS does not establish usable Unity dimensions or appearance. This import conversion defect remains unresolved. Do not integrate this prefab or treat the Unity screenshot as aesthetic evidence. Needs asset-specific FBX hierarchy/unit correction followed by guarded regeneration, validation and isolated review; review helper was not modified.

No active gameplay scene edits or Play Mode. All subprocesses exited. No human approval claimed.
