using System;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Save;

namespace DetectiveDesk.Progression
{
    public enum NodeStatus
    {
        /// <summary>Not revealed yet: no prerequisite owned.</summary>
        Hidden,
        /// <summary>Revealed, but not every prerequisite is owned.</summary>
        Locked,
        /// <summary>Would exceed a cap (e.g. auto speed above max speed).</summary>
        Capped,
        Unaffordable,
        Available,
        Maxed,
    }

    /// <summary>The player's owned ranks plus all purchase rules. Plain C#: unit-testable and saveable.</summary>
    public sealed class SkillTreeState
    {
        readonly Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<SkillNodeData, int> Purchased;

        public int Rank(string id) => id != null && ranks.TryGetValue(id, out var r) ? r : 0;
        public bool Owns(string id) => Rank(id) > 0;

        public double NextCost(SkillNodeData node) => node == null ? double.PositiveInfinity : node.cost.Cost(Rank(node.id));

        public NodeStatus Status(SkillNodeData node, Wallet wallet, StatSheet stats, out string reason)
        {
            reason = "";
            if (node == null) { reason = "Unknown skill."; return NodeStatus.Hidden; }
            int rank = Rank(node.id);
            if (rank >= node.maxRank) { reason = "Maxed."; return NodeStatus.Maxed; }

            if (!node.IsRoot)
            {
                bool anyOwned = false, allOwned = true;
                string missing = null;
                foreach (var p in node.prerequisites)
                {
                    if (Owns(p)) anyOwned = true;
                    else { allOwned = false; missing ??= p; }
                }
                if (!anyOwned && rank == 0) { reason = "Not discovered yet."; return NodeStatus.Hidden; }
                if (!allOwned) { reason = "Requires " + missing + "."; return NodeStatus.Locked; }
            }

            if (node.capStat != StatId.None && node.capByStat != StatId.None && stats != null)
            {
                double next = stats.Preview(node.capStat, node.modifiers, 1);
                double cap = stats.Permanent(node.capByStat);
                if (next > cap + 1e-9) { reason = "Upgrade " + node.capByStat + " first."; return NodeStatus.Capped; }
            }

            double cost = NextCost(node);
            if (wallet == null || !wallet.CanAfford(node.currency, cost))
            {
                reason = "Need " + (node.currency == Currency.Gold ? NumberFormatter.Format(cost) + " gold" : NumberFormatter.Money(cost)) + ".";
                return NodeStatus.Unaffordable;
            }
            reason = "Ready.";
            return NodeStatus.Available;
        }

        public bool TryPurchase(SkillTreeAsset tree, string id, Wallet wallet, StatSheet stats, out string reason)
        {
            var node = tree != null ? tree.Find(id) : null;
            if (Status(node, wallet, stats, out reason) != NodeStatus.Available) return false;
            if (!wallet.TrySpend(node.currency, NextCost(node))) { reason = "Not enough funds."; return false; }

            int newRank = Rank(node.id) + 1;
            ranks[node.id] = newRank;
            stats?.Rebuild(tree, this);
            Purchased?.Invoke(node, newRank);
            EventBus.Publish(new SkillPurchased(node.id, newRank));
            EventBus.Publish(new StatsChanged());
            return true;
        }

        public List<SkillRank> ToSave()
        {
            var list = new List<SkillRank>(ranks.Count);
            foreach (var kv in ranks) if (kv.Value > 0) list.Add(new SkillRank { id = kv.Key, rank = kv.Value });
            return list;
        }

        /// <summary>Restore owned ranks. Unknown ids (removed nodes) are dropped; ranks are clamped to maxRank.</summary>
        public void Load(SkillTreeAsset tree, IEnumerable<SkillRank> saved)
        {
            ranks.Clear();
            if (saved != null)
                foreach (var s in saved)
                {
                    var node = tree != null ? tree.Find(s.id) : null;
                    if (node == null || s.rank <= 0) continue;
                    ranks[s.id] = Math.Min(s.rank, node.maxRank);
                }
        }

        public void Reset() => ranks.Clear();
    }
}
