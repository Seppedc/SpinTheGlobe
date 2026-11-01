import { MonoBehaviour } from '../core/MonoBehaviour.ts';

/**
 * WindowRenderer renders the rainy late-night window on the left side of the room.
 */
export class WindowRenderer extends MonoBehaviour {
  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const winW = width * 0.28;
    const winH = height * 0.82;

    ctx.save();

    // Night sky pane
    ctx.fillStyle = '#090d13';
    ctx.fillRect(16, 16, winW, winH);

    // Window frame
    ctx.strokeStyle = '#2b1c12';
    ctx.lineWidth = 10;
    ctx.strokeRect(16, 16, winW, winH);

    // Animated rain droplets
    const time = Date.now() * 0.05;
    ctx.fillStyle = 'rgba(150, 200, 255, 0.2)';
    for (let i = 0; i < 25; i++) {
      const rx = 24 + ((i * 47) % (winW - 20));
      const ry = (time * ((i % 3) + 1) + i * 80) % (winH - 20);
      ctx.fillRect(rx, 24 + ry, 2, 8);
    }

    ctx.restore();
  }
}
