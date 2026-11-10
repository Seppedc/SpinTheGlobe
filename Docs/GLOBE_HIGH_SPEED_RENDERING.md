# High-speed globe rendering investigation — 2026-11-07

Recording inspected:
`C:/Users/decra/Videos/Screen Recordings/Screen Recording 2026-11-07 011014.mp4`.
It is 7.4 seconds at 30 FPS. Sampled frames show approximately 99-105 RPM.
At 105 RPM, rotation advances 21 degrees between recorded frames, so rapid
motion can appear to strobe or show perceptual afterimages. Individual sampled
frames show a single continent orientation rather than several translucent
copies. A recording alone cannot establish the game's actual render FPS.

The scene inspection also found two enabled surface groups on the same rotor:

- `Sphere_RotationPivot`: the older V1 shell and eight continent meshes.
- `GlobeRework_Sphere`: the current shell, continents and raised coastline.

The older group is now disabled with a scene instance override. The current
surface, rotor, gameplay controller, meridian, pins, materials, imported meshes
and model sources are retained. Only one globe shell now renders. This removes
redundant drawing and the risk of overlapping surfaces; it does not establish
that this caused the reported doubling.

The open scene was clean before the targeted repair was saved. Temporary
diagnostic render clones were removed. No rotation, upgrade, income or speed
settings were changed.

Comparison sequences were rendered using an isolated clone at 105 RPM and
30 FPS, under `SpinTheGlobe/Logs/globe-surface-check/`. The sampled before/after
images were pixel-identical: this comparison does **not** demonstrate a visual
fix for the reported effect. High-speed temporal strobing remains unconfirmed
in live gameplay and unresolved by this cleanup. Camera inspection found no
temporal antialiasing or active motion-blur component on the game's volume.

Changed files:

- `SpinTheGlobe/Assets/DetectiveDesk/Scenes/DetectiveOffice.unity`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/ScenePresentationTests.cs`
- `Docs/GLOBE_HIGH_SPEED_RENDERING.md`

The new EditMode regression opens a preview of the authored scene and checks
that the old surface is disabled, the current one is enabled and exactly one
shell renders. The EditMode run was attempted but skipped while the editor
reported a dirty scene after diagnostic cloning; a subsequent connection
stalled. No new Unity test pass is claimed. Seven Python economy tests and
`git diff --check` passed. Live motion and perceived doubling still need
verification before claiming the user-visible issue fixed.
