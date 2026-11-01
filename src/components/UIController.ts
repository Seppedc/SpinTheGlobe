import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { GlobeController } from './GlobeController.ts';
import { PinManager } from './PinManager.ts';

/**
 * UIController manages HUD indicators on the detective desk:
 * Funds, Gold Coins, Globe Rotation angle & RPM, and Corkboard Pin counts.
 * Maps directly to Unity's DesktopHUDManager MonoBehaviour.
 */
export class UIController extends MonoBehaviour {
  private fundsEl: HTMLElement | null = null;
  private coinsEl: HTMLElement | null = null;
  private revsEl: HTMLElement | null = null;
  private rpmEl: HTMLElement | null = null;
  private pinsEl: HTMLElement | null = null;

  constructor(
    private gameState: GameState,
    private globeController: GlobeController,
    private pinManager: PinManager
  ) {
    super();
  }

  public override awake(): void {
    this.fundsEl = document.getElementById('val-funds');
    this.coinsEl = document.getElementById('val-coins');
    this.revsEl = document.getElementById('val-revs');
    this.rpmEl = document.getElementById('val-rpm');
    this.pinsEl = document.getElementById('val-pins');
  }

  public override update(_deltaTime: number): void {
    // 1. Funds & Gold Coins
    if (this.fundsEl) {
      this.fundsEl.textContent = `€${this.gameState.funds.toFixed(2)}`;
    }
    if (this.coinsEl) {
      this.coinsEl.textContent = `🪙 ${this.gameState.goldCoins}`;
    }

    // 2. Rotation & Revolutions
    const angle = this.globeController.angle;
    const deg = Math.floor(((angle % (Math.PI * 2)) / (Math.PI * 2)) * 360);
    const turns = Math.floor(angle / (Math.PI * 2));
    if (this.revsEl) {
      this.revsEl.textContent = `${deg}° (Rev ${turns})`;
    }

    // 3. RPM
    const rpm = Math.round((this.globeController.velocity * 3600) / (Math.PI * 2));
    if (this.rpmEl) {
      this.rpmEl.textContent = `${rpm} RPM`;
    }

    // 4. Pin counts
    if (this.pinsEl) {
      this.pinsEl.textContent = `${this.pinManager.pins.length}`;
    }
  }
}
