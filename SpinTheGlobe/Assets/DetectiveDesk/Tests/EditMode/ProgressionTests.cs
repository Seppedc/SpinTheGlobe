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
            var motor = tree.Find("globe_auto");
            Assert.AreEqual(NodeStatus.Hidden, state.Status(motor, wallet, stats, out _));
            wallet.Add(Currency.Money, 1000);
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            Assert.AreEqual(NodeStatus.Available, state.Status(motor, wallet, stats, out _));
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
        public void AutoRotate_ManyCheapLevels_NeverAboveMaxSpeed()
        {
            wallet.Add(Currency.Money, 1e15);
            Buy(DefaultSkillTreeFactory.RootId);
            var auto = tree.Find("globe_auto");
            CollectionAssert.Contains(auto.prerequisites, DefaultSkillTreeFactory.RootId, "Clockwork Motor is the first upgrade after the root");
            Assert.GreaterOrEqual(auto.maxRank, 30, "lots of levels");
            Assert.Less(auto.cost.Cost(1) / auto.cost.Cost(0), 1.3, "cheap steps, bought one after another");
            double step = auto.modifiers[0].valuePerRank;
            int bought = 0;
            while (stats.Get(StatId.GlobeAutoSpeed) + step <= stats.Get(StatId.GlobeMaxSpeed) && bought < auto.maxRank) { Assert.IsTrue(Buy("globe_auto")); bought++; }
            Assert.Greater(bought, 0);
            Assert.AreEqual(NodeStatus.Capped, state.Status(auto, wallet, stats, out _));
            Assert.IsTrue(Buy("globe_max_speed"));
            Assert.AreEqual(NodeStatus.Available, state.Status(auto, wallet, stats, out _), "Greased Bearings opens room for more motor levels");
            Assert.LessOrEqual(stats.Get(StatId.GlobeAutoSpeed), stats.Get(StatId.GlobeMaxSpeed));
        }

        [Test]
        public void GreasedBearings_SteadyLevels_ReachAboutTenTimesTheOldTop()
        {
            var bearings = tree.Find("globe_max_speed");
            Assert.GreaterOrEqual(bearings.maxRank, 20, "lots of levels");
            double start = stats.Get(StatId.GlobeMaxSpeed);
            double one = stats.Preview(StatId.GlobeMaxSpeed, bearings.modifiers, 1);
            double full = stats.Preview(StatId.GlobeMaxSpeed, bearings.modifiers, bearings.maxRank);
            Assert.AreEqual((full - start) / bearings.maxRank, one - start, 1e-9, "every level adds the same amount (no percentage)");
            Assert.GreaterOrEqual(full / 6.0, 400, "top speed is roughly 10× the old 50 RPM");
            Assert.LessOrEqual(full / 6.0, 800, "but not overboard");
        }

        [Test]
        public void Purchase_SpendsMoneyAndRespectsMaxRank()
        {
            wallet.Add(Currency.Money, tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0));
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
        public void MultiplyModifier_Compounds()
        {
            stats.SetBase(StatId.GlobeMultiplier, 1);
            var mods = new[] { StatModifier.Multiply(StatId.GlobeMultiplier, 1.05) };
            Assert.AreEqual(System.Math.Pow(1.05, 10), stats.Preview(StatId.GlobeMultiplier, mods, 10), 1e-9);
        }

        [Test]
        public void SkillTree_IsMoneyOnly_AndEveryBranchHasMastery()
        {
            Assert.IsTrue(tree.Nodes.All(n => n.currency == Currency.Money), "everything bought with gold lives in the Gold shop");
            Assert.IsNull(tree.Find("pin_steel_tips"));
            Assert.IsTrue(tree.Nodes.Any(n => n.branch == SkillBranch.Lootbox));
            Assert.AreEqual(6, tree.Nodes.Count(n => n.maxRank >= DefaultSkillTreeFactory.MasteryRanks));
        }

        [Test]
        public void Dragging_AlwaysBeatsClicking_AtEveryEqualUpgradeLevel()
        {
            var drag = tree.Find("globe_drag");
            var click = tree.Find("globe_click");
            Assert.AreEqual(drag.maxRank, click.maxRank);
            for (int level = 0; level <= drag.maxRank; level++)
            {
                double dragPower = stats.Preview(StatId.GlobeDragPower, drag.modifiers, level);
                double clickPower = stats.Preview(StatId.GlobeClickPower, click.modifiers, level);
                // The weakest drag gets dragOverClick clicks' worth with drag power; a click gets one with click power.
                double weakestDrag = balance.clickImpulse * balance.dragOverClick * dragPower;
                double click1 = balance.clickImpulse * clickPower;
                Assert.Greater(weakestDrag, click1, "level " + level);
                Assert.GreaterOrEqual(dragPower, clickPower, "level " + level);
            }
        }

        [Test]
        public void PressCoverage_GoldChance_NeverExceedsTenPercent()
        {
            var press = GoldShopCatalog.Find(GoldShopCatalog.PressCoverage);
            var news = tree.Find("case_headlines");
            Assert.IsNotNull(press);
            Assert.AreEqual(GoldShopCatalog.PressCoverage, news.requiresUnlock, "Front Page News only shows once Press Coverage is bought");
            double chance = stats.Preview(StatId.CaseGoldChance, press.modifiers, 1);
            double max = 0;
            foreach (var m in press.modifiers) if (m.stat == StatId.CaseGoldChance) max += m.valuePerRank;
            foreach (var m in news.modifiers) if (m.stat == StatId.CaseGoldChance) max += m.valuePerRank * news.maxRank;
            Assert.Greater(chance, 0);
            Assert.LessOrEqual(max, balance.caseGoldChanceCap + 1e-6);
            Assert.AreEqual(0.10, balance.caseGoldChanceCap, 1e-6);
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
            Buy("globe_auto");
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

        // ------------------------------------------------------------------ Gold shop and prestige
        [Test]
        public void GoldShop_SpendsGold_RespectsRequirementsAndMaxRank()
        {
            var shop = new GoldShopState();
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.Statue2, wallet, out _), "needs the first pedestal");
            var first = GoldShopCatalog.Find(GoldShopCatalog.Statue1);
            wallet.Add(Currency.Gold, first.cost.Cost(0));
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _));
            Assert.AreEqual(0, wallet.Gold, 1e-9);
            wallet.Add(Currency.Gold, 1e6);
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _), "single rank");
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Statue2, wallet, out _));

            var restored = new GoldShopState();
            restored.Load(shop.ToSave());
            Assert.IsTrue(restored.Owns(GoldShopCatalog.Statue1) && restored.Owns(GoldShopCatalog.Statue2));
        }

        [Test]
        public void GoldShop_ModifiersReachTheStatSheet()
        {
            var shop = new GoldShopState();
            stats.ExtraSource = shop.Modifiers;
            stats.Rebuild(tree, state);
            double before = stats.Get(StatId.IncomeMultiplier);
            wallet.Add(Currency.Gold, 1e6);
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Reputation, wallet, out _));
            stats.Rebuild(tree, state);
            Assert.AreEqual(before * 1.10, stats.Get(StatId.IncomeMultiplier), 1e-9);
        }

        [Test]
        public void StatueNodes_StayHidden_UntilTheirUnlockIsOwned()
        {
            var shop = new GoldShopState();
            state.UnlockCheck = shop.Owns;
            wallet.Add(Currency.Money, 1e9);
            Buy(DefaultSkillTreeFactory.RootId);
            var fill = tree.Find("statue_gold_fill");
            Assert.AreEqual(NodeStatus.Hidden, state.Status(fill, wallet, stats, out _));
            wallet.Add(Currency.Gold, 1e6);
            shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _);
            Assert.AreEqual(NodeStatus.Available, state.Status(fill, wallet, stats, out _));
        }

        [Test]
        public void PrestigeGold_ZeroBelowThreshold_GrowsSlowerThanEarnings()
        {
            double t = balance.prestigeThreshold;
            Assert.AreEqual(0, Prestige.GoldFor(t * 0.99, balance));
            double g1 = Prestige.GoldFor(t, balance), g4 = Prestige.GoldFor(t * 4, balance), g100 = Prestige.GoldFor(t * 100, balance);
            Assert.Greater(g1, 0);
            Assert.Greater(g4, g1);
            Assert.Less(g4, g1 * 4, "4× the earnings gives less than 4× the gold");
            Assert.Greater(g100, g4);
        }
        [Test]
        public void DetectiveRanks_FollowRetirements_AndCanBeExtended()
        {
            Assert.AreEqual("Rookie", DetectiveRanks.For(balance, 0).title);
            Assert.AreEqual("Detective", DetectiveRanks.For(balance, 1).title);
            Assert.AreEqual("Detective", DetectiveRanks.For(balance, 2).title);
            Assert.IsTrue(DetectiveRanks.TryNext(balance, 1, out var next));
            Assert.Greater(next.retirements, 1);
            // A rank added at the end of the list (any order) is picked up without code changes.
            balance.ranks.Add(new GameBalance.DetectiveRank("Test Rank", 1000, 9f));
            Assert.AreEqual("Test Rank", DetectiveRanks.For(balance, 1000).title);
            Assert.IsFalse(DetectiveRanks.TryNext(balance, 1000, out _));
            // Bonuses never go down as the rank rises.
            float last = -1;
            foreach (var r in DetectiveRanks.Sorted(balance)) { Assert.GreaterOrEqual(r.incomeBonus, last, r.title); last = r.incomeBonus; }
        }

        [Test]
        public void StatueTiers_NeedThePedestal_AndHaveTwoRanks()
        {
            var shop = new GoldShopState();
            wallet.Add(Currency.Gold, 1e6);
            string tier = GoldShopCatalog.StatueTierId(0);
            Assert.IsFalse(shop.TryBuy(tier, wallet, out _), "needs the first pedestal");
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _));
            Assert.IsTrue(shop.TryBuy(tier, wallet, out _));
            Assert.IsTrue(shop.TryBuy(tier, wallet, out _));
            Assert.IsFalse(shop.TryBuy(tier, wallet, out _), "bronze → silver → gold, then maxed");
            Assert.AreEqual(GoldShopCatalog.StatueTierCount, shop.Rank(tier));
            Assert.AreEqual(GoldShopCatalog.StatueTierCount + 1, balance.statueTierNames.Length);
            Assert.AreEqual(GoldShopCatalog.StatueTierCount + 1, balance.statueTierGoldMultipliers.Length);
        }
    }
}
