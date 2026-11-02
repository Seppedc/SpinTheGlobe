using System;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Where a pin landed and how much that spot is worth.</summary>
    public readonly struct PinLanding
    {
        public readonly float Latitude, Longitude;
        public readonly bool OnLand;
        public readonly string NearCity;      // null when not near a city
        public readonly float CityCloseness;  // 1 at the centre, 0 at the radius
        public readonly double Multiplier;

        public PinLanding(float lat, float lon, bool onLand, string city, float closeness, double multiplier)
        {
            Latitude = lat; Longitude = lon; OnLand = onLand; NearCity = city; CityCloseness = closeness; Multiplier = multiplier;
        }

        public string Describe() =>
            NearCity != null ? "near " + NearCity : OnLand ? "on land" : "at sea";

        /// <summary>
        /// Random landing spot. With <paramref name="cityChance"/> it aims at a big city; otherwise it
        /// lands on land with <paramref name="landChance"/>, else at sea. Any spot that happens to be
        /// close to a city still earns the city bonus.
        /// </summary>
        public static PinLanding Sample(System.Random rng, double landChance, double cityChance, float cityRadius,
            double landMultiplier, double cityMultiplier, float minLatitude, float maxLatitude)
        {
            float lat = 0, lon = 0;
            bool placed = false;
            if (rng.NextDouble() < Math.Min(1, Math.Max(0, cityChance)))
            {
                for (int attempt = 0; attempt < 8 && !placed; attempt++)
                {
                    var city = WorldCities.All[rng.Next(WorldCities.All.Length)];
                    if (city.Latitude < minLatitude || city.Latitude > maxLatitude) continue;
                    float angle = (float)(rng.NextDouble() * Math.PI * 2);
                    float distance = cityRadius * 0.85f * (float)Math.Sqrt(rng.NextDouble());
                    lat = Mathf.Clamp(city.Latitude + Mathf.Sin(angle) * distance, minLatitude, maxLatitude);
                    lon = Wrap(city.Longitude + Mathf.Cos(angle) * distance / Mathf.Max(0.2f, Mathf.Cos(lat * Mathf.Deg2Rad)));
                    placed = true;
                }
            }
            if (!placed)
            {
                bool wantLand = rng.NextDouble() < Math.Min(1, Math.Max(0, landChance));
                for (int attempt = 0; attempt < 400; attempt++)
                {
                    lat = Mathf.Lerp(minLatitude, maxLatitude, (float)rng.NextDouble());
                    lon = Mathf.Lerp(-180f, 180f, (float)rng.NextDouble());
                    if (LandMask.IsLand(lat, lon) == wantLand) break;
                }
            }
            return Evaluate(lat, lon, cityRadius, landMultiplier, cityMultiplier);
        }

        public static PinLanding Evaluate(float lat, float lon, float cityRadius, double landMultiplier, double cityMultiplier)
        {
            bool land = LandMask.IsLand(lat, lon);
            string best = null; float bestDistance = float.MaxValue;
            foreach (var c in WorldCities.All)
            {
                float d = AngularDistance(lat, lon, c.Latitude, c.Longitude);
                if (d < bestDistance) { bestDistance = d; best = c.Name; }
            }
            float closeness = bestDistance < cityRadius ? 1f - bestDistance / cityRadius : 0f;
            if (closeness <= 0) best = null;
            double m = (land || best != null ? landMultiplier : 1) * (1 + (cityMultiplier - 1) * closeness);
            return new PinLanding(lat, lon, land || best != null, best, closeness, m);
        }

        /// <summary>Great-circle distance in degrees.</summary>
        public static float AngularDistance(float lat1, float lon1, float lat2, float lon2)
        {
            double p1 = lat1 * Math.PI / 180, p2 = lat2 * Math.PI / 180, dl = (lon2 - lon1) * Math.PI / 180;
            double cos = Math.Sin(p1) * Math.Sin(p2) + Math.Cos(p1) * Math.Cos(p2) * Math.Cos(dl);
            return (float)(Math.Acos(Math.Max(-1, Math.Min(1, cos))) * 180 / Math.PI);
        }

        static float Wrap(float lon) => Mathf.Repeat(lon + 180f, 360f) - 180f;
    }
}
