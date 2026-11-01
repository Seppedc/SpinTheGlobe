using System;
using UnityEngine;

namespace DetectiveDesk.Economy
{
    /// <summary>
    /// Price of the next rank: ceil(baseCost * growth^ownedRank).
    /// Optional explicit per-rank prices override the curve for hand-tuned early ranks.
    /// </summary>
    [Serializable]
    public struct CostCurve
    {
        [Min(0)] public double baseCost;
        [Min(1)] public double growth;
        [Tooltip("Optional hand-tuned prices for the first ranks. Ranks beyond this list use the curve.")]
        public double[] explicitCosts;

        public CostCurve(double baseCost, double growth)
        {
            this.baseCost = baseCost;
            this.growth = growth;
            explicitCosts = null;
        }

        /// <param name="ownedRank">How many ranks are already owned (0 = price of the first rank).</param>
        public double Cost(int ownedRank)
        {
            ownedRank = Math.Max(0, ownedRank);
            if (explicitCosts != null && ownedRank < explicitCosts.Length) return Math.Max(0, explicitCosts[ownedRank]);
            double g = growth < 1 ? 1 : growth;
            double cost = Math.Ceiling(Math.Max(0, baseCost) * Math.Pow(g, ownedRank));
            return double.IsInfinity(cost) ? double.MaxValue : cost;
        }
    }
}
