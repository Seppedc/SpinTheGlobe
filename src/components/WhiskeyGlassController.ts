import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { EventBus } from '../core/EventBus.ts';
import { GameState } from '../core/GameState.ts';

/**
 * WhiskeyGlassController manages the interactive detective tumbler:
 * - Click multiple times to pour amber whiskey into the glass.
 * - If an ice cube is present (won from Evidence Crate), glass only needs 2 pours instead of 4!
 * - When full, clicking drinks it, granting a 12-second "Focus Rush"
 *   with double spin torque and double payouts.
 * Maps directly to Unity's WhiskeyGlassController MonoBehaviour.
 */
export class WhiskeyGlassController extends MonoBehaviour {
  public fillLevel: number = 0;
  public rushTimeRemaining: number = 0;
  public readonly rushDuration: number = 12.0;

  constructor(
    private eventBus: EventBus,
    private gameState: GameState
  ) {
    super();
  }

  public get maxFill(): number {
    return this.gameState.hasIceCube ? 2 : 4;
  }

  public override awake(): void {
    window.addEventListener('mousedown', (e) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const { x, y, w, h } = this.getBounds(canvas.width, canvas.height);
      if (e.clientX >= x && e.clientX <= x + w && e.clientY >= y && e.clientY <= y + h) {
        this.handleClick(e.clientX, e.clientY);
      }
    });
  }

  public getBounds(width: number, height: number): { x: number; y: number; w: number; h: number } {
    const w = 34;
    const h = 42;
    const x = width * 0.22 - w * 0.5;
    const y = height * 0.74 - h;
    return { x, y, w, h };
  }

  public handleClick(clickX: number, clickY: number): void {
    const maxF = this.maxFill;

    if (this.fillLevel < maxF) {
      this.fillLevel++;
      this.eventBus.emit('audioPlay', 'pour');
      this.eventBus.emit('floatingText', {
        x: clickX,
        y: clickY - 15,
        text: `Pour (${this.fillLevel}/${maxF})`,
        color: '#f59e0b'
      });
    } else {
      // Drink the full glass!
      this.fillLevel = 0;
      this.gameState.hasIceCube = false; // Ice melts upon drinking
      const duration = this.gameState.rushDuration;
      this.rushTimeRemaining = duration;
      this.eventBus.emit('audioPlay', 'drink');
      this.eventBus.emit('rushStarted', duration);
      this.eventBus.emit('floatingText', {
        x: clickX,
        y: clickY - 20,
        text: '★ FOCUS RUSH 2X! ★',
        color: '#ffd700'
      });
    }
  }

  public get isRushActive(): boolean {
    return this.rushTimeRemaining > 0;
  }

  public override update(deltaTime: number): void {
    if (this.rushTimeRemaining > 0) {
      this.rushTimeRemaining -= deltaTime;
      if (this.rushTimeRemaining <= 0) {
        this.rushTimeRemaining = 0;
        this.eventBus.emit('rushEnded');
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const { x, y, w, h } = this.getBounds(width, height);
    const maxF = this.maxFill;

    ctx.save();

    // 1. Coaster / Glass Shadow on Mahogany Desk
    ctx.fillStyle = 'rgba(10, 6, 4, 0.6)';
    ctx.beginPath();
    ctx.ellipse(x + w * 0.5, y + h + 2, w * 0.6, 6, 0, 0, Math.PI * 2);
    ctx.fill();

    // 2. Crystal Tumbler Glass Outline
    ctx.fillStyle = 'rgba(210, 230, 255, 0.12)';
    ctx.beginPath();
    ctx.moveTo(x + 2, y);
    ctx.lineTo(x + w - 2, y);
    ctx.lineTo(x + w - 4, y + h);
    ctx.lineTo(x + 4, y + h);
    ctx.closePath();
    ctx.fill();

    // Heavy glass base
    ctx.fillStyle = 'rgba(220, 240, 255, 0.25)';
    ctx.fillRect(x + 4, y + h - 8, w - 8, 8);

    // 3. Amber Whiskey Liquid
    if (this.fillLevel > 0) {
      const fillRatio = Math.min(1.0, this.fillLevel / maxF);
      const liquidH = (h - 12) * fillRatio;
      const liquidY = y + h - 8 - liquidH;

      const amberGrad = ctx.createLinearGradient(x, liquidY, x, y + h - 8);
      amberGrad.addColorStop(0, '#f59e0b');
      amberGrad.addColorStop(0.5, '#d97706');
      amberGrad.addColorStop(1, '#92400e');
      ctx.fillStyle = amberGrad;

      ctx.beginPath();
      ctx.moveTo(x + 5, liquidY);
      ctx.lineTo(x + w - 5, liquidY);
      ctx.lineTo(x + w - 4, y + h - 8);
      ctx.lineTo(x + 4, y + h - 8);
      ctx.closePath();
      ctx.fill();
    }

    // 4. Crystal Ice Cube (Rendered if present in glass)
    if (this.gameState.hasIceCube) {
      const iceY = y + h - 18;
      ctx.fillStyle = 'rgba(235, 245, 255, 0.85)';
      ctx.fillRect(x + w * 0.5 - 6, iceY, 12, 10);
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 1;
      ctx.strokeRect(x + w * 0.5 - 6, iceY, 12, 10);
    }

    // 5. Glass Highlights & Rim
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(x + 2, y);
    ctx.lineTo(x + 4, y + h);
    ctx.stroke();

    ctx.strokeStyle = 'rgba(255, 255, 255, 0.25)';
    ctx.beginPath();
    ctx.moveTo(x + w - 2, y);
    ctx.lineTo(x + w - 4, y + h);
    ctx.stroke();

    // 6. Status Caption
    ctx.textAlign = 'center';
    if (this.rushTimeRemaining > 0) {
      ctx.fillStyle = '#ffd700';
      ctx.font = 'bold 9px sans-serif';
      ctx.fillText(`RUSH: ${this.rushTimeRemaining.toFixed(1)}s`, x + w * 0.5, y + h + 16);
    } else if (this.fillLevel >= maxF) {
      ctx.fillStyle = '#f59e0b';
      ctx.font = 'bold 9px sans-serif';
      ctx.fillText('DRINK!', x + w * 0.5, y + h + 16);
    } else {
      ctx.fillStyle = '#a89478';
      ctx.font = '9px sans-serif';
      const iceLabel = this.gameState.hasIceCube ? ' [ICE]' : '';
      ctx.fillText(`POUR (${this.fillLevel}/${maxF})${iceLabel}`, x + w * 0.5, y + h + 16);
    }

    // 7. Focus Rush Amber Screen Glow (Subtle pulse when rush is active)
    if (this.rushTimeRemaining > 0) {
      const pulse = 0.08 + Math.sin(Date.now() * 0.008) * 0.04;
      const rushGrad = ctx.createRadialGradient(
        width * 0.5,
        height * 0.5,
        width * 0.3,
        width * 0.5,
        height * 0.5,
        width * 0.75
      );
      rushGrad.addColorStop(0, 'rgba(0, 0, 0, 0)');
      rushGrad.addColorStop(1, `rgba(245, 158, 11, ${pulse.toFixed(3)})`);
      ctx.fillStyle = rushGrad;
      ctx.fillRect(0, 0, width, height);
    }

    ctx.restore();
  }
}
