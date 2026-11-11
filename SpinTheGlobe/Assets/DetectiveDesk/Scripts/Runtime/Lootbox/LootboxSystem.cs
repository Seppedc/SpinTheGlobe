using System;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Lootbox bought with money. Rolls the weighted loot table in GameBalance:
    /// ice cube, gold, statue piece, paper ball, or money (partial refund or jackpot).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootboxSystem : MonoBehaviour
    {
        public int Opened { get; private set; }
        public double Price => game ? System.Math.Ceiling(game.Balance.lootboxCost.Cost(Opened) * game.Stats.Get(StatId.LootboxPrice)) : 0;
        public event Action<LootOpened> Rolled;

        GameManager game;
        readonly System.Random rng = new System.Random();

        public void Initialize(GameManager owner) { game = owner; }

        public bool TryOpen()
        {
            if (!game || (game.PaperBalls && (game.PaperBalls.IsAiming || game.PaperBalls.IsThrowing)) || (game.LootboxView && game.LootboxView.IsOpen)) return false;
            double price = Price;
            if (!game.TrySpend(price)) return false;
            Opened++;
            var result = Apply(Roll(), price);
            // Double Box: chance of a second reward from the same box.
            if (rng.NextDouble() < game.Stats.Get(StatId.LootDoubleChance))
            {
                var second = Apply(Roll(), price);
                result = new LootOpened(result.Kind, result.Amount, result.Description + " DOUBLE BOX! Also " + second.Description);
            }
            if (game.audioManager) game.audioManager.Click();
            if (game.LootboxView) game.LootboxView.Show(result);
            Rolled?.Invoke(result);
            EventBus.Publish(result);
            game.Notify();
            return true;
        }

        LootKind Roll() => PreviewKind(rng);

        // Separate presentation RNG cannot change the actual reward sequence.
        public LootKind PreviewKind(System.Random random)
        {
            var table = game.Balance.lootTable;
            float luck = Mathf.Max(0, (float)game.Stats.Get(StatId.LootLuck));
            // No statue pieces until statues are unlocked in the Gold shop.
            bool statuesUnlocked = AnyStatueUnlocked();
            bool ice = game.Shop.Owns(GoldShopCatalog.Whiskey);
            float Weight(GameBalance.LootEntry e) =>
                e.kind.IsStatuePiece() && !statuesUnlocked ? 0 :
                e.kind == LootKind.IceCube && !ice ? 0 :
                Mathf.Max(0, e.weight) * (e.kind.IsStatuePiece() || e.kind == LootKind.Gold ? 1 + luck : 1);
            float total = 0;
            foreach (var e in table) total += Weight(e);
            if (table.Count == 0 || total <= 0) return LootKind.Money;   // e.g. a table with only statue pieces while statues are locked
            double pick = random.NextDouble() * total;
            foreach (var e in table)
            {
                float weight = Weight(e);
                if (weight <= 0) continue;
                pick -= weight;
                if (pick <= 0) return e.kind;
            }
            // Floating-point rounding must never fall back to a locked entry.
            for (int i = table.Count - 1; i >= 0; i--)
                if (Weight(table[i]) > 0) return table[i].kind;
            return LootKind.Money;
        }

        LootOpened Apply(LootKind kind, double price)
        {
            var b = game.Balance;
            switch (kind)
            {
                case LootKind.IceCube:
                    if (!game.Shop.Owns(GoldShopCatalog.Whiskey)) goto default;
                    bool added = game.Whiskey && game.Whiskey.TryAddIce();
                    if (!added)
                    {
                        game.Inventory.AddIce();
                        return new LootOpened(kind, 1, "an ice cube! (Glass has 3/3 ice cubes; stored in inventory).");
                    }
                    return new LootOpened(kind, 1, "an ice cube added to the glass (" + game.Whiskey.IceCount + "/3)! Melts in 60s.");
                case LootKind.Gold:
                {
                    double gold = Math.Max(1, Math.Round(b.lootGoldMin + rng.NextDouble() * Math.Max(0, b.lootGoldMax - b.lootGoldMin)));
                    game.CreditGold(gold);
                    return new LootOpened(kind, gold, gold == 1 ? "1 gold coin." : NumberFormatter.Format(gold) + " gold coins.");
                }
                case LootKind.StatuePiece:
                case LootKind.StatuePiece2:
                case LootKind.StatuePiece3:
                {
                    int statueIndex = kind.StatueIndex();
                    var target = GetStatue(statueIndex);
                    if (target && !target.IsComplete)
                    {
                        target.AddPiece();
                        return new LootOpened(kind, 1, "a statue piece for statue " + (target.Index + 1) + " (" + target.Pieces + "/" + target.PiecesRequired + ").");
                    }
                    if (!AnyStatueUnlocked()) goto default;   // statues are still locked: pay out money instead
                    game.Inventory.AddSparePiece();
                    return new LootOpened(kind, 1, "a spare piece for statue " + (statueIndex + 1) + " (kept until pedestal is smashed).");
                }
                case LootKind.PaperBall:
                    game.Inventory.AddPaperBall();
                    return new LootOpened(kind, 1, "a paper ball. Shoot hoops to build your score!");
                default:
                {
                    double roll = rng.NextDouble() * 47.0;
                    double mult = RollMoneyMultiplier(roll, rng.NextDouble(), out string prefix, out int tier);
                    double amount = Math.Ceiling(price * mult);
                    game.Credit(amount);
                    return new LootOpened(LootKind.Money, amount, prefix + UIFactory.Money(amount) + ".", tier);
                }
            }
        }

        /// <summary>
        /// Rolls money reward multiplier across 4 distinct positive tiers (out of 47% money drop rate):
        /// Tier 4: 1% Amazing prize (5x–8x), Tier 3: 3% Big win (2.5x–3.5x), Tier 2: 15% Nice win (1.5x–2.0x), Tier 1: 28% Small win (1.1x–1.3x).
        /// </summary>
        public static double RollMoneyMultiplier(double roll0to47, double rollWithinTier, out string prefix) =>
            RollMoneyMultiplier(roll0to47, rollWithinTier, out prefix, out _);

        public static double RollMoneyMultiplier(double roll0to47, double rollWithinTier, out string prefix, out int tier)
        {
            if (roll0to47 < 1.0)
            {
                tier = 4;
                prefix = "JACKPOT! ";
                return 5.0 + rollWithinTier * 3.0;
            }
            if (roll0to47 < 4.0)
            {
                tier = 3;
                prefix = "BIG WIN! ";
                return 2.5 + rollWithinTier * 1.0;
            }
            if (roll0to47 < 19.0)
            {
                tier = 2;
                prefix = "NICE WIN! ";
                return 1.5 + rollWithinTier * 0.5;
            }
            tier = 1;
            prefix = "";
            return 1.1 + rollWithinTier * 0.2;
        }

        FigurinePedestal GetStatue(int index)
        {
            if (game.figurines == null || game.figurines.Length == 0) return null;
            if (index >= 0 && index < game.figurines.Length) return game.figurines[index];
            return game.figurines[0];
        }

        bool AnyStatueUnlocked()
        {
            if (game.figurines != null) foreach (var f in game.figurines) if (f && f.Unlocked) return true;
            return false;
        }

        FigurinePedestal FirstIncompleteStatue()
        {
            if (game.figurines == null) return null;
            foreach (var f in game.figurines) if (f && f.Unlocked && !f.IsComplete && !f.IsBusy) return f;
            return null;
        }

        public void LoadState(SaveData d) { Opened = Mathf.Max(0, d.lootboxesOpened); }
        public void SaveState(SaveData d) { d.lootboxesOpened = Opened; }
    }
}
