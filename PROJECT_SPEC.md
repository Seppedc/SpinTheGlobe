# Spin the Globe — Prototype Specification

## Core Concept
A first-person atmospheric incremental/clicker prototype sitting at a late-night detective desk. 
- Perspective: Facing a mahogany desk with a rainy window on the left, an antique corkboard world map on the wall in front, a central 3D mechanical globe, and collectible figurines on the left shelf.
- Ultimate Target Engine: Unity (C#). Current Phase: Rapid iterative web prototype (Canvas / TypeScript).

## Confirmed Game Mechanics
1. **Unidirectional Globe Physics:**
   - The globe only spins from west-to-east (one-way). Reverse spinning is disallowed.
   - Natural exponential decay/friction slows the globe to a stop when unpowered.
   - Upgradable "Auto-Spin" sets a baseline floor speed; manual flicks accelerate above it, then coast back down.
   - Speed is capped by an upgradable terminal velocity (with a visual vintage Brass Tachometer showing "MAX" redline).
2. **The 0° Prime Meridian Origin:**
   - A marked brass notch/ribbon at 0° longitude rewards a base spin cash bonus on every full 360° turn.
3. **The Wall Map & The Brass Meridian Wire:**
   - Players click exact countries on the corkboard wall map to launch pins.
   - Pins in countries appear as illuminated coordinate markers on the 3D globe.
   - Payout triggers ONLY when a pinned country rotates beneath the central vertical Brass Meridian Wire.
   - One-Hit-Per-Revolution Lock: Once a country triggers at the wire, it locks and cannot pay out again until a full 360° rotation brings it back around.
4. **Pin Degradation & Drop-off:**
   - Base Level: A pin triggers once at the wire and immediately pops out and falls down.
   - Durability Upgrades: Pins survive multiple passes. On early passes, they bend crooked and wobble; on the final pass, they fall off the map.
5. **Collectible Figurine Restorations (Desk Upgrades):**
   - Pedestals on the left desk evolve through 4 visual tiers: Broken Shards -> Glued Stone -> Glowing Kintsugi Gold Veins -> Solid Polished Gold Masterpiece.
   - Each restored tier provides global income multipliers.

## Current Tech Stack & Goal
- Vanilla HTML5 Canvas + ES6 JS (moving to TypeScript/Vite for modularity).
- Web Audio API for synthesized sounds (no external asset dependencies).
- Architecture must mirror Unity components (GameState, GlobeController, PinManager, MeridianTrigger).