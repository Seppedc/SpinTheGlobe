using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Core
{
    /// <summary>
    /// Minimal typed publish/subscribe bus so gameplay systems never need direct references
    /// to each other (e.g. a pin falling off publishes <see cref="PinFellOff"/>; the case file listens).
    /// Event payloads are small readonly structs defined in GameEvents.cs.
    /// </summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            handlers.TryGetValue(typeof(T), out var existing);
            handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null || !handlers.TryGetValue(typeof(T), out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) handlers.Remove(typeof(T));
            else handlers[typeof(T)] = remaining;
        }

        public static void Publish<T>(T evt)
        {
            if (!handlers.TryGetValue(typeof(T), out var d)) return;
            // Isolate listeners: one failing listener must not stop the others.
            foreach (var listener in d.GetInvocationList())
            {
                try { ((Action<T>)listener)(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public static int ListenerCount<T>() =>
            handlers.TryGetValue(typeof(T), out var d) ? d.GetInvocationList().Length : 0;

        public static void Clear() => handlers.Clear();

        // Domain reload can be disabled in Enter Play Mode settings; start every session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => handlers.Clear();
    }
}
