import { GameState } from '../core/GameState.ts';

export type SkillBranch = 'pins' | 'globe' | 'cases';
export type SkillCurrency = 'cash' | 'gold';

export interface SkillNode {
  id: string;
  branch: SkillBranch;
  branchTitle: string;
  tier: number;
  title: string;
  desc: string;
  cost: number;
  currency: SkillCurrency;
  icon: string;
  prereqId?: string; // Required parent node ID
  x: number; // SVG coordinate (0 - 1000)
  y: number; // SVG coordinate (0 - 680)
  apply: (gameState: GameState) => void;
}

/**
 * 24 Node Radial Constellation Skill Tree matching Star Wars Jedi: Fallen Order layout:
 * - Left Branch: 📌 CARTOGRAPHY (Pins & Map Web)
 * - Center Branch: 🌐 CHRONOMETRY (Mechanical Globe & Motor)
 * - Right Branch: 📁 CRIMINOLOGY (Case Files & Clues)
 *
 * Coordinates optimized for a 60% window with centered details card directly beneath root nodes.
 */
export const SKILL_TREE_NODES: SkillNode[] = [
  // ==================== CENTER: CHRONOMETRY & GLOBE ====================
  {
    id: 'globe_root',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 0,
    title: 'Clockwork Motor I',
    desc: 'Sets a baseline passive floor spin speed. The globe never comes to a full stop.',
    cost: 15,
    currency: 'cash',
    icon: '⚙️',
    x: 500,
    y: 440,
    apply: (gs) => {
      gs.autoLevel++;
    }
  },
  {
    id: 'globe_motor_2',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 1,
    title: 'Dual Copper Dynamo',
    desc: 'Doubles the baseline motor speed, keeping the globe turning steadily.',
    cost: 45,
    currency: 'cash',
    icon: '⚡',
    prereqId: 'globe_root',
    x: 500,
    y: 345,
    apply: (gs) => {
      gs.autoLevel++;
    }
  },
  {
    id: 'globe_bearings_1',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 1,
    title: 'Precision Bearings I',
    desc: 'Polished steel ball bearings expand maximum terminal velocity (raises tachometer MAX redline).',
    cost: 30,
    currency: 'cash',
    icon: '🧭',
    prereqId: 'globe_root',
    x: 430,
    y: 365,
    apply: (gs) => {
      gs.bearingsLevel++;
    }
  },
  {
    id: 'globe_wire_1',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 1,
    title: 'Resonant Wire Alloy',
    desc: 'Braids harmonic resonant wire into the central Brass Meridian Wire (+25% cash per pass).',
    cost: 50,
    currency: 'cash',
    icon: '🎻',
    prereqId: 'globe_root',
    x: 570,
    y: 365,
    apply: (gs) => {
      gs.multiplier += 0.25;
    }
  },
  {
    id: 'globe_flywheel',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 2,
    title: 'Heavy Cast Flywheel',
    desc: 'Cast brass flywheel weight adds inertia, letting the globe coast much longer after a spin.',
    cost: 85,
    currency: 'cash',
    icon: '⚓',
    prereqId: 'globe_motor_2',
    x: 500,
    y: 250,
    apply: (gs) => {
      gs.autoLevel++;
    }
  },
  {
    id: 'globe_bearings_2',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 2,
    title: 'Ceramic Rollers',
    desc: 'Ceramic low-friction bearings push the tachometer needle further into the redline.',
    cost: 110,
    currency: 'cash',
    icon: '🏎️',
    prereqId: 'globe_bearings_1',
    x: 395,
    y: 275,
    apply: (gs) => {
      gs.bearingsLevel++;
    }
  },
  {
    id: 'globe_wire_gold',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 2,
    title: 'Midas Meridian Wire',
    desc: 'Pure alchemical gold wire! 1.5% chance on every meridian crossing to spontaneously drop a free Gold Coin!',
    cost: 5,
    currency: 'gold',
    icon: '🪙',
    prereqId: 'globe_wire_1',
    x: 605,
    y: 275,
    apply: (gs) => {
      gs.hasMidasWire = true;
      gs.multiplier += 0.30;
    }
  },
  {
    id: 'globe_crucible_gold',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 3,
    title: 'Molten Gold Crucible',
    desc: 'Crucible heats liquid gold on the desk; globe revolutions infuse assembled statues with gold 2x faster!',
    cost: 4,
    currency: 'gold',
    icon: '🏺',
    prereqId: 'globe_wire_gold',
    x: 585,
    y: 185,
    apply: (gs) => {
      gs.hasCrucible = true;
    }
  },
  {
    id: 'globe_capstone',
    branch: 'globe',
    branchTitle: 'CHRONOMETRY',
    tier: 3,
    title: 'Perpetual Motion Engine',
    desc: 'Capstone: Max RPM redline increased by +80% and all cash earnings permanently doubled!',
    cost: 450,
    currency: 'cash',
    icon: '🌟',
    prereqId: 'globe_flywheel',
    x: 500,
    y: 120,
    apply: (gs) => {
      gs.bearingsLevel += 2;
      gs.multiplier += 1.0;
    }
  },

  // ==================== LEFT: CARTOGRAPHY & PINS ====================
  {
    id: 'pins_root',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 0,
    title: 'Tempered Pin Tips',
    desc: 'Pins on the corkboard map are hardened to survive multiple passes before popping off.',
    cost: 45,
    currency: 'cash',
    icon: '📌',
    x: 370,
    y: 465,
    apply: (gs) => {
      gs.durabilityLevel++;
    }
  },
  {
    id: 'pins_box_1',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 1,
    title: 'Velvet Pin Box I',
    desc: 'Expands the velvet-lined brass desk coffer capacity from 4 to 6 simultaneous pins.',
    cost: 35,
    currency: 'cash',
    icon: '📦',
    prereqId: 'pins_root',
    x: 270,
    y: 450,
    apply: (gs) => {
      gs.pinBoxLevel++;
    }
  },
  {
    id: 'pins_durability_2',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 1,
    title: 'Hardened Steel Pins',
    desc: 'Pins survive 3 full wire passes on the corkboard map before popping off.',
    cost: 110,
    currency: 'cash',
    icon: '🛡️',
    prereqId: 'pins_root',
    x: 320,
    y: 375,
    apply: (gs) => {
      gs.durabilityLevel++;
    }
  },
  {
    id: 'pins_box_2',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 2,
    title: 'Two-Tiered Brass Caddy',
    desc: 'Expands pin box capacity to 8 simultaneous pins on the corkboard map.',
    cost: 95,
    currency: 'cash',
    icon: '🧰',
    prereqId: 'pins_box_1',
    x: 180,
    y: 430,
    apply: (gs) => {
      gs.pinBoxLevel++;
    }
  },
  {
    id: 'pins_durability_3',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 2,
    title: 'Diamond-Tipped Needles',
    desc: 'Pins survive 4 full wire passes on the corkboard map, multiplying contract lifetime.',
    cost: 240,
    currency: 'cash',
    icon: '💎',
    prereqId: 'pins_durability_2',
    x: 240,
    y: 315,
    apply: (gs) => {
      gs.durabilityLevel++;
    }
  },
  {
    id: 'pins_durability_gold',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 3,
    title: 'The Incorruptible Lead',
    desc: 'Pins withstand extreme centrifugal spin force without snapping; all pin payouts increased by +50%!',
    cost: 3,
    currency: 'gold',
    icon: '🥇',
    prereqId: 'pins_durability_3',
    x: 170,
    y: 265,
    apply: (gs) => {
      gs.hasIncorruptibleLead = true;
      gs.multiplier += 0.20;
    }
  },
  {
    id: 'pins_capstone',
    branch: 'pins',
    branchTitle: 'CARTOGRAPHY',
    tier: 3,
    title: 'Conspiracy Web Master',
    desc: 'Capstone: Connects corkboard pins with crimson thread; expands box to 10 pins and +50% payout!',
    cost: 380,
    currency: 'cash',
    icon: '🕸️',
    prereqId: 'pins_durability_gold',
    x: 110,
    y: 185,
    apply: (gs) => {
      gs.pinBoxLevel += 2;
      gs.multiplier += 0.50;
    }
  },

  // ==================== RIGHT: CRIMINOLOGY & CASES ====================
  {
    id: 'cases_root',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 0,
    title: 'Informant Network',
    desc: 'Local street contacts increase the cash bounty payout when stamping Case Files closed (+35%).',
    cost: 60,
    currency: 'cash',
    icon: '📁',
    x: 630,
    y: 465,
    apply: (gs) => {
      gs.caseBountyLevel++;
    }
  },
  {
    id: 'cases_speed_1',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 1,
    title: 'Pneumatic Desk Tube',
    desc: 'Fast evidence delivery: Every full 360° revolution grants +1 free Clue to the active Case File!',
    cost: 80,
    currency: 'cash',
    icon: '🧪',
    prereqId: 'cases_root',
    x: 730,
    y: 450,
    apply: (gs) => {
      gs.multiplier += 0.15;
    }
  },
  {
    id: 'cases_bounty_2',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 1,
    title: 'Syndicate Blackmail',
    desc: 'Case closed bounties doubled (+70% cash bonus when stamping Case Closed).',
    cost: 150,
    currency: 'cash',
    icon: '💼',
    prereqId: 'cases_root',
    x: 680,
    y: 375,
    apply: (gs) => {
      gs.caseBountyLevel++;
    }
  },
  {
    id: 'cases_single_malt_gold',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 2,
    title: '25-Year Single Malt',
    desc: 'Aged highland scotch extends the Focus Rush multiplier duration from 12 seconds to 20 seconds!',
    cost: 3,
    currency: 'gold',
    icon: '🥃',
    prereqId: 'cases_speed_1',
    x: 820,
    y: 430,
    apply: (gs) => {
      gs.rushDuration = 20;
    }
  },
  {
    id: 'cases_bounty_3',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 2,
    title: 'High-Commissioner Seal',
    desc: 'Massive case bounties (+120% bonus) and higher rewards for solving crimes.',
    cost: 320,
    currency: 'cash',
    icon: '🎖️',
    prereqId: 'cases_bounty_2',
    x: 760,
    y: 315,
    apply: (gs) => {
      gs.caseBountyLevel++;
    }
  },
  {
    id: 'cases_auto_ticker_gold',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 3,
    title: 'Automatic Evidence Ticker',
    desc: 'Spring-loaded ticker has a 35% chance to automatically slam the Case Stamp closed the moment 3 clues arrive!',
    cost: 4,
    currency: 'gold',
    icon: '🖨️',
    prereqId: 'cases_bounty_3',
    x: 830,
    y: 265,
    apply: (gs) => {
      gs.hasAutoTicker = true;
    }
  },
  {
    id: 'cases_capstone',
    branch: 'cases',
    branchTitle: 'CRIMINOLOGY',
    tier: 3,
    title: 'Chief Inspector Authority',
    desc: 'Capstone: Stamping a case closed grants an instant €500 bonus and a 25% chance of a free Gold Coin!',
    cost: 550,
    currency: 'cash',
    icon: '👑',
    prereqId: 'cases_auto_ticker_gold',
    x: 890,
    y: 185,
    apply: (gs) => {
      gs.caseBountyLevel += 2;
      gs.multiplier += 0.50;
    }
  }
];
