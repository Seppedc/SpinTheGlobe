# Spin The Globe — Game Design

_Last updated: 2026-11-04 · Unity 6000.4.7f1 · URP_

## 1. Concept
Spin The Globe is an active-play incremental clicker set on a detective's desk late at night. There is no offline income: the game has to be open to earn. Background play is enabled, so switching to YouTube or another application keeps rotation, automation and earnings running.

**Currencies**
- **Money (€):** the main currency. It comes from globe spins, pin payouts, approved cases and lootbox money drops.
- **Gold:** a separate currency that carries over between prestige runs. It comes from prestige (planned, the main source), smashed statues, lootboxes and Press Coverage. It buys unlocks and permanent upgrades in the Gold shop (see section 5).

## 2. Core loop
```
Spin globe ─► € ─► skill tree ─► faster globe · more pins · better payouts
   ▲                                   │
   │                                   ▼
Pins on globe pass the payout line ◄── Pin shooter fires pins at random map locations
   │ (pin loses 1 strength; at 0 it falls off)
   ▼
Case file evidence ─► stamp APPROVED ─► €
Globe revolutions ─► gold fill in completed statues ─► smash with hammer ─► gold
€ ─► lootbox ─► ice cube · gold · statue piece · paper ball · €
```

## 3. Systems

### 3.1 Globe (main feature)
- **First-use guidance:** a new player sees a slow white overlay pulse over the globe until their first valid click or drag. Completion persists across sessions and retirement. Existing progressed saves skip it. See [FIRST_USE_GUIDANCE.md](FIRST_USE_GUIDANCE.md).
- Hold left click on the globe, swipe forward, then release to add a single spin boost. Strength uses the net horizontal distance from press to release, weighted by average swipe speed. Moving back and forth during the same hold does not accumulate power.
- **Deliberate swipes beat clicks** at equal upgrade levels. Tiny drags give no drag boost; a normal click still adds a smaller impulse. One swipe adds at most half the current speed limit. The globe spins one way only (west to east), and friction slows it down.
- A start or meridian line marks the payout point. It's a floating line for now. Later a holder structure goes there, and the payout happens when a pin passes under it.
- **Upgrades:**
  - **Max speed:** a hard cap on both dragging and auto-rotate.
  - **Auto-rotate:** ranges from slow to fast, and can never go above max speed. A rank that would exceed max speed shows as *Capped*.
  - **Base multiplier.**
  - **Less Friction:** ten levels, each reducing the speed-loss rate by 15%. Replaces Stronger Drag; swipe power stays at its base value.
  - **Click power.**
- **Direct choices:** Auto Spin, Spin Limit, Less Friction and Stronger Click each require only Unlock Upgrades. They are grouped under a free Globe heading; manual spinning upgrades can be bought without purchasing the motor. Spin Pay still follows Spin Limit. See [LESS_FRICTION_UPGRADE.md](LESS_FRICTION_UPGRADE.md).

### 3.2 Wall map and pin shooter (main money loop)
- **First pin introduction:** the coffer's first opening waits until the skill tree is closed and the desk is visible. After it opens, a white overlay pulses over the shooter until the first successful pin launch. Completion persists across saves and retirement. See [FIRST_USE_GUIDANCE.md](FIRST_USE_GUIDANCE.md).
- **Pin colours:** all slots share a Strength-based progression, with distinct shades for levels 0-20 and metallic gold at maximum Strength. Colours match across coffer, flight, map and globe, and refresh after purchase. See [PIN_STRENGTH_COLOURS.md](PIN_STRENGTH_COLOURS.md).
- The shooter fires pins from the pin box to **random** spots on the wall map. There is no aiming.
- Each map pin is mirrored at the same latitude/longitude on the globe.
- Each time a pin passes the payout line, the player is paid and the pin loses 1 strength. At 0 strength the pin falls off the map, and that fallen pin becomes evidence for the case file.
- **Location-neutral payout:** sea, land and city locations pay the same. Pin Pay and global income bonuses apply. Spin Pay upgrades affect spins only.
- **Upgrades:** shooter cooldown, auto-fire, per-pin Strength and Pay. Location-chance skills are removed.
- **Reload progression:** start at 4.5 seconds; each Faster Reload rank cuts that time by 12%. The first upgrade takes it to 3.96 seconds, while 40 ranks still reach roughly 0.027 seconds.
- **Shooter branch:** Faster Reload sits below-right of Unlock Upgrades, with Auto Fire directly below it. Faster Reload is revealed by the root but stays locked until Pin 1 is bought; Auto Fire requires Faster Reload.
- **Pins:** the player starts with 0. They are unlocked one at a time up to 12. Each pin has 20 **Strength** levels and 20 **Pay** levels. Added levels keep the same per-level effects. Strength prices grow by 80% through level 10, then 25%; Pay prices grow by 60%, then 15%.

### 3.3 Case file (second income)
- The folder stays on the desk from the start, with an **Unlock Case Files** plate. Buy Pin 1, then the **Case Files** skill (EUR 30) to enable evidence collection and case rewards. Pin falls before unlocking are not banked.
- After unlocking, each pin that falls off adds a clue (sheet, photo, memo) and the folder thickens.
- The visible stack stops growing at five clues. Further clues still animate into the folder and replace the top sheet; all evidence continues counting. See [CASE_FOLDER_STACK.md](CASE_FOLDER_STACK.md).
- A case is done when it reaches its evidence threshold. Five consecutive cases share each threshold: 3 clues for cases 1-5, then 4 for 6-10, then 5 for 11-15, continuing without an evidence cap.
- Cases 1-15 accept any pin. Cases 16-45 require Strength 1+, 46-90 require Strength 2+, then tiers last 60 and 75 cases. Strength 5+ is the final requirement; cases continue indefinitely.
- Base money starts at EUR 5 and grows by EUR 0.10 per case before upgrades. The income-linked minimum is 1.5 seconds of recent income. Both are half the preceding settings. The brass case plate displays `Evidence: collected/required` while a case is open, and a `NEXT CASE` countdown during cooldown.
- When a case is full, the player presses the stamp: it stamps the folder physically, SOLVED appears in red, the folder slides away and a new empty one drops in.
- While a case is ready to claim, the stamp pulses with the white object overlay. It stops when stamping begins and returns for every completed case.
- After that there is a cooldown before the next case opens.
- **Upgrades:** payout, cooldown, auto-stamp chance, and ★ Press Coverage (gold-drop chance, hard cap 10%).

### 3.4 Whiskey glass (power-up)
- **Whiskey Set:** a permanent, one-time 10-Gold purchase reveals the glass, bottle and drink skill branch. They are hidden and unusable until purchased. The unlock survives retirement. See [WHISKEY_GOLD_UNLOCK.md](WHISKEY_GOLD_UNLOCK.md).
- Each click fills the unlocked glass a step. When full, the glass is lifted and drunk, then the boost (payout and spin speed) starts, followed by a cooldown.
- **Upgrades:** boost duration, cooldown and boost strength. Lootbox ice cubes make the glass fill faster.

### 3.5 Statues
- The pedestals start empty. Statue pieces come **only from lootboxes** and rebuild a statue bottom to top.
- A completed statue fills with gold, driven by globe revolutions. When full, the player smashes it with the hammer and earns **gold**.
- **Upgrades:** gold fill rate and gold payout.
- Planned: statues locked behind gold (section 5).

### 3.6 Lootbox
- Bought with money. Loot: ice cube, gold, statue piece, paper ball (pays out instantly for now), money (a fraction of the price or a jackpot).

### 3.7 Skill tree
- **Purchase colours:** grey means unavailable, brown means another level can be bought now, and gold means fully owned/maxed. The same palette applies to the Gold shop and tooltips. See [PURCHASE_COLOURS.md](PURCHASE_COLOURS.md).
- One root node, **Skill Tree Unlock**, with 64 purchasable skills and four free information hubs: Globe above, Pins right, Case Files left and Desk Items below.
- Hover or click a hub for a short explanation. Hubs have no cost, rank or purchase action. Globe and Desk Items use shared straight connectors; Pins groups the pin chain and reload branch, while Case Files leads into the case upgrades. Purchase prerequisites remain unchanged. Drinks and statues retain their Gold unlock requirements. See [SKILL_TREE_SECTIONS.md](SKILL_TREE_SECTIONS.md).
- Case File remains its own branch to the left: Case Files unlock → three choices: Case Pay, Case Wait or Auto Stamp. The later upgrades follow Wait and Auto Stamp. The unlock requires both the root and Pin 1. Cross-branch requirements appear in the tooltip; branch links stay local for readability.
- Nodes appear as soon as one of their prerequisites is owned. Pins branch: Pin 1 → Pin 12 chain, each pin with Durability and Payout side nodes.
- **Bulk purchases:** Buy 1 / Buy 5 / Buy 10 selects up to that many affordable levels, stopping at level limits and permanent speed caps. The tooltip previews the actual total price and result.
- **Retirement:** available at EUR 15,000 total earned this run, without an additional wallet payment. The first retirement awards 25 gold; see [ECONOMY_REBALANCE.md](ECONOMY_REBALANCE.md).
- The data is in `Data/SkillTree/SkillTree.asset`. To generate it, use **Tools ▸ Spin The Globe ▸ Create Default Data**.

### 3.8 Settings
Language (stored; translations later), master / effects / music volume, window mode, resolution, frame-rate limit, Save & Quit, Restart (two confirmations). Background music: a looping late-night jazz track.

## 4. Build status (2026-11-04)
1. ✅ Foundation: wallet (money + gold), save, event bus, stat system, data-driven skill tree, tests
2. ✅ Globe, pins, shooter, case file, whiskey, lootbox, statues, skill tree, desk lamp, settings menu, music
3. ✅ Opening rebalanced: tree in ~1 min, Pin 1 in ~2 min
4. ⏳ Prestige and Gold shop (section 5) · ⏳ payout-line holder model · ⏳ story mode (section 6)

See `Docs/BUILD_LOG.md` for how to play, the testing shortcuts and the decisions taken.

## 5. Prestige and gold (planned, agreed 2026-11-04)
Goal: stretch the game from ~5½ h (ideal) to ~15–25 h without making any single run slow. Gold becomes the currency that carries over between runs.

### 5.1 Loop
1. **Run 1:** money only. Statues are locked; the skill tree is money-only.
2. **Retire the agency (prestige):** resets money, the skill tree, pins, case progress, whiskey and lootbox count. Keeps gold, everything bought with gold, settings and the lamp.
3. **Gold shop:** spend gold on unlocks and permanent upgrades.
4. **Later runs:** statues (once unlocked) produce gold too, so each run earns more gold.

### 5.2 Prestige gold
- Based on money earned this run, growing slower than earnings, e.g. **gold = 10 × √(run earnings ÷ €1M)** (to be tuned in `Tools/economy_sim.py`).
- Unlocks at about **€1M earned** (~45 min into run 1).
- The retire button shows the live preview, e.g. "Retire now: +34 gold".
- Retiring asks for confirmation (like Restart in the settings).

### 5.3 Gold sources
- Prestige: the main source.
- Statue smashes, only once statues are unlocked.
- Small amounts from lootboxes and ★ Press Coverage.
- Run 1 lootboxes can't drop statue pieces (statues are locked); they drop money, ice, paper balls and a little gold instead.

### 5.4 Menus
- **Skill tree:** money only; resets on prestige.
- **Gold shop:** a normal list menu (not a tree), never resets. Ideally opened from a physical desk object (a small safe or leather ledger). Tabs:
  - **Unlocks:** statue 1, 2, 3 (pieces only drop once a statue is unlocked), more pedestals, whiskey glass, Press Coverage.
  - **Permanent:** bonuses that survive prestige, including repeatable gold sinks with rising cost (e.g. +10% all income per level).
- All current ★ gold nodes move from the skill tree into the Gold shop: Golden Meridian, Steel Tips, Twin Barrel, Press Coverage, Fast Casting, Double Box, Bottomless Glass.

### 5.5 Fast reruns
Gold perks that speed up the start of a run so run 2+ never feels like a repeat: start with €X, root already unlocked, start with Pin 1, auto-stamp from the start, auto-loader at half price. Target: run 1 ≈ 45 min to prestige, run 5 ≈ 15 min.

### 5.6 Extras
- **Detective ranks:** each prestige raises the rank (Rookie → Detective → Inspector → Chief). Each rank gives a small permanent bonus and opens the next part of the Gold shop.
- **Statue tiers:** bronze → silver → gold statues, each a gold unlock, each paying more when smashed.
- Rejected: optional challenge runs; case-collection album.

### 5.7 Build order
1. Prestige and the gold formula.
2. Gold shop menu; ★ nodes moved out of the skill tree.
3. Statue unlocks behind gold; lootbox change.
4. Fast-rerun perks.
5. Ranks, statue tiers, extras.

## 6. Story mode (later)
Gives the game an ending. A sequence of tasks that starts simple and gets harder:
- Early: spin the globe, buy and fire a pin, have a drink.
- Middle: solve cases, build and smash statues, retire the agency.
- Final task: hand over a large amount of gold, which takes several prestiges. Completing it finishes the story.
