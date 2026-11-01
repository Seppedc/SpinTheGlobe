import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';

/**
 * CaseDossierController manages the confidential case folder and rubber stamp:
 * - Each pin that expires on the map adds +1 Clue to the dossier.
 * - When clues reach 100% (3/3), the rubber stamp unlocks.
 * - Clicking the stamp SLAMS it down with a heavy thud, branding CASE CLOSED
 *   and exploding a massive case bounty payout!
 */
export class CaseDossierController extends MonoBehaviour {
  public clues: number = 0;
  public readonly targetClues: number = 3;
  public casesSolved: number = 0;

  // Stamp slam animation
  private isStamping: boolean = false;
  private stampProgress: number = 0;
  private stampOffsetY: number = 0;

  constructor(
    private gameState: GameState,
    private eventBus: EventBus
  ) {
    super();
  }

  public override awake(): void {
    // When a pin expires/falls off the map, it solves a lead and adds a clue to the case file
    this.eventBus.on('pinDestroyed', () => {
      if (this.clues < this.targetClues) {
        this.clues++;
        this.eventBus.emit('audioPlay', 'pin');
        this.eventBus.emit('floatingText', {
          x: window.innerWidth * 0.58,
          y: window.innerHeight * 0.74,
          text: `+1 Clue (${this.clues}/${this.targetClues})`,
          color: '#facc15'
        });

        if (this.clues >= this.targetClues) {
          this.eventBus.emit('audioPlay', 'win');
          // Auto-Ticker Perk: 35% chance to immediately slam stamp closed!
          if (this.gameState.hasAutoTicker && Math.random() < 0.35) {
            setTimeout(() => {
              if (this.clues >= this.targetClues && !this.isStamping) {
                const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
                if (canvas) {
                  this.slamStamp(canvas.width * 0.58, canvas.height * 0.76);
                }
              }
            }, 600);
          }
        }
      }
    });

    window.addEventListener('mousedown', (e) => {
      if (this.isStamping) return;

      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const { x, y, w, h } = this.getDossierBounds(canvas.width, canvas.height);
      if (e.clientX >= x && e.clientX <= x + w && e.clientY >= y && e.clientY <= y + h) {
        if (this.clues >= this.targetClues) {
          this.slamStamp(canvas.width * 0.58, canvas.height * 0.76);
        }
      }
    });
  }

  public getDossierBounds(width: number, height: number): { x: number; y: number; w: number; h: number } {
    const w = 86;
    const h = 46;
    const x = width * 0.58 - w * 0.5;
    const y = height * 0.77 - h * 0.5;
    return { x, y, w, h };
  }

  public slamStamp(sx: number, sy: number): void {
    this.isStamping = true;
    this.stampProgress = 0;

    // Trigger slam sequence
    setTimeout(() => {
      this.eventBus.emit('audioPlay', 'impact');
      this.casesSolved++;

      // Award Bounty
      const bounty = (140 + this.casesSolved * 30) * this.gameState.totalMultiplier;
      this.gameState.addFunds(bounty);

      this.eventBus.emit('floatingText', {
        x: sx,
        y: sy - 20,
        text: `★ CASE CLOSED! +€${bounty.toFixed(0)} ★`,
        color: '#ef4444'
      });

      this.clues = 0;
      this.isStamping = false;
    }, 280);
  }

  public override update(deltaTime: number): void {
    if (this.isStamping) {
      this.stampProgress += deltaTime * 4.0;
      // Parabolic stamp slam down
      this.stampOffsetY = Math.sin(Math.min(1.0, this.stampProgress) * Math.PI) * 22;
    } else {
      this.stampOffsetY = 0;
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const { x, y, w, h } = this.getDossierBounds(width, height);

    ctx.save();

    // 1. Dossier Shadow on Desk
    ctx.fillStyle = 'rgba(10, 6, 4, 0.65)';
    ctx.fillRect(x - 4, y + 4, w + 8, h + 4);

    // 2. Manila Folder Body
    ctx.fillStyle = '#d4b483';
    ctx.fillRect(x, y, w, h);
    ctx.strokeStyle = '#a6824c';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(x, y, w, h);

    // Folder Tab at top
    ctx.fillStyle = '#c5a06d';
    ctx.fillRect(x + 4, y - 6, 28, 6);
    ctx.strokeRect(x + 4, y - 6, 28, 6);

    // Red String Tie across file
    ctx.strokeStyle = '#b91c1c';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(x + w * 0.5, y);
    ctx.lineTo(x + w * 0.5, y + h);
    ctx.stroke();

    // Case Header / Label
    ctx.fillStyle = '#261b11';
    ctx.font = 'bold 8px monospace';
    ctx.textAlign = 'center';
    ctx.fillText(`CASE #${100 + this.casesSolved}`, x + w * 0.5, y + 16);

    // Clues Progress Bar / Pips
    const isReady = this.clues >= this.targetClues;
    if (isReady) {
      // Flashing ready prompt
      const flash = Math.sin(Date.now() * 0.008) > 0;
      ctx.fillStyle = flash ? '#dc2626' : '#991b1b';
      ctx.font = 'bold 9px sans-serif';
      ctx.fillText('STAMP TO SOLVE!', x + w * 0.5, y + 32);

      // Rubber Stamp sitting on folder or in air slamming
      const stampY = y - 18 + this.stampOffsetY;
      ctx.save();
      ctx.translate(x + w * 0.5, stampY);

      // Wood Stamp Handle
      ctx.fillStyle = '#854d0e';
      ctx.fillRect(-4, -14, 8, 14);

      // Brass Mount & Red Rubber Base
      ctx.fillStyle = '#ca8a04';
      ctx.fillRect(-14, 0, 28, 4);
      ctx.fillStyle = '#dc2626';
      ctx.fillRect(-16, 4, 32, 6);
      ctx.restore();
    } else {
      ctx.fillStyle = '#785d3f';
      ctx.font = '8px sans-serif';
      ctx.fillText(`CLUES: ${this.clues}/${this.targetClues}`, x + w * 0.5, y + 32);
    }

    // Status Caption
    ctx.fillStyle = isReady ? '#ef4444' : '#a89478';
    ctx.font = 'bold 9px sans-serif';
    ctx.fillText(isReady ? 'READY!' : 'INVESTIGATING', x + w * 0.5, y + h + 14);

    ctx.restore();
  }
}
