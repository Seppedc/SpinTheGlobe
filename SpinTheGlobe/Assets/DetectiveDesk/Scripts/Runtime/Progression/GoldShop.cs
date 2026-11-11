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
    [Serializable]
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
    /// Defaults for the Gold shop. Active tuning lives in GameBalance; ids are saved, never rename them.
    /// </summary>
    public static class GoldShopCatalog
    {
        public const string Statue1 = "unlock_statue_1", Statue2 = "unlock_statue_2", Statue3 = "unlock_statue_3";
        public const string PressCoverage = "unlock_press_coverage";
        public const string Whiskey = "unlock_whiskey";
        public const string KeptLedger = "start_ledger", SparePin = "start_pin", WoundClockwork = "start_motor", SeedMoney = "start_money";
        public const string StartCases = "start_cases", StartSpinLimit = "start_spin_limit", StatueGoldChance = "statue_gold_chance";
        public const string Reputation = "agency_reputation";

        // Preserve the first shipped unlock id: this purchase now opens every pedestal.
        public static string StatueUnlockId(int index) => Statue1;
        public const int WoundClockworkRanks = 1;
        public const int StartingSpinLimitRanks = 1;

        static List<GoldShopItem> items;
        public static IReadOnlyList<GoldShopItem> Items => items ??= CreateDefaults();

        public static GoldShopItem Find(string id)
        {
            foreach (var i in Items) if (i.id == id) return i;
            return null;
        }

        public static List<GoldShopItem> CreateDefaults()
        {
            var list = new List<GoldShopItem>();
            void Add(string id, string title, string description, GoldShopCategory category, int maxRank, double baseCost, double growth,
                     string requires, params StatModifier[] modifiers) =>
                list.Add(new GoldShopItem
                {
                    id = id, title = title, description = description, category = category, maxRank = maxRank,
                    cost = new CostCurve(baseCost, growth), requires = requires, modifiers = modifiers ?? Array.Empty<StatModifier>(),
                });

            // Fresh starts are independent choices after keeping the skill tree.
            Add(KeptLedger, "Keep the Skill Tree", "Unlock the skill tree now and in every future run.",
                GoldShopCategory.FreshStart, 1, 10, 1, null);
            Add(SparePin, "Keep Pin 1", "Unlock Pin 1 now and in every future run.",
                GoldShopCategory.FreshStart, 1, 30, 1, KeptLedger);
            Add(StartCases, "Keep Case Files", "Unlock Case Files now and in every future run.",
                GoldShopCategory.FreshStart, 1, 30, 1, KeptLedger);
            Add(WoundClockwork, "Start Auto Spin", "Get Auto Spin level 1 now and in every future run.",
                GoldShopCategory.FreshStart, 1, 20, 1, KeptLedger);
            Add(StartSpinLimit, "Start Spin Limit", "Get Spin Limit level 1 now and in every future run.",
                GoldShopCategory.FreshStart, 1, 25, 1, KeptLedger);

            // Unlocks: one purchase opens all statues.
            Add(Statue1, "Unlock Statues", "Unlock all statues and statue pieces in lootboxes.",
                GoldShopCategory.Unlock, 1, 10, 1, null);
            Add(Whiskey, "Unlock Whiskey", "Unlock the glass, bottle and drink upgrades.",
                GoldShopCategory.Unlock, 1, 5, 1, null);
            Add(StatueGoldChance, "More Statue Gold", "+10% chance of double Gold from any statue per level.",
                GoldShopCategory.Unlock, 10, 20, 1, Statue1,
                StatModifier.Add(StatId.StatueBonusGoldChance, 0.10));
            list[list.Count - 1].cost.explicitCosts = new double[] { 20, 25, 30, 35, 40, 45, 50, 55, 60, 65 };
            Add(PressCoverage, "Press Coverage", "Cases gain a 2% chance to drop Gold. Unlocks more chance upgrades.",
                GoldShopCategory.Unlock, 1, 30, 1, null, StatModifier.Add(StatId.CaseGoldChance, 0.02), StatModifier.Add(StatId.CaseGold, 5));

            // Permanent
            Add(Reputation, "Agency Reputation", "+5% global money income per level. Adds to your prestige bonus.",
                GoldShopCategory.Permanent, 50, 5, 1, null, StatModifier.Percent(StatId.IncomeMultiplier, 0.05));
            var reputationPrices = new double[list[list.Count - 1].maxRank];
            for (int rank = 0; rank < reputationPrices.Length; rank++) reputationPrices[rank] = 5 * (rank + 1);
            list[list.Count - 1].cost.explicitCosts = reputationPrices;
            Add("steel_tips", "Steel Tips", "+1 durability for every pin.",
                GoldShopCategory.Permanent, 1, 50, 1, null, StatModifier.Add(StatId.PinDurability, 1));
            Add("twin_barrel", "Twin Barrel", "20% chance a shot fires two pins.",
                GoldShopCategory.Permanent, 1, 50, 1, null, StatModifier.Add(StatId.PinDoubleShotChance, 0.2));
            Add("double_box", "Double Box", "15% chance a lootbox gives two rewards.",
                GoldShopCategory.Permanent, 1, 50, 1, null, StatModifier.Add(StatId.LootDoubleChance, 0.15));
            Add("bottomless_glass", "Bottomless Glass", "The whiskey glass refills itself after the cooldown.",
                GoldShopCategory.Permanent, 1, 50, 1, Whiskey, StatModifier.Add(StatId.WhiskeyAutoRefill, 1));
            Add("fast_casting", "Fast Casting", "Statues need 1 fewer piece per level. Two levels.",
                GoldShopCategory.Permanent, 2, 50, 1.5, Statue1, StatModifier.Add(StatId.StatuePieceReduction, 1));
            return list;
        }
    }

    /// <summary>Owned Gold shop ranks plus the purchase rules. Plain C#: unit-testable and saveable.</summary>
    public sealed class GoldShopState
    {
        public GameBalance Balance { get; set; }
        public IReadOnlyList<GoldShopItem> Items => Balance != null && Balance.goldShopItems != null && Balance.goldShopItems.Count > 0
            ? Balance.goldShopItems : GoldShopCatalog.Items;
        public GoldShopItem Find(string id)
        {
            foreach (var item in Items) if (item.id == id) return item;
            return null;
        }

        public List<SkillRank> FreshStartSkills()
        {
            var result = new List<SkillRank>();
            if (!Owns(GoldShopCatalog.KeptLedger)) return result;
            result.Add(new SkillRank { id = DefaultSkillTreeFactory.RootId, rank = 1 });
            if (Owns(GoldShopCatalog.SparePin)) result.Add(new SkillRank { id = DefaultSkillTreeFactory.PinId(1), rank = 1 });
            if (Owns(GoldShopCatalog.StartCases)) result.Add(new SkillRank { id = DefaultSkillTreeFactory.CaseUnlockId, rank = 1 });
            if (Owns(GoldShopCatalog.WoundClockwork)) result.Add(new SkillRank { id = "globe_auto", rank = GoldShopCatalog.WoundClockworkRanks });
            if (Owns(GoldShopCatalog.StartSpinLimit)) result.Add(new SkillRank { id = "globe_max_speed", rank = GoldShopCatalog.StartingSpinLimitRanks });
            return result;
        }
        readonly Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<GoldShopItem, int> Purchased;

        public int Rank(string id) => id != null && ranks.TryGetValue(id, out var r) ? r : 0;
        public bool Owns(string id) => Rank(id) > 0;

        public double NextCost(GoldShopItem item) => item == null ? double.PositiveInfinity :
            item.id == GoldShopCatalog.Whiskey && Balance != null
                ? Math.Ceiling(Math.Max(1, Balance.whiskeyUnlockGoldCost)) : item.cost.Cost(Rank(item.id));

        public bool IsMaxed(GoldShopItem item) => item != null && Rank(item.id) >= item.maxRank;
        public bool IsLocked(GoldShopItem item) => item != null && !string.IsNullOrEmpty(item.requires) && !Owns(item.requires);

        public bool CanBuy(GoldShopItem item, Wallet wallet, out string reason)
        {
            reason = "";
            if (item == null) { reason = "Unknown item."; return false; }
            if (IsMaxed(item)) { reason = "Owned."; return false; }
            if (IsLocked(item)) { var r = Find(item.requires); reason = "Requires " + (r != null ? r.title : item.requires) + "."; return false; }
            double cost = NextCost(item);
            if (wallet == null || !wallet.CanAfford(Currency.Gold, cost)) { reason = "Need " + NumberFormatter.Format(cost) + " gold."; return false; }
            return true;
        }

        public bool TryBuy(string id, Wallet wallet, out string reason)
        {
            var item = Find(id);
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
            foreach (var item in Items)
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
                var item = Find(s.id);
                if (item == null || s.rank <= 0) continue;
                ranks[s.id] = Math.Min(s.rank, item.maxRank);
            }
        }
    }

    /// <summary>Retiring the agency (prestige): how much gold a run is worth.</summary>
    public static class Prestige
    {
        public static double IncomeBonusFor(int retirements, GameBalance balance) => balance == null ? 0
            : Math.Clamp(retirements, 0, Math.Max(0, balance.maxPrestiges)) * Math.Max(0, balance.prestigeIncomeBonusPerRetirement);

        /// <summary>Permanent global money multiplier for a run; skill upgrades and temporary boosts are excluded.</summary>
        public static double IncomeMultiplierFor(int retirements, GameBalance balance, GoldShopState shop)
        {
            var sheet = new StatSheet();
            balance?.ApplyTo(sheet);
            sheet.ExtraSource = Modifiers;
            sheet.Rebuild(null, null);
            return sheet.Permanent(StatId.IncomeMultiplier);

            IEnumerable<(StatModifier modifier, int rank)> Modifiers()
            {
                if (shop != null) foreach (var modifier in shop.Modifiers()) yield return modifier;
                foreach (var modifier in DetectiveRanks.Modifiers(balance, retirements)) yield return modifier;
            }
        }

        /// <summary>gold = floor(scale × (earned ÷ threshold)^exponent), 0 below the threshold.</summary>
        public static bool IsFinalRank(int retirements, GameBalance balance) => balance != null && retirements >= Math.Max(0, balance.maxPrestiges);

        public static double RequirementFor(int retirements, GameBalance balance)
        {
            if (balance == null) return double.PositiveInfinity;
            return IsFinalRank(retirements, balance) ? balance.finalRunGoal
                : balance.prestigeThreshold + Math.Max(0, retirements) * Math.Max(0, balance.prestigeRequirementIncrease);
        }

        public static bool HasCompleted(int retirements, double earnings, GameBalance balance) =>
            IsFinalRank(retirements, balance) && earnings >= RequirementFor(retirements, balance);

        public static double GoldFor(double runEarnings, GameBalance balance, int retirements = 0)
        {
            if (balance == null || IsFinalRank(retirements, balance) || runEarnings < RequirementFor(retirements, balance) || balance.prestigeThreshold <= 0) return 0;
            return Math.Floor(balance.prestigeGoldScale * Math.Pow(runEarnings / balance.prestigeThreshold, balance.prestigeExponent));
        }
    }
}
