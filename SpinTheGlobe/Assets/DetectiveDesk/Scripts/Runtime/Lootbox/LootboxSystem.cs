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
            if (!game) return false;
            double price = Price;
            if (!game.TrySpend(price)) { game.ShowHint("A lootbox costs " + UIFactory.Money(price) + "."); return false; }
            Opened++;
            var result = Apply(Roll(), price);
            // Double Box: chance of a second reward from the same box.
            if (rng.NextDouble() < game.Stats.Get(StatId.LootDoubleChance))
            {
                var second = Apply(Roll(), price);
                result = new LootOpened(result.Kind, result.Amount, result.Description + " DOUBLE BOX! Also " + second.Description);
            }
            if (game.audioManager) game.audioManager.Click();
            game.ShowHint("Lootbox: " + result.Description);
            Rolled?.Invoke(result);
            EventBus.Publish(result);
            game.Notify();
            return true;
        }

        LootKind Roll()
        {
            var table = game.Balance.lootTable;
            float luck = Mathf.Max(0, (float)game.Stats.Get(StatId.LootLuck));
            // No statue pieces until a statue is unlocked in the Gold shop (and has room for one).
            bool pieces = FirstIncompleteStatue();
            float Weight(GameBalance.LootEntry e) =>
                e.kind == LootKind.StatuePiece && !pieces ? 0 :
                Mathf.Max(0, e.weight) * (e.kind == LootKind.StatuePiece || e.kind == LootKind.Gold ? 1 + luck : 1);
            float total = 0;
            foreach (var e in table) total += Weight(e);
            if (table.Count == 0 || total <= 0) return LootKind.Money;   // e.g. a table with only statue pieces while statues are locked
            double pick = rng.NextDouble() * total;
            foreach (var e in table)
            {
                pick -= Weight(e);
                if (pick <= 0) return e.kind;
            }
            return table[table.Count - 1].kind;
        }

        LootOpened Apply(LootKind kind, double price)
        {
            var b = game.Balance;
            switch (kind)
            {
                case LootKind.IceCube:
                    game.Inventory.AddIce();
                    return new LootOpened(kind, 1, "an ice cube! The whiskey glass fills faster next time.");
                case LootKind.Gold:
                {
                    double gold = Math.Round(b.lootGoldMin + rng.NextDouble() * Math.Max(0, b.lootGoldMax - b.lootGoldMin));
                    game.CreditGold(gold);
                    return new LootOpened(kind, gold, NumberFormatter.Format(gold) + " gold coins.");
                }
                case LootKind.StatuePiece:
                {
                    var target = FirstIncompleteStatue();
                    if (target) { target.AddPiece(); return new LootOpened(kind, 1, "a statue piece for statue " + (target.Index + 1) + " (" + target.Pieces + "/" + target.PiecesRequired + ")."); }
                    if (!AnyStatueUnlocked()) goto default;   // statues are still locked: pay out money instead
                    game.Inventory.AddSparePiece();
                    return new LootOpened(kind, 1, "a statue piece (kept until a pedestal is free).");
                }
                case LootKind.PaperBall:
                    game.Inventory.AddPaperBall();
                    return new LootOpened(kind, 1, "a crumpled paper ball. Throw it in the bin!");
                default:
                {
                    bool jackpot = rng.NextDouble() < b.moneyJackpotChance;
                    double amount = Math.Ceiling(price * (jackpot ? 2 + rng.NextDouble() * 2 : 0.3 + rng.NextDouble() * 0.5));
                    game.Credit(amount);
                    return new LootOpened(LootKind.Money, amount, (jackpot ? "JACKPOT! " : "") + UIFactory.Money(amount) + ".");
                }
            }
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
