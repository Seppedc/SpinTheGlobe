import { EventBus } from './EventBus.ts';

export type UpgradeType =
  | 'auto'
  | 'bearings'
  | 'durability'
  | 'figurine'
  | 'pinBox'
  | 'caseBounty';

/**
 * GameState holds pure game model state, dual currencies (Money € & Gold Coins),
 * statue reconstruction progress, and temporary desk modifiers.
 * Maps directly to Unity ScriptableObject or EconomyManager.
 */
export class GameState {
  public funds: number = 0.0;
  public goldCoins: number = 0;
  public multiplier: number = 1.0;
  public rushMultiplier: number = 1.0;
  public lampMultiplier: number = 1.20; // Default lamp is ON (+20%)

  // Upgrades
  public autoLevel: number = 0;
  public bearingsLevel: number = 0;
  public durabilityLevel: number = 0;
  public figurineTier: number = 0; // Prestiges/Smashes completed
  public pinBoxLevel: number = 0;
  public caseBountyLevel: number = 0;

  // Statue Pieces & Kintsugi Gold Infusion
  public statuePieces: number = 0; // 0 (shards), 1 (base), 2 (torso), 3 (assembled)
  public goldInfusionProgress: number = 0.0; // 0.0 to 1.0 (fills as globe spins)

  // Desk Modifiers
  public hasIceCube: boolean = false; // Won from evidence crate; halves whiskey pours

  // Skill Tree tracking & Gold Upgrades
  public purchasedSkills: Set<string> = new Set<string>();
  public hasMidasWire: boolean = false; // 1% chance on meridian pass for gold shard/cash
  public hasCrucible: boolean = false; // Globe revolutions fill statue gold 2x faster
  public hasIncorruptibleLead: boolean = false; // Pins never bend at high RPM, +50% payout
  public hasAutoTicker: boolean = false; // 30% chance to auto-stamp completed cases
  public rushDuration: number = 12; // 12 seconds default, 20 with Single Malt

  constructor(private eventBus: EventBus) {
    this.eventBus.on('rushStarted', () => {
      this.rushMultiplier = 2.0;
    });

    this.eventBus.on('rushEnded', () => {
      this.rushMultiplier = 1.0;
    });
  }

  public get totalMultiplier(): number {
    return this.multiplier * this.rushMultiplier * this.lampMultiplier;
  }

  public addFunds(amount: number): void {
    if (amount <= 0) return;
    this.funds += amount;
    this.eventBus.emit('fundsChanged', this.funds, amount);
  }

  public deductFunds(amount: number): boolean {
    if (this.funds >= amount) {
      this.funds -= amount;
      this.eventBus.emit('fundsChanged', this.funds, -amount);
      return true;
    }
    return false;
  }

  public addGoldCoin(amount: number = 1): void {
    this.goldCoins += amount;
    this.eventBus.emit('goldCoinsChanged', this.goldCoins);
    this.eventBus.emit('floatingText', {
      x: window.innerWidth * 0.35,
      y: window.innerHeight * 0.78,
      text: `+${amount} GOLD COIN!`,
      color: '#ffd700'
    });
  }

  public deductGoldCoins(amount: number): boolean {
    if (this.goldCoins >= amount) {
      this.goldCoins -= amount;
      this.eventBus.emit('goldCoinsChanged', this.goldCoins);
      return true;
    }
    return false;
  }

  public isSkillPurchased(id: string): boolean {
    return this.purchasedSkills.has(id);
  }

  public getUpgradeCost(type: UpgradeType): number {
    switch (type) {
      case 'auto':
        return 15 * Math.pow(2.2, this.autoLevel);
      case 'bearings':
        return 30 * Math.pow(2.4, this.bearingsLevel);
      case 'durability':
        return 45 * Math.pow(2.5, this.durabilityLevel);
      case 'figurine':
        return 50 * Math.pow(3.0, this.figurineTier);
      case 'pinBox':
        return 35 * Math.pow(2.3, this.pinBoxLevel);
      case 'caseBounty':
        return 60 * Math.pow(2.2, this.caseBountyLevel);
    }
  }

  public purchaseUpgrade(type: UpgradeType): boolean {
    const cost = this.getUpgradeCost(type);
    if (!this.deductFunds(cost)) {
      return false;
    }

    switch (type) {
      case 'auto':
        this.autoLevel++;
        this.eventBus.emit('upgradePurchased', 'auto', this.autoLevel);
        break;
      case 'bearings':
        this.bearingsLevel++;
        this.eventBus.emit('upgradePurchased', 'bearings', this.bearingsLevel);
        break;
      case 'durability':
        this.durabilityLevel++;
        this.eventBus.emit('upgradePurchased', 'durability', this.durabilityLevel);
        break;
      case 'pinBox':
        this.pinBoxLevel++;
        this.eventBus.emit('upgradePurchased', 'pinBox', this.pinBoxLevel);
        break;
      case 'caseBounty':
        this.caseBountyLevel++;
        this.eventBus.emit('upgradePurchased', 'caseBounty', this.caseBountyLevel);
        break;
    }

    return true;
  }

  // Base auto speed in rad/fixedUpdate
  public get baseAutoSpeed(): number {
    return 0.008 * this.autoLevel;
  }

  // Max velocity in rad/fixedUpdate
  public get maxVelocity(): number {
    return 0.08 + 0.04 * this.bearingsLevel;
  }

  // Max active pins allowed based on Pin Box level
  public get maxPins(): number {
    return 4 + this.pinBoxLevel * 2;
  }
}
