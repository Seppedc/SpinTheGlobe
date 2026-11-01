import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';

export type LootRarity = 'gold' | 'purple' | 'blue' | 'green' | 'gray';

export interface LootItem {
  id: string;
  name: string;
  rarity: LootRarity;
  color: string;
  icon: string;
}

const ALL_LOOT_TEMPLATES: LootItem[] = [
  { id: 'statue', name: 'Statue Shard', rarity: 'gold', color: '#ffd700', icon: '🏛️' },
  { id: 'ice', name: 'Crystal Ice', rarity: 'purple', color: '#c084fc', icon: '🧊' },
  { id: 'paper', name: 'Paper Ball', rarity: 'blue', color: '#60a5fa', icon: '📄' },
  { id: 'cash_big', name: 'Informant €80', rarity: 'green', color: '#4ade80', icon: '💵' },
  { id: 'cash_small', name: 'Bribe €35', rarity: 'gray', color: '#9ca3af', icon: '🪙' }
];

/**
 * EvidenceCaseOpener manages the CS:GO style lootbox opening with:
 * - A rolling horizontal carousel with teasing deceleration physics.
 * - Audio mechanical ticks on each card crossing the center needle.
 * - Dynamic drop table: Statue shards excluded once active statue is assembled.
 * - Drops physical desk rewards: Statue pieces, Whiskey ice cubes, Paper ball slingshots, or Cash.
 */
export class EvidenceCaseOpener extends MonoBehaviour {
  public readonly crateCost: number = 40.0;

  // Carousel modal state
  public isOpening: boolean = false;
  private carouselItems: LootItem[] = [];
  private winningItem: LootItem | null = null;
  private rollProgress: number = 0; // 0 to 1
  private rollDuration: number = 4.2; // seconds
  private rollOffset: number = 0;
  private targetOffset: number = 0;
  private lastTickIndex: number = -1;
  private postRollTimer: number = 0;

  private readonly cardWidth: number = 100;

  constructor(
    private gameState: GameState,
    private eventBus: EventBus
  ) {
    super();
  }

  public override awake(): void {
    window.addEventListener('mousedown', (e) => {
      if (this.isOpening) return;

      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const { x, y, w, h } = this.getCrateBounds(canvas.width, canvas.height);
      if (e.clientX >= x && e.clientX <= x + w && e.clientY >= y && e.clientY <= y + h) {
        this.openCrate();
      }
    });
  }

  public getCrateBounds(width: number, height: number): { x: number; y: number; w: number; h: number } {
    const w = 84;
    const h = 46;
    const x = width * 0.44 - w * 0.5;
    const y = height * 0.77 - h * 0.5;
    return { x, y, w, h };
  }

  public openCrate(): void {
    if (this.gameState.funds < this.crateCost) {
      this.eventBus.emit('audioPlay', 'click');
      this.eventBus.emit('floatingText', {
        x: window.innerWidth * 0.44,
        y: window.innerHeight * 0.74,
        text: `Need €${this.crateCost.toFixed(0)}!`,
        color: '#ef4444'
      });
      return;
    }

    this.gameState.deductFunds(this.crateCost);
    this.isOpening = true;
    this.rollProgress = 0;
    this.postRollTimer = 0;
    this.lastTickIndex = -1;

    // Build available drop table
    // (Statue pieces excluded if all 3 pieces are currently assembled!)
    const canDropStatue = this.gameState.statuePieces < 3;
    const availableTemplates = canDropStatue
      ? ALL_LOOT_TEMPLATES
      : ALL_LOOT_TEMPLATES.filter((t) => t.id !== 'statue');

    // Roll the winning item based on weighted chances
    const rand = Math.random();
    let winner: LootItem;
    if (canDropStatue && rand < 0.12) {
      winner = ALL_LOOT_TEMPLATES[0]; // Statue Shard (Gold)
    } else if (rand < 0.32) {
      winner = ALL_LOOT_TEMPLATES[1]; // Crystal Ice (Purple)
    } else if (rand < 0.58) {
      winner = ALL_LOOT_TEMPLATES[2]; // Paper Ball (Blue)
    } else if (rand < 0.82) {
      winner = ALL_LOOT_TEMPLATES[3]; // Informant Cash (Green)
    } else {
      winner = ALL_LOOT_TEMPLATES[4]; // Bribe Roll (Gray)
    }
    this.winningItem = winner;

    // Generate strip of 50 cards with teasing gold/purple placements
    const winningIndex = 38;
    this.carouselItems = [];
    for (let i = 0; i < 50; i++) {
      if (i === winningIndex) {
        this.carouselItems.push(winner);
      } else if (i === winningIndex - 1 || i === winningIndex + 1) {
        // High tension tease! Place rare items right adjacent to the winner
        this.carouselItems.push(
          canDropStatue && Math.random() < 0.5 ? ALL_LOOT_TEMPLATES[0] : ALL_LOOT_TEMPLATES[1]
        );
      } else {
        const pool = availableTemplates;
        this.carouselItems.push(pool[Math.floor(Math.random() * pool.length)]);
      }
    }

    // Target offset to center winning card under the needle with slight random jitter
    const jitter = (Math.random() - 0.5) * (this.cardWidth * 0.6);
    this.targetOffset = winningIndex * this.cardWidth + this.cardWidth * 0.5 + jitter;
    this.rollOffset = 0;
  }

  public override update(deltaTime: number): void {
    if (!this.isOpening) return;

    if (this.rollProgress < 1.0) {
      this.rollProgress += deltaTime / this.rollDuration;

      // Heavy cubic ease-out deceleration curve
      const t = Math.min(1.0, this.rollProgress);
      const ease = 1 - Math.pow(1 - t, 3.8);
      this.rollOffset = ease * this.targetOffset;

      // Check card boundary crossing for audio tick
      const currentTickIndex = Math.floor((this.rollOffset + this.cardWidth * 0.5) / this.cardWidth);
      if (currentTickIndex !== this.lastTickIndex) {
        this.lastTickIndex = currentTickIndex;
        this.eventBus.emit('audioPlay', 'tick');
      }

      if (this.rollProgress >= 1.0) {
        // Roll finished! Deliver rewards with fanfare
        this.eventBus.emit('audioPlay', 'win');
        this.deliverReward();
      }
    } else {
      // Hold on the winning card briefly before dismissing
      this.postRollTimer += deltaTime;
      if (this.postRollTimer >= 2.0) {
        this.isOpening = false;
      }
    }
  }

  private deliverReward(): void {
    if (!this.winningItem) return;

    if (this.winningItem.id === 'statue') {
      this.gameState.statuePieces = Math.min(3, this.gameState.statuePieces + 1);
      const pieceName =
        this.gameState.statuePieces === 1 ? 'Base' : this.gameState.statuePieces === 2 ? 'Torso' : 'Crown';
      this.eventBus.emit('floatingText', {
        x: window.innerWidth * 0.5,
        y: window.innerHeight * 0.35,
        text: `★ UNLOCKED STATUE ${pieceName.toUpperCase()}! ★`,
        color: '#ffd700'
      });
    } else if (this.winningItem.id === 'ice') {
      this.gameState.hasIceCube = true;
      this.eventBus.emit('audioPlay', 'pour');
      this.eventBus.emit('floatingText', {
        x: window.innerWidth * 0.22,
        y: window.innerHeight * 0.70,
        text: '🧊 Ice Cube Placed in Whiskey! (2 Pours)',
        color: '#c084fc'
      });
    } else if (this.winningItem.id === 'paper') {
      this.eventBus.emit('paperBallSpawned');
    } else if (this.winningItem.id === 'cash_big') {
      this.gameState.addFunds(80);
      this.eventBus.emit('floatingText', {
        x: window.innerWidth * 0.5,
        y: window.innerHeight * 0.35,
        text: '+€80 Informant Stash!',
        color: '#4ade80'
      });
    } else if (this.winningItem.id === 'cash_small') {
      this.gameState.addFunds(35);
      this.eventBus.emit('floatingText', {
        x: window.innerWidth * 0.5,
        y: window.innerHeight * 0.35,
        text: '+€35 Bribe Roll',
        color: '#a3e635'
      });
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    // 1. Render Sealed Evidence Crate on the Desk
    this.renderDeskCrate(ctx, width, height);

    // 2. Render CS:GO Style Rolling Carousel Overlay (if opening)
    if (this.isOpening) {
      this.renderCarouselOverlay(ctx, width, height);
    }
  }

  private renderDeskCrate(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const { x, y, w, h } = this.getCrateBounds(width, height);

    ctx.save();

    // Box Shadow
    ctx.fillStyle = 'rgba(10, 6, 4, 0.65)';
    ctx.fillRect(x - 4, y + 4, w + 8, h + 4);

    // Heavy Iron & Brass Casing
    const crateGrad = ctx.createLinearGradient(x, y, x + w, y + h);
    crateGrad.addColorStop(0, '#3f3f46');
    crateGrad.addColorStop(0.5, '#52525b');
    crateGrad.addColorStop(1, '#27272a');
    ctx.fillStyle = crateGrad;
    ctx.fillRect(x, y, w, h);
    ctx.strokeStyle = '#a1a1aa';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(x, y, w, h);

    // Brass corner braces & rivets
    ctx.fillStyle = '#b45309';
    ctx.fillRect(x, y, 6, 6);
    ctx.fillRect(x + w - 6, y, 6, 6);
    ctx.fillRect(x, y + h - 6, 6, 6);
    ctx.fillRect(x + w - 6, y + h - 6, 6, 6);

    // Stamped Red Police Evidence Tag
    ctx.fillStyle = '#991b1b';
    ctx.fillRect(x + 10, y + 8, w - 20, 16);
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 8px monospace';
    ctx.textAlign = 'center';
    ctx.fillText('SEIZED #419', x + w * 0.5, y + 19);

    // Price label
    ctx.fillStyle = this.gameState.funds >= this.crateCost ? '#facc15' : '#ef4444';
    ctx.font = 'bold 9px sans-serif';
    ctx.fillText(`OPEN (€${this.crateCost.toFixed(0)})`, x + w * 0.5, y + h + 14);

    ctx.restore();
  }

  private renderCarouselOverlay(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    ctx.save();

    // Dark detective vignette backdrop
    ctx.fillStyle = 'rgba(10, 8, 6, 0.82)';
    ctx.fillRect(0, 0, width, height);

    // Header title
    ctx.textAlign = 'center';
    ctx.fillStyle = '#e5b972';
    ctx.font = 'bold 15px Georgia';
    ctx.fillText('CONFISCATED CONTRABAND LOCKBOX', width * 0.5, height * 0.22);

    // Carousel viewport box
    const modalW = Math.min(560, width * 0.85);
    const modalH = 120;
    const modalX = width * 0.5 - modalW * 0.5;
    const modalY = height * 0.32 - modalH * 0.5;

    // Viewport background
    ctx.fillStyle = '#18120c';
    ctx.fillRect(modalX, modalY, modalW, modalH);
    ctx.strokeStyle = '#5a3e24';
    ctx.lineWidth = 2.5;
    ctx.strokeRect(modalX, modalY, modalW, modalH);

    // Clip to carousel window
    ctx.save();
    ctx.beginPath();
    ctx.rect(modalX + 2, modalY + 2, modalW - 4, modalH - 4);
    ctx.clip();

    // Render rolling cards
    const centerX = width * 0.5;
    const startRenderX = centerX - this.rollOffset;

    for (let i = 0; i < this.carouselItems.length; i++) {
      const cardX = startRenderX + i * this.cardWidth;

      // Skip cards off screen
      if (cardX + this.cardWidth < modalX || cardX > modalX + modalW) continue;

      const item = this.carouselItems[i];

      // Card body
      ctx.fillStyle = '#221912';
      ctx.fillRect(cardX + 4, modalY + 8, this.cardWidth - 8, modalH - 16);

      // Rarity bottom stripe
      ctx.fillStyle = item.color;
      ctx.fillRect(cardX + 4, modalY + modalH - 14, this.cardWidth - 8, 6);

      // Card border
      ctx.strokeStyle = item.color;
      ctx.lineWidth = 1.5;
      ctx.strokeRect(cardX + 4, modalY + 8, this.cardWidth - 8, modalH - 16);

      // Icon & Name
      ctx.font = '22px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(item.icon, cardX + this.cardWidth * 0.5, modalY + 48);

      ctx.fillStyle = '#f3f4f6';
      ctx.font = 'bold 9px sans-serif';
      ctx.fillText(item.name, cardX + this.cardWidth * 0.5, modalY + 76);

      ctx.fillStyle = item.color;
      ctx.font = 'bold 8px monospace';
      ctx.fillText(item.rarity.toUpperCase(), cardX + this.cardWidth * 0.5, modalY + 92);
    }

    ctx.restore(); // Exit clip

    // Central Gold Indicator Needle / Crosshair
    ctx.strokeStyle = '#eab308';
    ctx.lineWidth = 3;
    ctx.shadowColor = '#facc15';
    ctx.shadowBlur = 8;
    ctx.beginPath();
    ctx.moveTo(centerX, modalY - 6);
    ctx.lineTo(centerX, modalY + modalH + 6);
    ctx.stroke();

    // Top & Bottom Gold Needle Triangles
    ctx.fillStyle = '#facc15';
    ctx.beginPath();
    ctx.moveTo(centerX - 6, modalY - 8);
    ctx.lineTo(centerX + 6, modalY - 8);
    ctx.lineTo(centerX, modalY + 4);
    ctx.closePath();
    ctx.fill();

    ctx.beginPath();
    ctx.moveTo(centerX - 6, modalY + modalH + 8);
    ctx.lineTo(centerX + 6, modalY + modalH + 8);
    ctx.lineTo(centerX, modalY + modalH - 4);
    ctx.closePath();
    ctx.fill();
    ctx.shadowBlur = 0;

    // Winning announcement when roll concludes
    if (this.rollProgress >= 1.0 && this.winningItem) {
      ctx.fillStyle = this.winningItem.color;
      ctx.font = 'bold 16px Georgia';
      ctx.fillText(`OBTAINED: ${this.winningItem.name.toUpperCase()}!`, width * 0.5, modalY + modalH + 34);
    }

    ctx.restore();
  }
}
