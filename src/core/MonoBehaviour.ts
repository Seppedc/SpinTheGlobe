import { IMonoBehaviour } from './Types.ts';

/**
 * Abstract MonoBehaviour base class mirroring Unity's UnityEngine.MonoBehaviour.
 * Provides standard lifecycle hooks that can be overridden by any game component.
 */
export abstract class MonoBehaviour implements IMonoBehaviour {
  public awake?(): void;
  public start?(): void;
  public update?(deltaTime: number): void;
  public fixedUpdate?(fixedDeltaTime: number): void;
  public render?(ctx: CanvasRenderingContext2D, width: number, height: number): void;
  public onDestroy?(): void;
}
