/**
 * GameLoop provides a decoupled fixed-timestep physics loop and variable-rate render loop,
 * precisely matching Unity's FixedUpdate / Update execution order.
 */
export class GameLoop {
  private isRunning: boolean = false;
  private lastTime: number = 0;
  private accumulator: number = 0;
  private readonly fixedDeltaTime: number = 1 / 60; // 60Hz fixed physics step
  private animationFrameId: number | null = null;

  constructor(
    private fixedUpdateFn: (fixedDt: number) => void,
    private updateFn: (dt: number) => void,
    private renderFn: () => void
  ) {}

  public start(): void {
    if (this.isRunning) return;
    this.isRunning = true;
    this.lastTime = performance.now();
    this.accumulator = 0;
    this.loop = this.loop.bind(this);
    this.animationFrameId = requestAnimationFrame(this.loop);
  }

  public stop(): void {
    this.isRunning = false;
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
  }

  private loop(currentTime: number): void {
    if (!this.isRunning) return;

    let frameTime = (currentTime - this.lastTime) / 1000;
    if (frameTime > 0.25) {
      // Prevent spiral of death on long lag spikes or background tab
      frameTime = 0.25;
    }
    this.lastTime = currentTime;

    this.accumulator += frameTime;

    // Fixed timestep execution for physics (Unity FixedUpdate)
    while (this.accumulator >= this.fixedDeltaTime) {
      this.fixedUpdateFn(this.fixedDeltaTime);
      this.accumulator -= this.fixedDeltaTime;
    }

    // Variable timestep execution for logic/animations (Unity Update)
    this.updateFn(frameTime);

    // Render pass
    this.renderFn();

    this.animationFrameId = requestAnimationFrame(this.loop);
  }
}
