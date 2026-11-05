using System;
using DetectiveDesk.Progression;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Positive phase means geographic west-to-east, independent of Euler wrap.</summary>
    public static class GameplayMath
    {
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static double Repeat(double value, double length) => value - Math.Floor(value / length) * length;
        /// <summary>Scale manual impulses with the globe while retaining the drag/click strength difference.</summary>
        public static double SpinImpulse(double amount, double power, double maxSpeed, double referenceSpeed) =>
            amount * power * Math.Max(1, maxSpeed / Math.Max(1, referenceSpeed));
        /// <summary>Equal distances moved slowly and quickly give different impulses.</summary>
        public static double DragImpulse(double pixels, double seconds, GameBalance balance, double power, double maxSpeed)
        {
            if (!Finite(pixels) || !Finite(seconds) || pixels <= 0 || seconds <= 0) return 0;
            double speedFactor = Math.Min(balance.dragMaxSpeedFactor,
                pixels / seconds / Math.Max(1, balance.dragReferencePixelsPerSecond));
            return SpinImpulse(pixels * balance.dragAcceleration * speedFactor, power, maxSpeed, balance.impulseReferenceSpeed);
        }
        public static Vector3 Spherical(float latitude, float longitude, float radius)
        {
            double lat = latitude * Math.PI / 180, lon = longitude * Math.PI / 180;
            return new Vector3((float)(radius * Math.Cos(lat) * Math.Sin(lon)),
                (float)(radius * Math.Sin(lat)), (float)(-radius * Math.Cos(lat) * Math.Cos(lon)));
        }

        // The motor maintains its baseline; friction decays only the manual speed above it.
        // Integrate the baseline and exponential excess exactly, independent of frame rate.
        public static double Integrate(ref double velocity, double seconds, double friction,
            double floor, double ceiling)
        {
            if (!Finite(seconds) || seconds <= 0) return 0;
            floor = Math.Max(0, Math.Min(floor, ceiling));
            velocity = Math.Max(floor, Math.Min(ceiling, Finite(velocity) ? velocity : floor));
            friction = Math.Max(.000001, Math.Min(1, friction));
            if (friction >= .999999999) return velocity * seconds;
            double drag = -60 * Math.Log(friction);
            double excess = velocity - floor;
            double decay = Math.Exp(-drag * seconds);
            double angle = floor * seconds + excess * (1 - decay) / drag;
            velocity = floor + excess * decay;
            // No hidden auto-spin: exponentially small unpowered speeds settle completely.
            if (floor == 0 && velocity < .005) velocity = 0;
            return angle;
        }

        public static long Crossings(double previous, double current, double first, double period = 360)
        {
            if (current <= previous || current < first) return 0;
            return Math.Max(0, (long)Math.Floor((current - first) / period + 1e-10)
                - (previous < first ? -1 : (long)Math.Floor((previous - first) / period + 1e-10)));
        }

        public static double NextCrossing(double phase, double distance)
        {
            distance = Repeat(distance, 360);
            return phase + (distance < .00001 ? 360 : distance);
        }

        public static bool PointInRing(Vector2 point, Vector2[] ring)
        {
            bool inside = false;
            if (ring == null || ring.Length < 3) return false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                Vector2 a = ring[i], b = ring[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
    }
}
