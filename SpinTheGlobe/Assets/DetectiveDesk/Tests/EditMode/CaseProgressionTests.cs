using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class CaseProgressionTests
    {
        GameBalance balance;
        [SetUp] public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(balance);

        [Test]
        public void EvidenceRequirements_RepeatExactlyFiveTimes_ThenIncreaseByOne()
        {
            for (int group = 0; group < 100; group++)
                for (int repeat = 0; repeat < 5; repeat++)
                    Assert.AreEqual(3 + group, CaseProgression.RequiredEvidence(balance, group * 5 + repeat));
        }

        [TestCase(1, 0)]
        [TestCase(15, 0)]
        [TestCase(16, 1)]
        [TestCase(45, 1)]
        [TestCase(46, 2)]
        [TestCase(90, 2)]
        [TestCase(91, 3)]
        [TestCase(150, 3)]
        [TestCase(151, 4)]
        [TestCase(225, 4)]
        [TestCase(226, 5)]
        public void StrengthTiers_LastFifteen_ThenThirty_ThenFortyFiveCases(int caseNumber, int strength)
        {
            Assert.AreEqual(strength, CaseProgression.RequiredStrength(balance, caseNumber - 1));
        }

        [Test]
        public void LateCases_KeepGrowing_WithoutRequiringAnUnavailableStrength()
        {
            Assert.AreEqual(103, CaseProgression.RequiredEvidence(balance, 500));
            Assert.Greater(CaseProgression.RequiredEvidence(balance, 10000), 103);
            Assert.AreEqual(5, CaseProgression.RequiredStrength(balance, 10000));
            Assert.AreEqual(5, CaseProgression.RequiredStrength(balance, int.MaxValue));
            Assert.Greater(CaseProgression.RequiredEvidence(balance, int.MaxValue), 0);
        }

        [Test]
        public void EvidenceReduction_StillWorks_AndNeverMakesAnEmptyCase()
        {
            Assert.AreEqual(1, CaseProgression.RequiredEvidence(balance, 0, 3));
            Assert.AreEqual(100, CaseProgression.RequiredEvidence(balance, 500, 3));
        }

        [TestCase(0)]
        [TestCase(50)]
        public void FirstReward_IsOneEighthOfTheOriginalReward_WithUpgradesApplied(double income)
        {
            double oldReward = System.Math.Max(40 * 2, 12 * income) * 1.5;
            Assert.AreEqual(oldReward / 8, CaseProgression.Reward(balance, 0, income, 2, 1.5), 1e-9);
        }

        [Test]
        public void RepeatedCases_GiveOnlySmallLinearRewardIncreases()
        {
            for (int completed = 0; completed < 100; completed++)
                Assert.AreEqual(5 + completed * .1, CaseProgression.Reward(balance, completed, 0, 1, 1), 1e-9);
        }

        [TestCase(0, 3, false, "Evidence: 0/3")]
        [TestCase(2, 3, false, "Evidence: 2/3")]
        [TestCase(3, 3, false, "Evidence: 3/3")]
        [TestCase(10, 3, false, "Evidence: 3/3")]
        [TestCase(0, 4, true, "Evidence: 0/0")]
        public void PlateText_ShowsOnlyEvidence(int collected, int required, bool cooldown, string expected)
        {
            Assert.AreEqual(expected, CaseProgression.EvidenceLabel(collected, required, cooldown));
        }
    }
}
