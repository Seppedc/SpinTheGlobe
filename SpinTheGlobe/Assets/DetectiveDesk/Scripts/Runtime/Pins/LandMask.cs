using System;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>1°×1° land/sea lookup (data in LandMask.Data.cs, generated from Natural Earth).</summary>
    public static partial class LandMask
    {
        static byte[] bits;

        static byte[] Bits => bits ?? (bits = Convert.FromBase64String(Data));

        public static bool IsLand(float latitude, float longitude)
        {
            int row = Mathf.Clamp(Mathf.FloorToInt(90f - latitude), 0, Height - 1);
            float lon = Mathf.Repeat(longitude + 180f, 360f);
            int col = Mathf.Clamp(Mathf.FloorToInt(lon), 0, Width - 1);
            int k = row * Width + col;
            return (Bits[k >> 3] >> (k & 7) & 1) != 0;
        }
    }
}
