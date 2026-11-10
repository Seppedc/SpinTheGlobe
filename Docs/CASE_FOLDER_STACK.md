# Capped folder stack — 2026-11-07

The folder stops gaining permanent height at five evidence items. Later items
still enter with the existing slide and cover-lift animation, replacing the
top visible clue at the same resting height. The folder keeps at most five
clue meshes, avoiding overlapping stacks of extra sheets.

The visual tracks the full evidence count separately from visible sheets, so
17 evidence items still count as 17. Requirements, rewards and save data are
unchanged. Cover height, label anchor and stamp point share the five-item cap.
Clearing or swapping the folder resets both visual count and contents.

Evidence template discovery is restricted to the folder's scene, allowing
isolated preview tests without hiding objects in other open scenes.

Changed files:

- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Cases/CaseFolderVisual.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Tests/EditMode/ScenePresentationTests.cs`
- `Docs/GAME_DESIGN.md`
- `Docs/CASE_FOLDER_STACK.md`

Validation: 145/145 EditMode tests passed, including a new regression covering
five versus 17 items, an 18th arrival, loading a smaller count and clearing for
the next case. Result: `SpinTheGlobe/Logs/folder-five-items-editmode.xml`.
`git diff --check` passed. No gameplay scene was modified or saved.
The animation path is retained; manual Play-mode animation inspection was not
performed.
