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
                case StatId.GlobeMaxSpeed: return "Spin Limit";
                case StatId.GlobeAutoSpeed: return "Auto Spin";
                case StatId.GlobeAutoShare: return "Auto-rotate (share of max speed)";
                case StatId.GlobeDragPower: return "Drag power";
                case StatId.GlobeSpeedDecay: return "Speed loss";
                case StatId.GlobeClickPower: return "Click power";
                case StatId.GlobeMultiplier: return "Spin pay";
                case StatId.PinSlots: return "Pins";
                case StatId.ShooterCooldown: return "Reload";
                case StatId.PinLandChance: return "Land chance";
                case StatId.PinCityChance: return "City chance";
                case StatId.PinDurability: return "Strength";
                case StatId.PinPayout: return "Pin payout";
                case StatId.CasePayout: return "Case reward";
                case StatId.CaseUnlocked: return "Case files";
                case StatId.CaseCooldown: return "Case wait";
                case StatId.CaseAutoStampChance: return "Auto-stamp chance";
                case StatId.WhiskeyBoostDuration: return "Boost duration";
                case StatId.WhiskeyCooldown: return "Drink wait";
                case StatId.WhiskeyBoostStrength: return "Boost strength";
                case StatId.StatueGoldFillRate: return "Gold fill speed";
                case StatId.StatuePayout: return "Statue gold";
                case StatId.IncomeMultiplier: return "All income";
                case StatId.PinAutoFire: return "Auto-fire";
                case StatId.PinDoubleShotChance: return "Double-shot chance";
                case StatId.CaseEvidenceReduction: return "Evidence saved";
                case StatId.CaseGold: return "Gold when it drops";
                case StatId.CaseGoldChance: return "Gold drop chance";
                case StatId.WhiskeyAutoRefill: return "Auto-refill";
                case StatId.StatuePieceReduction: return "Pieces saved";
                case StatId.LootboxPrice: return "Lootbox price";
                case StatId.LootLuck: return "Loot luck";
                case StatId.LootDoubleChance: return "Double-box chance";
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
                case StatId.GlobeAutoShare:
                    return System.Math.Round(System.Math.Min(1, v) * 100) + "%";
                case StatId.GlobeSpeedDecay:
                    return (v * 100).ToString("0.#") + "%";
                case StatId.PinSlots:
                case StatId.PinDurability:
                case StatId.CaseEvidenceReduction:
                case StatId.CaseGold:
                case StatId.StatuePieceReduction:
                    return System.Math.Round(v).ToString("0");
                case StatId.PinAutoFire:
                case StatId.CaseUnlocked:
                case StatId.WhiskeyAutoRefill:
                    return v >= 1 ? "On" : "Off";
                case StatId.PinDoubleShotChance:
                case StatId.LootDoubleChance:
                case StatId.LootLuck:
                    return "+" + (v * 100).ToString("0.#") + "%";
                case StatId.ShooterCooldown:
                    return v.ToString("0.###") + "s";
                case StatId.CaseCooldown:
                case StatId.WhiskeyCooldown:
                case StatId.WhiskeyBoostDuration:
                    return v.ToString("0.#") + "s";
                case StatId.PinLandChance:
                case StatId.PinCityChance:
                case StatId.CaseAutoStampChance:
                case StatId.CaseGoldChance:
                    return (v * 100).ToString("0.#") + "%";
                default:
                    return "×" + v.ToString("0.##");
            }
        }
    }
}
