import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';

interface PaperBall {
  id: number;
  x: number;
  y: number;
  vx: number;
  vy: number;
  isFlying: boolean;
  isDragged: boolean;
  dragCurrentX: number;
  dragCurrentY: number;
  inBasket: boolean;
  bounces: number;
  life: number;
}

/**
 * PaperTossManager manages the interactive crumpled paper ball slingshot minigame
 * and the green wire-mesh wastebasket on the office floor.
 */
export class PaperTossManager extends MonoBehaviour {
  public paperBalls: PaperBall[] = [];

  constructor(
    private gameState: GameState,
    private eventBus: EventBus
  ) {
    super();
  }

  public override awake(): void {
    // When a paper ball is won from the evidence crate
    this.eventBus.on('paperBallSpawned', (deskX?: number, deskY?: number) => {
      this.spawnBall(deskX, deskY);
    });

    window.addEventListener('mousedown', (e) => {
      // Check if clicked an unlaunched paper ball
      for (const ball of this.paperBalls) {
        if (!ball.isFlying) {
          const dist = Math.hypot(e.clientX - ball.x, e.clientY - ball.y);
          if (dist <= 18) {
            ball.isDragged = true;
            ball.dragCurrentX = e.clientX;
            ball.dragCurrentY = e.clientY;
            break;
          }
        }
      }
    });

    window.addEventListener('mousemove', (e) => {
      for (const ball of this.paperBalls) {
        if (ball.isDragged) {
          ball.dragCurrentX = e.clientX;
          ball.dragCurrentY = e.clientY;
        }
      }
    });

    window.addEventListener('mouseup', () => {
      for (const ball of this.paperBalls) {
        if (ball.isDragged) {
          ball.isDragged = false;
          // Launch using slingshot pull-back vector
          const pullX = ball.x - ball.dragCurrentX;
          const pullY = ball.y - ball.dragCurrentY;
          const power = Math.hypot(pullX, pullY);

          if (power > 12) {
            ball.isFlying = true;
            ball.vx = pullX * 4.8;
            ball.vy = pullY * 4.8;
            this.eventBus.emit('audioPlay', 'swish');
          }
        }
      }
    });
  }

  public spawnBall(deskX?: number, deskY?: number): void {
    const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
    const defaultX = canvas ? canvas.width * 0.28 : 300;
    const defaultY = canvas ? canvas.height * 0.68 : 500;

    this.paperBalls.push({
      id: Date.now() + Math.random(),
      x: deskX ?? defaultX,
      y: deskY ?? defaultY,
      vx: 0,
      vy: 0,
      isFlying: false,
      isDragged: false,
      dragCurrentX: 0,
      dragCurrentY: 0,
      inBasket: false,
      bounces: 0,
      life: 1.0
    });

    this.eventBus.emit('floatingText', {
      x: deskX ?? defaultX,
      y: (deskY ?? defaultY) - 20,
      text: 'Paper Ball Ready! Flick to Bin',
      color: '#e5b972'
    });
  }

  public getBasketBounds(width: number, height: number): { x: number; y: number; w: number; h: number } {
    const w = 44;
    const h = 50;
    const x = width * 0.10;
    const y = height * 0.88;
    return { x, y, w, h };
  }

  public override update(deltaTime: number): void {
    const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
    if (!canvas) return;

    const basket = this.getBasketBounds(canvas.width, canvas.height);
    const floorY = canvas.height * 0.96;
    const gravity = 680;

    for (let i = this.paperBalls.length - 1; i >= 0; i--) {
      const b = this.paperBalls[i];

      if (b.isFlying && !b.inBasket) {
        b.vy += gravity * deltaTime;
        b.x += b.vx * deltaTime;
        b.y += b.vy * deltaTime;

        // Check Basket entry (Rim target)
        const insideRimX = b.x >= basket.x && b.x <= basket.x + basket.w;
        const insideRimY = b.y >= basket.y && b.y <= basket.y + 16;

        if (insideRimX && insideRimY && b.vy > 0) {
          // SWISH! Scored in the basket!
          b.inBasket = true;
          b.vx = 0;
          b.vy = 80;
          const reward = 100 * this.gameState.multiplier;
          this.gameState.addFunds(reward);
          this.eventBus.emit('audioPlay', 'win');
          this.eventBus.emit('floatingText', {
            x: basket.x + basket.w * 0.5,
            y: basket.y - 10,
            text: `SWISH! +€${reward.toFixed(0)}`,
            color: '#4ade80'
          });
        }

        // Floor collision
        if (b.y >= floorY && b.vy > 0) {
          b.y = floorY;
          if (b.bounces < 2) {
            b.bounces++;
            b.vy = -b.vy * 0.35;
            b.vx *= 0.6;
            this.eventBus.emit('audioPlay', 'bounce');
          } else {
            b.vy = 0;
            b.vx = 0;
            b.life -= deltaTime * 0.5;
            if (b.life <= 0) {
              this.paperBalls.splice(i, 1);
            }
          }
        }
      } else if (b.inBasket) {
        b.life -= deltaTime * 0.5;
        if (b.life <= 0) {
          this.paperBalls.splice(i, 1);
        }
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const basket = this.getBasketBounds(width, height);

    ctx.save();

    // 1. Green Wire-Mesh Wastebasket on Floor
    // Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
    ctx.beginPath();
    ctx.ellipse(basket.x + basket.w * 0.5, basket.y + basket.h + 2, basket.w * 0.6, 6, 0, 0, Math.PI * 2);
    ctx.fill();

    // Wire mesh bucket body
    ctx.fillStyle = 'rgba(20, 50, 35, 0.7)';
    ctx.beginPath();
    ctx.moveTo(basket.x, basket.y);
    ctx.lineTo(basket.x + 6, basket.y + basket.h);
    ctx.lineTo(basket.x + basket.w - 6, basket.y + basket.h);
    ctx.lineTo(basket.x + basket.w, basket.y);
    ctx.closePath();
    ctx.fill();

    // Wire mesh criss-cross
    ctx.strokeStyle = '#2d6a4f';
    ctx.lineWidth = 1;
    for (let lx = basket.x + 8; lx < basket.x + basket.w; lx += 8) {
      ctx.beginPath();
      ctx.moveTo(lx, basket.y);
      ctx.lineTo(lx - 4, basket.y + basket.h);
      ctx.stroke();
    }

    // Metal Rim
    ctx.strokeStyle = '#52b788';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.ellipse(basket.x + basket.w * 0.5, basket.y, basket.w * 0.5, 6, 0, 0, Math.PI * 2);
    ctx.stroke();

    // 2. Render Paper Balls
    for (const b of this.paperBalls) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, b.life);

      // Trajectory pull-back indicator if dragged
      if (b.isDragged) {
        const pullX = b.x - b.dragCurrentX;
        const pullY = b.y - b.dragCurrentY;

        ctx.strokeStyle = 'rgba(255, 235, 150, 0.6)';
        ctx.lineWidth = 1.5;
        ctx.setLineDash([4, 4]);
        ctx.beginPath();
        ctx.moveTo(b.x, b.y);
        ctx.lineTo(b.x + pullX * 1.5, b.y + pullY * 1.5);
        ctx.stroke();
        ctx.setLineDash([]);
      }

      // Ball shadow on desk if resting
      if (!b.isFlying) {
        ctx.fillStyle = 'rgba(0, 0, 0, 0.4)';
        ctx.beginPath();
        ctx.ellipse(b.x, b.y + 7, 7, 3, 0, 0, Math.PI * 2);
        ctx.fill();
      }

      // Crumpled paper ball
      ctx.fillStyle = '#f3f4f6';
      ctx.beginPath();
      ctx.arc(b.x, b.y, 7, 0, Math.PI * 2);
      ctx.fill();

      // Crumpled creases
      ctx.strokeStyle = '#9ca3af';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(b.x - 4, b.y - 2);
      ctx.lineTo(b.x + 2, b.y - 4);
      ctx.lineTo(b.x + 4, b.y + 3);
      ctx.stroke();

      ctx.restore();
    }

    ctx.restore();
  }
}
