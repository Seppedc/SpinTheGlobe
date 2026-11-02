using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Country outline on the wall map from the aiming prototype. Pins now land randomly, so this only
    /// carries reference data (polygon + base value) that future features can use.
    /// </summary>
    public sealed class CountryTarget : MonoBehaviour
    {
        public CountryData data;
        public WallMapManager map;
        public bool Contains(float latitude, float longitude) => data && data.Contains(latitude, longitude);
    }
}
