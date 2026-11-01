using System;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public enum UpgradeKind { Speed, AutoMotor, Durability }

    [CreateAssetMenu(menuName = "Detective Desk/Gameplay/Upgrade Configuration")]
    public sealed class UpgradeConfig : ScriptableObject
    {
        [Min(.01f)] public float baseMaximumSpeed = 30;
        [Min(1)] public float maximumSpeedPerLevel = 15;
        [Range(.001f, 1)] public float frictionAt60Hz = .985f;
        [Min(0)] public float autoSpeedPerLevel = 6;
        [Min(.01f)] public float swipeAcceleration = .35f;
        [Min(0)] public double baseRevolutionReward = 1;
        public double speedBaseCost = 75, motorBaseCost = 100, durabilityBaseCost = 60, restorationBaseCost = 120;
        [Min(1)] public float costGrowth = 1.8f;
        [Range(1, 20)] public int maximumUpgradeLevel = 10;
        // Skin colors remain cosmetic until explicitly assigned game balance values.
        public float[] skinMultipliers = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        public double Cost(UpgradeKind kind, int level)
        {
            double basis = kind == UpgradeKind.Speed ? speedBaseCost : kind == UpgradeKind.AutoMotor ? motorBaseCost : durabilityBaseCost;
            return Math.Ceiling(basis * Math.Pow(costGrowth, Math.Max(0, level)));
        }
        public double RestorationCost(int tier) => Math.Ceiling(restorationBaseCost * Math.Pow(costGrowth, Mathf.Clamp(tier, 0, 3)));
        public float SkinMultiplier(int skin) => skinMultipliers != null && skin >= 0 && skin < skinMultipliers.Length
            && GameplayMath.Finite(skinMultipliers[skin]) ? Mathf.Max(0, skinMultipliers[skin]) : 1;
    }
}
