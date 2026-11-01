import { EventBus } from './EventBus.ts';
import { GameState } from './GameState.ts';
import { GameLoop } from './GameLoop.ts';
import { MonoBehaviour } from './MonoBehaviour.ts';
import { GlobeController } from '../components/GlobeController.ts';
import { PinManager } from '../components/PinManager.ts';
import { MeridianTrigger } from '../components/MeridianTrigger.ts';
import { CorkboardController } from '../components/CorkboardController.ts';
import { DeskFigurineController } from '../components/DeskFigurineController.ts';
import { TachometerController } from '../components/TachometerController.ts';
import { WindowRenderer } from '../components/WindowRenderer.ts';
import { DeskRenderer } from '../components/DeskRenderer.ts';
import { PinFlightManager } from '../components/PinFlightManager.ts';
import { FloatingTextManager } from '../components/FloatingTextManager.ts';
import { AudioController } from '../components/AudioController.ts';
import { UIController } from '../components/UIController.ts';
import { WhiskeyGlassController } from '../components/WhiskeyGlassController.ts';
import { DeskLampController } from '../components/DeskLampController.ts';
import { PinBoxController } from '../components/PinBoxController.ts';
import { PaperTossManager } from '../components/PaperTossManager.ts';
import { EvidenceCaseOpener } from '../components/EvidenceCaseOpener.ts';
import { CaseDossierController } from '../components/CaseDossierController.ts';
import { SkillTreeModal } from '../components/SkillTreeModal.ts';

/**
 * GameManager is the master coordinator mirroring Unity's GameManager MonoBehaviour.
 * It manages component lifecycles, fixed-timestep physics updates, and render passes.
 */
export class GameManager {
  private canvas!: HTMLCanvasElement;
  private ctx!: CanvasRenderingContext2D;
  private gameLoop!: GameLoop;

  // Core singletons
  public readonly eventBus: EventBus;
  public readonly gameState: GameState;

  // Registered Components
  public readonly pinManager: PinManager;
  public readonly globeController: GlobeController;
  public readonly meridianTrigger: MeridianTrigger;
  public readonly corkboardController: CorkboardController;
  public readonly figurineController: DeskFigurineController;
  public readonly tachometerController: TachometerController;
  public readonly windowRenderer: WindowRenderer;
  public readonly deskRenderer: DeskRenderer;
  public readonly pinFlightManager: PinFlightManager;
  public readonly floatingTextManager: FloatingTextManager;
  public readonly audioController: AudioController;
  public readonly uiController: UIController;
  public readonly whiskeyGlass: WhiskeyGlassController;
  public readonly deskLamp: DeskLampController;
  public readonly pinBox: PinBoxController;
  public readonly paperToss: PaperTossManager;
  public readonly caseOpener: EvidenceCaseOpener;
  public readonly caseDossier: CaseDossierController;
  public readonly skillTreeModal: SkillTreeModal;

  private readonly components: MonoBehaviour[];

  constructor() {
    this.eventBus = new EventBus();
    this.gameState = new GameState(this.eventBus);

    this.pinManager = new PinManager(this.gameState, this.eventBus);
    this.globeController = new GlobeController(this.gameState, this.eventBus, this.pinManager);
    this.meridianTrigger = new MeridianTrigger(this.globeController, this.pinManager);
    this.corkboardController = new CorkboardController(this.pinManager, this.eventBus);
    this.figurineController = new DeskFigurineController(this.gameState, this.eventBus);
    this.tachometerController = new TachometerController(this.globeController, this.gameState);
    this.windowRenderer = new WindowRenderer();
    this.deskRenderer = new DeskRenderer();
    this.pinFlightManager = new PinFlightManager(
      this.eventBus,
      this.globeController,
      this.corkboardController
    );
    this.floatingTextManager = new FloatingTextManager(this.eventBus);
    this.audioController = new AudioController(this.eventBus);
    this.uiController = new UIController(this.gameState, this.globeController, this.pinManager);

    // Detective Desk Stations
    this.whiskeyGlass = new WhiskeyGlassController(this.eventBus, this.gameState);
    this.deskLamp = new DeskLampController(this.eventBus);
    this.pinBox = new PinBoxController(this.gameState, this.pinManager, this.eventBus);
    this.corkboardController.setPinBoxController(this.pinBox);
    this.paperToss = new PaperTossManager(this.gameState, this.eventBus);
    this.caseOpener = new EvidenceCaseOpener(this.gameState, this.eventBus);
    this.caseDossier = new CaseDossierController(this.gameState, this.eventBus);
    this.skillTreeModal = new SkillTreeModal(this.gameState, this.eventBus);

    this.components = [
      this.windowRenderer,
      this.corkboardController,
      this.pinManager,
      this.globeController,
      this.meridianTrigger,
      this.figurineController,
      this.tachometerController,
      this.deskRenderer,
      this.paperToss,
      this.whiskeyGlass,
      this.caseOpener,
      this.caseDossier,
      this.pinBox,
      this.deskLamp,
      this.pinFlightManager,
      this.floatingTextManager,
      this.audioController,
      this.uiController,
      this.skillTreeModal
    ];
  }

  public init(canvasElement: HTMLCanvasElement): void {
    this.canvas = canvasElement;
    const context = this.canvas.getContext('2d');
    if (!context) {
      throw new Error('Could not get 2D canvas context');
    }
    this.ctx = context;

    this.handleResize();
    window.addEventListener('resize', () => this.handleResize());

    // Unity Lifecycle: Awake pass
    for (const comp of this.components) {
      comp.awake?.();
    }

    // Unity Lifecycle: Start pass
    for (const comp of this.components) {
      comp.start?.();
    }

    // Setup GameLoop with fixed physics & variable render
    this.gameLoop = new GameLoop(
      (fixedDt) => this.fixedUpdate(fixedDt),
      (dt) => this.update(dt),
      () => this.render()
    );

    this.gameLoop.start();
  }

  private handleResize(): void {
    this.canvas.width = window.innerWidth;
    this.canvas.height = window.innerHeight - 40;
  }

  /**
   * Fixed physics timestep step (Unity FixedUpdate)
   */
  private fixedUpdate(fixedDeltaTime: number): void {
    this.globeController.fixedUpdate?.(fixedDeltaTime);
    this.meridianTrigger.fixedUpdate?.(fixedDeltaTime);
  }

  /**
   * Variable frame update (Unity Update)
   */
  private update(deltaTime: number): void {
    this.deskRenderer.update?.(deltaTime);
    this.paperToss.update?.(deltaTime);
    this.whiskeyGlass.update?.(deltaTime);
    this.deskLamp.update?.(deltaTime);
    this.pinBox.update?.(deltaTime);
    this.figurineController.update?.(deltaTime);
    this.caseOpener.update?.(deltaTime);
    this.caseDossier.update?.(deltaTime);
    this.pinManager.update?.(deltaTime);
    this.pinFlightManager.update?.(deltaTime);
    this.floatingTextManager.update?.(deltaTime);
    this.uiController.update?.(deltaTime);
    this.skillTreeModal.update?.(deltaTime);

    // Keep lamp multiplier synchronized with GameState
    this.gameState.lampMultiplier = this.deskLamp.lightMultiplier;
  }

  /**
   * Canvas visual rendering pass with layered detective atmosphere
   */
  private render(): void {
    const width = this.canvas.width;
    const height = this.canvas.height;

    // 1. Room tone clear
    this.ctx.clearRect(0, 0, width, height);
    this.ctx.fillStyle = '#140f0b';
    this.ctx.fillRect(0, 0, width, height);

    // 2. Rainy Window
    this.windowRenderer.render?.(this.ctx, width, height);

    // 3. Wall Corkboard Map
    this.corkboardController.render?.(this.ctx, width, height);

    // 4. Mahogany Desk & Room Atmosphere
    this.deskRenderer.render?.(this.ctx, width, height);

    // 5. Wastebasket & Paper Balls
    this.paperToss.render?.(this.ctx, width, height);

    // 6. Vintage Brass Tachometer
    this.tachometerController.render?.(this.ctx, width, height);

    // 7. 3D Mechanical Globe (Continents, Stand, Rotating Lines, Meridian Wire)
    this.globeController.render?.(this.ctx, width, height);

    // 8. Pedestal Figurine (Reconstruction -> Gold Infusion -> Smash)
    this.figurineController.render?.(this.ctx, width, height);

    // 9. Interactive Whiskey Glass
    this.whiskeyGlass.render?.(this.ctx, width, height);

    // 10. Confidential Case Dossier & Rubber Stamp
    this.caseDossier.render?.(this.ctx, width, height);

    // 11. Seized Evidence Crate
    this.caseOpener.render?.(this.ctx, width, height);

    // 12. Velvet-Lined Brass Pin Box
    this.pinBox.render?.(this.ctx, width, height);

    // 13. Vintage Green Banker's Desk Lamp with Light Cone & Beaded Chain
    this.deskLamp.render?.(this.ctx, width, height);

    // 14. Flying Pin Trajectories & Tumbling Dropped Pins
    this.pinFlightManager.render?.(this.ctx, width, height);

    // 15. Rising Cash Popups & Floating Notifications
    this.floatingTextManager.render?.(this.ctx, width, height);
  }
}
