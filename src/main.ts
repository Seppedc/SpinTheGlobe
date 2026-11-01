import './style.css';
import { GameManager } from './core/GameManager.ts';

window.addEventListener('DOMContentLoaded', () => {
  const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
  if (!canvas) {
    throw new Error('gameCanvas element not found in DOM');
  }

  const gameManager = new GameManager();
  gameManager.init(canvas);

  // Expose to window for debugging if desired
  (window as any).gameManager = gameManager;
});
