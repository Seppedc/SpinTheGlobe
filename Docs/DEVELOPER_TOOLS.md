# Developer controls

The everyday menu is **Tools > Spin The Globe**, with three actions:

| Action | Shortcut | Behavior |
|---|---|---|
| Reset Progress (Delete Save) | Menu only | Confirms, then deletes the main save, fallback backup and temporary save. In Play mode it reloads the game immediately; otherwise the next Play session starts fresh. |
| Add EUR 10 | F1 | Adds EUR 10 to the running game. |
| Add 1 Gold | F2 | Adds 1 gold to the running game. |

Money and gold actions require Play mode. Keyboard shortcuts require the Game
view to have focus and exist only in the Editor or Development Builds. Shift
does not increase their amounts. F3-F12 have no project developer bindings.
Grants are normal currency credits and are saved by the normal save system.

Reset clears all saved progress, including purchased skills, money, gold,
retirements, Gold shop purchases, statues, items and cases. Settings are kept.
There is no separate player skill-tree reset: owned skills are part of that save.
It does not delete skill definitions or the balance assets.

The former Create Default Data menu generated skill definitions, rather than
resetting a player's purchased skills. That, economy export and save backup
helpers remain callable by editor automation without visible menu commands:
`DefaultDataMenu.CreateDefaults()`, `EconomySnapshotMenu.Export()` and
`SaveBackupMenu.Backup()/Restore()`.

Production workflows are separate: **File > Build Spin The Globe Windows (test)**
builds the game, and **Tools > Detective Desk > Blender Assets > Refresh Pin
Coffer From Blender** preserves the established art pipeline. That test build is
a regular build, so it does not include F-key cheats.

Removed: old large/Shift currency grants, inventory/statue/case/whiskey cheats,
reveal-all and slow-motion shortcuts, test-pin population menus, one-off desk
layout commands, Reveal Save File, and Remove Missing Scripts menu commands.

Implementation changes:

- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Debug/DevCheats.cs`
- `SpinTheGlobe/Assets/DetectiveDesk/Scripts/Editor/Tools/SceneMaintenanceMenu.cs`
- Menu registrations removed from `DefaultDataMenu.cs`, `EconomySnapshotMenu.cs`
  and `SaveBackupMenu.cs` in that same editor Tools folder.
- Build menu moved in `Scripts/Editor/Tools/BuildMenu.cs`.
- Deleted `Scripts/Editor/Tools/DeskLayoutMenu.cs` and
  `Scripts/Editor/Pipeline/PinCofferPopulationTools.cs`, with their `.meta` files.
- Reset verification in `Tests/EditMode/SaveTests.cs`.
- Documentation: this file, `Docs/ECONOMY.md`, `Docs/BUILD_LOG.md`, `README.md`,
  and the simulator usage text in `Tools/economy_sim.py`.

Validation: 56/56 EditMode tests passed. Reset deletion was tested against
temporary saves, including backup and temporary-file cleanup. Unity's menu
registry confirms exactly three Spin The Globe commands. The player's actual
save was not reset or granted currency during this cleanup. Scene files were
not modified or saved.
