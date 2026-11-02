# Spin The Globe — Game Design

_Last updated: 2026-11-02 · Unity 6000.4.7f1 · URP_

## 1. Concept
Spin The Globe is an active-play incremental clicker set on a detective's desk late at night. There is no offline income: the game has to be open to earn.

**Currencies**
- **Money (€):** the main currency. It comes from globe spins, pin payouts, approved cases and lootbox money drops.
- **Gold:** a separate currency. It comes from smashed statues and from lootboxes. What gold buys is still to be decided.

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
- A start or meridian line marks the payout point. It's a floating line for now. Later a holder structure goes there, and the payout happens when a pin passes under it.
- **Upgrades:**
  - **Max speed:** a hard cap on both dragging and auto-rotate.
  - **Auto-rotate:** ranges from slow to fast, and can never go above max speed. A rank that would exceed max speed shows as *Capped*.
  - **Base multiplier.**
  - **Drag power** and **click power.**

### 3.2 Wall map and pin shooter (main money loop)
- The shooter fires pins from the pin box to **random** spots on the wall map. There is no aiming.
- The pin box is open for now. A closed box that has to be unlocked comes later.
- Each map pin is mirrored at the same latitude/longitude on the globe.
- Each time a pin passes the payout line, the player is paid and the pin loses 1 strength. At 0 strength the pin falls off the map, and that fallen pin becomes evidence for the case file.
- **Landing bonuses:**
  - Land pays more than sea.
  - Landing near a big city pays more.
  - The *chance* of each can be upgraded.
- **Upgrades:** shooter cooldown, land chance and city chance.
- **Pins:** the player starts with 0. They are unlocked one at a time up to 12. Each pin has its own **durability** and **payout** ranks.

### 3.3 Case file (second income)
- Each pin that falls off adds one item to the case: a file, a photo, and so on.
- A case is done when it reaches its threshold. Later cases only accept pins of a minimum durability level.
- When a case is full, the player presses the stamp. An APPROVED animation plays and the case pays out.
- After that there is a cooldown before the next case opens.
- **Upgrades:** payout, cooldown, and auto-stamp chance (the chance that a full case stamps itself).

### 3.4 Whiskey glass (power-up)
- The player presses the glass to fill it. When full, it temporarily boosts payout and spin speed, then goes on cooldown.
- **Upgrades:** boost duration, cooldown and boost strength.
- Lootbox ice cubes make the glass fill faster.

### 3.5 Statues
- The pedestals start empty.
- Statue pieces come **only from lootboxes** and rebuild a statue piece by piece.
- A completed statue fills with gold. Globe revolutions drive the fill.
- When the statue is full, the player smashes it with the hammer and earns **gold**.
- **Upgrades:** gold fill rate and gold payout.

### 3.6 Lootbox
- Bought with money.
- **Loot table:**
  - Ice cube
  - Gold
  - Statue piece
  - Paper ball: throw it into the bin for a reward. This is a placeholder for now.
  - Money: either a fraction of the price or a jackpot.

### 3.7 Skill tree
- One root node, **Skill Tree Unlock**. Five branches grow out of it in a radial layout like *Bills Must Be Paid*: Globe, Pins, Case File, Whiskey and Statues.
- Nodes appear as soon as one of their prerequisites is owned.
- **Pins branch** (like *Spellbound* / *Shelved*):
  - A chain Pin 1 → Pin 12, where buying a pin reveals the next one.
  - Every pin has its own Durability and Payout side nodes.
- The data is in `Data/SkillTree/SkillTree.asset`. To generate it, use **Tools ▸ Spin The Globe ▸ Create Default Data**.

## 4. Build status (2026-11-02)
1. ✅ Foundation: wallet (money + gold), save, event bus, stat system, data-driven skill tree, tests
2. ✅ Globe: click impulse, max/auto speed (auto ≤ max), multiplier, HUD, autosave
3. ✅ Pins: random landing with land/city bonus, shooter cooldown, Pin 1→12, per-pin durability and payout
4. ✅ Skill tree screen: radial, pan/zoom, nodes revealed by prerequisites
5. ✅ Case file: evidence, growing thresholds, durability requirement, stamp, cooldown, auto-stamp
6. ✅ Whiskey glass
7. ✅ Lootbox, inventory and paper ball (placeholder bin and reward)
8. ✅ Statues: pieces → gold fill → hammer smash
9. ✅ Closed pin box · ⏳ payout-line holder model · ⏳ balancing · ⏳ what gold buys

See `Docs/BUILD_LOG.md` for how to play, the testing shortcuts and the decisions taken.
