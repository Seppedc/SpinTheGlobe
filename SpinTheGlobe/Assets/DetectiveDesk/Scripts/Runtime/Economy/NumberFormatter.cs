using System;
using System.Globalization;

namespace DetectiveDesk.Economy
{
    /// <summary>
    /// Incremental-game number formatting: 999 → "999", 1234 → "1.23K", 4.5e15 → "4.50Qa",
    /// then two-letter suffixes (aa, ab, …) once the named suffixes run out.
    /// Output keeps 3 significant digits so HUD text does not jitter in width.
    /// </summary>
    public static class NumberFormatter
    {
        static readonly string[] Named = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return value > 0 ? "∞" : "-∞";
            string sign = value < 0 ? "-" : "";
            value = Math.Abs(value);

            if (value < 1000)
            {
                // Small values: allow up to two decimals early in the game (e.g. 12.5), whole numbers after 100.
                string small = value < 100 ? value.ToString("0.##", Invariant) : Math.Floor(value).ToString("0", Invariant);
                return sign + small;
            }

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            double scaled = value / Math.Pow(10, tier * 3);
            string text = Scaled(scaled);
            // Rounding can push 999.995 up to "1000": move to the next suffix instead.
            if (text == "1000") { tier++; scaled /= 1000; text = Scaled(scaled); }
            return sign + text + Suffix(tier);
        }

        public static string Money(double value) => "€" + Format(value);

        static string Scaled(double scaled)
        {
            if (scaled < 10) return Truncate(scaled, 2).ToString("0.00", Invariant);
            if (scaled < 100) return Truncate(scaled, 1).ToString("0.0", Invariant);
            return Math.Floor(scaled).ToString("0", Invariant);
        }

        // Truncate rather than round so a value never displays as more than the player owns.
        static double Truncate(double v, int decimals)
        {
            double f = Math.Pow(10, decimals);
            return Math.Floor(v * f + 1e-9) / f;
        }

        public static string Suffix(int tier)
        {
            if (tier < Named.Length) return Named[tier];
            int n = tier - Named.Length;
            char first = (char)('a' + (n / 26) % 26);
            char second = (char)('a' + n % 26);
            return new string(new[] { first, second });
        }
    }
}
