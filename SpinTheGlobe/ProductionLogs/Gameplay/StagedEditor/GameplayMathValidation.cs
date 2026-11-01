using System;
using System.IO;
using UnityEngine;
using DetectiveDesk.Gameplay;

namespace DetectiveDesk.Editor
{
    /// <summary>Executed deterministic checks against production math and geographic data.</summary>
    public static class GameplayMathValidation
    {
        static int assertions;
        static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
        public static void Run()
        {
            assertions = 0;
            double v = 30;
            double angle = GameplayMath.Integrate(ref v, 1d / 60, .985, 0, 30);
            Check(Math.Abs(v - 29.55) < 1e-8 && angle > 0 && angle < .5, "60Hz reference friction");
            double a = 0, b = 0, va = 30, vb = 30;
            for (int i = 0; i < 30; i++) a += GameplayMath.Integrate(ref va, 1d / 30, .985, 0, 30);
            for (int i = 0; i < 144; i++) b += GameplayMath.Integrate(ref vb, 1d / 144, .985, 0, 30);
            Check(Math.Abs(a - b) < 1e-8 && Math.Abs(va - vb) < 1e-8, "Frame-rate-independent angle and speed");
            v = 30; a = GameplayMath.Integrate(ref v, 20, .985, 6, 30);
            Check(Math.Abs(v - 6) < 1e-9 && a >= 120, "Auto-floor integrated correctly when crossed inside one frame");
            v = 0; a = GameplayMath.Integrate(ref v, 600, .985, 0, 30);
            Check(a == 0 && v == 0, "Unpowered globe does not mint motion");
            v = -50; a = GameplayMath.Integrate(ref v, 1, .985, 0, 30);
            Check(a == 0 && v == 0, "Negative velocity rejected");
            v = 30; a = GameplayMath.Integrate(ref v, double.NaN, .985, 0, 30);
            Check(a == 0 && v == 30, "Invalid time rejected");
            Check(GameplayMath.Crossings(0, 1081, 360) == 3, "Skipped frames include every revolution");
            Check(GameplayMath.Crossings(360, 360, 360) == 0, "Boundary is not paid twice");
            Check(GameplayMath.Crossings(359.9, 360, 360) == 1 && GameplayMath.Crossings(360, 720, 360) == 1, "Revolution boundaries");
            Check(GameplayMath.NextCrossing(0, 0) == 360, "Pin on wire waits for a new pass");
            Check(GameplayMath.NextCrossing(100, 10) == 110 && GameplayMath.NextCrossing(100, -10) == 450, "Forward crossing schedule");
            Vector3 prime = GameplayMath.Spherical(0, 0, 1), east = GameplayMath.Spherical(0, 90, 1);
            Check((prime - Vector3.back).magnitude < 1e-6 && (east - Vector3.right).magnitude < 1e-6, "Geographic axis calibration");
            Check((Quaternion.AngleAxis(-90, Vector3.up) * prime - east).magnitude < 1e-5, "West-to-east physical rotation sign");
            int countries = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CountryData", new[] { "Assets/DetectiveDesk/Gameplay/Data" }))
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CountryData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                Check(data.Contains(data.latitude, data.longitude), data.id + " representative position inside real country");
                Check(!data.Contains(-59, -179), data.id + " does not claim empty ocean");
                countries++;
            }
            Check(countries == 8, "Eight playable countries configured");
            var square = new[] { new Vector2(0,0), new Vector2(4,0), new Vector2(4,4), new Vector2(0,4) };
            Check(GameplayMath.PointInRing(new Vector2(2,2), square) && !GameplayMath.PointInRing(new Vector2(8,2), square), "Polygon interior/exterior");
            Directory.CreateDirectory("ProductionLogs/Gameplay");
            File.WriteAllText("ProductionLogs/Gameplay/math-validation.json", "{\"status\":\"PASS\",\"assertions\":" + assertions + ",\"production_math_executed\":true,\"actual_country_assets_checked\":8}");
            Debug.Log("Gameplay math and geography PASS: " + assertions + " assertions");
        }
    }
}
