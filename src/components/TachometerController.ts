import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GlobeController } from './GlobeController.ts';
import { GameState } from '../core/GameState.ts';

/**
 * TachometerController renders the vintage brass tachometer gauge,
 * displaying speed ratio, needle deflection, redline "MAX!" state,
 * and a tactile brass rotation direction toggle.
 * Maps directly to Unity's Tachometer MonoBehaviour.
 */
export class TachometerController extends MonoBehaviour {
  constructor(
    private globeController: GlobeController,
    private gameState: GameState
  ) {
    super();
  }

  public override awake(): void {
    window.addEventListener('mousedown', (e) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;
      const g = this.globeController.getGeometry(canvas.width, canvas.height);
      const tx = g.cx + g.r + 35;
      const ty = g.cy + 30;
      const tr = 34;
      const bx = tx - 34;
      const by = ty + tr + 8;
      const bw = 68;
      const bh = 20;

      if (e.clientX >= bx && e.clientX <= bx + bw && e.clientY >= by && e.clientY <= by + bh) {
        this.globeController.toggleDirection();
      }
    });
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const g = this.globeController.getGeometry(width, height);
    const tx = g.cx + g.r + 35;
    const ty = g.cy + 30;
    const tr = 34;

    ctx.save();

    // 1. Outer brass dial casing
    ctx.fillStyle = '#2b1b11';
    ctx.beginPath();
    ctx.arc(tx, ty, tr, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#a47838';
    ctx.lineWidth = 3;
    ctx.stroke();

    // 2. Compute speed ratio relative to max velocity
    const maxVel = this.gameState.maxVelocity;
    const ratio = maxVel > 0 ? Math.min(1.0, this.globeController.velocity / maxVel) : 0;

    // 3. Redline zone
    ctx.strokeStyle = '#dc2626';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.arc(tx, ty, tr - 6, Math.PI * 0.1, Math.PI * 0.5);
    ctx.stroke();

    // 4. Indicator Needle
    const needleAngle = Math.PI * 0.8 + ratio * (Math.PI * 1.4);
    ctx.strokeStyle = ratio > 0.95 ? '#ef4444' : '#e5b972';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(tx, ty);
    ctx.lineTo(tx + Math.cos(needleAngle) * (tr - 8), ty + Math.sin(needleAngle) * (tr - 8));
    ctx.stroke();

    // 5. Dial label / MAX readout
    ctx.fillStyle = ratio > 0.95 ? '#ef4444' : '#b89c74';
    ctx.font = 'bold 9px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(ratio > 0.95 ? 'MAX!' : 'RPM', tx, ty + 18);

    // 6. Vintage Brass Direction Toggle Badge
    const bx = tx - 34;
    const by = ty + tr + 8;
    const bw = 68;
    const bh = 20;

    ctx.fillStyle = '#1e140d';
    ctx.fillRect(bx, by, bw, bh);
    ctx.strokeStyle = '#8a6230';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(bx, by, bw, bh);

    const dirText = this.globeController.spinDirection === -1 ? 'DIR: ⟲ WEST' : 'DIR: ⟳ EAST';
    ctx.fillStyle = '#e5b972';
    ctx.font = 'bold 9px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(dirText, tx, by + 13);

    ctx.restore();
  }
}
