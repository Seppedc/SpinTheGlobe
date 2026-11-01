using System;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    [Serializable] public sealed class CountryRing { public Vector2[] points; public bool hole; public int polygon; }

    [CreateAssetMenu(menuName = "Detective Desk/Gameplay/Country")]
    public sealed class CountryData : ScriptableObject
    {
        public string id, displayName;
        [Range(-90, 90)] public float latitude;
        [Range(-180, 180)] public float longitude;
        [Min(0)] public double baseValue = 2;
        public CountryRing[] boundaries;

        public bool Contains(float lat, float lon)
        {
            if (boundaries == null) return false;
            Vector2 point = new Vector2(lon, lat);
            foreach (var exterior in boundaries)
            {
                if (exterior.hole || !GameplayMath.PointInRing(point, exterior.points)) continue;
                bool hole = false;
                foreach (var interior in boundaries)
                    if (interior.hole && interior.polygon == exterior.polygon && GameplayMath.PointInRing(point, interior.points)) { hole = true; break; }
                if (!hole) return true;
            }
            return false;
        }
    }
}
