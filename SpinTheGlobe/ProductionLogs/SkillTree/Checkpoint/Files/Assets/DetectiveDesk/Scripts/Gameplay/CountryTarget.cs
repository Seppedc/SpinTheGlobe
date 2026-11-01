using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Exact polygon hit region on the map plane, without giant overlapping box colliders.</summary>
    public sealed class CountryTarget : MonoBehaviour
    {
        public CountryData data;
        public WallMapManager map;
        public bool Contains(float latitude, float longitude) => data && data.Contains(latitude, longitude);
        public bool Select() => map && data && map.Launch(data, data.latitude, data.longitude);
    }
}
