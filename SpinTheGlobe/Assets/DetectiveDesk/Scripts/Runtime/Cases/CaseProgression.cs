using System;
using DetectiveDesk.Progression;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Endless case requirements: repeated evidence groups and gradually longer Strength tiers.</summary>
    public static class CaseProgression
    {
        public static int RequiredEvidence(GameBalance balance, int completed, int reduction = 0)
        {
            long groups = Math.Max(0, completed) / Math.Max(1, balance.caseRepeatCount);
            long evidence = balance.caseBaseEvidence + groups * Math.Max(0, balance.caseEvidencePerGroup);
            return (int)Math.Max(1, Math.Min(int.MaxValue, evidence - Math.Max(0, reduction)));
        }

        public static int RequiredStrength(GameBalance balance, int completed)
        {
            long remaining = Math.Max(0, completed);
            int strength = 0;
            while (strength < balance.caseMaxDurabilityRequirement)
            {
                long length = (long)Math.Max(1, balance.caseBaseTierLength) * (strength + 1);
                if (remaining < length) break;
                remaining -= length;
                strength++;
            }
            return strength;
        }

        public static double Reward(GameBalance balance, int completed, double incomeRate,
            double allIncomeMultiplier, double caseMultiplier)
        {
            double baseReward = balance.caseBaseReward * (1 + Math.Max(0, completed) * balance.caseRewardGrowthPerLevel);
            return Math.Max(baseReward * allIncomeMultiplier, balance.caseIncomeSeconds * incomeRate) * caseMultiplier;
        }

        public static string EvidenceLabel(int collected, int required, bool onCooldown) =>
            onCooldown ? "Evidence: 0/0" : "Evidence: " + Math.Max(0, Math.Min(collected, required)) + "/" + required;
    }
}
