using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class LootboxOpeningTests
    {
        GameObject root;
        GameManager game;
        GameBalance balance;
        SkillTreeAsset tree;
        LootboxSystem loot;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Lootbox test");
            root.SetActive(false);
            game = root.AddComponent<GameManager>();
            balance = ScriptableObject.CreateInstance<GameBalance>();
            tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            tree.SetNodes(DefaultSkillTreeFactory.Create());
            game.balance = balance;
            game.skillTreeAsset = tree;
            game.useSaveFile = false;
            game.PrepareScenePresentation();
            loot = root.AddComponent<LootboxSystem>();
            typeof(GameManager).GetProperty("Lootbox").SetValue(game, loot);
            loot.Initialize(game);
            game.LootboxView.CreateLayout(game).transform.SetParent(root.transform, false);
            game.LootboxView.Initialize(game);
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.Gold, weight = 1 });
            balance.lootGoldMin = balance.lootGoldMax = 3;
            game.Stats.SetBase(StatId.LootDoubleChance, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(balance);
            Object.DestroyImmediate(tree);
        }

        [Test]
        public void UnaffordablePurchaseDoesNotOpenOrAward()
        {
            Assert.IsFalse(loot.TryOpen());
            Assert.IsFalse(game.LootboxView.IsOpen);
            Assert.AreEqual(0, loot.Opened);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Gold));
        }

        [Test]
        public void LootboxPrice_StartsAtFiftyAndIncreasesByFivePerPurchase()
        {
            Assert.AreEqual(50, loot.Price);
            game.Wallet.Add(Currency.Money, 1000);
            Assert.IsTrue(loot.TryOpen());
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();
            Assert.AreEqual(1, loot.Opened);
            Assert.AreEqual(55, loot.Price);
            Assert.IsTrue(loot.TryOpen());
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();
            Assert.AreEqual(2, loot.Opened);
            Assert.AreEqual(60, loot.Price);
        }

        [Test]
        public void SkipAndCloseNeverAwardTwiceAndBlockRepeatPurchase()
        {
            game.Wallet.Add(Currency.Money, 10000);
            double price = loot.Price;
            Assert.IsTrue(loot.TryOpen());
            Assert.IsTrue(game.WorldInputBlocked);
            Assert.IsFalse(loot.TryOpen());
            Assert.AreEqual(10000 - price, game.Wallet.Get(Currency.Money));
            game.LootboxView.AdvanceOrClose();
            Assert.IsFalse(game.LootboxView.IsRolling);
            Assert.IsTrue(game.LootboxView.IsOpen);
            Assert.AreEqual(LootKind.Gold, game.LootboxView.Reward.Kind);
            Assert.AreEqual(3, game.LootboxView.Reward.Amount);
            game.LootboxView.Reveal();
            game.LootboxView.AdvanceOrClose();
            Assert.IsFalse(game.WorldInputBlocked);
            Assert.AreEqual(3, game.Wallet.Get(Currency.Gold));
            Assert.AreEqual(1, loot.Opened);
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(6, game.Wallet.Get(Currency.Gold));
        }

        [Test]
        public void TimedRevealIncludesDoubleBoxWithoutAnotherPayment()
        {
            game.Stats.SetBase(StatId.LootDoubleChance, 1);
            game.Wallet.Add(Currency.Money, 10000);
            double price = loot.Price;
            Assert.IsTrue(loot.TryOpen());
            game.LootboxView.AdvanceAnimation(balance.lootboxOpeningSeconds + 1);
            Assert.IsFalse(game.LootboxView.IsRolling);
            StringAssert.Contains("DOUBLE BOX", game.LootboxView.Reward.Description);
            Assert.AreEqual(6, game.Wallet.Get(Currency.Gold));
            Assert.AreEqual(10000 - price, game.Wallet.Get(Currency.Money));
            game.LootboxView.AdvanceAnimation(100);
            Assert.AreEqual(6, game.Wallet.Get(Currency.Gold));
        }

        [Test]
        public void IceBecomesAvailableImmediatelyAfterWhiskeyUnlock()
        {
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.IceCube, weight = 100 });
            var random = new System.Random(1);
            for (int i = 0; i < 100; i++) Assert.AreEqual(LootKind.Money, loot.PreviewKind(random));
            game.Wallet.Add(Currency.Gold, 100);
            Assert.IsTrue(game.Shop.TryBuy(GoldShopCatalog.Whiskey, game.Wallet, out var reason), reason);
            Assert.AreEqual(LootKind.IceCube, loot.PreviewKind(random));
            game.Wallet.Add(Currency.Money, 10000);
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(LootKind.IceCube, game.LootboxView.Reward.Kind);
            Assert.AreEqual(1, game.Inventory.IceCubes);
        }

        [Test]
        public void StatueUnlockEnablesPiecesWithoutEnablingIce()
        {
            var pedestal = root.AddComponent<FigurinePedestal>();
            pedestal.Bind(game, 0);
            game.figurines = new[] { pedestal };
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.StatuePiece, weight = 1 });
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.IceCube, weight = 10000 });
            var random = new System.Random(3);
            Assert.AreEqual(LootKind.Money, loot.PreviewKind(random));
            game.Wallet.Add(Currency.Gold, 100);
            Assert.IsTrue(game.Shop.TryBuy(GoldShopCatalog.StatueUnlockId(0), game.Wallet, out var reason), reason);
            for (int i = 0; i < 100; i++) Assert.AreEqual(LootKind.StatuePiece, loot.PreviewKind(random));
            game.Wallet.Add(Currency.Money, 10000);
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(1, pedestal.Pieces);
            Assert.AreEqual(0, game.Inventory.IceCubes);
        }

        [Test]
        public void LockedItemsAreExcludedFromBothDoubleBoxRewards()
        {
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.Gold, weight = 1 });
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.IceCube, weight = 10000 });
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.StatuePiece, weight = 10000 });
            game.Stats.SetBase(StatId.LootDoubleChance, 1);
            game.Wallet.Add(Currency.Money, 10000);
            var random = new System.Random(2);
            for (int i = 0; i < 100; i++) Assert.AreEqual(LootKind.Gold, loot.PreviewKind(random));
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(6, game.Wallet.Get(Currency.Gold));
            Assert.AreEqual(0, game.Inventory.IceCubes);
            Assert.AreEqual(0, game.Inventory.SparePieces);
        }

        [Test]
        public void DecorativeReelCannotOfferLockedStatuePieces()
        {
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.StatuePiece, weight = 100 });
            var random = new System.Random(1);
            for (int i = 0; i < 100; i++) Assert.AreEqual(LootKind.Money, loot.PreviewKind(random));
            Assert.AreEqual(0, loot.Opened);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Money));
        }

        [Test]
        public void LootTable_ConfiguredProbabilities_MatchRequirements()
        {
            var defaultBalance = ScriptableObject.CreateInstance<GameBalance>();
            float total = 0;
            float paper = 0, statue1 = 0, statue2 = 0, statue3 = 0, gold = 0, ice = 0, money = 0;
            foreach (var entry in defaultBalance.lootTable)
            {
                total += entry.weight;
                if (entry.kind == LootKind.PaperBall) paper += entry.weight;
                else if (entry.kind == LootKind.StatuePiece) statue1 += entry.weight;
                else if (entry.kind == LootKind.StatuePiece2) statue2 += entry.weight;
                else if (entry.kind == LootKind.StatuePiece3) statue3 += entry.weight;
                else if (entry.kind == LootKind.Gold) gold += entry.weight;
                else if (entry.kind == LootKind.IceCube) ice += entry.weight;
                else if (entry.kind == LootKind.Money) money += entry.weight;
            }
            Assert.AreEqual(100f, total, "Total loot table weights must equal 100%");
            Assert.AreEqual(20f, paper, "Paper ball must be 20%");
            Assert.AreEqual(4f, statue1, "Statue 1 piece must be 4%");
            Assert.AreEqual(4f, statue2, "Statue 2 piece must be 4%");
            Assert.AreEqual(4f, statue3, "Statue 3 piece must be 4%");
            Assert.AreEqual(12f, statue1 + statue2 + statue3, "Total statue pieces must be 12%");
            Assert.AreEqual(1f, gold, "Gold must be 1%");
            Assert.AreEqual(20f, ice, "Ice cube must be 20%");
            Assert.AreEqual(47f, money, "Money must be 47%");
            Assert.AreEqual(1.0, defaultBalance.lootGoldMin, "Gold reward min must be 1");
            Assert.AreEqual(1.0, defaultBalance.lootGoldMax, "Gold reward max must be 1");
            Assert.AreEqual(7, defaultBalance.statueBasePieces, "All statues must require 7 pieces by default");
            Assert.AreEqual(0, defaultBalance.statuePiecesPerIndex, "All statues must have the same piece requirement");
            Object.DestroyImmediate(defaultBalance);
        }

        [Test]
        public void MoneyRewardTiers_AreAlwaysPositive_AndIncludeOnePercentAmazingPrize()
        {
            // Tier 1: Amazing Prize (roll < 1.0, 1% overall) -> 5.0x to 8.0x
            double amazing = LootboxSystem.RollMoneyMultiplier(0.5, 0.5, out string p1);
            Assert.GreaterOrEqual(amazing, 5.0);
            Assert.LessOrEqual(amazing, 8.0);
            Assert.AreEqual("JACKPOT! ", p1);

            // Tier 2: Big Win (1.0 <= roll < 4.0, 3% overall) -> 2.5x to 3.5x
            double big = LootboxSystem.RollMoneyMultiplier(2.5, 0.5, out string p2);
            Assert.GreaterOrEqual(big, 2.5);
            Assert.LessOrEqual(big, 3.5);
            Assert.AreEqual("BIG WIN! ", p2);

            // Tier 3: Nice Win (4.0 <= roll < 19.0, 15% overall) -> 1.5x to 2.0x
            double nice = LootboxSystem.RollMoneyMultiplier(10.0, 0.5, out string p3);
            Assert.GreaterOrEqual(nice, 1.5);
            Assert.LessOrEqual(nice, 2.0);
            Assert.AreEqual("NICE WIN! ", p3);

            // Tier 4: Small Win (19.0 <= roll < 47.0, 28% overall) -> 1.1x to 1.3x
            double small = LootboxSystem.RollMoneyMultiplier(35.0, 0.5, out string p4);
            Assert.GreaterOrEqual(small, 1.1);
            Assert.LessOrEqual(small, 1.3);
            Assert.AreEqual("", p4);

            // Ensure every multiplier is strictly positive profit (> 1.0x price)
            foreach (double testRoll in new[] { 0.0, 0.99, 1.0, 3.99, 4.0, 18.99, 19.0, 46.99 })
            {
                double mult = LootboxSystem.RollMoneyMultiplier(testRoll, 0.0, out _);
                Assert.Greater(mult, 1.0, $"Multiplier at roll {testRoll} must be strictly positive profit");
            }
        }

        [Test]
        public void LootboxOpeningView_DisplaysDistinctIconsAndTitlesForMoneyTiers()
        {
            var view = game.LootboxView;
            // Test Tier 1 (Small Win / 1 bill)
            var smallWin = new LootOpened(LootKind.Money, 55, "€55.", 1);
            view.Show(smallWin);
            view.Reveal();
            Assert.AreEqual("YOU FOUND", view.HeadingText);

            // Test Tier 4 (Jackpot / a lot of money)
            var jackpot = new LootOpened(LootKind.Money, 350, "JACKPOT! €350.", 4);
            view.Show(jackpot);
            view.Reveal();
            Assert.AreEqual("JACKPOT!", view.HeadingText);
            Assert.AreEqual(UITheme.GoldBright, view.HeadingColor);
        }

        [Test]
        public void IceCubes_FillGlassInstantlyUpToThree_AndMeltOneByOneEveryMinute()
        {
            var glassGo = new GameObject("WhiskeyGlassTest");
            var glass = glassGo.AddComponent<WhiskeyGlass>();
            typeof(GameManager).GetProperty("Whiskey").SetValue(game, glass);
            glass.Initialize(game);

            // Unlock whiskey
            game.Wallet.Add(Currency.Gold, 100);
            Assert.IsTrue(game.Shop.TryBuy(GoldShopCatalog.Whiskey, game.Wallet, out _));
            glass.RefreshUnlock();

            Assert.AreEqual(0, glass.IceCount);
            Assert.AreEqual(0, game.Inventory.IceCubes);

            // Configure lootbox to drop ice cubes
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.IceCube, weight = 100 });
            game.Wallet.Add(Currency.Money, 10000);

            // First ice drop: goes directly into glass (1/3)
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(1, glass.IceCount);
            Assert.AreEqual(0, game.Inventory.IceCubes);
            Assert.AreEqual(60f, glass.MeltRemaining, 0.01f);
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();

            // Second ice drop: goes directly into glass (2/3)
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(2, glass.IceCount);
            Assert.AreEqual(0, game.Inventory.IceCubes);
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();

            // Third ice drop: goes directly into glass (3/3)
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(3, glass.IceCount);
            Assert.AreEqual(0, game.Inventory.IceCubes);
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();

            // Fourth ice drop: glass is full (3/3), goes to inventory!
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(3, glass.IceCount);
            Assert.AreEqual(1, game.Inventory.IceCubes);
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();

            // Advance melt by 60 seconds: 1 cube melts, inventory cube immediately tops it back up to 3!
            glass.AdvanceMelt(60f);
            Assert.AreEqual(3, glass.IceCount);
            Assert.AreEqual(0, game.Inventory.IceCubes);

            // Advance melt by another 60 seconds: 1 cube melts, no spare cubes left -> 2 cubes remain
            glass.AdvanceMelt(60f);
            Assert.AreEqual(2, glass.IceCount);

            // Advance melt by another 60 seconds -> 1 cube remains
            glass.AdvanceMelt(60f);
            Assert.AreEqual(1, glass.IceCount);

            // Advance melt by another 60 seconds -> 0 cubes remain
            glass.AdvanceMelt(60f);
            Assert.AreEqual(0, glass.IceCount);
            Assert.AreEqual(0f, glass.MeltRemaining);

            Object.DestroyImmediate(glassGo);
        }

        [Test]
        public void AllStatuesRequireSevenPieces_AndPiecesDropRandomlyPerStatue()
        {
            var p0 = new GameObject("P0").AddComponent<FigurinePedestal>();
            var p1 = new GameObject("P1").AddComponent<FigurinePedestal>();
            var p2 = new GameObject("P2").AddComponent<FigurinePedestal>();
            p0.Bind(game, 0);
            p1.Bind(game, 1);
            p2.Bind(game, 2);
            game.figurines = new[] { p0, p1, p2 };

            // All statues must require 7 pieces
            Assert.AreEqual(7, p0.PiecesRequired);
            Assert.AreEqual(7, p1.PiecesRequired);
            Assert.AreEqual(7, p2.PiecesRequired);

            // Unlock statues
            game.Wallet.Add(Currency.Gold, 100);
            Assert.IsTrue(game.Shop.TryBuy(GoldShopCatalog.Statue1, game.Wallet, out _));

            // Test drops for statue 2 directly
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.StatuePiece2, weight = 100 });
            game.Wallet.Add(Currency.Money, 10000);

            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(1, p1.Pieces, "Statue 2 should receive the piece");
            Assert.AreEqual(0, p0.Pieces);
            Assert.AreEqual(0, p2.Pieces);

            // Test drops for statue 3
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.StatuePiece3, weight = 100 });
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();

            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(1, p2.Pieces, "Statue 3 should receive the piece");
            Assert.AreEqual(0, p0.Pieces);
            Assert.AreEqual(1, p1.Pieces);

            // Test drops for statue 1
            balance.lootTable.Clear();
            balance.lootTable.Add(new GameBalance.LootEntry { kind = LootKind.StatuePiece, weight = 100 });
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();

            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(1, p0.Pieces, "Statue 1 should receive the piece");

            // Fill statue 1 to completion (7/7)
            while (p0.Pieces < p0.PiecesRequired) p0.AddPiece();
            Assert.IsTrue(p0.IsComplete);

            // Further drops for statue 1 should be stored as spare pieces in inventory
            game.LootboxView.AdvanceOrClose();
            game.LootboxView.AdvanceOrClose();
            Assert.IsTrue(loot.TryOpen());
            Assert.AreEqual(1, game.Inventory.SparePieces, "Complete statue pieces are stored as spare pieces");

            Object.DestroyImmediate(p0.gameObject);
            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
        }
    }
}
