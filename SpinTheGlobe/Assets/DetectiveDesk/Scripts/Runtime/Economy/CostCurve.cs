using System;
using UnityEngine;

namespace DetectiveDesk.Economy
{
    /// <summary>
    /// Price of the next rank: ceil(baseCost * growth^ownedRank).
    /// Optional explicit per-rank prices override the curve for hand-tuned early ranks.
    /// The final price is rounded to the configured step for both kinds of curve.
    /// </summary>
    [Serializable]
    public struct CostCurve
    {
        [Min(0)] public double baseCost;
        [Min(1)] public double growth;
        [Tooltip("Optional hand-tuned prices for the first ranks. Ranks beyond this list use the curve.")]
        public double[] explicitCosts;
        [Tooltip("Round the final price to the nearest multiple. 1 keeps whole-euro pricing; 5 gives prices ending in 0 or 5.")]
        [Min(1)] public double roundingStep;

        public CostCurve(double baseCost, double growth)
        {
            this.baseCost = baseCost;
            this.growth = growth;
            explicitCosts = null;
            roundingStep = 1;
        }

        /// <param name="ownedRank">How many ranks are already owned (0 = price of the first rank).</param>
        public double Cost(int ownedRank)
        {
            ownedRank = Math.Max(0, ownedRank);
            if (explicitCosts != null && ownedRank < explicitCosts.Length) return RoundPrice(Math.Max(0, explicitCosts[ownedRank]));
            double g = growth < 1 ? 1 : growth;
            double cost = Math.Ceiling(Math.Max(0, baseCost) * Math.Pow(g, ownedRank));
            return RoundPrice(double.IsInfinity(cost) ? double.MaxValue : cost);
        }

        public double RoundPrice(double price)
        {
            if (price <= 0) return 0;
            if (roundingStep <= 1) return price;
            double rounded = Math.Max(roundingStep, Math.Floor(price / roundingStep + .5) * roundingStep);
            return double.IsInfinity(rounded) ? double.MaxValue : rounded;
        }
    }
}
