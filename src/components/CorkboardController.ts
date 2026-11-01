import { Rect, Country } from '../core/Types.ts';
import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { COUNTRIES } from '../data/Countries.ts';
import { PinManager } from './PinManager.ts';
import { EventBus } from '../core/EventBus.ts';
import { PinBoxController } from './PinBoxController.ts';

/**
 * CorkboardController manages the wall-mounted corkboard map,
 * country coordinate raycast hit-testing, and pin placement.
 * Maps directly to Unity's CorkboardMap MonoBehaviour.
 */
export class CorkboardController extends MonoBehaviour {
  private pinBoxController: PinBoxController | null = null;

  constructor(
    private pinManager: PinManager,
    private eventBus: EventBus
  ) {
    super();
  }

  public setPinBoxController(controller: PinBoxController): void {
    this.pinBoxController = controller;
  }

  public override awake(): void {
    this.setupInput();
  }

  public getMapRect(width: number, height: number): Rect {
    const mapW = width * 0.55;
    const mapH = height * 0.44;
    return {
      x: width * 0.35,
      y: height * 0.05,
      w: mapW,
      h: mapH
    };
  }

  private setupInput(): void {
    window.addEventListener('mousedown', (e) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const rect = this.getMapRect(canvas.width, canvas.height);
      if (
        e.clientX >= rect.x &&
        e.clientX <= rect.x + rect.w &&
        e.clientY >= rect.y &&
        e.clientY <= rect.y + rect.h
      ) {
        this.handleMapClick(e.clientX, e.clientY, rect);
      }
    });
  }

  private handleMapClick(mx: number, my: number, rect: Rect): void {
    const normX = (mx - rect.x) / rect.w;
    const normY = (my - rect.y) / rect.h;

    let target: Country | null = null;
    let minDist = 0.07;

    for (const c of COUNTRIES) {
      const dist = Math.hypot(c.x - normX, c.y - normY);
      if (dist < minDist) {
        minDist = dist;
        target = c;
      }
    }

    if (target) {
      // Check Pin Box Capacity
      if (this.pinBoxController && !this.pinBoxController.canTakePin()) {
        this.eventBus.emit('audioPlay', 'click');
        this.eventBus.emit('floatingText', {
          x: mx,
          y: my,
          text: 'Pin Box Empty! Wait for Drop',
          color: '#ef4444'
        });
        return;
      }

      const pin = this.pinManager.spawnPin(target);
      this.eventBus.emit('pinCreatedWithOrigin', pin, mx, my);
      this.eventBus.emit('floatingText', {
        x: mx,
        y: my,
        text: `Pinned: ${target.name}!`,
        color: '#e5b972'
      });
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const r = this.getMapRect(width, height);
    ctx.save();

    // 1. Corkboard outer frame with wood shadow
    ctx.shadowColor = 'rgba(0, 0, 0, 0.6)';
    ctx.shadowBlur = 12;
    ctx.fillStyle = '#8b5a2b';
    ctx.fillRect(r.x - 8, r.y - 8, r.w + 16, r.h + 16);
    ctx.shadowBlur = 0;

    // 2. Parchment map paper
    ctx.fillStyle = '#d4b886';
    ctx.fillRect(r.x, r.y, r.w, r.h);

    // 3. Aged Map Grid lines
    ctx.strokeStyle = 'rgba(90, 60, 30, 0.15)';
    ctx.lineWidth = 1;
    for (let x = r.x; x < r.x + r.w; x += 40) {
      ctx.beginPath();
      ctx.moveTo(x, r.y);
      ctx.lineTo(x, r.y + r.h);
      ctx.stroke();
    }
    for (let y = r.y; y < r.y + r.h; y += 40) {
      ctx.beginPath();
      ctx.moveTo(r.x, y);
      ctx.lineTo(r.x + r.w, y);
      ctx.stroke();
    }

    // 4. Country nodes
    for (const c of COUNTRIES) {
      const cx = r.x + c.x * r.w;
      const cy = r.y + c.y * r.h;

      ctx.fillStyle = '#b59360';
      ctx.beginPath();
      ctx.arc(cx, cy, 14, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#6e4720';
      ctx.stroke();

      ctx.fillStyle = '#3a2310';
      ctx.font = '10px Georgia';
      ctx.textAlign = 'center';
      ctx.fillText(c.name, cx, cy + 3);
    }

    // 5. Draw active pins & connecting red strings
    this.pinManager.renderCorkboardPins(ctx, r, COUNTRIES);

    ctx.restore();
  }
}
