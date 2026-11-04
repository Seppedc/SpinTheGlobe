using System;
using System.Collections.Generic;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Every tuning number of the game: base stat values (before skill-tree upgrades) plus the
    /// rules of each system. All balancing happens in this asset and in the SkillTreeAsset, never in code.
    /// Create the asset via Tools ▸ Spin The Globe ▸ Create Default Data; without it the defaults below are used.
    /// </summary>
    [CreateAssetMenu(menuName = "Spin The Globe/Game Balance", fileName = "GameBalance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Serializable]
        public struct StatBase
        {
            public StatId stat;
            public double value;
            public StatBase(StatId stat, double value) { this.stat = stat; this.value = value; }
        }

        [Serializable]
        public struct LootEntry
        {
            public LootKind kind;
            [Min(0)] public float weight;
            public LootEntry(LootKind kind, float weight) { this.kind = kind; this.weight = weight; }
        }

        /// <summary>A detective rank, earned by retiring the agency. Add more in the inspector; they are sorted by retirements.</summary>
        [Serializable]
        public struct DetectiveRank
        {
            public string title;
            [Tooltip("Retirements needed to reach this rank.")]
            [Min(0)] public int retirements;
            [Tooltip("Permanent bonus on every source of income while at this rank (0.25 = +25%). Not cumulative: the rank's own value.")]
            [Min(0)] public float incomeBonus;
            public DetectiveRank(string title, int retirements, float incomeBonus) { this.title = title; this.retirements = retirements; this.incomeBonus = incomeBonus; }
        }

        [Header("Start")]
        [Min(0)] public double startingMoney = 0;
        [Min(0)] public double startingGold = 0;
        [Tooltip("Seconds between automatic saves.")]
        [Min(5)] public float autosaveInterval = 30;

        [Header("Prestige (Retire the agency)")]
        [Tooltip("Money that must be earned in a run before retiring is possible.")]
        [Min(1)] public double prestigeThreshold = 1_000_000;
        [Tooltip("Gold for retiring = floor(scale × (earned this run ÷ threshold)^exponent).")]
        [Min(0)] public double prestigeGoldScale = 25;
        [Range(0.1f, 1f)] public double prestigeExponent = 0.5;

        [Header("Detective ranks (earned by retiring; add more any time)")]
        public List<DetectiveRank> ranks = new List<DetectiveRank>
        {
            new DetectiveRank("Rookie", 0, 0f),
            new DetectiveRank("Detective", 1, 0.10f),
            new DetectiveRank("Senior Detective", 3, 0.25f),
            new DetectiveRank("Inspector", 5, 0.50f),
            new DetectiveRank("Chief Inspector", 8, 1.00f),
            new DetectiveRank("Commissioner", 12, 2.00f),
            new DetectiveRank("Legend of the Bureau", 20, 4.00f),
        };

        [Header("Base stats (before upgrades)")]
        public List<StatBase> baseStats = new List<StatBase>(Defaults());

        [Header("Globe")]
        [Range(0.5f, 1)] public float frictionAt60Hz = 0.985f;
        [Tooltip("°/s gained per pixel dragged (× Drag Power).")]
        [Min(0)] public float dragAcceleration = 0.35f;
        [Tooltip("°/s gained per click on the globe (× Click Power). Smaller than a good drag.")]
        [Min(0)] public float clickImpulse = 10f;
        [Tooltip("A drag always gives at least this many clicks' worth of speed (with drag power), so dragging beats clicking.")]
        [Min(1)] public float dragOverClick = 1.5f;
        [Tooltip("Money for each full revolution past the start line (× Globe Multiplier).")]
        [Min(0)] public double revolutionReward = 1.6;

        [Header("Pins")]
        [Tooltip("Money each time a pin passes the payout line, before landing bonuses and upgrades.")]
        [Min(0)] public double pinBasePayout = 3;
        [Min(1)] public double landMultiplier = 2;
        [Tooltip("Multiplier at the exact city centre; fades to ×1 at the city radius.")]
        [Min(1)] public double cityMultiplier = 3;
        [Min(0.5f)] public float cityRadiusDegrees = 4f;
        [Min(0.05f)] public float pinFlightSeconds = 0.32f;

        [Header("Case file")]
        [Min(1)] public int caseBaseEvidence = 3;
        [Min(0)] public int caseEvidencePerLevel = 2;
        [Min(1)] public int caseMaxEvidence = 40;
        [Tooltip("Every N cases the required pin durability level rises by one.")]
        [Min(1)] public int caseLevelsPerDurabilityStep = 4;
        [Min(0)] public int caseMaxDurabilityRequirement = 5;
        [Tooltip("Minimum case reward: this × case number.")]
        [Min(0)] public double caseBaseReward = 40;
        [Tooltip("A case pays at least this many seconds of your recent income (globe + pins). Keeps cases relevant all game without exploding.")]
        [Min(0)] public float caseIncomeSeconds = 12;
        [Tooltip("Gold per approved case is CaseGold × (1 + case number / this).")]
        [Min(1)] public float caseGoldLevelDivisor = 10;
        [Tooltip("Highest chance an approved case drops gold (Press Coverage and its upgrades never go above this).")]
        [Range(0, 1)] public float caseGoldChanceCap = 0.10f;

        [Header("Whiskey glass")]
        [Range(0.01f, 1)] public float whiskeyFillPerClick = 0.1f;
        [Tooltip("Fill speed multiplier while an ice cube is in the glass.")]
        [Min(1)] public float iceCubeFillMultiplier = 2f;

        [Header("Statues")]
        [Min(1)] public int statueBasePieces = 5;
        [Tooltip("Extra pieces needed for each further pedestal.")]
        [Min(0)] public int statuePiecesPerIndex = 2;
        [Tooltip("Gold fill (0..1) added per globe revolution (× Gold Fill Rate).")]
        [Range(0.001f, 1)] public float statueGoldFillPerRevolution = 0.04f;
        [Min(0)] public double statueBaseGold = 10;
        [Tooltip("Statue tiers, bought per statue in the Gold shop: names, gold multipliers and extra pieces per tier.")]
        public string[] statueTierNames = { "Bronze", "Silver", "Gold" };
        public float[] statueTierGoldMultipliers = { 1f, 2.5f, 6f };
        [Min(0)] public int statueTierExtraPieces = 1;

        [Header("Lootbox")]
        public CostCurve lootboxCost = new CostCurve(150, 1.12);
        public List<LootEntry> lootTable = new List<LootEntry>
        {
            new LootEntry(LootKind.StatuePiece, 30),
            new LootEntry(LootKind.Money, 25),
            new LootEntry(LootKind.IceCube, 20),
            new LootEntry(LootKind.Gold, 15),
            new LootEntry(LootKind.PaperBall, 10),
        };
        [Min(0)] public double lootGoldMin = 2;
        [Min(0)] public double lootGoldMax = 8;
        [Tooltip("Chance a money drop is a jackpot (2–4× the price) instead of a partial refund (30–80%).")]
        [Range(0, 1)] public float moneyJackpotChance = 0.25f;

        [Header("Paper ball (placeholder reward)")]
        [Min(0)] public double paperBallReward = 25;

        /// <summary>Code defaults first, then the asset's list on top, so stats added later never start at 0.</summary>
        public void ApplyTo(StatSheet sheet)
        {
            foreach (var s in Defaults()) sheet.SetBase(s.stat, s.value);
            foreach (var s in baseStats) if (s.stat != StatId.None) sheet.SetBase(s.stat, s.value);
        }

        /// <summary>Provisional starting values; tune in the asset.</summary>
        public static StatBase[] Defaults() => new[]
        {
            new StatBase(StatId.GlobeMaxSpeed, 60),        // °/s (10 RPM)
            new StatBase(StatId.GlobeAutoSpeed, 0),
            new StatBase(StatId.GlobeDragPower, 1),
            new StatBase(StatId.GlobeClickPower, 1),
            new StatBase(StatId.GlobeMultiplier, 1),
            new StatBase(StatId.IncomeMultiplier, 1),

            new StatBase(StatId.PinSlots, 0),              // start with no pins
            new StatBase(StatId.ShooterCooldown, 4),       // seconds
            new StatBase(StatId.PinLandChance, 0.3),       // ≈ Earth's land fraction
            new StatBase(StatId.PinCityChance, 0.05),
            new StatBase(StatId.PinDurability, 1),         // passes before falling off
            new StatBase(StatId.PinPayout, 1),
            new StatBase(StatId.PinAutoFire, 0),
            new StatBase(StatId.PinDoubleShotChance, 0),

            new StatBase(StatId.CasePayout, 1),
            new StatBase(StatId.CaseCooldown, 30),
            new StatBase(StatId.CaseAutoStampChance, 0),
            new StatBase(StatId.CaseEvidenceReduction, 0),
            new StatBase(StatId.CaseGold, 0),
            new StatBase(StatId.CaseGoldChance, 0),

            new StatBase(StatId.WhiskeyBoostDuration, 10),
            new StatBase(StatId.WhiskeyCooldown, 60),
            new StatBase(StatId.WhiskeyBoostStrength, 1.5),
            new StatBase(StatId.WhiskeyAutoRefill, 0),

            new StatBase(StatId.StatueGoldFillRate, 1),
            new StatBase(StatId.StatuePayout, 1),
            new StatBase(StatId.StatuePieceReduction, 0),

            new StatBase(StatId.LootboxPrice, 1),
            new StatBase(StatId.LootLuck, 0),
            new StatBase(StatId.LootDoubleChance, 0),
        };
    }
}
