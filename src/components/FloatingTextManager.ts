import { FloatingText } from '../core/Types.ts';
import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { EventBus } from '../core/EventBus.ts';

/**
 * FloatingTextManager handles rising cash popup and feedback text particles.
 */
export class FloatingTextManager extends MonoBehaviour {
  private texts: FloatingText[] = [];

  constructor(private eventBus: EventBus) {
    super();
  }

  public override awake(): void {
    this.eventBus.on('floatingText', (data: { x: number; y: number; text: string; color?: string }) => {
      this.addText(data.x, data.y, data.text, data.color || '#ffd700');
    });

    this.eventBus.on('revolutionCompleted', (_rev: number, reward: number) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;
      const cx = canvas.width * 0.5;
      const cy = canvas.height * 0.72 - (Math.min(canvas.width, canvas.height) * 0.22) - 20;
      this.addText(cx, cy, `360° Turn: +€${reward.toFixed(2)}`, '#4ade80');
    });

    this.eventBus.on('meridianTriggered', (pin: any, payout: number) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;
      const cx = canvas.width * 0.5;
      const cy = canvas.height * 0.72;
      this.addText(cx, cy, `${pin.name}: +€${payout.toFixed(2)}`, '#ffd700');
    });
  }

  public addText(x: number, y: number, text: string, color: string = '#ffd700'): void {
    this.texts.push({
      id: Date.now() + Math.random(),
      x,
      y,
      text,
      color,
      life: 1.0
    });
  }

  public override update(_deltaTime: number): void {
    for (let i = this.texts.length - 1; i >= 0; i--) {
      const ft = this.texts[i];
      ft.y -= 0.8;
      ft.life -= 0.018;
      if (ft.life <= 0) {
        this.texts.splice(i, 1);
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, _width: number, _height: number): void {
    for (const ft of this.texts) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, ft.life);
      ctx.fillStyle = ft.color;
      ctx.font = 'bold 13px Georgia';
      ctx.textAlign = 'center';
      ctx.fillText(ft.text, ft.x, ft.y);
      ctx.restore();
    }
  }
}
