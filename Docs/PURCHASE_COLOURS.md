# Consistent upgrade colours

The skill tree and Gold shop share one purchase-state palette:

| State | Appearance |
|---|---|
| Cannot afford, locked or temporarily speed-capped | Existing dark grey fill with grey frame, icon and text |
| Can buy another level now | Brown fill and frame, with warm readable text |
| Fully owned or maxed | Gold fill, frame, icon and text |

Partial ownership does not select a branch colour. A partly upgraded skill is
brown when its next level is affordable and grey otherwise. Single-purchase
unlocks become gold once owned. A temporarily capped Auto Spin skill stays
grey because it is not actually maxed. Hidden skills stay hidden under the
existing discovery rules.

Tree node fills, frames, labels, level counters and connectors use the shared
palette. Hover-card background, border, heading, stat preview and price match
the same state. The previous red/blue/green/purple branch colours, green/red
tooltip prices and red invalid-click flash have been removed. Existing shake
and purchase animations remain, without changing the state colour.

Gold shop rows and action buttons use the same palette. Owned/maxed buttons
retain gold while disabled; hovering an affordable button lightens its brown
instead of changing it to gold. Lock reasons and OWNED/MAXED labels remain so
the meaning is not communicated by colour alone. The retirement action uses
brown when available and grey while unavailable. Menu navigation and decorative
gold headings retain their ordinary theme.

`PurchasePalette` is the shared source. `ThemedButton.SetPurchaseState` opts
purchase buttons into the palette; ordinary menu controls keep their existing
theme. Hover styling updates immediately on pointer events as well as normal
frame updates. The authored scene presentation was refreshed without moving
skill nodes.

Validation executed:

- 131/131 Unity EditMode tests passed (`Logs/purchase-colours-editmode.xml`).
- New integration coverage checks grey/brown/gold transitions, partial
  ownership, different skill branches, temporary speed caps, tooltip matching,
  Gold shop affordability, owned disabled buttons and hover states.
- Native Unity renders of both menus were visually inspected:
  `Logs/purchase-colours-preview.png` and
  `Logs/purchase-colours-shop-preview.png`.
- The previews used disposable player state with saving disabled. Temporary
  players, canvases and cameras were removed. They are layout illustrations,
  including normally undiscovered nodes; the long pin row is cropped.
- Diff whitespace check passed. The corrupt-save warning from SaveTests is an
  expected fixture; the result XML reports zero failures.

No prices, upgrade effects or player save were changed. The scene was returned
to Desk view. A separate real-player session was not recorded.

Changed paths, relative to SpinTheGlobe/Assets/DetectiveDesk/:

- Scripts/Runtime/UI/PurchasePalette.cs and .meta
- Scripts/Runtime/UI/SkillTreeView.cs
- Scripts/Runtime/UI/GoldShopView.cs
- Scripts/Runtime/UI/ThemedButton.cs
- Tests/EditMode/ScenePresentationTests.cs
- Scenes/DetectiveOffice.unity

Other paths: Docs/GAME_DESIGN.md, Docs/ARCHITECTURE.md,
Docs/SCENE_PRESENTATION.md and this document.
