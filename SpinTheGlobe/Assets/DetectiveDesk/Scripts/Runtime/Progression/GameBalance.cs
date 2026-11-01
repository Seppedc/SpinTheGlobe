using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Base values of every stat before upgrades, plus starting resources.
    /// All tuning lives here and in the SkillTreeAsset, never in code.
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

        [Min(0)] public double startingMoney = 0;
        [Min(0)] public double startingGold = 0;
        [Tooltip("Seconds between automatic saves.")]
        [Min(5)] public float autosaveInterval = 30;
        public List<StatBase> baseStats = new List<StatBase>(Defaults());

        public void ApplyTo(StatSheet sheet)
        {
            foreach (var s in baseStats) if (s.stat != StatId.None) sheet.SetBase(s.stat, s.value);
        }

        /// <summary>Provisional starting values; tune in the asset.</summary>
        public static StatBase[] Defaults() => new[]
        {
            new StatBase(StatId.GlobeMaxSpeed, 30),        // °/s (5 RPM), matches the current prototype
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
