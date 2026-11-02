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

        [Header("Start")]
        [Min(0)] public double startingMoney = 0;
        [Min(0)] public double startingGold = 0;
        [Tooltip("Seconds between automatic saves.")]
        [Min(5)] public float autosaveInterval = 30;

        [Header("Base stats (before upgrades)")]
        public List<StatBase> baseStats = new List<StatBase>(Defaults());

        [Header("Globe")]
        [Range(0.5f, 1)] public float frictionAt60Hz = 0.985f;
        [Tooltip("°/s gained per pixel dragged (× Drag Power).")]
        [Min(0)] public float dragAcceleration = 0.35f;
        [Tooltip("°/s gained per click on the globe (× Click Power). Smaller than a good drag.")]
        [Min(0)] public float clickImpulse = 10f;
        [Tooltip("Money for each full revolution past the start line (× Globe Multiplier).")]
        [Min(0)] public double revolutionReward = 1.5;

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
        [Min(0)] public int caseEvidencePerLevel = 1;
        [Min(1)] public int caseMaxEvidence = 12;
        [Tooltip("Every N cases the required pin durability level rises by one.")]
        [Min(1)] public int caseLevelsPerDurabilityStep = 3;
        [Min(0)] public int caseMaxDurabilityRequirement = 5;
        [Min(0)] public double caseBaseReward = 40;
        [Min(1)] public double caseRewardGrowth = 1.6;

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

        public void ApplyTo(StatSheet sheet)
        {
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

            new StatBase(StatId.PinSlots, 0),              // start with no pins
            new StatBase(StatId.ShooterCooldown, 4),       // seconds
            new StatBase(StatId.PinLandChance, 0.3),       // ≈ Earth's land fraction
            new StatBase(StatId.PinCityChance, 0.05),
            new StatBase(StatId.PinDurability, 1),         // passes before falling off
            new StatBase(StatId.PinPayout, 1),

            new StatBase(StatId.CasePayout, 1),
            new StatBase(StatId.CaseCooldown, 30),
            new StatBase(StatId.CaseAutoStampChance, 0),

            new StatBase(StatId.WhiskeyBoostDuration, 10),
            new StatBase(StatId.WhiskeyCooldown, 60),
            new StatBase(StatId.WhiskeyBoostStrength, 1.5),

            new StatBase(StatId.StatueGoldFillRate, 1),
            new StatBase(StatId.StatuePayout, 1),
        };
    }
}
