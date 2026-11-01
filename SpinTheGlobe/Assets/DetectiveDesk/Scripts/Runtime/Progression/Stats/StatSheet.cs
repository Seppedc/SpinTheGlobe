using System;
using System.Collections.Generic;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Final value of every stat:  (base + Σadd) × max(MinimumFactor, 1 + Σpercent)
    /// Per-pin stats combine the global ("all pins", index -1) contributions with that pin's own.
    /// Rebuilt from the skill tree whenever a node is purchased; temporary buffs (whiskey) are layered on top.
    /// </summary>
    public sealed class StatSheet
    {
        /// <summary>Stacked percentage reductions never push a stat below 5% of its base.</summary>
        public const double MinimumFactor = 0.05;

        readonly Dictionary<StatId, double> baseValues = new Dictionary<StatId, double>();
        readonly Dictionary<(StatId, int), double> additive = new Dictionary<(StatId, int), double>();
        readonly Dictionary<(StatId, int), double> percent = new Dictionary<(StatId, int), double>();
        readonly Dictionary<StatId, double> temporaryMultipliers = new Dictionary<StatId, double>();

        public event Action Changed;

        public void SetBase(StatId stat, double value) { baseValues[stat] = value; Changed?.Invoke(); }

        public double GetBase(StatId stat) => baseValues.TryGetValue(stat, out var v) ? v : 0;

        /// <summary>Final value including skill modifiers and active temporary multipliers.</summary>
        public double Get(StatId stat, int pinIndex = -1) => Permanent(stat, pinIndex) * Temporary(stat);

        /// <summary>Value from base + skills only (ignores temporary buffs). Use for upgrade previews and caps.</summary>
        public double Permanent(StatId stat, int pinIndex = -1) => Evaluate(stat, pinIndex, null, 0);

        /// <summary>What the stat would be if <paramref name="extraRanks"/> more ranks of these modifiers were owned.</summary>
        public double Preview(StatId stat, IReadOnlyList<StatModifier> modifiers, int extraRanks, int pinIndex = -1) =>
            Evaluate(stat, pinIndex, modifiers, extraRanks);

        public void SetTemporaryMultiplier(StatId stat, double multiplier)
        {
            if (Math.Abs(multiplier - 1) < 1e-12) temporaryMultipliers.Remove(stat);
            else temporaryMultipliers[stat] = Math.Max(0, multiplier);
            Changed?.Invoke();
        }

        public void ClearTemporary() { temporaryMultipliers.Clear(); Changed?.Invoke(); }

        /// <summary>Recompute all skill contributions from the owned ranks.</summary>
        public void Rebuild(SkillTreeAsset tree, SkillTreeState state)
        {
            additive.Clear();
            percent.Clear();
            if (tree != null && state != null)
            {
                foreach (var node in tree.Nodes)
                {
                    int rank = state.Rank(node.id);
                    if (rank <= 0 || node.modifiers == null) continue;
                    foreach (var m in node.modifiers) Accumulate(m, rank);
                }
            }
            Changed?.Invoke();
        }

        void Accumulate(StatModifier m, int rank)
        {
            if (m.stat == StatId.None) return;
            var key = (m.stat, m.pinIndex < 0 ? -1 : m.pinIndex);
            var target = m.kind == ModifierKind.Percent ? percent : additive;
            target.TryGetValue(key, out var current);
            target[key] = current + m.valuePerRank * rank;
        }

        double Evaluate(StatId stat, int pinIndex, IReadOnlyList<StatModifier> extra, int extraRanks)
        {
            double add = Lookup(additive, stat, -1);
            double pct = Lookup(percent, stat, -1);
            if (pinIndex >= 0) { add += Lookup(additive, stat, pinIndex); pct += Lookup(percent, stat, pinIndex); }

            if (extra != null && extraRanks > 0)
            {
                foreach (var m in extra)
                {
                    if (m.stat != stat) continue;
                    bool applies = m.pinIndex < 0 || m.pinIndex == pinIndex;
                    if (!applies) continue;
                    if (m.kind == ModifierKind.Percent) pct += m.valuePerRank * extraRanks;
                    else add += m.valuePerRank * extraRanks;
                }
            }
            return (GetBase(stat) + add) * Math.Max(MinimumFactor, 1 + pct);
        }

        double Temporary(StatId stat) => temporaryMultipliers.TryGetValue(stat, out var m) ? m : 1;

        static double Lookup(Dictionary<(StatId, int), double> map, StatId stat, int index) =>
            map.TryGetValue((stat, index), out var v) ? v : 0;
    }
}
