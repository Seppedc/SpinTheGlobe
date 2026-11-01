import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';

/**
 * DeskFigurineController manages the Kintsugi statue lifecycle:
 * 1. Piece assembly via Evidence Crate drops (0 Shards -> 1 Base -> 2 Torso -> 3 Crown).
 * 2. When complete (3/3), globe revolutions channel molten liquid gold into cracks (0% -> 100%).
 * 3. When 100% full of gold, clicking the statue SMASHES it with an explosion,
 *    awarding a Gold Coin and resetting it back to shards!
 * Maps directly to Unity's DeskFigurineRestorer MonoBehaviour.
 */
export class DeskFigurineController extends MonoBehaviour {
  private smashShards: Array<{ x: number; y: number; vx: number; vy: number; rot: number; life: number }> = [];

  constructor(
    private gameState: GameState,
    private eventBus: EventBus
  ) {
    super();
  }

  public override awake(): void {
    // Globe revolutions infuse liquid gold into assembled statue
    this.eventBus.on('revolutionCompleted', () => {
      if (this.gameState.statuePieces >= 3 && this.gameState.goldInfusionProgress < 1.0) {
        const rate = this.gameState.hasCrucible ? 0.28 : 0.14;
        this.gameState.goldInfusionProgress = Math.min(1.0, this.gameState.goldInfusionProgress + rate);
        if (this.gameState.goldInfusionProgress >= 1.0) {
          this.eventBus.emit('audioPlay', 'win');
          this.eventBus.emit('floatingText', {
            x: window.innerWidth * 0.35,
            y: window.innerHeight * 0.74,
            text: '★ STATUE READY TO SMASH! ★',
            color: '#ffd700'
          });
        }
      }
    });

    window.addEventListener('mousedown', (e) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const fx = canvas.width * 0.35;
      const fy = canvas.height * 0.82;

      // Click on statue pedestal
      if (Math.abs(e.clientX - fx) <= 35 && e.clientY >= fy - 45 && e.clientY <= fy + 15) {
        if (this.gameState.statuePieces >= 3 && this.gameState.goldInfusionProgress >= 1.0) {
          this.smashStatue(fx, fy - 20);
        }
      }
    });
  }

  public smashStatue(fx: number, fy: number): void {
    // 1. Award Gold Coin
    this.gameState.addGoldCoin(1);
    this.gameState.statuePieces = 0;
    this.gameState.goldInfusionProgress = 0;

    this.eventBus.emit('audioPlay', 'clatter');
    this.eventBus.emit('audioPlay', 'win');

    // 2. Spawn explosive gold/stone shard particles
    for (let i = 0; i < 18; i++) {
      this.smashShards.push({
        x: fx + (Math.random() - 0.5) * 12,
        y: fy + (Math.random() - 0.5) * 12,
        vx: (Math.random() - 0.5) * 260,
        vy: -140 - Math.random() * 120,
        rot: Math.random() * Math.PI * 2,
        life: 1.0
      });
    }
  }

  public override update(deltaTime: number): void {
    // Advance smash shard particles
    for (let i = this.smashShards.length - 1; i >= 0; i--) {
      const s = this.smashShards[i];
      s.vy += 650 * deltaTime;
      s.x += s.vx * deltaTime;
      s.y += s.vy * deltaTime;
      s.life -= deltaTime * 0.8;
      if (s.life <= 0) {
        this.smashShards.splice(i, 1);
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const fx = width * 0.35;
    const fy = height * 0.82;

    ctx.save();

    // 1. Wood Pedestal Shelf Base
    ctx.fillStyle = '#3a2312';
    ctx.fillRect(fx - 40, fy, 80, 14);
    ctx.strokeStyle = '#5a3818';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(fx - 40, fy, 80, 14);

    const pieces = this.gameState.statuePieces;
    const goldRatio = this.gameState.goldInfusionProgress;

    // 2. Progressive Piece Rendering
    if (pieces === 0) {
      // 0 Pieces: Scattered broken shards on pedestal
      ctx.fillStyle = '#7a7066';
      ctx.fillRect(fx - 15, fy - 6, 8, 6);
      ctx.fillRect(fx + 2, fy - 8, 12, 8);
      ctx.fillRect(fx - 6, fy - 14, 10, 6);

      ctx.textAlign = 'center';
      ctx.fillStyle = '#8c7b6c';
      ctx.font = '8px sans-serif';
      ctx.fillText('SHARDS (0/3)', fx, fy + 24);
    } else if (pieces === 1) {
      // 1 Piece: Base assembled
      ctx.fillStyle = '#8f8377';
      ctx.fillRect(fx - 14, fy - 12, 28, 12);
      ctx.strokeStyle = '#5c544d';
      ctx.lineWidth = 1;
      ctx.strokeRect(fx - 14, fy - 12, 28, 12);

      ctx.textAlign = 'center';
      ctx.fillStyle = '#a89478';
      ctx.font = '8px sans-serif';
      ctx.fillText('BASE (1/3)', fx, fy + 24);
    } else if (pieces === 2) {
      // 2 Pieces: Base + Torso
      ctx.fillStyle = '#8f8377';
      ctx.fillRect(fx - 14, fy - 12, 28, 12);
      ctx.fillRect(fx - 9, fy - 22, 18, 10);
      ctx.strokeStyle = '#5c544d';
      ctx.lineWidth = 1;
      ctx.strokeRect(fx - 9, fy - 22, 18, 10);

      ctx.textAlign = 'center';
      ctx.fillStyle = '#a89478';
      ctx.font = '8px sans-serif';
      ctx.fillText('TORSO (2/3)', fx, fy + 24);
    } else {
      // 3 Pieces: Complete Assembled Idol
      const shake = goldRatio >= 1.0 ? (Math.random() - 0.5) * 2 : 0;

      ctx.save();
      ctx.translate(fx + shake, fy);

      // Stone Idol Body
      ctx.fillStyle = '#9e9183';
      ctx.fillRect(-14, -12, 28, 12); // Base
      ctx.fillRect(-9, -23, 18, 11); // Torso
      ctx.beginPath();
      ctx.arc(0, -31, 10, 0, Math.PI * 2); // Head/Crown
      ctx.fill();

      // Molten Glowing Gold Kintsugi Veins
      if (goldRatio > 0) {
        ctx.strokeStyle = '#eab308';
        ctx.lineWidth = 1.5 + goldRatio * 1.5;
        if (goldRatio >= 1.0) {
          ctx.shadowColor = '#ffd700';
          ctx.shadowBlur = 12;
        }

        // Gold vein fracture lines
        ctx.beginPath();
        ctx.moveTo(-10, -6);
        ctx.lineTo(2, -12);
        ctx.lineTo(-4, -22);
        ctx.lineTo(6, -30);
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(8, -8);
        ctx.lineTo(-2, -16);
        ctx.lineTo(2, -26);
        ctx.stroke();
        ctx.shadowBlur = 0;
      }

      ctx.restore();

      // Status / Gavel Prompt
      ctx.textAlign = 'center';
      if (goldRatio >= 1.0) {
        ctx.fillStyle = '#ffd700';
        ctx.font = 'bold 9px sans-serif';
        ctx.fillText('🔨 SMASH STATUE!', fx, fy + 24);

        // Hovering Brass Gavel icon
        ctx.font = '16px sans-serif';
        ctx.fillText('🔨', fx, fy - 48);
      } else {
        ctx.fillStyle = '#f59e0b';
        ctx.font = '8px sans-serif';
        ctx.fillText(`GOLD: ${(goldRatio * 100).toFixed(0)}%`, fx, fy + 24);
      }
    }

    // 3. Render Explosive Smash Shards
    for (const s of this.smashShards) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, s.life);
      ctx.translate(s.x, s.y);
      ctx.rotate(s.rot);
      ctx.fillStyle = Math.random() < 0.5 ? '#ffd700' : '#8f8377';
      ctx.fillRect(-3, -3, 6, 6);
      ctx.restore();
    }

    ctx.restore();
  }
}
