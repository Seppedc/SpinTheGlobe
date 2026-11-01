type EventCallback = (...args: any[]) => void;

/**
 * EventBus provides decoupled pub/sub messaging across components,
 * mirroring Unity's C# Action/UnityEvent system.
 */
export class EventBus {
  private listeners: Map<string, Set<EventCallback>> = new Map();

  public on(event: string, callback: EventCallback): () => void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(callback);

    // Return unbind handle
    return () => this.off(event, callback);
  }

  public off(event: string, callback: EventCallback): void {
    const list = this.listeners.get(event);
    if (list) {
      list.delete(callback);
      if (list.size === 0) {
        this.listeners.delete(event);
      }
    }
  }

  public emit(event: string, ...args: any[]): void {
    const list = this.listeners.get(event);
    if (list) {
      for (const cb of list) {
        cb(...args);
      }
    }
  }

  public clear(): void {
    this.listeners.clear();
  }
}
