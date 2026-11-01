import { MonoBehaviour } from '../core/MonoBehaviour.ts';

/**
 * DeskRenderer renders the rich atmospheric mahogany detective desk,
 * left bookshelf pedestal, warm incandescent lamp glow, and room vignette.
 */
export class DeskRenderer extends MonoBehaviour {
  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const deskY = height * 0.62;

    ctx.save();

    // 1. Mahogany Desk Surface (Lower Half)
    const deskGrad = ctx.createLinearGradient(0, deskY, 0, height);
    deskGrad.addColorStop(0, '#2c190f');
    deskGrad.addColorStop(0.3, '#211209');
    deskGrad.addColorStop(0.7, '#180c06');
    deskGrad.addColorStop(1, '#0e0603');
    ctx.fillStyle = deskGrad;
    ctx.fillRect(0, deskY, width, height - deskY);

    // Desk Wood Grain Streaks
    ctx.strokeStyle = 'rgba(60, 32, 18, 0.35)';
    ctx.lineWidth = 1;
    for (let y = deskY + 14; y < height; y += 18) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.bezierCurveTo(width * 0.3, y - 2, width * 0.7, y + 3, width, y);
      ctx.stroke();
    }

    // Polished Brass Desk Edge Trim
    const trimGrad = ctx.createLinearGradient(0, deskY - 3, 0, deskY + 5);
    trimGrad.addColorStop(0, '#755427');
    trimGrad.addColorStop(0.5, '#c99846');
    trimGrad.addColorStop(1, '#4a3215');
    ctx.fillStyle = trimGrad;
    ctx.fillRect(0, deskY - 2, width, 5);

    // Subtle edge highlight
    ctx.strokeStyle = '#e5b972';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(0, deskY - 2);
    ctx.lineTo(width, deskY - 2);
    ctx.stroke();

    // 2. Left Pedestal Shelf for Figurines
    const shelfX = width * 0.26;
    const shelfW = width * 0.18;
    const shelfY = height * 0.82 + 14;

    ctx.fillStyle = '#1c0f08';
    ctx.fillRect(shelfX, shelfY, shelfW, 16);
    ctx.strokeStyle = '#5a3818';
    ctx.lineWidth = 2;
    ctx.strokeRect(shelfX, shelfY, shelfW, 16);

    // 3. Warm Radial Incandescent Lamp Glow
    const lampX = width * 0.52;
    const lampY = height * 0.38;
    const lampGrad = ctx.createRadialGradient(lampX, lampY, 80, lampX, lampY, width * 0.55);
    lampGrad.addColorStop(0, 'rgba(255, 220, 140, 0.14)');
    lampGrad.addColorStop(0.4, 'rgba(230, 170, 80, 0.08)');
    lampGrad.addColorStop(0.8, 'rgba(150, 90, 30, 0.03)');
    lampGrad.addColorStop(1, 'rgba(0, 0, 0, 0)');
    ctx.fillStyle = lampGrad;
    ctx.fillRect(0, 0, width, height);

    // 4. Rainy Window Sill Bounce Light (Cool Blue)
    const winSillGrad = ctx.createLinearGradient(0, deskY, width * 0.3, deskY);
    winSillGrad.addColorStop(0, 'rgba(110, 160, 210, 0.12)');
    winSillGrad.addColorStop(1, 'rgba(0, 0, 0, 0)');
    ctx.fillStyle = winSillGrad;
    ctx.fillRect(0, deskY, width * 0.3, 80);

    // 5. Perimeter Detective Room Vignette
    const vigGrad = ctx.createRadialGradient(
      width * 0.5,
      height * 0.5,
      width * 0.35,
      width * 0.5,
      height * 0.5,
      width * 0.7
    );
    vigGrad.addColorStop(0, 'rgba(0, 0, 0, 0)');
    vigGrad.addColorStop(1, 'rgba(5, 3, 2, 0.75)');
    ctx.fillStyle = vigGrad;
    ctx.fillRect(0, 0, width, height);

    ctx.restore();
  }
}
