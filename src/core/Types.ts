export interface Country {
  id: string;
  name: string;
  x: number; // Normalized corkboard X (0 to 1)
  y: number; // Normalized corkboard Y (0 to 1)
  lon: number; // Longitude in degrees (-180 to 180)
  lat: number; // Latitude in degrees (-90 to 90)
  baseVal: number;
}

export interface Pin {
  id: number;
  countryId: string;
  name: string;
  lon: number;
  lat: number;
  baseVal: number;
  passesLeft: number;
  maxPasses: number;
  wobble: number;
  lastTriggeredRev: number; // Revolution index when last triggered to avoid re-triggering in same revolution
}

export interface FloatingText {
  id: number;
  x: number;
  y: number;
  text: string;
  color: string;
  life: number; // 1.0 -> 0.0
}

export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface GlobeGeometry {
  cx: number;
  cy: number;
  r: number;
}

/**
 * IMonoBehaviour defines standard component lifecycle methods
 * modeled directly after Unity's MonoBehaviour.
 */
export interface IMonoBehaviour {
  awake?(): void;
  start?(): void;
  update?(deltaTime: number): void;
  fixedUpdate?(fixedDeltaTime: number): void;
  render?(ctx: CanvasRenderingContext2D, width: number, height: number): void;
  onDestroy?(): void;
}

export type AudioSoundType =
  | 'spin'
  | 'meridian'
  | 'pin'
  | 'impact'
  | 'clatter'
  | 'wobble'
  | 'click'
  | 'pour'
  | 'drink'
  | 'chain'
  | 'tick'
  | 'win'
  | 'swish'
  | 'bounce';
