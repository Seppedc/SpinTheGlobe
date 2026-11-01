import { Pin, Country, Rect } from '../core/Types.ts';
import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';

/**
 * PinManager handles the lifecycle of pins on the corkboard map:
 * placement, durability tracking with visual remaining-pass pips,
 * spring wobble tilt, and removal upon reaching zero durability.
 * Maps directly to Unity's PinManager MonoBehaviour.
 */
export class PinManager extends MonoBehaviour {
  public pins: Pin[] = [];

  constructor(
    private gameState: GameState,
    private eventBus: EventBus
  ) {
    super();
  }

  public override update(deltaTime: number): void {
    // Decay pin spring wobble
    for (const pin of this.pins) {
      if (pin.wobble > 0) {
        pin.wobble = Math.max(0, pin.wobble - deltaTime * 2.5);
      }
    }
  }

  public spawnPin(country: Country): Pin {
    const maxPasses = 1 + this.gameState.durabilityLevel;
    const pin: Pin = {
      id: Date.now() + Math.random(),
      countryId: country.id,
      name: country.name,
      lon: country.lon,
      lat: country.lat,
      baseVal: country.baseVal,
      passesLeft: maxPasses,
      maxPasses: maxPasses,
      wobble: 0,
      lastTriggeredRev: -1
    };

    this.pins.push(pin);
    this.eventBus.emit('pinCreated', pin);
    this.eventBus.emit('audioPlay', 'pin');
    return pin;
  }

  public handleMeridianHit(pin: Pin, currentRev: number): boolean {
    // 1-Hit-Per-Revolution lock check
    if (pin.lastTriggeredRev === currentRev) {
      return false;
    }

    pin.lastTriggeredRev = currentRev;
    let payout = pin.baseVal * this.gameState.totalMultiplier;
    if (this.gameState.hasIncorruptibleLead) {
      payout *= 1.5;
    }
    this.gameState.addFunds(payout);

    // Midas Meridian Wire Perk: 1% chance to drop 1 Gold Coin on wire pass!
    if (this.gameState.hasMidasWire && Math.random() < 0.015) {
      this.gameState.addGoldCoin(1);
    }

    this.eventBus.emit('meridianTriggered', pin, payout);
    this.eventBus.emit('audioPlay', 'meridian');

    pin.passesLeft--;
    if (pin.passesLeft <= 0) {
      // Degraded completely - pops out of the corkboard wall map and falls down
      this.removePin(pin.id);
      this.eventBus.emit('pinDestroyed', pin);
    } else {
      // Survives another pass - bends crooked on the map & spring wobbles
      pin.wobble = 1.0;
      this.eventBus.emit('pinDegraded', pin, pin.passesLeft);
      this.eventBus.emit('audioPlay', 'wobble');
    }

    return true;
  }

  public removePin(pinId: number): void {
    const idx = this.pins.findIndex((p) => p.id === pinId);
    if (idx !== -1) {
      this.pins.splice(idx, 1);
    }
  }

  public getPins(): readonly Pin[] {
    return this.pins;
  }

  public renderCorkboardPins(ctx: CanvasRenderingContext2D, mapRect: Rect, countries: Country[]): void {
    const countryMap = new Map<string, Country>(countries.map((c) => [c.id, c]));

    // Draw red string connections between pins if > 1 pin
    if (this.pins.length > 1) {
      ctx.save();
      ctx.strokeStyle = 'rgba(210, 40, 40, 0.7)';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      for (let i = 0; i < this.pins.length; i++) {
        const c = countryMap.get(this.pins[i].countryId);
        if (!c) continue;
        const px = mapRect.x + c.x * mapRect.w;
        const py = mapRect.y + c.y * mapRect.h;
        if (i === 0) ctx.moveTo(px, py);
        else ctx.lineTo(px, py);
      }
      ctx.stroke();
      ctx.restore();
    }

    // Draw individual pins on the corkboard
    for (const pin of this.pins) {
      const c = countryMap.get(pin.countryId);
      if (!c) continue;
      const px = mapRect.x + c.x * mapRect.w;
      const py = mapRect.y + c.y * mapRect.h;

      ctx.save();
      ctx.translate(px, py);

      // Crooked bend with spring wobble vibration if degraded
      const isDegraded = pin.passesLeft < pin.maxPasses;
      if (isDegraded) {
        const springVibe = Math.sin(Date.now() * 0.04) * (pin.wobble * 0.35);
        ctx.rotate(0.40 + springVibe);
      }

      // 1. Metal needle stem into cork
      ctx.strokeStyle = '#c5a059';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.lineTo(0, isDegraded ? 8 : 4);
      ctx.stroke();

      // 2. Red Pin Head
      ctx.fillStyle = '#dc2626';
      ctx.beginPath();
      ctx.arc(0, 0, 6, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 1.5;
      ctx.stroke();

      // 3. Visual Durability Indicator: Pips showing how long pin stays on map
      if (pin.maxPasses > 1) {
        const totalPips = pin.maxPasses;
        const pipRadius = 2.5;
        const pipSpacing = 7;
        const startX = -((totalPips - 1) * pipSpacing) / 2;
        const pipY = -12;

        for (let p = 0; p < totalPips; p++) {
          const pipX = startX + p * pipSpacing;
          const isActive = p < pin.passesLeft;

          ctx.beginPath();
          ctx.arc(pipX, pipY, pipRadius, 0, Math.PI * 2);
          if (isActive) {
            // Active pass remaining: Bright glowing gold pip
            ctx.fillStyle = '#facc15';
            ctx.fill();
            ctx.strokeStyle = '#78350f';
            ctx.lineWidth = 1;
            ctx.stroke();
          } else {
            // Spent pass: Dark hollow sepia pip
            ctx.fillStyle = '#3e2715';
            ctx.fill();
            ctx.strokeStyle = '#6b4c30';
            ctx.lineWidth = 0.8;
            ctx.stroke();
          }
        }
      }

      ctx.restore();
    }
  }
}
