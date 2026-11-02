using DetectiveDesk.Progression;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Player-facing names and number formats for stats (skill-tree tooltips).</summary>
    public static class StatInfo
    {
        public static string Name(StatId stat)
        {
            switch (stat)
            {
                case StatId.GlobeMaxSpeed: return "Max spin speed";
                case StatId.GlobeAutoSpeed: return "Auto-rotate";
                case StatId.GlobeDragPower: return "Drag power";
                case StatId.GlobeClickPower: return "Click power";
                case StatId.GlobeMultiplier: return "Globe earnings";
                case StatId.PinSlots: return "Pins";
                case StatId.ShooterCooldown: return "Shooter reload";
                case StatId.PinLandChance: return "Land chance";
                case StatId.PinCityChance: return "City chance";
                case StatId.PinDurability: return "Passes before falling";
                case StatId.PinPayout: return "Pin payout";
                case StatId.CasePayout: return "Case reward";
                case StatId.CaseCooldown: return "Next case after";
                case StatId.CaseAutoStampChance: return "Auto-stamp chance";
                case StatId.WhiskeyBoostDuration: return "Boost duration";
                case StatId.WhiskeyCooldown: return "Whiskey cooldown";
                case StatId.WhiskeyBoostStrength: return "Boost strength";
                case StatId.StatueGoldFillRate: return "Gold fill speed";
                case StatId.StatuePayout: return "Statue gold";
                default: return stat.ToString();
            }
        }

        public static string Format(StatId stat, double v)
        {
            switch (stat)
            {
                case StatId.GlobeMaxSpeed:
                case StatId.GlobeAutoSpeed:
                    return (v / 6.0).ToString("0.#") + " RPM";
                case StatId.PinSlots:
                case StatId.PinDurability:
                    return System.Math.Round(v).ToString("0");
                case StatId.ShooterCooldown:
                case StatId.CaseCooldown:
                case StatId.WhiskeyCooldown:
                case StatId.WhiskeyBoostDuration:
                    return v.ToString("0.#") + "s";
                case StatId.PinLandChance:
                case StatId.PinCityChance:
                case StatId.CaseAutoStampChance:
                    return (v * 100).ToString("0.#") + "%";
                default:
                    return "×" + v.ToString("0.##");
            }
        }
    }
}
