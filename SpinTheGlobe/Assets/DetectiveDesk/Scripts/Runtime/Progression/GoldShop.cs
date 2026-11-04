using System;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Save;

namespace DetectiveDesk.Progression
{
    public enum GoldShopCategory
    {
        /// <summary>Opens a feature (statues, Press Coverage).</summary>
        Unlock,
        /// <summary>Bonuses that apply all the time.</summary>
        Permanent,
        /// <summary>Head starts applied at the beginning of every new run (after retiring).</summary>
        FreshStart,
    }

    /// <summary>One item in the Gold shop. Bought with gold; never reset by retiring.</summary>
    public sealed class GoldShopItem
    {
        public string id, title, description;
        public GoldShopCategory category;
        public int maxRank = 1;
        public CostCurve cost;
        public string requires;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
    }

    /// <summary>
    /// Everything the Gold shop sells. Defined in code (like the default skill tree); ids are saved, never rename them.
    /// </summary>
    public static class GoldShopCatalog
    {
        public const string Statue1 = "unlock_statue_1", Statue2 = "unlock_statue_2", Statue3 = "unlock_statue_3";
        public const string PressCoverage = "unlock_press_coverage";
        public const string KeptLedger = "start_ledger", SparePin = "start_pin", WoundClockwork = "start_motor", SeedMoney = "start_money";
        public const string Reputation = "agency_reputation";

        /// <summary>Statue n (0-based) needs this item. Extra pedestals beyond the third share the third unlock.</summary>
        public static string StatueUnlockId(int index) => index <= 0 ? Statue1 : index == 1 ? Statue2 : Statue3;

        /// <summary>Starting money per Seed Money rank (rank 0 = nothing extra).</summary>
        public static readonly double[] SeedMoneyAmounts = { 0, 100, 1_000, 10_000, 100_000, 1_000_000 };
        /// <summary>Clockwork Motor ranks a new run starts with when Wound Clockwork is owned.</summary>
        public const int WoundClockworkRanks = 3;

        static List<GoldShopItem> items;
        public static IReadOnlyList<GoldShopItem> Items => items ??= Create();

        public static GoldShopItem Find(string id)
        {
            foreach (var i in Items) if (i.id == id) return i;
            return null;
        }

        static List<GoldShopItem> Create()
        {
            var list = new List<GoldShopItem>();
            void Add(string id, string title, string description, GoldShopCategory category, int maxRank, double baseCost, double growth,
                     string requires, params StatModifier[] modifiers) =>
                list.Add(new GoldShopItem
                {
                    id = id, title = title, description = description, category = category, maxRank = maxRank,
                    cost = new CostCurve(baseCost, growth), requires = requires, modifiers = modifiers ?? Array.Empty<StatModifier>(),
                });

            // Unlocks
            Add(Statue1, "First Pedestal", "Statue 1 can be built: lootboxes start dropping statue pieces, and a finished statue fills with gold.",
                GoldShopCategory.Unlock, 1, 25, 1, null);
            Add(Statue2, "Second Pedestal", "Statue 2 can be built. It needs more pieces and pays twice the gold.",
                GoldShopCategory.Unlock, 1, 75, 1, Statue1);
            Add(Statue3, "Third Pedestal", "Statue 3 can be built. It needs the most pieces and pays three times the gold.",
                GoldShopCategory.Unlock, 1, 150, 1, Statue2);
            Add(PressCoverage, "Press Coverage", "Approved cases get a 2% chance to also drop gold. Opens Front Page News in the skill tree (up to 10%).",
                GoldShopCategory.Unlock, 1, 40, 1, null, StatModifier.Add(StatId.CaseGoldChance, 0.02), StatModifier.Add(StatId.CaseGold, 5));

            // Permanent
            Add(Reputation, "Agency Reputation", "+10% on every source of income per rank.",
                GoldShopCategory.Permanent, 50, 20, 1.35, null, StatModifier.Percent(StatId.IncomeMultiplier, 0.10));
            Add("golden_meridian", "Golden Meridian", "×1.5 on every source of income.",
                GoldShopCategory.Permanent, 1, 150, 1, Reputation, StatModifier.Multiply(StatId.IncomeMultiplier, 1.5));
            Add("steel_tips", "Steel Tips", "+1 durability for every pin.",
                GoldShopCategory.Permanent, 1, 60, 1, null, StatModifier.Add(StatId.PinDurability, 1));
            Add("twin_barrel", "Twin Barrel", "20% chance a shot fires two pins.",
                GoldShopCategory.Permanent, 1, 120, 1, "steel_tips", StatModifier.Add(StatId.PinDoubleShotChance, 0.2));
            Add("double_box", "Double Box", "15% chance a lootbox gives two rewards.",
                GoldShopCategory.Permanent, 1, 100, 1, null, StatModifier.Add(StatId.LootDoubleChance, 0.15));
            Add("bottomless_glass", "Bottomless Glass", "The whiskey glass refills itself after the cooldown.",
                GoldShopCategory.Permanent, 1, 200, 1, null, StatModifier.Add(StatId.WhiskeyAutoRefill, 1));
            Add("fast_casting", "Fast Casting", "Statues need 2 fewer pieces.",
                GoldShopCategory.Permanent, 1, 250, 1, Statue1, StatModifier.Add(StatId.StatuePieceReduction, 2));

            // Fresh start (applied when a new run begins)
            Add(KeptLedger, "Kept Ledger", "Every new run starts with the skill tree already unlocked.",
                GoldShopCategory.FreshStart, 1, 10, 1, null);
            Add(WoundClockwork, "Wound Clockwork", "Every new run starts with Clockwork Motor at rank " + WoundClockworkRanks + ".",
                GoldShopCategory.FreshStart, 1, 20, 1, KeptLedger);
            Add(SparePin, "Spare Pin", "Every new run starts with Pin 1 bought.",
                GoldShopCategory.FreshStart, 1, 30, 1, KeptLedger);
            Add(SeedMoney, "Seed Money", "Every new run starts with extra money: €100, €1K, €10K, €100K, €1M.",
                GoldShopCategory.FreshStart, SeedMoneyAmounts.Length - 1, 15, 2.6, null);
            return list;
        }
    }

    /// <summary>Owned Gold shop ranks plus the purchase rules. Plain C#: unit-testable and saveable.</summary>
    public sealed class GoldShopState
    {
        readonly Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<GoldShopItem, int> Purchased;

        public int Rank(string id) => id != null && ranks.TryGetValue(id, out var r) ? r : 0;
        public bool Owns(string id) => Rank(id) > 0;

        public double NextCost(GoldShopItem item) => item == null ? double.PositiveInfinity : item.cost.Cost(Rank(item.id));

        public bool IsMaxed(GoldShopItem item) => item != null && Rank(item.id) >= item.maxRank;
        public bool IsLocked(GoldShopItem item) => item != null && !string.IsNullOrEmpty(item.requires) && !Owns(item.requires);

        public bool CanBuy(GoldShopItem item, Wallet wallet, out string reason)
        {
            reason = "";
            if (item == null) { reason = "Unknown item."; return false; }
            if (IsMaxed(item)) { reason = "Owned."; return false; }
            if (IsLocked(item)) { var r = GoldShopCatalog.Find(item.requires); reason = "Requires " + (r != null ? r.title : item.requires) + "."; return false; }
            double cost = NextCost(item);
            if (wallet == null || !wallet.CanAfford(Currency.Gold, cost)) { reason = "Need " + NumberFormatter.Format(cost) + " gold."; return false; }
            return true;
        }

        public bool TryBuy(string id, Wallet wallet, out string reason)
        {
            var item = GoldShopCatalog.Find(id);
            if (!CanBuy(item, wallet, out reason)) return false;
            if (!wallet.TrySpend(Currency.Gold, NextCost(item))) { reason = "Not enough gold."; return false; }
            int rank = Rank(id) + 1;
            ranks[id] = rank;
            Purchased?.Invoke(item, rank);
            EventBus.Publish(new StatsChanged());
            return true;
        }

        /// <summary>Stat modifiers of every owned item, for <see cref="StatSheet.ExtraSource"/>.</summary>
        public IEnumerable<(StatModifier modifier, int rank)> Modifiers()
        {
            foreach (var item in GoldShopCatalog.Items)
            {
                int rank = Rank(item.id);
                if (rank <= 0) continue;
                foreach (var m in item.modifiers) yield return (m, rank);
            }
        }

        public List<SkillRank> ToSave()
        {
            var list = new List<SkillRank>(ranks.Count);
            foreach (var kv in ranks) if (kv.Value > 0) list.Add(new SkillRank { id = kv.Key, rank = kv.Value });
            return list;
        }

        public void Load(IEnumerable<SkillRank> saved)
        {
            ranks.Clear();
            if (saved == null) return;
            foreach (var s in saved)
            {
                var item = GoldShopCatalog.Find(s.id);
                if (item == null || s.rank <= 0) continue;
                ranks[s.id] = Math.Min(s.rank, item.maxRank);
            }
        }
    }

    /// <summary>Retiring the agency (prestige): how much gold a run is worth.</summary>
    public static class Prestige
    {
        /// <summary>gold = floor(scale × (earned ÷ threshold)^exponent), 0 below the threshold.</summary>
        public static double GoldFor(double runEarnings, GameBalance balance)
        {
            if (balance == null || runEarnings < balance.prestigeThreshold || balance.prestigeThreshold <= 0) return 0;
            return Math.Floor(balance.prestigeGoldScale * Math.Pow(runEarnings / balance.prestigeThreshold, balance.prestigeExponent));
        }
    }
}
