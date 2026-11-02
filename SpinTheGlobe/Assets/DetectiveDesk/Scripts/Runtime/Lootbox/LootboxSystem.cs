using System;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
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
        public double Price => game ? game.Balance.lootboxCost.Cost(Opened) : 0;
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
            var kind = Roll();
            var result = Apply(kind, price);
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
            float total = 0;
            foreach (var e in table) total += Mathf.Max(0, e.weight);
            if (table.Count == 0 || total <= 0) return LootKind.Money;
            double pick = rng.NextDouble() * total;
            foreach (var e in table)
            {
                pick -= Mathf.Max(0, e.weight);
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

        FigurinePedestal FirstIncompleteStatue()
        {
            if (game.figurines == null) return null;
            foreach (var f in game.figurines) if (f && !f.IsComplete && !f.IsBusy) return f;
            return null;
        }

        public void LoadState(SaveData d) { Opened = Mathf.Max(0, d.lootboxesOpened); }
        public void SaveState(SaveData d) { d.lootboxesOpened = Opened; }
    }
}
