using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public readonly struct GlobeDragRelease
    {
        public readonly double HorizontalPixels, Seconds;
        public readonly bool IsClick;

        public GlobeDragRelease(double horizontalPixels, double seconds, bool isClick)
        {
            HorizontalPixels = horizontalPixels;
            Seconds = seconds;
            IsClick = isClick;
        }
    }

    /// <summary>One press-to-release gesture. Movement never accumulates rotation or path-length power.</summary>
    public sealed class GlobeDragGesture
    {
        public bool Active { get; private set; }
        Vector2 start;
        double beganAt;
        float maxExcursionSquared;

        public void Begin(Vector2 position, double time)
        {
            Cancel();
            if (!Valid(position) || !Finite(time)) return;
            start = position;
            beganAt = time;
            Active = true;
        }

        public void Move(Vector2 position)
        {
            if (!Active) return;
            if (!Valid(position)) { Cancel(); return; }
            maxExcursionSquared = Mathf.Max(maxExcursionSquared, (position - start).sqrMagnitude);
        }

        public bool TryRelease(Vector2 position, double time, float clickMaxPixels, out GlobeDragRelease release)
        {
            release = default;
            if (!Active) return false;
            Move(position);
            if (!Active || !Finite(time) || time < beganAt) { Cancel(); return false; }
            float clickDistance = Mathf.Max(0, clickMaxPixels);
            release = new GlobeDragRelease(position.x - start.x, time - beganAt,
                maxExcursionSquared <= clickDistance * clickDistance);
            Cancel();
            return true;
        }

        public void Cancel()
        {
            Active = false;
            maxExcursionSquared = 0;
        }

        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        static bool Valid(Vector2 position) => Finite(position.x) && Finite(position.y);
    }
}
