# Bottom-screen messages

The bottom toast is reserved for the messages still listed below. Routine money
uses floating payout numbers; cases use the brass evidence/countdown plate and
the stamp's ready cue.

Removed bottom messages:

- Pin landing summaries, including money per pass.
- Evidence added; fallen pins ignored during cooldown or because a case is full.
- Case opened, status, complete, solved/reward and cooldown notices.
- Shooter reloading and all-pins-busy notices.
- All lootbox notices, including contents/jackpots and insufficient funds.
- All paper-ball notices, including payouts and empty inventory.
- Successful skill unlock and Gold shop purchase confirmations.

Remaining: insufficient pin Strength, first-pin/unlock guidance, shooter setup
errors, skill/shop purchase errors, whiskey messages, statue messages, retirement
messages, welcome instructions, and developer currency confirmations.

Lootbox reward payloads, `Rolled` and `LootOpened` events remain available for a
future visual presentation. Rewards, inventory, purchases, auto-stamping, case
animations and sounds still run. The case-folder legacy click method is retained
for authored scene bindings, with case status supplied by the brass plate.

Runtime files changed for the latest cleanup, under
`SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/`:

- `Cases/CaseFileSystem.cs`
- `Core/GameManager.cs`
- `Legacy/DetectiveGame.cs`
- `Lootbox/LootboxSystem.cs`
- `PaperBall/PaperBallThrower.cs`

The earlier pin landing change is in `Pins/WallMapManager.cs`.

Validation: 78/78 EditMode tests passed; script references and remaining message
calls were inspected. Evidence: `SpinTheGlobe/Logs/quiet-notifications-editmode.xml`.
The gameplay scene was not modified or saved. A full visual playthrough was not
performed for this text-only cleanup.
