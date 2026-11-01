import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { Pin } from '../core/Types.ts';
import { EventBus } from '../core/EventBus.ts';
import { GlobeController } from './GlobeController.ts';
import { CorkboardController } from './CorkboardController.ts';
import { COUNTRIES } from '../data/Countries.ts';

interface FlyingPin {
  id: number;
  startX: number;
  startY: number;
  targetPin: Pin;
  progress: number; // 0 to 1
  duration: number; // in seconds
  arcHeight: number;
}

interface DroppingPin {
  id: number;
  x: number;
  y: number;
  vx: number;
  vy: number;
  rot: number;
  vRot: number;
  bounces: number;
  floorY: number;
  life: number; // 1.0 -> 0.0
}

interface ImpactRing {
  id: number;
  x: number;
  y: number;
  r: number;
  alpha: number;
}

/**
 * PinFlightManager handles:
 * 1. Initial pin flight trajectory from corkboard map to the 3D globe.
 * 2. Visual impact shockwave rings at the Meridian wire.
 * 3. Physical drop-off pins popping loose FROM THE CORKBOARD WALL MAP
 *    and tumbling down onto the mahogany desk when durability is exhausted.
 */
export class PinFlightManager extends MonoBehaviour {
  private flyingPins: FlyingPin[] = [];
  private droppingPins: DroppingPin[] = [];
  private impactRings: ImpactRing[] = [];

  constructor(
    private eventBus: EventBus,
    private globeController: GlobeController,
    private corkboardController: CorkboardController
  ) {
    super();
  }

  public override awake(): void {
    // When a pin is created, launch it along a trajectory from the corkboard to the globe
    this.eventBus.on('pinCreatedWithOrigin', (pin: Pin, originX: number, originY: number) => {
      this.flyingPins.push({
        id: Date.now() + Math.random(),
        startX: originX,
        startY: originY,
        targetPin: pin,
        progress: 0,
        duration: 0.38,
        arcHeight: 50 + Math.random() * 25
      });
    });

    // When pin durability reaches zero, the pin pops out OF THE CORKBOARD WALL MAP
    // and tumbles down onto the mahogany desk
    this.eventBus.on('pinDestroyed', (pin: Pin) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;

      const mapRect = this.corkboardController.getMapRect(canvas.width, canvas.height);
      const country = COUNTRIES.find((c) => c.id === pin.countryId);

      const startX = country ? mapRect.x + country.x * mapRect.w : mapRect.x + mapRect.w * 0.5;
      const startY = country ? mapRect.y + country.y * mapRect.h : mapRect.y + mapRect.h * 0.5;

      this.droppingPins.push({
        id: Date.now() + Math.random(),
        x: startX,
        y: startY,
        vx: (Math.random() - 0.5) * 80,
        vy: -60 - Math.random() * 30, // Pop up and out of the cork
        rot: Math.random() * Math.PI * 2,
        vRot: (Math.random() - 0.5) * 14,
        bounces: 0,
        floorY: canvas.height * 0.88 + (Math.random() - 0.5) * 20,
        life: 1.0
      });
    });

    // When meridian wire is triggered on the globe, spawn an illuminated impact ripple
    this.eventBus.on('meridianTriggered', (pin: Pin) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;
      const g = this.globeController.getGeometry(canvas.width, canvas.height);
      const pt = this.globeController.projectSpherical(pin.lon, pin.lat, g);

      this.impactRings.push({
        id: Date.now() + Math.random(),
        x: g.cx,
        y: pt.y,
        r: 6,
        alpha: 1.0
      });
    });
  }

  public override update(deltaTime: number): void {
    const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
    if (!canvas) return;
    const g = this.globeController.getGeometry(canvas.width, canvas.height);

    // 1. Advance Flying Pins
    for (let i = this.flyingPins.length - 1; i >= 0; i--) {
      const fp = this.flyingPins[i];
      fp.progress += deltaTime / fp.duration;

      if (fp.progress >= 1.0) {
        // Impact at globe surface
        const pt = this.globeController.projectSpherical(fp.targetPin.lon, fp.targetPin.lat, g);
        this.impactRings.push({
          id: Date.now() + Math.random(),
          x: pt.x,
          y: pt.y,
          r: 5,
          alpha: 1.0
        });

        this.eventBus.emit('audioPlay', 'impact');
        this.flyingPins.splice(i, 1);
      }
    }

    // 2. Advance Dropping Pins falling from Corkboard Map to Desk Floor
    const gravity = 680; // px/s^2
    for (let i = this.droppingPins.length - 1; i >= 0; i--) {
      const dp = this.droppingPins[i];

      dp.vy += gravity * deltaTime;
      dp.x += dp.vx * deltaTime;
      dp.y += dp.vy * deltaTime;
      dp.rot += dp.vRot * deltaTime;

      // Desk Floor collision
      if (dp.y >= dp.floorY && dp.vy > 0) {
        dp.y = dp.floorY;
        if (dp.bounces < 3) {
          dp.bounces++;
          dp.vy = -dp.vy * 0.42; // Restitution bounce
          dp.vx *= 0.65;
          dp.vRot *= 0.5;
          this.eventBus.emit('audioPlay', 'clatter');
          if (dp.bounces === 3) {
            // Signal pin settled on desk floor to recover into pin box!
            this.eventBus.emit('pinSettledOnDesk', dp.x, dp.y);
          }
        } else {
          dp.vy = 0;
          dp.vx = 0;
          dp.vRot = 0;
        }
      }

      if (dp.bounces >= 3) {
        dp.life -= deltaTime * 0.4;
        if (dp.life <= 0) {
          this.droppingPins.splice(i, 1);
        }
      }
    }

    // 3. Advance Impact Rings
    for (let i = this.impactRings.length - 1; i >= 0; i--) {
      const ring = this.impactRings[i];
      ring.r += deltaTime * 45;
      ring.alpha -= deltaTime * 2.2;
      if (ring.alpha <= 0) {
        this.impactRings.splice(i, 1);
      }
    }
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const g = this.globeController.getGeometry(width, height);

    ctx.save();

    // 1. Render Flying Pins with Trailing Thread
    for (const fp of this.flyingPins) {
      const t = Math.min(1.0, fp.progress);
      const pt = this.globeController.projectSpherical(fp.targetPin.lon, fp.targetPin.lat, g);
      const targetX = pt.x;
      const targetY = pt.y;

      // Parabolic flight curve
      const currX = (1 - t) * fp.startX + t * targetX;
      const arcOffset = Math.sin(Math.PI * t) * fp.arcHeight;
      const currY = (1 - t) * fp.startY + t * targetY - arcOffset;

      // Trailing red thread
      ctx.strokeStyle = 'rgba(239, 68, 68, 0.6)';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(fp.startX, fp.startY);
      ctx.quadraticCurveTo(
        (fp.startX + targetX) * 0.5,
        (fp.startY + targetY) * 0.5 - fp.arcHeight,
        currX,
        currY
      );
      ctx.stroke();

      // Flying Pin Head
      ctx.fillStyle = '#dc2626';
      ctx.beginPath();
      ctx.arc(currX, currY, 5, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#fff';
      ctx.lineWidth = 1.5;
      ctx.stroke();
    }

    // 2. Render Dropping Pins Falling from Corkboard down to Desk
    for (const dp of this.droppingPins) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, dp.life);
      ctx.translate(dp.x, dp.y);
      ctx.rotate(dp.rot);

      // Brass needle stem
      ctx.strokeStyle = '#c5a059';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.lineTo(0, 10);
      ctx.stroke();

      // Red pin head
      ctx.fillStyle = '#dc2626';
      ctx.beginPath();
      ctx.arc(0, 0, 4.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#fff';
      ctx.lineWidth = 1;
      ctx.stroke();

      ctx.restore();
    }

    // 3. Render Impact Rings on the Globe
    for (const ring of this.impactRings) {
      ctx.save();
      ctx.strokeStyle = `rgba(245, 197, 66, ${Math.max(0, ring.alpha).toFixed(3)})`;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(ring.x, ring.y, ring.r, 0, Math.PI * 2);
      ctx.stroke();
      ctx.restore();
    }

    ctx.restore();
  }
}
