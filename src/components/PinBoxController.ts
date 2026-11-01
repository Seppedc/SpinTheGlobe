import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { PinManager } from './PinManager.ts';
import { EventBus } from '../core/EventBus.ts';

interface RecoveringPin {
  id: number;
  startX: number;
  startY: number;
  targetX: number;
  targetY: number;
  progress: number;
}

/**
 * PinBoxController manages the antique velvet-lined brass pin box:
 * - Displays active available pin inventory.
 * - Caps maximum simultaneous pins on the map (upgradable).
 * - Animates spent pins recovering back into the box after dropping off the map.
 * Maps directly to Unity's PinBoxController MonoBehaviour.
 */
export class PinBoxController extends MonoBehaviour {
  private recoveringPins: RecoveringPin[] = [];

  constructor(
    private gameState: GameState,
    private pinManager: PinManager,
    private eventBus: EventBus
  ) {
    super();
  }

  public override awake(): void {
    // When a pin drops off the map and settles on the desk, recover it into the box
    this.eventBus.on('pinSettledOnDesk', (deskX: number, deskY: number) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const boxPos = this.getBoxCenter(canvas.width, canvas.height);
      this.recoveringPins.push({
        id: Date.now() + Math.random(),
        startX: deskX,
        startY: deskY,
        targetX: boxPos.x,
        targetY: boxPos.y,
        progress: 0
      });
    });
  }

  public get availablePins(): number {
    return Math.max(0, this.gameState.maxPins - this.pinManager.pins.length);
  }

  public canTakePin(): boolean {
    return this.availablePins > 0;
  }

  public getBoxBounds(width: number, height: number): { x: number; y: number; w: number; h: number } {
    const w = 90;
    const h = 48;
    const x = width * 0.72 - w * 0.5;
    const y = height * 0.76 - h * 0.5;
    return { x, y, w, h };
  }

  public getBoxCenter(width: number, height: number): { x: number; y: number } {
    const bounds = this.getBoxBounds(width, height);
    return {
      x: bounds.x + bounds.w * 0.5,
      y: bounds.y + bounds.h * 0.5
    };
  }

  public override update(deltaTime: number): void {
    // Animate recovering pins sliding into box
    for (let i = this.recoveringPins.length - 1; i >= 0; i--) {
      const rp = this.recoveringPins[i];
      rp.progress += deltaTime * 2.2; // ~0.45s recovery flight

      if (rp.progress >= 1.0) {
        this.eventBus.emit('audioPlay', 'click');
        this.recoveringPins.splice(i, 1);
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const { x, y, w, h } = this.getBoxBounds(width, height);

    ctx.save();

    // 1. Box Cast Shadow on Mahogany Desk
    ctx.fillStyle = 'rgba(8, 4, 2, 0.65)';
    ctx.fillRect(x - 4, y + 4, w + 8, h + 4);

    // 2. Heavy Cast Brass Box Rim
    const brassGrad = ctx.createLinearGradient(x, y, x + w, y + h);
    brassGrad.addColorStop(0, '#a17537');
    brassGrad.addColorStop(0.5, '#d4a853');
    brassGrad.addColorStop(1, '#694619');
    ctx.fillStyle = brassGrad;
    ctx.fillRect(x, y, w, h);
    ctx.strokeStyle = '#facc15';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(x, y, w, h);

    // 3. Deep Burgundy Velvet Interior Cushion
    ctx.fillStyle = '#4c0519';
    ctx.fillRect(x + 5, y + 5, w - 10, h - 10);
    ctx.strokeStyle = '#2c030e';
    ctx.lineWidth = 1;
    ctx.strokeRect(x + 5, y + 5, w - 10, h - 10);

    // 4. Render Available Pins in the Box
    const maxPins = this.gameState.maxPins;
    const avail = this.availablePins;
    const cols = Math.min(6, Math.ceil(maxPins / 2));
    const rows = 2;
    const startX = x + 12;
    const startY = y + 14;
    const stepX = (w - 24) / Math.max(1, cols - 1);
    const stepY = 18;

    let pinIdx = 0;
    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        if (pinIdx >= maxPins) break;

        const px = startX + c * stepX;
        const py = startY + r * stepY;

        if (pinIdx < avail) {
          // Pin present in velvet cushion
          ctx.fillStyle = '#dc2626';
          ctx.beginPath();
          ctx.arc(px, py, 4.5, 0, Math.PI * 2);
          ctx.fill();
          ctx.strokeStyle = '#ffffff';
          ctx.lineWidth = 1;
          ctx.stroke();

          // Shiny highlight
          ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
          ctx.beginPath();
          ctx.arc(px - 1.5, py - 1.5, 1.2, 0, Math.PI * 2);
          ctx.fill();
        } else {
          // Empty velvet pin indent
          ctx.fillStyle = '#22030b';
          ctx.beginPath();
          ctx.arc(px, py, 2.5, 0, Math.PI * 2);
          ctx.fill();
        }

        pinIdx++;
      }
    }

    // 5. Box Inventory Badge / Caption
    ctx.textAlign = 'center';
    ctx.fillStyle = avail > 0 ? '#e5b972' : '#ef4444';
    ctx.font = 'bold 9px sans-serif';
    ctx.fillText(`PINS: ${avail}/${maxPins}`, x + w * 0.5, y + h + 14);

    // 6. Draw Recovering Pin Particles Gliding into Box
    for (const rp of this.recoveringPins) {
      const t = Math.min(1.0, rp.progress);
      const currX = (1 - t) * rp.startX + t * rp.targetX;
      const arc = Math.sin(Math.PI * t) * 35;
      const currY = (1 - t) * rp.startY + t * rp.targetY - arc;

      ctx.fillStyle = '#dc2626';
      ctx.beginPath();
      ctx.arc(currX, currY, 4, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#fff';
      ctx.lineWidth = 1;
      ctx.stroke();
    }

    ctx.restore();
  }
}
