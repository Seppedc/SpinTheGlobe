import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { EventBus } from '../core/EventBus.ts';

/**
 * DeskLampController manages the vintage green banker's desk lamp:
 * - Click the lamp or beaded pull-chain to toggle ON / OFF.
 * - Animates the physical pull-chain spring motion.
 * - Projects an atmospheric warm incandescent cone of light across the detective desk.
 * - Grants a +20% "Focused Illumination" cash bonus while lit.
 * Maps directly to Unity's DeskLampController MonoBehaviour.
 */
export class DeskLampController extends MonoBehaviour {
  public isOn: boolean = true; // Lamp starts turned on
  private chainPullY: number = 0; // Spring offset animation for pull-chain
  private chainSpringVelocity: number = 0;

  constructor(private eventBus: EventBus) {
    super();
  }

  public override awake(): void {
    window.addEventListener('mousedown', (e) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const { x, y, w, h } = this.getBounds(canvas.width, canvas.height);
      if (e.clientX >= x && e.clientX <= x + w && e.clientY >= y && e.clientY <= y + h) {
        this.toggle();
      }
    });
  }

  public getBounds(width: number, height: number): { x: number; y: number; w: number; h: number } {
    const lx = width * 0.88;
    const ly = height * 0.20;
    return {
      x: lx - 40,
      y: ly - 10,
      w: 80,
      h: 85
    };
  }

  public toggle(): void {
    this.isOn = !this.isOn;
    this.chainPullY = 14; // Snap pull-chain downward
    this.chainSpringVelocity = 0;
    this.eventBus.emit('audioPlay', 'chain');
    this.eventBus.emit('floatingText', {
      x: window.innerWidth * 0.88 - 15,
      y: window.innerHeight * 0.26,
      text: this.isOn ? 'Lamp: ON (+20% Focus)' : 'Lamp: OFF',
      color: this.isOn ? '#facc15' : '#9ca3af'
    });
  }

  public get lightMultiplier(): number {
    return this.isOn ? 1.20 : 1.0;
  }

  public override update(deltaTime: number): void {
    // Spring physics for pull-chain return
    if (this.chainPullY > 0.05 || Math.abs(this.chainSpringVelocity) > 0.05) {
      const springStiffness = 320;
      const damping = 18;
      const force = -springStiffness * this.chainPullY - damping * this.chainSpringVelocity;
      this.chainSpringVelocity += force * deltaTime;
      this.chainPullY += this.chainSpringVelocity * deltaTime;
      if (Math.abs(this.chainPullY) < 0.05 && Math.abs(this.chainSpringVelocity) < 0.05) {
        this.chainPullY = 0;
        this.chainSpringVelocity = 0;
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const lx = width * 0.88;
    const ly = height * 0.20;

    ctx.save();

    // 1. Warm Incandescent Cone of Light (when turned ON)
    if (this.isOn) {
      // Wide atmospheric radial wash
      const coneGrad = ctx.createRadialGradient(lx - 10, ly + 20, 15, lx - 100, ly + 180, width * 0.65);
      coneGrad.addColorStop(0, 'rgba(255, 240, 180, 0.28)');
      coneGrad.addColorStop(0.35, 'rgba(245, 200, 100, 0.12)');
      coneGrad.addColorStop(0.7, 'rgba(180, 120, 40, 0.04)');
      coneGrad.addColorStop(1, 'rgba(0, 0, 0, 0)');
      ctx.fillStyle = coneGrad;
      ctx.fillRect(0, 0, width, height);

      // Desk surface golden pool of light
      const poolGrad = ctx.createRadialGradient(lx - 60, height * 0.65, 20, lx - 60, height * 0.65, 140);
      poolGrad.addColorStop(0, 'rgba(255, 235, 160, 0.22)');
      poolGrad.addColorStop(0.7, 'rgba(220, 160, 60, 0.06)');
      poolGrad.addColorStop(1, 'rgba(0, 0, 0, 0)');
      ctx.fillStyle = poolGrad;
      ctx.fillRect(lx - 200, height * 0.60, 280, 120);
    } else {
      // Moody dim overlay when lamp is switched OFF
      ctx.fillStyle = 'rgba(5, 3, 2, 0.35)';
      ctx.fillRect(0, 0, width, height);
    }

    // 2. Brass Lamp Gooseneck Arm & Wall Mounting Base
    ctx.strokeStyle = '#c59d5f';
    ctx.lineWidth = 5;
    ctx.beginPath();
    ctx.moveTo(lx + 25, ly + 40);
    ctx.quadraticCurveTo(lx + 20, ly - 5, lx - 10, ly);
    ctx.stroke();

    // Brass wall bracket / base
    ctx.fillStyle = '#6d4b24';
    ctx.fillRect(lx + 20, ly + 32, 12, 20);
    ctx.strokeStyle = '#c59d5f';
    ctx.lineWidth = 2;
    ctx.strokeRect(lx + 20, ly + 32, 12, 20);

    // 3. Vintage Emerald Glass Lamp Shade
    ctx.save();
    ctx.translate(lx - 12, ly + 5);
    ctx.rotate(-0.15);

    // Shade Body
    const shadeGrad = ctx.createLinearGradient(-30, -10, 30, 10);
    shadeGrad.addColorStop(0, '#064e3b'); // Dark emerald
    shadeGrad.addColorStop(0.5, '#059669'); // Rich translucent green
    shadeGrad.addColorStop(1, '#064e3b');
    ctx.fillStyle = shadeGrad;
    ctx.beginPath();
    ctx.ellipse(0, 0, 32, 14, 0, 0, Math.PI * 2);
    ctx.fill();

    // Brass shade rim
    ctx.strokeStyle = '#e5b972';
    ctx.lineWidth = 2.5;
    ctx.stroke();

    // Bulb glow underneath shade
    if (this.isOn) {
      ctx.fillStyle = '#fffbeb';
      ctx.shadowColor = '#fef08a';
      ctx.shadowBlur = 12;
      ctx.beginPath();
      ctx.arc(0, 4, 7, 0, Math.PI * 2);
      ctx.fill();
      ctx.shadowBlur = 0;
    }
    ctx.restore();

    // 4. Beaded Brass Pull-Chain
    const chainStartX = lx - 4;
    const chainStartY = ly + 18;
    const chainLength = 36 + this.chainPullY;

    ctx.strokeStyle = '#c59d5f';
    ctx.lineWidth = 1.5;
    ctx.setLineDash([2, 3]); // Beaded chain look
    ctx.beginPath();
    ctx.moveTo(chainStartX, chainStartY);
    ctx.lineTo(chainStartX, chainStartY + chainLength);
    ctx.stroke();
    ctx.setLineDash([]); // Reset line dash

    // Brass bell pull-fob at the bottom of chain
    const fobY = chainStartY + chainLength;
    ctx.fillStyle = '#eab308';
    ctx.beginPath();
    ctx.arc(chainStartX, fobY + 3, 3.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#78350f';
    ctx.lineWidth = 1;
    ctx.stroke();

    ctx.restore();
  }
}
