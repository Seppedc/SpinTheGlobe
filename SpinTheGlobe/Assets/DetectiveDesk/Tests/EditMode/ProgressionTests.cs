using System.Linq;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class ProgressionTests
    {
        SkillTreeAsset tree;
        GameBalance balance;
        StatSheet stats;
        SkillTreeState state;
        Wallet wallet;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            tree.SetNodes(DefaultSkillTreeFactory.Create());
            balance = ScriptableObject.CreateInstance<GameBalance>();
            stats = new StatSheet();
            balance.ApplyTo(stats);
            state = new SkillTreeState();
            wallet = new Wallet();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(balance);
        }

        bool Buy(string id) => state.TryPurchase(tree, id, wallet, stats, out _);

        [Test]
        public void DefaultTree_IsValid_WithTwelvePinsEachHavingDurabilityAndPayout()
        {
            CollectionAssert.IsEmpty(tree.Validate());
            for (int pin = 1; pin <= DefaultSkillTreeFactory.MaxPins; pin++)
            {
                Assert.IsNotNull(tree.Find(DefaultSkillTreeFactory.PinId(pin)), "pin " + pin);
                Assert.IsNotNull(tree.Find(DefaultSkillTreeFactory.PinDurabilityId(pin)));
                Assert.IsNotNull(tree.Find(DefaultSkillTreeFactory.PinPayoutId(pin)));
            }
            Assert.AreEqual(1, tree.Nodes.Count(n => n.IsRoot));
        }

        [Test]
        public void Root_MustBeBoughtBeforeBranchesAppear()
        {
            var maxSpeed = tree.Find("globe_max_speed");
            Assert.AreEqual(NodeStatus.Hidden, state.Status(maxSpeed, wallet, stats, out _));
            wallet.Add(Currency.Money, 1000);
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            Assert.AreEqual(NodeStatus.Available, state.Status(maxSpeed, wallet, stats, out _));
        }

        [Test]
        public void PinChain_RevealsNextPinOnlyAfterBuyingPrevious()
        {
            wallet.Add(Currency.Money, 1e9);
            Buy(DefaultSkillTreeFactory.RootId);
            var pin2 = tree.Find(DefaultSkillTreeFactory.PinId(2));
            Assert.AreEqual(NodeStatus.Hidden, state.Status(pin2, wallet, stats, out _));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.PinId(1)));
            Assert.AreEqual(NodeStatus.Available, state.Status(pin2, wallet, stats, out _));
            Assert.AreEqual(1, stats.Get(StatId.PinSlots), 1e-9);
        }

        [Test]
        public void PerPinUpgrades_AffectOnlyThatPin()
        {
            wallet.Add(Currency.Money, 1e9);
            Buy(DefaultSkillTreeFactory.RootId);
            Buy(DefaultSkillTreeFactory.PinId(1));
            Buy(DefaultSkillTreeFactory.PinId(2));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.PinDurabilityId(2)));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.PinPayoutId(1)));

            Assert.AreEqual(1, stats.Get(StatId.PinDurability, 0), 1e-9);
            Assert.AreEqual(2, stats.Get(StatId.PinDurability, 1), 1e-9);
            Assert.AreEqual(1.5, stats.Get(StatId.PinPayout, 0), 1e-9);
            Assert.AreEqual(1, stats.Get(StatId.PinPayout, 1), 1e-9);
        }

        [Test]
        public void AutoRotate_CannotExceedMaxSpeed()
        {
            wallet.Add(Currency.Money, 1e15);
            Buy(DefaultSkillTreeFactory.RootId);
            Assert.IsTrue(Buy("globe_max_speed"), "auto-rotate requires max speed first");
            double max = stats.Get(StatId.GlobeMaxSpeed);
            var auto = tree.Find("globe_auto");
            // Buy auto-rotate until the next rank would pass the cap.
            int bought = 0;
            while (stats.Get(StatId.GlobeAutoSpeed) + 6 <= max && bought < auto.maxRank) { Assert.IsTrue(Buy("globe_auto"), "auto rank " + (bought + 1)); bought++; }
            Assert.Greater(bought, 0);
            Assert.AreEqual(NodeStatus.Capped, state.Status(auto, wallet, stats, out _));
            Assert.IsTrue(Buy("globe_max_speed"));
            Assert.AreEqual(NodeStatus.Available, state.Status(auto, wallet, stats, out _));
            Assert.LessOrEqual(stats.Get(StatId.GlobeAutoSpeed), stats.Get(StatId.GlobeMaxSpeed));
        }

        [Test]
        public void Purchase_SpendsMoneyAndRespectsMaxRank()
        {
            wallet.Add(Currency.Money, 25);
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            Assert.AreEqual(0, wallet.Money, 1e-9);
            wallet.Add(Currency.Money, 1000);
            Assert.IsFalse(Buy(DefaultSkillTreeFactory.RootId), "root is single rank");
            Assert.AreEqual(NodeStatus.Maxed, state.Status(tree.Find(DefaultSkillTreeFactory.RootId), wallet, stats, out _));
        }

        [Test]
        public void PercentReductions_NeverDropBelowMinimumFactor()
        {
            stats.SetBase(StatId.ShooterCooldown, 4);
            var mods = new[] { StatModifier.Percent(StatId.ShooterCooldown, -0.5) };
            Assert.AreEqual(4 * StatSheet.MinimumFactor, stats.Preview(StatId.ShooterCooldown, mods, 10), 1e-9);
        }

        [Test]
        public void TemporaryMultiplier_LayersOnTopOfPermanentValue()
        {
            stats.SetTemporaryMultiplier(StatId.GlobeMultiplier, 1.5);
            Assert.AreEqual(1.5, stats.Get(StatId.GlobeMultiplier), 1e-9);
            Assert.AreEqual(1, stats.Permanent(StatId.GlobeMultiplier), 1e-9);
            stats.ClearTemporary();
            Assert.AreEqual(1, stats.Get(StatId.GlobeMultiplier), 1e-9);
        }

        [Test]
        public void State_SaveAndLoad_RoundTripsAndDropsUnknownIds()
        {
            wallet.Add(Currency.Money, 1e9);
            Buy(DefaultSkillTreeFactory.RootId);
            Buy("globe_max_speed");
            Buy("globe_max_speed");
            var saved = state.ToSave();
            saved.Add(new Save.SkillRank { id = "removed_node", rank = 3 });

            var restored = new SkillTreeState();
            restored.Load(tree, saved);
            Assert.AreEqual(2, restored.Rank("globe_max_speed"));
            Assert.AreEqual(1, restored.Rank(DefaultSkillTreeFactory.RootId));
            Assert.AreEqual(0, restored.Rank("removed_node"));
        }
    }
}
