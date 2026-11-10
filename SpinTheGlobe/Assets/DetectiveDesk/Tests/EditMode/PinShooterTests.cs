using System;
using System.Collections.Generic;
using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class PinShooterTests
    {
        [Test]
        public void ReturningFirstPin_DoesNotStarveTheOtherEleven()
        {
            var cycle = new PinShooterCycle();
            var busy = new HashSet<int>();
            for (int round = 0; round < 3; round++)
                for (int index = 0; index < 12; index++)
                    Assert.AreEqual(index, cycle.TakeNextReady(12, busy));

            busy.UnionWith(new[] { 0, 1, 3 });
            Assert.AreEqual(2, cycle.TakeNextReady(4, busy), "skip pins still in flight or on the map");
            busy.Add(2);
            Assert.AreEqual(-1, cycle.TakeNextReady(4, busy));
            Assert.AreEqual(-1, cycle.TakeNextReady(0, busy));
        }

        [Test]
        public void FastReload_IsConsistentAcrossFrameRates_AndIdleTimeDoesNotBankShots()
        {
            int baseline = ShotsInTenSeconds(144);
            Assert.That(Math.Abs(ShotsInTenSeconds(30) - baseline), Is.LessThanOrEqualTo(1));
            Assert.That(Math.Abs(ShotsInTenSeconds(60) - baseline), Is.LessThanOrEqualTo(1));
            Assert.Greater(baseline, 360, "fast reload must exceed one shot per 30 FPS frame");

            var cycle = new PinShooterCycle();
            cycle.Advance(60);
            cycle.DiscardOverdueTime();
            cycle.Reload(0.027);
            Assert.IsFalse(cycle.Ready, "waiting for a pin must not create a burst of stored shots");
        }

        static int ShotsInTenSeconds(int fps)
        {
            var balance = ScriptableObject.CreateInstance<GameBalance>();
            var tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            try
            {
                tree.SetNodes(DefaultSkillTreeFactory.Create());
                var stats = new StatSheet();
                balance.ApplyTo(stats);
                var reload = tree.Find("shooter_cooldown");
                return ShotsInTenSeconds(fps, stats.Preview(StatId.ShooterCooldown, reload.modifiers, reload.maxRank));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(balance);
                UnityEngine.Object.DestroyImmediate(tree);
            }
        }

        static int ShotsInTenSeconds(int fps, double cooldown)
        {
            var cycle = new PinShooterCycle();
            int shots = 0;
            for (int frame = 0; frame < fps * 10; frame++)
            {
                cycle.Advance(1.0 / fps);
                for (int shot = 0; shot < 12 && cycle.Ready; shot++)
                {
                    cycle.Reload(cooldown);
                    shots++;
                }
                cycle.DiscardOverdueTime();
            }
            return shots;
        }

        [Test]
        public void ReloadRanks_KeepImproving_WithIncreasingCosts_AndSupportTwelvePinsAtTopSpeed()
        {
            var tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            var balance = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                tree.SetNodes(DefaultSkillTreeFactory.Create());
                var reload = tree.Find("shooter_cooldown");
                var stats = new StatSheet();
                balance.ApplyTo(stats);
                double last = stats.Get(StatId.ShooterCooldown);
                double firstPinLatestReturn = balance.pinFlightSeconds + 360 / stats.Get(StatId.GlobeMaxSpeed);
                Assert.Greater(last, firstPinLatestReturn,
                    "at the starting spin limit, even one pin returns before the base reload finishes");
                Assert.Greater(reload.maxRank, 15);
                for (int rank = 1; rank <= reload.maxRank; rank++)
                {
                    double next = stats.Preview(StatId.ShooterCooldown, reload.modifiers, rank);
                    Assert.That(next, Is.GreaterThan(0).And.LessThan(last), "rank " + rank);
                    if (rank > 1)
                        Assert.Greater(reload.cost.Cost(rank - 1), reload.cost.Cost(rank - 2), "later reload ranks cost more");
                    last = next;
                }
                Assert.That(last, Is.InRange(0.025, 0.030),
                    "the slower opening retains approximately the same fully upgraded firing speed");

                // At maximum speed, even a one-pass pin is busy during flight plus up to a turn.
                var bearings = tree.Find("globe_max_speed");
                double rpm = stats.Preview(StatId.GlobeMaxSpeed, bearings.modifiers, bearings.maxRank) / 6;
                double averageLife = balance.pinFlightSeconds + 0.5 * 60 / rpm;
                Assert.Less(last * 12, averageLife, "a three-pin pool must bottleneck before a twelve-pin pool");
                Assert.Less(balance.pinBasePayout, 3, "individual passes pay less to offset faster firing");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tree);
                UnityEngine.Object.DestroyImmediate(balance);
            }
        }
    }
}
