# Spin The Globe — Game Design

_Last updated: 2026-11-04 · Unity 6000.4.7f1 · URP_

## 1. Concept
Spin The Globe is an active-play incremental clicker set on a detective's desk late at night. There is no offline income: the game has to be open to earn.

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
- The player drags the globe to spin it fast. Clicking adds a smaller impulse. It spins one way only (west to east), and friction slows it down.
- **Dragging always beats clicking** at equal upgrade levels.
- A start or meridian line marks the payout point. It's a floating line for now. Later a holder structure goes there, and the payout happens when a pin passes under it.
- **Upgrades:**
  - **Max speed:** a hard cap on both dragging and auto-rotate.
  - **Auto-rotate:** ranges from slow to fast, and can never go above max speed. A rank that would exceed max speed shows as *Capped*.
  - **Base multiplier.**
  - **Drag power** and **click power.**

### 3.2 Wall map and pin shooter (main money loop)
- The shooter fires pins from the pin box to **random** spots on the wall map. There is no aiming.
- Each map pin is mirrored at the same latitude/longitude on the globe.
- Each time a pin passes the payout line, the player is paid and the pin loses 1 strength. At 0 strength the pin falls off the map, and that fallen pin becomes evidence for the case file.
- **Landing bonuses:** land pays more than sea; landing near a big city pays more; the *chance* of each can be upgraded.
- **Upgrades:** shooter cooldown, land chance and city chance.
- **Pins:** the player starts with 0. They are unlocked one at a time up to 12. Each pin has its own **durability** and **payout** ranks.

### 3.3 Case file (second income)
- One folder on the desk, empty at the start. Each pin that falls off adds a clue (sheet, photo, memo) and the folder thickens.
- A case is done when it reaches its threshold. Later cases only accept pins of a minimum durability level.
- When a case is full, the player presses the stamp: it stamps the folder physically, SOLVED appears in red, the folder slides away and a new empty one drops in.
- After that there is a cooldown before the next case opens.
- **Upgrades:** payout, cooldown, auto-stamp chance, and ★ Press Coverage (gold-drop chance, hard cap 10%).

### 3.4 Whiskey glass (power-up)
- Each click fills the glass a step. When full, the glass is lifted and drunk, then the boost (payout and spin speed) starts, followed by a cooldown. Usable immediately at game start.
- **Upgrades:** boost duration, cooldown and boost strength. Lootbox ice cubes make the glass fill faster.

### 3.5 Statues
- The pedestals start empty. Statue pieces come **only from lootboxes** and rebuild a statue bottom to top.
- A completed statue fills with gold, driven by globe revolutions. When full, the player smashes it with the hammer and earns **gold**.
- **Upgrades:** gold fill rate and gold payout.
- Planned: statues locked behind gold (section 5).

### 3.6 Lootbox
- Bought with money. Loot: ice cube, gold, statue piece, paper ball (pays out instantly for now), money (a fraction of the price or a jackpot).

### 3.7 Skill tree
- One root node, **Skill Tree Unlock**, with branches Globe, Pins, Case File, Whiskey, Statues and Lootbox on a clean grid (72 nodes).
- Nodes appear as soon as one of their prerequisites is owned. Pins branch: Pin 1 → Pin 12 chain, each pin with Durability and Payout side nodes.
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
