using System.Collections.Generic;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Detective ranks: earned by retiring the agency. The list lives in <see cref="GameBalance.ranks"/> so more ranks
    /// can be added in the inspector without code changes. Rank titles are separate from the permanent bonus granted for each retirement.
    /// </summary>
    public static class DetectiveRanks
    {
        static readonly GameBalance.DetectiveRank Fallback = new GameBalance.DetectiveRank("Rookie", 0, 0f);

        /// <summary>The ranks sorted by retirements needed (lowest first).</summary>
        public static List<GameBalance.DetectiveRank> Sorted(GameBalance balance)
        {
            var list = new List<GameBalance.DetectiveRank>();
            if (balance != null && balance.ranks != null) list.AddRange(balance.ranks);
            if (list.Count == 0) list.Add(Fallback);
            list.Sort((a, b) => a.retirements.CompareTo(b.retirements));
            return list;
        }

        /// <summary>The rank reached with this many retirements.</summary>
        public static GameBalance.DetectiveRank For(GameBalance balance, int retirements)
        {
            var list = Sorted(balance);
            var best = list[0];
            foreach (var r in list) if (r.retirements <= retirements) best = r;
            return best;
        }

        /// <summary>The next rank, or false when the highest rank is reached.</summary>
        public static bool TryNext(GameBalance balance, int retirements, out GameBalance.DetectiveRank next)
        {
            foreach (var r in Sorted(balance)) if (r.retirements > retirements) { next = r; return true; }
            next = default;
            return false;
        }

        /// <summary>Each retirement adds the same income bonus; rank milestones do not add a second bonus.</summary>
        public static IEnumerable<(StatModifier modifier, int rank)> Modifiers(GameBalance balance, int retirements)
        {
            double bonus = Prestige.IncomeBonusFor(retirements, balance);
            if (bonus > 0) yield return (StatModifier.Percent(StatId.IncomeMultiplier, bonus), 1);
        }
    }
}
