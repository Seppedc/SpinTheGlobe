import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GlobeController } from './GlobeController.ts';
import { PinManager } from './PinManager.ts';

/**
 * MeridianTrigger implements continuous angular sweep detection across
 * the central vertical Brass Meridian Wire strictly on the FRONT-FACING hemisphere (Z > 0).
 * Eliminates tunneling at high velocities and guarantees strict 1-hit-per-revolution lock.
 * Maps directly to Unity's MeridianTrigger MonoBehaviour.
 */
export class MeridianTrigger extends MonoBehaviour {
  constructor(
    private globeController: GlobeController,
    private pinManager: PinManager
  ) {
    super();
  }

  public override fixedUpdate(_fixedDeltaTime: number): void {
    const prevGlobe = this.globeController.prevAngle;
    const currGlobe = this.globeController.angle;

    // If globe did not advance, nothing to check
    if (currGlobe === prevGlobe) return;

    const dir = this.globeController.spinDirection; // -1 or 1
    const pins = [...this.pinManager.pins]; // Clone array to allow safe removal during iteration

    for (const pin of pins) {
      const lonRad = (pin.lon * Math.PI) / 180;

      // Continuous angular sweep across the front-facing vertical wire (alpha = 0, Z > 0)
      const prevTurn = Math.floor((prevGlobe + dir * lonRad) / (Math.PI * 2));
      const currTurn = Math.floor((currGlobe + dir * lonRad) / (Math.PI * 2));

      if (currTurn > prevTurn) {
        // Continuous crossing detected directly on the front face!
        this.pinManager.handleMeridianHit(pin, currTurn);
      }
    }
  }
}
