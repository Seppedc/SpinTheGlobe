import { GlobeGeometry } from '../core/Types.ts';
import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';
import { PinManager } from './PinManager.ts';
import { CONTINENTS } from '../data/ContinentData.ts';

/**
 * GlobeController handles the 3D visual globe, unidirectional physics,
 * real rotating continent landmasses, antique brass meridian lines,
 * click-to-nudge, and drag-to-spin in both directions.
 * Maps directly to Unity's GlobeController MonoBehaviour.
 */
export class GlobeController extends MonoBehaviour {
  public angle: number = 0;
  public prevAngle: number = 0;
  public velocity: number = 0;
  public friction: number = 0.985; // Drag factor per 60Hz step
  public lastFullTurns: number = 0;

  // Spin direction: -1 = right-to-left (new requested direction), 1 = left-to-right
  public spinDirection: 1 | -1 = -1;

  // Drag & Click interaction
  private isDragging: boolean = false;
  private dragStartX: number = 0;
  private dragStartY: number = 0;
  private totalDragDist: number = 0;
  private readonly torqueMultiplier: number = 0.00010;
  private lastSpinSoundTime: number = 0;

  constructor(
    private gameState: GameState,
    private eventBus: EventBus,
    private pinManager: PinManager
  ) {
    super();
  }

  public override awake(): void {
    this.setupInput();
  }

  public toggleDirection(): void {
    this.spinDirection = this.spinDirection === -1 ? 1 : -1;
    this.eventBus.emit('audioPlay', 'click');
  }

  public getGeometry(width: number, height: number): GlobeGeometry {
    const r = Math.min(width, height) * 0.22;
    return {
      cx: width * 0.5,
      cy: height * 0.72,
      r: r
    };
  }

  public override fixedUpdate(_fixedDeltaTime: number): void {
    this.prevAngle = this.angle;

    // Apply baseline motor floor or natural friction
    if (this.gameState.autoLevel > 0) {
      if (this.velocity < this.gameState.baseAutoSpeed) {
        this.velocity = this.gameState.baseAutoSpeed;
      } else {
        this.velocity = Math.max(this.gameState.baseAutoSpeed, this.velocity * this.friction);
      }
    } else {
      this.velocity *= this.friction;
      if (this.velocity < 0.0001) this.velocity = 0;
    }

    // Enforce terminal velocity cap
    const maxVel = this.gameState.maxVelocity;
    if (this.velocity > maxVel) {
      this.velocity = maxVel;
    }

    // Advance angle
    this.angle += this.velocity;

    // Check 360° turn revolution crossing
    const currentTurns = Math.floor(this.angle / (Math.PI * 2));
    if (currentTurns > this.lastFullTurns) {
      this.lastFullTurns = currentTurns;
      const baseReward = 5.0 * this.gameState.totalMultiplier;
      this.gameState.addFunds(baseReward);

      this.eventBus.emit('revolutionCompleted', currentTurns, baseReward);
      this.eventBus.emit('audioPlay', 'meridian');
    }
  }

  public applyTorque(amount: number): void {
    if (amount <= 0) return;
    const rushBoost = this.gameState.rushMultiplier > 1 ? 1.8 : 1.0;
    this.velocity = Math.min(this.gameState.maxVelocity, this.velocity + amount * rushBoost);

    // Throttle audio spin trigger
    const now = performance.now();
    if (now - this.lastSpinSoundTime > 160) {
      this.lastSpinSoundTime = now;
      this.eventBus.emit('audioPlay', 'spin');
    }
  }

  private setupInput(): void {
    window.addEventListener('mousedown', (e) => {
      const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
      if (!canvas) return;
      const geom = this.getGeometry(canvas.width, canvas.height);
      const dx = e.clientX - geom.cx;
      const dy = e.clientY - geom.cy;
      if (Math.hypot(dx, dy) <= geom.r) {
        this.isDragging = true;
        this.dragStartX = e.clientX;
        this.dragStartY = e.clientY;
        this.totalDragDist = 0;
      }
    });

    window.addEventListener('mousemove', (e) => {
      if (!this.isDragging) return;
      const deltaX = e.clientX - this.dragStartX;
      const deltaY = e.clientY - this.dragStartY;
      this.totalDragDist += Math.hypot(deltaX, deltaY);
      this.dragStartX = e.clientX;
      this.dragStartY = e.clientY;

      // Drag acceleration in current spin direction
      const pullDelta = this.spinDirection === -1 ? -deltaX : deltaX;
      if (pullDelta > 0) {
        this.applyTorque(pullDelta * this.torqueMultiplier);
      }
    });

    window.addEventListener('mouseup', (e) => {
      if (this.isDragging) {
        // Click detection: if cursor moved less than 8px, it is a click/tap nudge!
        if (this.totalDragDist < 8) {
          this.applyTorque(0.0038); // Nudge forward
          this.eventBus.emit('audioPlay', 'click');
          this.eventBus.emit('floatingText', {
            x: e.clientX,
            y: e.clientY - 20,
            text: '+Nudge',
            color: '#e5b972'
          });
        }
        this.isDragging = false;
      }
    });
  }

  /**
   * Projects geographic spherical [lon, lat] coordinates onto the 3D orthographic globe.
   * Positive Z represents the visible front-facing hemisphere towards the user.
   */
  public projectSpherical(
    lonDeg: number,
    latDeg: number,
    g: GlobeGeometry
  ): { x: number; y: number; z: number; visible: boolean } {
    const latRad = (latDeg * Math.PI) / 180;
    const lonRad = (lonDeg * Math.PI) / 180;
    // Current longitude in world space
    const alpha = (this.spinDirection * this.angle) + lonRad;

    // 3D Unit sphere coordinates
    const X = Math.cos(latRad) * Math.sin(alpha);
    const Y = Math.sin(latRad);
    const Z = Math.cos(latRad) * Math.cos(alpha);

    return {
      x: g.cx + X * g.r,
      y: g.cy - Y * g.r,
      z: Z,
      visible: Z > 0
    };
  }

  public override render(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    const g = this.getGeometry(width, height);
    ctx.save();

    // 1. Ornate Cast Brass Stand & Turned Pedestal
    ctx.fillStyle = '#4a2f13';
    ctx.beginPath();
    ctx.ellipse(g.cx, g.cy + g.r + 52, 68, 18, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#825925';
    ctx.lineWidth = 2.5;
    ctx.stroke();

    // Central brass shaft
    const shaftGrad = ctx.createLinearGradient(g.cx - 12, 0, g.cx + 12, 0);
    shaftGrad.addColorStop(0, '#533718');
    shaftGrad.addColorStop(0.5, '#c99846');
    shaftGrad.addColorStop(1, '#43290e');
    ctx.fillStyle = shaftGrad;
    ctx.fillRect(g.cx - 10, g.cy + g.r, 20, 52);

    // Cast brass cradle arm (semicircle arc holding the polar axis)
    ctx.strokeStyle = '#c99846';
    ctx.lineWidth = 6;
    ctx.beginPath();
    ctx.arc(g.cx, g.cy, g.r + 14, 0.15 * Math.PI, 0.85 * Math.PI);
    ctx.stroke();

    // 2. Globe Sphere Clipping
    ctx.beginPath();
    ctx.arc(g.cx, g.cy, g.r, 0, Math.PI * 2);
    ctx.clip();

    // 3. Antique Nautical Ocean Fill
    const oceanGrad = ctx.createRadialGradient(
      g.cx - g.r * 0.35,
      g.cy - g.r * 0.35,
      g.r * 0.1,
      g.cx,
      g.cy,
      g.r
    );
    oceanGrad.addColorStop(0, '#284152');
    oceanGrad.addColorStop(0.6, '#1a2c38');
    oceanGrad.addColorStop(1, '#0e181f');
    ctx.fillStyle = oceanGrad;
    ctx.fillRect(g.cx - g.r, g.cy - g.r, g.r * 2, g.r * 2);

    // 4. Rotating Latitude Parallels (Equator & Tropics)
    ctx.strokeStyle = 'rgba(218, 165, 32, 0.22)';
    ctx.lineWidth = 1;
    const parallels = [0, 30, 60, -30, -60];
    for (const lat of parallels) {
      const latRad = (lat * Math.PI) / 180;
      const parallelY = g.cy - Math.sin(latRad) * g.r;
      const parallelR = Math.cos(latRad) * g.r;
      ctx.beginPath();
      ctx.ellipse(g.cx, parallelY, parallelR, parallelR * 0.22, 0, 0, Math.PI * 2);
      ctx.stroke();
    }

    // 5. Rotating Longitude Meridian Lines
    const numLongs = 12;
    for (let i = 0; i < numLongs; i++) {
      const longAngle = (this.spinDirection * this.angle) + i * ((Math.PI * 2) / numLongs);
      const xOffset = Math.sin(longAngle) * g.r;
      if (Math.cos(longAngle) > 0) {
        ctx.beginPath();
        ctx.ellipse(g.cx + xOffset, g.cy, Math.abs(Math.sin(longAngle) * 14), g.r, 0, 0, Math.PI * 2);
        ctx.stroke();
      }
    }

    // 6. Real Rotating Continent Landmasses
    this.renderContinents(ctx, g);

    // 7. 0° START Prime Meridian Origin Ribbon
    const originAngle = (this.spinDirection * this.angle) % (Math.PI * 2);
    if (Math.cos(originAngle) > 0) {
      const ox = g.cx + Math.sin(originAngle) * g.r;
      ctx.fillStyle = '#eab308';
      ctx.fillRect(ox - 3, g.cy - g.r, 6, g.r * 2);
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 10px Georgia';
      ctx.textAlign = 'left';
      ctx.fillText('0° START', ox + 6, g.cy);
    }

    // 8. Draw Pinned Country Dots (Strictly Front-Facing)
    this.renderGlobePins(ctx, g);

    // 9. 3D Spherical Volume Shading & Rim Vignette
    const rimGrad = ctx.createRadialGradient(
      g.cx - g.r * 0.25,
      g.cy - g.r * 0.25,
      g.r * 0.55,
      g.cx,
      g.cy,
      g.r
    );
    rimGrad.addColorStop(0, 'rgba(0, 0, 0, 0)');
    rimGrad.addColorStop(0.75, 'rgba(10, 15, 20, 0.45)');
    rimGrad.addColorStop(1, 'rgba(5, 8, 12, 0.82)');
    ctx.fillStyle = rimGrad;
    ctx.fillRect(g.cx - g.r, g.cy - g.r, g.r * 2, g.r * 2);

    // Specular desk lamp highlight on the globe glass/varnish
    const specGrad = ctx.createRadialGradient(
      g.cx - g.r * 0.42,
      g.cy - g.r * 0.42,
      2,
      g.cx - g.r * 0.42,
      g.cy - g.r * 0.42,
      g.r * 0.4
    );
    specGrad.addColorStop(0, 'rgba(255, 240, 200, 0.28)');
    specGrad.addColorStop(0.5, 'rgba(255, 220, 160, 0.1)');
    specGrad.addColorStop(1, 'rgba(255, 220, 160, 0)');
    ctx.fillStyle = specGrad;
    ctx.fillRect(g.cx - g.r, g.cy - g.r, g.r * 2, g.r * 2);

    ctx.restore(); // Exit sphere clipping

    // 10. Central Brass Meridian Wire with Warm Golden Aura
    ctx.save();
    ctx.strokeStyle = '#e5b972';
    ctx.lineWidth = 3;
    ctx.shadowColor = '#e5b972';
    ctx.shadowBlur = 10;
    ctx.beginPath();
    ctx.moveTo(g.cx, g.cy - g.r - 10);
    ctx.lineTo(g.cx, g.cy + g.r + 10);
    ctx.stroke();

    // Top and bottom brass finial caps on the meridian wire
    ctx.fillStyle = '#c99846';
    ctx.beginPath();
    ctx.arc(g.cx, g.cy - g.r - 10, 4, 0, Math.PI * 2);
    ctx.arc(g.cx, g.cy + g.r + 10, 4, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }

  /**
   * Renders rotating continent landmasses projected with 3D spherical orthographic curve.
   */
  private renderContinents(ctx: CanvasRenderingContext2D, g: GlobeGeometry): void {
    ctx.save();
    ctx.fillStyle = '#b39564'; // Antique sepia parchment landmass
    ctx.strokeStyle = '#5a4123'; // Engraved map contour
    ctx.lineWidth = 1.2;

    for (const continent of CONTINENTS) {
      for (const poly of continent.polygons) {
        if (poly.length < 3) continue;

        // Check if any vertex is on the visible front hemisphere
        let hasVisibleVertex = false;
        const projected: Array<{ x: number; y: number; z: number }> = [];

        for (const [lon, lat] of poly) {
          const pt = this.projectSpherical(lon, lat, g);
          if (pt.visible) hasVisibleVertex = true;
          projected.push(pt);
        }

        if (!hasVisibleVertex) continue;

        // Draw projected continent polygon
        ctx.beginPath();
        let isFirst = true;

        for (let i = 0; i < projected.length; i++) {
          const pt = projected[i];

          // Clamp back-facing vertices to horizon circle edge to avoid cross-hemisphere artifacts
          let px = pt.x;
          let py = pt.y;
          if (pt.z <= 0) {
            const dx = pt.x - g.cx;
            const dy = pt.y - g.cy;
            const dist = Math.hypot(dx, dy);
            if (dist > 0.001) {
              px = g.cx + (dx / dist) * g.r;
              py = g.cy + (dy / dist) * g.r;
            }
          }

          if (isFirst) {
            ctx.moveTo(px, py);
            isFirst = false;
          } else {
            ctx.lineTo(px, py);
          }
        }

        ctx.closePath();
        ctx.fill();
        ctx.stroke();
      }
    }

    ctx.restore();
  }

  /**
   * Renders active pins on the globe using unified projectSpherical projection,
   * guaranteeing pins appear strictly on the front-facing hemisphere facing the player.
   */
  private renderGlobePins(ctx: CanvasRenderingContext2D, g: GlobeGeometry): void {
    for (const pin of this.pinManager.pins) {
      const pt = this.projectSpherical(pin.lon, pin.lat, g);

      if (pt.visible) {
        ctx.save();
        ctx.fillStyle = '#ef4444';
        ctx.shadowColor = '#ef4444';
        ctx.shadowBlur = 8;
        ctx.beginPath();
        ctx.arc(pt.x, pt.y, 5, 0, Math.PI * 2);
        ctx.fill();
        ctx.shadowBlur = 0;

        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 1.5;
        ctx.stroke();
        ctx.restore();
      }
    }
  }
}
