using System;

namespace DetectiveDesk.Progression
{
    public enum ModifierKind
    {
        /// <summary>Adds valuePerRank × rank to the base value.</summary>
        Add = 0,
        /// <summary>Adds valuePerRank × rank to the percentage multiplier (0.25 = +25%, -0.1 = -10%).</summary>
        Percent = 1,
    }

    /// <summary>One effect of a skill node, scaled by the node's owned rank.</summary>
    [Serializable]
    public struct StatModifier
    {
        public StatId stat;
        public ModifierKind kind;
        public double valuePerRank;
        /// <summary>Only for per-pin stats: which pin (0-based). -1 applies to every pin / non-pin stats.</summary>
        public int pinIndex;

        public StatModifier(StatId stat, ModifierKind kind, double valuePerRank, int pinIndex = -1)
        {
            this.stat = stat;
            this.kind = kind;
            this.valuePerRank = valuePerRank;
            this.pinIndex = pinIndex;
        }

        public static StatModifier Add(StatId stat, double perRank, int pinIndex = -1) => new StatModifier(stat, ModifierKind.Add, perRank, pinIndex);
        public static StatModifier Percent(StatId stat, double perRank, int pinIndex = -1) => new StatModifier(stat, ModifierKind.Percent, perRank, pinIndex);
    }
}
