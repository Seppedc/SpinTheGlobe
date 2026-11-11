using System.Linq;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Gameplay;
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
            tree.SetSections(DefaultSkillTreeFactory.CreateSections());
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
        public void WhiskeySet_IsFiveGoldOnce_AndPersistsAcrossRetirementSave()
        {
            var shop = new GoldShopState { Balance = balance };
            var item = GoldShopCatalog.Find(GoldShopCatalog.Whiskey);
            Assert.AreEqual(5, balance.whiskeyUnlockGoldCost);
            balance.whiskeyUnlockGoldCost = 11;
            Assert.AreEqual(11, shop.NextCost(item), "Gold price comes from balance data");
            balance.whiskeyUnlockGoldCost = 5;
            Assert.AreEqual(5, shop.NextCost(item));
            Assert.AreEqual(1, item.maxRank);
            wallet.Add(Currency.Gold, 4);
            Assert.IsFalse(shop.TryBuy(item.id, wallet, out _));
            wallet.Add(Currency.Gold, 1);
            Assert.IsTrue(shop.TryBuy(item.id, wallet, out _));
            Assert.AreEqual(0, wallet.Gold);
            var data = new Save.SaveData { goldShop = shop.ToSave(), prestiges = 1 };
            var restored = new GoldShopState();
            restored.Load(JsonUtility.FromJson<Save.SaveData>(JsonUtility.ToJson(data)).goldShop);
            Assert.IsTrue(restored.Owns(item.id));
            wallet.Add(Currency.Gold, 10);
            Assert.IsFalse(restored.TryBuy(item.id, wallet, out _));
            Assert.AreEqual(10, wallet.Gold, "owned unlock cannot charge again");
            Assert.AreEqual(GoldShopCatalog.Whiskey, GoldShopCatalog.Find("bottomless_glass").requires);
        }

        [Test]
        public void WhiskeySkills_AreHiddenUntilGoldUnlock_EvenForPreviouslyMaxedSkills()
        {
            var shop = new GoldShopState();
            state.UnlockCheck = shop.Owns;
            wallet.Add(Currency.Money, 1e6);
            Assert.IsTrue(Buy("root"));
            foreach (var node in tree.Nodes.Where(n => n.branch == SkillBranch.Whiskey))
            {
                Assert.AreEqual(GoldShopCatalog.Whiskey, node.requiresUnlock);
                Assert.AreEqual(NodeStatus.Hidden, state.Status(node, wallet, stats, out _));
            }
            Assert.IsFalse(Buy("whiskey_duration"));
            wallet.Add(Currency.Gold, 10);
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Whiskey, wallet, out _));
            Assert.AreEqual(NodeStatus.Available, state.Status(tree.Find("whiskey_duration"), wallet, stats, out _));
            Assert.IsTrue(Buy("whiskey_duration"));
            state.Load(tree, new[] { new Save.SkillRank { id = "whiskey_duration", rank = 10 } });
            shop.Load(null);
            Assert.AreEqual(NodeStatus.Hidden, state.Status(tree.Find("whiskey_duration"), wallet, stats, out _));
        }

        [Test]
        public void ExistingDragRanks_BecomeLessFriction_WithoutIncreasingSwipePower()
        {
            state.Load(tree, new[] { new DetectiveDesk.Save.SkillRank { id = "globe_drag", rank = 5 } });
            stats.Rebuild(tree, state);
            Assert.AreEqual(5, state.Rank("globe_drag"));
            Assert.AreEqual(10, tree.Find("globe_drag").maxRank);
            Assert.AreEqual(System.Math.Pow(.85, 5), stats.Get(StatId.GlobeSpeedDecay), 1e-9);
            Assert.AreEqual(1, stats.Get(StatId.GlobeDragPower));
        }

        [Test]
        public void PinUpgrades_SharePinOnePrices_WithTenStrengthAndTwentyPayLevels()
        {
            var strengthPrices = tree.Find(DefaultSkillTreeFactory.PinDurabilityId(1)).cost;
            var payPrices = tree.Find(DefaultSkillTreeFactory.PinPayoutId(1)).cost;
            for (int pin = 1; pin <= DefaultSkillTreeFactory.MaxPins; pin++)
            {
                var strength = tree.Find(DefaultSkillTreeFactory.PinDurabilityId(pin));
                var pay = tree.Find(DefaultSkillTreeFactory.PinPayoutId(pin));
                Assert.AreEqual(10, strength.maxRank);
                Assert.AreEqual(20, pay.maxRank);
                for (int rank = 0; rank < strength.maxRank; rank++) Assert.AreEqual(strengthPrices.Cost(rank), strength.cost.Cost(rank), $"Pin {pin} Strength {rank + 1}");
                for (int rank = 0; rank < pay.maxRank; rank++) Assert.AreEqual(payPrices.Cost(rank), pay.cost.Cost(rank), $"Pin {pin} Pay {rank + 1}");
                var unlock = tree.Find(DefaultSkillTreeFactory.PinId(pin));
                double expectedUnlock = System.Math.Max(5, System.Math.Floor(System.Math.Ceiling(DefaultSkillTreeFactory.PinPrice(pin) * .95) / 5 + .5) * 5);
                Assert.AreEqual(expectedUnlock, unlock.cost.Cost(0));
            }
            state.Load(tree, new[]
            {
                new DetectiveDesk.Save.SkillRank { id = "root", rank = 1 },
                new DetectiveDesk.Save.SkillRank { id = "pin_01", rank = 1 },
                new DetectiveDesk.Save.SkillRank { id = "pin_01_durability", rank = 20 },
                new DetectiveDesk.Save.SkillRank { id = "pin_01_payout", rank = 21 },
            });
            stats.Rebuild(tree, state);
            Assert.AreEqual(10, state.Rank("pin_01_durability"), "old Strength ranks clamp to the lower limit");
            Assert.AreEqual(20, state.Rank("pin_01_payout"), "saved ranks clamp to the new limit");
            Assert.AreEqual(11, stats.Get(StatId.PinDurability, 0));
            Assert.AreEqual(11, stats.Get(StatId.PinPayout, 0));
        }

        [Test]
        public void BulkPurchase_ChargesEachRank_AndPublishesOneFinalRank()
        {
            wallet.Add(Currency.Money, 1000);
            Assert.IsTrue(Buy("root"));
            Assert.IsTrue(Buy("pin_01"));
            wallet = new Wallet();
            wallet.Add(Currency.Money, 25);
            var node = tree.Find("shooter_cooldown");
            Assert.AreEqual(2, state.QuotePurchase(node, wallet, stats, 10, out double quote));
            Assert.AreEqual(25, quote);
            Assert.AreEqual(0, state.Rank(node.id), "a quote must not spend or change ranks");
            int callbacks = 0, notifiedRank = 0;
            state.Purchased += (_, rank) => { callbacks++; notifiedRank = rank; };
            Assert.IsTrue(state.TryPurchase(tree, node.id, wallet, stats, 10, out int purchased, out _));
            Assert.AreEqual(2, purchased);
            Assert.AreEqual(2, notifiedRank);
            Assert.AreEqual(1, callbacks);
            Assert.AreEqual(0, wallet.Money);
            Assert.AreEqual(25, wallet.LifetimeMoney, "spending does not change total earned");
            Assert.AreEqual(balance.baseStats.Find(x => x.stat == StatId.ShooterCooldown).value * .88 * .88,
                stats.Get(StatId.ShooterCooldown), 1e-8);
        }

        [Test]
        public void BulkMotorPurchase_StopsAtPermanentSpinLimit()
        {
            wallet.Add(Currency.Money, 10000);
            Assert.IsTrue(Buy("root"));
            double before = wallet.Money;
            Assert.IsTrue(state.TryPurchase(tree, "globe_auto", wallet, stats, 10, out int purchased, out _));
            Assert.AreEqual(3, purchased, "three motor ranks fill the starting 15 RPM limit");
            Assert.AreEqual(stats.Permanent(StatId.GlobeMaxSpeed), stats.Get(StatId.GlobeAutoSpeed));
            Assert.AreEqual(tree.Find("globe_auto").cost.Cost(0) + tree.Find("globe_auto").cost.Cost(1)
                + tree.Find("globe_auto").cost.Cost(2), before - wallet.Money);
            Assert.IsFalse(state.TryPurchase(tree, "globe_auto", wallet, stats, 10, out purchased, out _));
            Assert.AreEqual(0, purchased);
        }

        [Test]
        public void BulkPurchase_StopsAtMaxRank_AndRetainsRanksInSave()
        {
            wallet.Add(Currency.Money, 10000);
            Assert.IsTrue(state.TryPurchase(tree, "root", wallet, stats, 10, out int rootRanks, out _));
            Assert.AreEqual(1, rootRanks);
            Assert.IsTrue(state.TryPurchase(tree, "globe_drag", wallet, stats, 10, out int purchased, out _));
            Assert.AreEqual(10, purchased);
            var restored = new SkillTreeState();
            restored.Load(tree, state.ToSave());
            Assert.AreEqual(10, restored.Rank("globe_drag"));
            Assert.AreEqual(1, restored.Rank("root"));
        }

        [Test]
        public void BulkPurchase_CannotBypassLocks_OrSpendForZeroQuantity()
        {
            wallet.Add(Currency.Money, 10000);
            Assert.IsTrue(Buy("root"));
            double before = wallet.Money;
            Assert.IsFalse(state.TryPurchase(tree, "case_unlock", wallet, stats, 10, out _, out _));
            Assert.IsFalse(state.TryPurchase(tree, "globe_drag", wallet, stats, 0, out _, out _));
            Assert.AreEqual(before, wallet.Money);
            Assert.AreEqual(0, state.Rank("globe_drag"));
        }

        [Test]
        public void SectionHeadings_AreFreePresentation_NotPurchasableSkills()
        {
            wallet.Add(Currency.Money, 1000);
            double money = wallet.Money;
            Assert.AreEqual(4, tree.Sections.Count);
            foreach (var section in tree.Sections)
            {
                Assert.IsNull(tree.Find(section.id));
                Assert.IsFalse(Buy(section.id));
                Assert.AreEqual(0, state.Rank(section.id));
            }
            Assert.AreEqual(money, wallet.Money);
            Assert.AreEqual(0, state.ToSave().Count);
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            Assert.IsTrue(Buy("globe_drag"), "the Globe heading adds no extra unlock requirement");
        }

        [Test]
        public void ManualGlobeUpgrades_AreAvailableFromRootWithoutAutoSpin()
        {
            wallet.Add(Currency.Money, 10000);
            foreach (var id in new[] { "globe_max_speed", "globe_drag", "globe_click" })
                Assert.AreEqual(NodeStatus.Hidden, state.Status(tree.Find(id), wallet, stats, out _));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            foreach (var id in new[] { "globe_drag", "globe_click", "globe_max_speed" })
            {
                CollectionAssert.AreEqual(new[] { DefaultSkillTreeFactory.RootId }, tree.Find(id).prerequisites);
                Assert.AreEqual(NodeStatus.Available, state.Status(tree.Find(id), wallet, stats, out _));
                Assert.IsTrue(Buy(id), id);
            }
            Assert.AreEqual(0, state.Rank("globe_auto"));
            Assert.AreEqual(0, stats.Get(StatId.GlobeAutoSpeed), "manual upgrades do not start the motor");
        }

        [Test]
        public void ShooterUpgrades_RequirePinOne_AndAutoFireFollowsReload()
        {
            wallet.Add(Currency.Money, 100000);
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            var reload = tree.Find("shooter_cooldown");
            Assert.AreEqual(NodeStatus.Locked, state.Status(reload, wallet, stats, out _));
            Assert.IsFalse(Buy(reload.id));
            Assert.AreEqual(NodeStatus.Hidden, state.Status(tree.Find("pin_autoloader"), wallet, stats, out _));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.PinId(1)));
            Assert.IsTrue(Buy(reload.id));
            Assert.IsTrue(Buy("pin_autoloader"));
            Assert.AreEqual(1, stats.Get(StatId.PinAutoFire));
        }

        [Test]
        public void CaseFiles_RequirePinOne_AndUnlockBeforeCaseUpgrades()
        {
            wallet.Add(Currency.Money, 10000);
            Assert.AreEqual(0, stats.Get(StatId.CaseUnlocked));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            var unlock = tree.Find(DefaultSkillTreeFactory.CaseUnlockId);
            Assert.AreEqual(30, unlock.cost.Cost(0));
            Assert.AreEqual(NodeStatus.Locked, state.Status(unlock, wallet, stats, out _));
            Assert.IsFalse(Buy(unlock.id));
            Assert.AreEqual(NodeStatus.Hidden, state.Status(tree.Find("case_payout"), wallet, stats, out _));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.PinId(1)));
            Assert.IsTrue(Buy(unlock.id));
            Assert.AreEqual(1, stats.Get(StatId.CaseUnlocked));
            foreach (var id in new[] { "case_payout", "case_cooldown", "case_autostamp" })
            {
                CollectionAssert.AreEqual(new[] { unlock.id }, tree.Find(id).prerequisites);
                Assert.AreEqual(NodeStatus.Available, state.Status(tree.Find(id), wallet, stats, out _));
            }
            Assert.IsTrue(Buy("case_payout"));
            Assert.AreEqual(1, unlock.maxRank);
        }

        [Test]
        public void SpinPayUpgrades_DoNotIncreasePinIncome()
        {
            var root = new GameObject("Inactive payout test");
            root.SetActive(false);
            try
            {
                var game = root.AddComponent<Gameplay.GameManager>();
                balance.ApplyTo(game.Stats);
                // Global income bonuses must continue to apply to both sources.
                game.Stats.SetBase(StatId.IncomeMultiplier, 2);
                double pinBefore = game.PinPayoutMultiplier(0) * game.PinIncomeScale;
                double spinBefore = game.IncomeScale;
                var ranks = new System.Collections.Generic.List<Save.SkillRank>();
                foreach (var id in new[] { "globe_multiplier", "globe_gilded_axis", "globe_mastery" })
                    ranks.Add(new Save.SkillRank { id = id, rank = 1 });
                state.Load(tree, ranks);
                game.Stats.Rebuild(tree, state);
                Assert.Greater(game.IncomeScale, spinBefore);
                Assert.AreEqual(pinBefore, game.PinPayoutMultiplier(0) * game.PinIncomeScale, 1e-9);
                ranks.Add(new Save.SkillRank { id = DefaultSkillTreeFactory.PinPayoutId(1), rank = 1 });
                state.Load(tree, ranks);
                game.Stats.Rebuild(tree, state);
                Assert.Greater(game.PinPayoutMultiplier(0) * game.PinIncomeScale, pinBefore);
                Assert.AreEqual(pinBefore, game.PinPayoutMultiplier(1) * game.PinIncomeScale, 1e-9);
                double upgraded = game.PinPayoutMultiplier(0) * game.PinIncomeScale;
                game.Stats.SetTemporaryMultiplier(StatId.PinPayout, 2);
                Assert.AreEqual(upgraded * 2, game.PinPayoutMultiplier(0) * game.PinIncomeScale, 1e-9);
            }
            finally { Object.DestroyImmediate(root); }
        }

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
        public void LocationSkills_AreRemoved_AndOldSavedRanksAreIgnored()
        {
            Assert.IsNull(tree.Find("pin_land_chance"));
            Assert.IsNull(tree.Find("pin_city_chance"));
            state.Load(tree, new[]
            {
                new Save.SkillRank { id = "root", rank = 1 },
                new Save.SkillRank { id = "pin_land_chance", rank = 5 },
                new Save.SkillRank { id = "pin_city_chance", rank = 5 },
            });
            Assert.IsTrue(state.Owns("root"));
            Assert.AreEqual(0, state.Rank("pin_land_chance"));
            Assert.AreEqual(0, state.Rank("pin_city_chance"));
            Assert.AreEqual(1, state.ToSave().Count);
            Assert.AreEqual(0, wallet.Money, "removed test upgrades do not grant a refund");
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
        public void AutoRotate_ManyLevels_NeverAboveMaxSpeed()
        {
            wallet.Add(Currency.Money, 1e15);
            Buy(DefaultSkillTreeFactory.RootId);
            var auto = tree.Find("globe_auto");
            CollectionAssert.Contains(auto.prerequisites, DefaultSkillTreeFactory.RootId, "Clockwork Motor is the first upgrade after the root");
            Assert.GreaterOrEqual(auto.maxRank, 30, "lots of levels");
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
        public void SpinLimit_SmallSteps_LeaveHeadroomAboveAutoSpin()
        {
            var bearings = tree.Find("globe_max_speed");
            Assert.GreaterOrEqual(bearings.maxRank, 100, "small steps spread speed gains across the run");
            double start = stats.Get(StatId.GlobeMaxSpeed);
            double one = stats.Preview(StatId.GlobeMaxSpeed, bearings.modifiers, 1);
            double full = stats.Preview(StatId.GlobeMaxSpeed, bearings.modifiers, bearings.maxRank);
            Assert.AreEqual((full - start) / bearings.maxRank, one - start, 1e-9, "every level adds the same amount (no percentage)");
            Assert.AreEqual(15, start / 6.0, 1e-9, "half the 30 RPM opening cap");
            Assert.AreEqual(7.5, (one - start) / 6.0, 1e-9, "gradual speed steps");
            Assert.AreEqual(772.5, full / 6.0, 1e-9, "manual headroom above the motor's 500 RPM");
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
        public void SkillTree_IsMoneyOnly_AndStatuesHaveNoMastery()
        {
            Assert.IsTrue(tree.Nodes.All(n => n.currency == Currency.Money), "everything bought with gold lives in the Gold shop");
            Assert.IsNull(tree.Find("pin_steel_tips"));
            Assert.IsTrue(tree.Nodes.Any(n => n.branch == SkillBranch.Lootbox));
            Assert.AreEqual(5, tree.Nodes.Count(n => n.maxRank >= DefaultSkillTreeFactory.MasteryRanks));
            Assert.IsNull(tree.Find("statue_mastery"));
            Assert.IsTrue(tree.Nodes.Where(n => n.branch == SkillBranch.Statues)
                .All(n => n.maxRank < DefaultSkillTreeFactory.MasteryRanks));
        }

        [Test]
        public void DeliberateSwipes_BeatClicks_AtEveryEqualUpgradeLevel()
        {
            var drag = tree.Find("globe_drag");
            var click = tree.Find("globe_click");
            for (int level = 0; level <= drag.maxRank; level++)
            {
                double dragPower = stats.Preview(StatId.GlobeDragPower, drag.modifiers, level);
                double clickPower = stats.Preview(StatId.GlobeClickPower, click.modifiers, System.Math.Min(level, click.maxRank));
                double swipe = GameplayMath.DragImpulse(200, .2, balance, dragPower, 90);
                double click1 = GameplayMath.SpinImpulse(balance.clickImpulse, clickPower, 90, balance.impulseReferenceSpeed);
                Assert.Greater(swipe, click1, "level " + level);
            }
        }

        [Test]
        public void CaseGold_FollowsCaseFiles_WithoutAutoStamp_AndStillRequiresPressCoverage()
        {
            var shop = new GoldShopState { Balance = balance };
            state.UnlockCheck = shop.Owns;
            stats.ExtraSource = shop.Modifiers;
            wallet.Add(Currency.Money, 1e6);
            wallet.Add(Currency.Gold, 1e6);
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.RootId));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.PinId(1)));
            Assert.IsTrue(Buy(DefaultSkillTreeFactory.CaseUnlockId));
            var gold = tree.Find("case_headlines");
            var cases = tree.Find(DefaultSkillTreeFactory.CaseUnlockId);
            CollectionAssert.AreEqual(new[] { cases.id }, gold.prerequisites);
            Assert.AreEqual(cases.position.x, gold.position.x);
            Assert.Less(gold.position.y, cases.position.y);
            Assert.AreEqual(NodeStatus.Hidden, state.Status(gold, wallet, stats, out _));
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.PressCoverage, wallet, out _));
            stats.Rebuild(tree, state);
            Assert.AreEqual(NodeStatus.Available, state.Status(gold, wallet, stats, out _));
            Assert.IsTrue(Buy(gold.id));
            Assert.AreEqual(0, state.Rank("case_autostamp"));
            Assert.AreEqual(.03, stats.Get(StatId.CaseGoldChance), 1e-9);
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
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.StatueGoldChance, wallet, out _), "needs statues");
            var first = GoldShopCatalog.Find(GoldShopCatalog.Statue1);
            wallet.Add(Currency.Gold, first.cost.Cost(0));
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _));
            Assert.AreEqual(0, wallet.Gold, 1e-9);
            wallet.Add(Currency.Gold, 1e6);
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _), "single rank");
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.StatueGoldChance, wallet, out _));
            Assert.IsTrue(shop.TryBuy("twin_barrel", wallet, out _), "Twin Barrel no longer requires Steel Tips");
            Assert.IsFalse(shop.Owns("steel_tips"));

            var restored = new GoldShopState();
            restored.Load(shop.ToSave());
            Assert.IsTrue(restored.Owns(GoldShopCatalog.Statue1) && restored.Owns(GoldShopCatalog.StatueGoldChance));
        }

        [Test]
        public void PrestigeAddsTenPercent_AndReputationAddsFive_WithLinearGoldPrices()
        {
            var shop = new GoldShopState { Balance = balance };
            Assert.AreEqual(1, Prestige.IncomeMultiplierFor(0, balance, shop), 1e-9);
            Assert.AreEqual(1.10, Prestige.IncomeMultiplierFor(1, balance, shop), 1e-9);
            Assert.AreEqual(1.20, Prestige.IncomeMultiplierFor(2, balance, shop), 1e-9);
            wallet.Add(Currency.Gold, 25);
            var reputation = shop.Find(GoldShopCatalog.Reputation);
            Assert.AreEqual(5, shop.NextCost(reputation));
            Assert.IsTrue(shop.TryBuy(reputation.id, wallet, out _));
            Assert.AreEqual(20, wallet.Gold);
            Assert.AreEqual(1.15, Prestige.IncomeMultiplierFor(1, balance, shop), 1e-9);
            Assert.AreEqual(10, shop.NextCost(reputation));
            Assert.IsTrue(shop.TryBuy(reputation.id, wallet, out _));
            Assert.AreEqual(10, wallet.Gold);
            Assert.AreEqual(1.20, Prestige.IncomeMultiplierFor(1, balance, shop), 1e-9);
            Assert.AreEqual(15, shop.NextCost(reputation));
            for (int rank = 0; rank < reputation.maxRank; rank++) Assert.AreEqual(5 * (rank + 1), reputation.cost.Cost(rank));
            var restored = new GoldShopState { Balance = balance };
            restored.Load(shop.ToSave());
            Assert.AreEqual(1.20, Prestige.IncomeMultiplierFor(1, balance, restored), 1e-9);
            balance.ranks.Add(new GameBalance.DetectiveRank("No double bonus", 1, 100));
            Assert.AreEqual(1.20, Prestige.IncomeMultiplierFor(1, balance, restored), 1e-9, "rank titles must not add the legacy income bonus");
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
            Assert.AreEqual(before * 1.05, stats.Get(StatId.IncomeMultiplier), 1e-9);
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
        public void PrestigeGoals_IncreaseByFiveThousand_AndLegendEndsAtFiftyThousand()
        {
            string[] titles = { "Rookie", "Detective", "Inspector", "Chief Inspector", "Commissioner", "Legend of the Bureau" };
            double[] goals = { 15000, 20000, 25000, 30000, 35000, 50000 };
            Assert.AreEqual(5, balance.maxPrestiges);
            Assert.AreEqual(6, balance.ranks.Count);
            for (int count = 0; count < titles.Length; count++)
            {
                Assert.AreEqual(titles[count], DetectiveRanks.For(balance, count).title);
                Assert.AreEqual(goals[count], Prestige.RequirementFor(count, balance));
                Assert.AreEqual(0, Prestige.GoldFor(goals[count] - 1, balance, count));
                Assert.AreEqual(count == 5, Prestige.IsFinalRank(count, balance));
                Assert.IsFalse(Prestige.HasCompleted(count, goals[count] - 1, balance));
                if (count < 5)
                {
                    Assert.IsTrue(DetectiveRanks.TryNext(balance, count, out var next));
                    Assert.AreEqual(count + 1, next.retirements);
                    Assert.AreEqual(System.Math.Floor(25 * System.Math.Sqrt(goals[count] / 15000)), Prestige.GoldFor(goals[count], balance, count));
                }
                else
                {
                    Assert.IsFalse(DetectiveRanks.TryNext(balance, count, out _));
                    Assert.IsTrue(Prestige.HasCompleted(count, goals[count], balance));
                    Assert.AreEqual(0, Prestige.GoldFor(1e9, balance, count), "final rank cannot prestige again");
                }
            }
            Assert.AreEqual(.50, Prestige.IncomeBonusFor(100, balance), 1e-9, "bonus stops at the five-reset cap");
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
            Assert.AreEqual("Inspector", DetectiveRanks.For(balance, 2).title);
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
        public void SharedStatueBonus_RequiresUnlock_AndReachesGuaranteedDoubleGold()
        {
            var shop = new GoldShopState { Balance = balance };
            wallet.Add(Currency.Gold, 1e6);
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.StatueGoldChance, wallet, out _));
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _));
            for (int index = 0; index < 3; index++)
                Assert.IsTrue(shop.Owns(GoldShopCatalog.StatueUnlockId(index)));
            for (int rank = 0; rank < 10; rank++)
            {
                Assert.AreEqual(20 + rank * 5, shop.NextCost(shop.Find(GoldShopCatalog.StatueGoldChance)));
                Assert.IsTrue(shop.TryBuy(GoldShopCatalog.StatueGoldChance, wallet, out _));
            }
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.StatueGoldChance, wallet, out _));
            stats.ExtraSource = shop.Modifiers;
            stats.Rebuild(tree, state);
            Assert.AreEqual(1, stats.Get(StatId.StatueBonusGoldChance), 1e-9);
            Assert.AreEqual(1, DetectiveDesk.Gameplay.GameplayMath.StatueGoldReward(1, 0, 0));
            Assert.AreEqual(2, DetectiveDesk.Gameplay.GameplayMath.StatueGoldReward(1, .1, .05));
            Assert.AreEqual(1, DetectiveDesk.Gameplay.GameplayMath.StatueGoldReward(1, .1, .5));
            Assert.AreEqual(2, DetectiveDesk.Gameplay.GameplayMath.StatueGoldReward(1, 1, 1));
        }

        [Test]
        public void FastCasting_TwoPurchases_RemoveOnePieceEach()
        {
            var shop = new GoldShopState { Balance = balance };
            wallet.Add(Currency.Gold, 1e6);
            shop.TryBuy(GoldShopCatalog.Statue1, wallet, out _);
            stats.ExtraSource = shop.Modifiers;
            for (int level = 1; level <= 2; level++)
            {
                Assert.AreEqual(level == 1 ? 50 : 75, shop.NextCost(shop.Find("fast_casting")));
                Assert.IsTrue(shop.TryBuy("fast_casting", wallet, out _));
                stats.Rebuild(tree, state);
                Assert.AreEqual(level, stats.Get(StatId.StatuePieceReduction));
            }
            Assert.IsFalse(shop.TryBuy("fast_casting", wallet, out _));
        }

        [Test]
        public void FreshStarts_OnlyRequireLedger_AndRestoreAllFiveSkills()
        {
            var shop = new GoldShopState { Balance = balance };
            wallet.Add(Currency.Gold, 1e6);
            Assert.IsFalse(shop.TryBuy(GoldShopCatalog.StartCases, wallet, out _));
            Assert.IsTrue(shop.TryBuy(GoldShopCatalog.KeptLedger, wallet, out _));
            string[] ids = { GoldShopCatalog.StartSpinLimit, GoldShopCatalog.WoundClockwork,
                GoldShopCatalog.StartCases, GoldShopCatalog.SparePin };
            foreach (var id in ids) Assert.IsFalse(shop.IsLocked(shop.Find(id)), id);
            foreach (var id in ids) Assert.IsTrue(shop.TryBuy(id, wallet, out _), id);
            var restored = new SkillTreeState();
            restored.Load(tree, shop.FreshStartSkills());
            foreach (var id in new[] { "root", "pin_01", "case_unlock", "globe_auto", "globe_max_speed" })
                Assert.AreEqual(1, restored.Rank(id), id);
            Assert.IsNull(shop.Find("start_money"));
            Assert.IsNull(shop.Find("golden_meridian"));
            Assert.IsNull(shop.Find("unlock_statue_2"));
            Assert.IsNull(shop.Find("statue_tier_1"));
        }

        [Test]
        public void GoldShop_UsesAuthoredBalanceCostsAndModifiers()
        {
            var item = balance.goldShopItems.Find(i => i.id == GoldShopCatalog.Reputation);
            item.cost = new CostCurve(7, 1);
            item.modifiers = new[] { StatModifier.Percent(StatId.IncomeMultiplier, .25) };
            var shop = new GoldShopState { Balance = balance };
            wallet.Add(Currency.Gold, 7);
            Assert.IsTrue(shop.TryBuy(item.id, wallet, out _));
            Assert.AreEqual(0, wallet.Gold);
            stats.ExtraSource = shop.Modifiers;
            stats.Rebuild(tree, state);
            Assert.AreEqual(1.25, stats.Get(StatId.IncomeMultiplier), 1e-9);
        }
    }
}
