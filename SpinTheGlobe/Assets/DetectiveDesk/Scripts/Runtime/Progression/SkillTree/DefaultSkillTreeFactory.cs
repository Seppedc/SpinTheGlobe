using System.Collections.Generic;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Builds the default skill tree: one root ("Skill Tree Unlock") with six branches radiating outward
    /// (Globe, Pins, Case File, Statues, Lootbox, Whiskey). Each branch ends in an endless Mastery node.
    /// The tree is money only: everything bought with gold lives in the Gold shop (<see cref="GoldShopCatalog"/>).
    /// Numbers come from the economy simulation (Docs/ECONOMY.md); tune them in the asset afterwards.
    /// </summary>
    public static class DefaultSkillTreeFactory
    {
        public const string RootId = "root";
        public const string CaseUnlockId = "case_unlock";
        public const int MaxPins = 12;
        public const int MasteryRanks = 999;
        const float Step = 190f;

        public static string PinId(int pin) => $"pin_{pin:00}";               // 1-based for readability
        public static string PinDurabilityId(int pin) => $"pin_{pin:00}_durability";
        public static string PinPayoutId(int pin) => $"pin_{pin:00}_payout";
        // Pin 1 stays cheap; later unlocks are 25% below the doubled curve, before EUR 5 rounding.
        public static double PinPrice(int pin) => pin <= 1 ? 10 : 1.5 * System.Math.Ceiling(15 * System.Math.Pow(1.7, pin - 1));

        public static List<SkillNodeData> Create()
        {
            var nodes = new List<SkillNodeData>
            {
                Node(RootId, "Unlock Upgrades", "Open the skill tree.", SkillBranch.Root,
                    1, Money(10, 1), Vector2.zero, null),
            };
            BuildGlobe(nodes, Direction(90));
            BuildPins(nodes, Direction(30));
            BuildCaseFile(nodes, Direction(-30));
            BuildStatues(nodes, Direction(-90));
            BuildLootbox(nodes, Direction(-150));
            BuildWhiskey(nodes, Direction(150));
            ApplyLayout(nodes);
            return nodes;
        }

        /// <summary>Grid spacing of the layout in tree units (a node is 78 wide, so cells leave clear gaps).</summary>
        public const float Cell = 160f;

        public static List<SkillSectionData> CreateSections() => new List<SkillSectionData>
        {
            new SkillSectionData { id = "globe_section", title = "Globe", icon = "globe", position = new Vector2(0, Cell * 1.5f),
                description = "Spin to earn. Improve speed and keep your momentum.",
                upward = true, connectorOffset = Cell * .75f, branches = new[] { SkillBranch.Globe } },
            new SkillSectionData { id = "pins_section", title = "Pins", icon = "pin", position = new Vector2(Cell * 1.5f, 0),
                description = "Fire pins at the map. Each turn pays money and uses Strength.",
                sharedConnector = false, branches = new[] { SkillBranch.Pins } },
            new SkillSectionData { id = "cases_section", title = "Case Files", icon = "folder", position = new Vector2(-Cell * 1.5f, 0),
                description = "Fallen pins add evidence. Stamp a full case to claim money.",
                sharedConnector = false, branches = new[] { SkillBranch.CaseFile } },
            new SkillSectionData { id = "desk_section", title = "Desk Items", icon = "gift", position = new Vector2(0, -Cell * 1.5f),
                description = "Open boxes. Unlock drinks and statues with Gold.",
                connectorOffset = Cell * .75f, branches = new[] { SkillBranch.Lootbox, SkillBranch.Whiskey, SkillBranch.Statues } },
        };

        /// <summary>
        /// Clean grid layout (cell coordinates; x right, y up). Every node sits at least one cell from every other
        /// node and no link runs through a node (checked with Tools/skilltree_layout.py).
        /// Root in the middle; Globe up, Pins to the right (pin chain with durability above and payout below,
        /// shooter upgrades underneath), Case File left, and Whiskey/Lootbox/Statues grouped below.
        /// </summary>
        static void ApplyLayout(List<SkillNodeData> nodes)
        {
            var at = new Dictionary<string, Vector2>
            {
                [RootId] = new Vector2(0, 0),
                // Globe
                ["globe_auto"] = new Vector2(-1.5f, 3),
                ["globe_max_speed"] = new Vector2(-.5f, 3), ["globe_drag"] = new Vector2(.5f, 3), ["globe_click"] = new Vector2(1.5f, 3),
                ["globe_multiplier"] = new Vector2(-.5f, 4), ["globe_gilded_axis"] = new Vector2(-.5f, 5),
                ["globe_mastery"] = new Vector2(-.5f, 6),
                // Pins: shooter track
                ["shooter_cooldown"] = new Vector2(1.5f, -1.5f), ["pin_autoloader"] = new Vector2(1.5f, -3),
                ["pin_mastery"] = new Vector2(MaxPins + 3, 0),
                // Case file
                [CaseUnlockId] = new Vector2(-3, 0), ["case_payout"] = new Vector2(-4, 0),
                ["case_cooldown"] = new Vector2(-4, 1), ["case_autostamp"] = new Vector2(-3, 1),
                ["case_cold_cases"] = new Vector2(-5, 1), ["case_mastery"] = new Vector2(-6, 1),
                ["case_headlines"] = new Vector2(-5, 2),
                // Whiskey
                ["whiskey_duration"] = new Vector2(-1.5f, -3),
                ["whiskey_cooldown"] = new Vector2(-2, -4.5f), ["whiskey_strength"] = new Vector2(-1, -4.5f),
                ["whiskey_mastery"] = new Vector2(-1, -6),
                // Statues
                ["statue_gold_fill"] = new Vector2(-3, -3), ["statue_payout"] = new Vector2(-3, -4.5f),
                ["statue_mastery"] = new Vector2(-3, -6),
                // Lootbox
                ["loot_bulk"] = new Vector2(0, -3), ["loot_luck"] = new Vector2(0, -4.5f),
                ["loot_mastery"] = new Vector2(0, -6),
            };
            for (int pin = 1; pin <= MaxPins; pin++)
            {
                at[PinId(pin)] = new Vector2(pin + 2, 0);
                at[PinDurabilityId(pin)] = new Vector2(pin + 2, 1);
                at[PinPayoutId(pin)] = new Vector2(pin + 2, -1);
            }
            var byId = new Dictionary<string, SkillNodeData>();
            foreach (var node in nodes) byId[node.id] = node;
            foreach (var kv in at) if (byId.TryGetValue(kv.Key, out var node)) node.position = kv.Value * Cell;
            // Track prerequisites follow the track so its links stay short and never cross the pin rows.
            if (byId.TryGetValue("pin_autoloader", out var loader)) loader.prerequisites = new[] { "shooter_cooldown" };
        }

        // ------------------------------------------------------------------ branches
        static void BuildGlobe(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            // Small motor steps ease the opening: 100 levels of +5 RPM, capped by Spin Limit.
            var auto = Add(n, "globe_auto", "Auto Spin", "Keeps the globe spinning. Limited by Spin Limit.",
                SkillBranch.Globe, 100, AcceleratingSpinPrices(new double[] { 22, 32, 42, 52, 62 }, 100, 10, .1), dir * Step, new[] { RootId }, StatModifier.Add(StatId.GlobeAutoSpeed, 30));
            auto.capStat = StatId.GlobeAutoSpeed; auto.capByStat = StatId.GlobeMaxSpeed;
            // Extra headroom keeps hand dragging useful above the motor's 500 RPM ceiling.
            Add(n, "globe_max_speed", "Spin Limit", "Raise the top spin speed.",
                SkillBranch.Globe, 101, AcceleratingSpinPrices(new double[] { 24, 34, 44, 54, 64 }, 101, 12, .12), dir * Step * 2, new[] { RootId }, StatModifier.Add(StatId.GlobeMaxSpeed, 45));
            Add(n, "globe_drag", "Less Friction", "Lose spin speed 15% slower per level.",
                SkillBranch.Globe, 10, Money(12, 1.8), dir * Step * 1.5f + side * Step, new[] { RootId }, StatModifier.Multiply(StatId.GlobeSpeedDecay, 0.85));
            Add(n, "globe_click", "Stronger Click", "Gain more speed from clicking.",
                SkillBranch.Globe, 5, Money(12, 1.8), dir * Step * 1.5f - side * Step, new[] { RootId }, StatModifier.Percent(StatId.GlobeClickPower, 0.2));
            Add(n, "globe_multiplier", "Spin Pay", "Earn more from spins.",
                SkillBranch.Globe, 25, SpinPayPrices(), dir * Step * 3, new[] { "globe_max_speed" }, StatModifier.Percent(StatId.GlobeMultiplier, 0.25));
            Add(n, "globe_gilded_axis", "Double Spin Pay", "Double spin earnings.",
                SkillBranch.Globe, 1, Money(5000, 1), dir * Step * 4, new[] { "globe_multiplier" }, StatModifier.Multiply(StatId.GlobeMultiplier, 2));
            Add(n, "globe_mastery", "Spin Bonus", "Earn 5% more from spins.",
                SkillBranch.Globe, MasteryRanks, Money(2e6, 1.25), dir * Step * 5, new[] { "globe_gilded_axis" }, StatModifier.Multiply(StatId.GlobeMultiplier, 1.05));
        }

        static void BuildPins(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            string previous = RootId;
            for (int pin = 1; pin <= MaxPins; pin++)
            {
                int index = pin - 1;
                double price = PinPrice(pin);
                Vector2 at = dir * Step * pin;
                Add(n, PinId(pin), $"Pin {pin}", $"Unlock pin {pin} for the shooter.",
                    SkillBranch.Pins, 1, Money(price, 1), at, new[] { previous }, StatModifier.Add(StatId.PinSlots, 1));
                Add(n, PinDurabilityId(pin), $"Pin {pin} · Strength", "Lasts one extra turn.",
                    SkillBranch.Pins, 20, PinUpgradePrices(index, true), at + side * Step * 0.55f, new[] { PinId(pin) }, StatModifier.Add(StatId.PinDurability, 1, index));
                Add(n, PinPayoutId(pin), $"Pin {pin} · Pay", "Earn more from this pin.",
                    SkillBranch.Pins, 20, PinUpgradePrices(index), at - side * Step * 0.55f, new[] { PinId(pin) }, StatModifier.Percent(StatId.PinPayout, 0.5, index));
                previous = PinId(pin);
            }
            // Shooter upgrades run on a parallel track beside the pin chain.
            Vector2 track = side * Step * 1.45f;
            // Multiplicative reductions keep every rank useful beyond the percentage-stat floor.
            var reload = Add(n, "shooter_cooldown", "Faster Reload", "Reload 12% faster each level.",
                SkillBranch.Pins, 40, ReloadPrices(), dir * Step * 1.5f + track, new[] { RootId, PinId(1) }, StatModifier.Multiply(StatId.ShooterCooldown, 0.88));
            reload.cost = ShooterDiscount(reload.cost);
            var autoFire = Add(n, "pin_autoloader", "Auto Fire", "Shoot ready pins automatically.",
                SkillBranch.Pins, 1, Money(350, 1), dir * Step * 4.5f + track, new[] { "shooter_cooldown" }, StatModifier.Add(StatId.PinAutoFire, 1));
            autoFire.cost = ShooterDiscount(autoFire.cost);
            Add(n, "pin_mastery", "All Pin Pay", "Multiply every pin payout by 1.05.",
                SkillBranch.Pins, MasteryRanks, Money(5e6, 1.25), dir * Step * (MaxPins + 1), new[] { PinId(MaxPins) }, StatModifier.Multiply(StatId.PinPayout, 1.05));
        }

        static void BuildCaseFile(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, CaseUnlockId, "Case Files", "Turn fallen pins into evidence. Solve cases for money.",
                SkillBranch.CaseFile, 1, Money(30, 1), dir * Step, new[] { RootId, PinId(1) }, StatModifier.Add(StatId.CaseUnlocked, 1));
            Add(n, "case_payout", "Case Pay", "Earn more from solved cases.",
                SkillBranch.CaseFile, 10, Money(60, 1.5), dir * Step * 2, new[] { CaseUnlockId }, StatModifier.Percent(StatId.CasePayout, 0.25));
            Add(n, "case_cooldown", "Case Wait", "Open the next case sooner.",
                SkillBranch.CaseFile, 10, Money(60, 1.5), dir * Step * 2 + side * Step * 0.6f, new[] { CaseUnlockId }, StatModifier.Percent(StatId.CaseCooldown, -0.07));
            Add(n, "case_autostamp", "Auto Stamp", "Chance to stamp a full case automatically.",
                SkillBranch.CaseFile, 10, Money(150, 1.5), dir * Step * 2 - side * Step * 0.6f, new[] { CaseUnlockId }, StatModifier.Add(StatId.CaseAutoStampChance, 0.05));
            Add(n, "case_cold_cases", "Less Evidence", "Solve each case with fewer clues.",
                SkillBranch.CaseFile, 3, Money(500, 2), dir * Step * 3 + side * Step * 0.6f, new[] { "case_cooldown" }, StatModifier.Add(StatId.CaseEvidenceReduction, 1));
            Add(n, "case_headlines", "Case Gold", "More chance of gold from solved cases. Max 10%.",
                SkillBranch.CaseFile, 8, Money(500, 1.6), dir * Step * 3 - side * Step * 0.6f, new[] { "case_autostamp" }, StatModifier.Add(StatId.CaseGoldChance, 0.01)).requiresUnlock = GoldShopCatalog.PressCoverage;
            Add(n, "case_mastery", "Case Pay Bonus", "Multiply case rewards by 1.05.",
                SkillBranch.CaseFile, MasteryRanks, Money(1e6, 1.25), dir * Step * 4, new[] { "case_cold_cases" }, StatModifier.Multiply(StatId.CasePayout, 1.05));
        }

        static void BuildStatues(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "statue_gold_fill", "Faster Gold Fill", "Statues fill with gold faster.",
                SkillBranch.Statues, 10, Money(150, 1.6), dir * Step, new[] { RootId }, StatModifier.Percent(StatId.StatueGoldFillRate, 0.2)).requiresUnlock = GoldShopCatalog.Statue1;
            Add(n, "statue_payout", "Statue Gold", "Earn more gold when smashing statues.",
                SkillBranch.Statues, 10, Money(250, 1.6), dir * Step * 2, new[] { "statue_gold_fill" }, StatModifier.Percent(StatId.StatuePayout, 0.25)).requiresUnlock = GoldShopCatalog.Statue1;
            Add(n, "statue_mastery", "Statue Gold Bonus", "Multiply statue gold rewards by 1.05.",
                SkillBranch.Statues, MasteryRanks, Money(1e6, 1.25), dir * Step * 3, new[] { "statue_payout" }, StatModifier.Multiply(StatId.StatuePayout, 1.05)).requiresUnlock = GoldShopCatalog.Statue1;
        }

        static void BuildLootbox(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "loot_bulk", "Cheaper Boxes", "Pay less for each lootbox.",
                SkillBranch.Lootbox, 5, Money(200, 1.7), dir * Step, new[] { RootId }, StatModifier.Percent(StatId.LootboxPrice, -0.06));
            Add(n, "loot_luck", "Better Loot", "Better odds of gold and statue pieces.",
                SkillBranch.Lootbox, 5, Money(350, 1.7), dir * Step * 2, new[] { "loot_bulk" }, StatModifier.Add(StatId.LootLuck, 0.2));
            Add(n, "loot_mastery", "More Loot Luck", "Further improve gold and statue piece odds.",
                SkillBranch.Lootbox, MasteryRanks, Money(1e6, 1.25), dir * Step * 3, new[] { "loot_luck" }, StatModifier.Add(StatId.LootLuck, 0.05));
        }

        static void BuildWhiskey(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "whiskey_duration", "Longer Boost", "Keep the drink boost active longer.",
                SkillBranch.Whiskey, 10, Money(70, 1.45), dir * Step, new[] { RootId }, StatModifier.Add(StatId.WhiskeyBoostDuration, 2)).requiresUnlock = GoldShopCatalog.Whiskey;
            Add(n, "whiskey_cooldown", "Drink Wait", "Use the glass again sooner.",
                SkillBranch.Whiskey, 10, Money(85, 1.45), dir * Step * 2 + side * Step * 0.6f, new[] { "whiskey_duration" }, StatModifier.Percent(StatId.WhiskeyCooldown, -0.06)).requiresUnlock = GoldShopCatalog.Whiskey;
            Add(n, "whiskey_strength", "Stronger Boost", "Boost spin speed and earnings further.",
                SkillBranch.Whiskey, 5, Money(300, 1.8), dir * Step * 2 - side * Step * 0.6f, new[] { "whiskey_duration" }, StatModifier.Add(StatId.WhiskeyBoostStrength, 0.25)).requiresUnlock = GoldShopCatalog.Whiskey;
            Add(n, "whiskey_mastery", "Extra Boost Time", "Add more time to the drink boost.",
                SkillBranch.Whiskey, MasteryRanks, Money(1e6, 1.25), dir * Step * 3 - side * Step * 0.6f, new[] { "whiskey_strength" }, StatModifier.Add(StatId.WhiskeyBoostDuration, 1)).requiresUnlock = GoldShopCatalog.Whiskey;
        }

        // ------------------------------------------------------------------ helpers
        static CostCurve SpinPayPrices()
        {
            var prices = new double[25];
            prices[0] = 50;
            prices[1] = 100;
            for (int rank = 2; rank < prices.Length; rank++)
                prices[rank] = System.Math.Ceiling(200 * System.Math.Pow(1.4, rank - 2) - 1e-8);
            return new CostCurve(50, 1.4) { explicitCosts = prices };
        }

        // Retain the opening purchases, then increase the absolute price jumps smoothly.
        static CostCurve AcceleratingSpinPrices(double[] opening, int ranks, double linear, double cubic)
        {
            var prices = new double[ranks];
            System.Array.Copy(opening, prices, opening.Length);
            for (int rank = opening.Length; rank < prices.Length; rank++)
            {
                double tier = rank - opening.Length + 1;
                prices[rank] = System.Math.Ceiling(opening[opening.Length - 1]
                    + linear * tier + cubic * tier * tier * tier - 1e-8);
            }
            return new CostCurve(opening[0], 1) { explicitCosts = prices };
        }

        static CostCurve PinUpgradePrices(int index, bool strength = false)
        {
            double first = 15 * System.Math.Pow(1.5, index);
            double earlyGrowth = strength ? 1.8 : 1.6;
            double lateGrowth = strength ? 1.25 : 1.15;
            var prices = new double[20];
            for (int rank = 0; rank < prices.Length; rank++)
                prices[rank] = System.Math.Ceiling(first * System.Math.Pow(earlyGrowth, System.Math.Min(rank, 9))
                    * System.Math.Pow(lateGrowth, System.Math.Max(0, rank - 9)));
            return new CostCurve(first, earlyGrowth) { explicitCosts = prices };
        }

        static CostCurve ReloadPrices()
        {
            var prices = new double[40];
            double[] opening = { 10, 15, 23, 34, 51, 76 };
            System.Array.Copy(opening, prices, opening.Length);
            for (int rank = opening.Length; rank < prices.Length; rank++)
                prices[rank] = System.Math.Ceiling(76 * System.Math.Pow(1.15, rank - 5));
            return new CostCurve(10, 1.15) { explicitCosts = prices };
        }

        static CostCurve ShooterDiscount(CostCurve cost)
        {
            cost.baseCost *= .9;
            if (cost.explicitCosts != null)
                for (int rank = 0; rank < cost.explicitCosts.Length; rank++)
                    cost.explicitCosts[rank] = System.Math.Ceiling(cost.explicitCosts[rank] * .9);
            return cost;
        }

        static CostCurve Money(double baseCost, double growth) => new CostCurve(baseCost, growth);

        static SkillNodeData Add(List<SkillNodeData> n, string id, string title, string description, SkillBranch branch, int maxRank,
            CostCurve cost, Vector2 position, string[] prerequisites, params StatModifier[] modifiers)
        {
            var node = Node(id, title, description, branch, maxRank, cost, position, prerequisites, modifiers);
            n.Add(node);
            return node;
        }

        static SkillNodeData Node(string id, string title, string description, SkillBranch branch, int maxRank,
            CostCurve cost, Vector2 position, string[] prerequisites, params StatModifier[] modifiers)
        {
            cost.roundingStep = 5;
            // Apply the general money-price reduction before whole-euro and EUR 5 rounding.
            cost.baseCost *= .95;
            if (cost.explicitCosts != null)
                for (int rank = 0; rank < cost.explicitCosts.Length; rank++)
                    cost.explicitCosts[rank] = System.Math.Ceiling(cost.explicitCosts[rank] * .95);
            return new SkillNodeData
            {
                id = id,
                title = title,
                description = description,
                branch = branch,
                maxRank = maxRank,
                currency = Currency.Money,
                cost = cost,
                position = position,
                prerequisites = prerequisites ?? System.Array.Empty<string>(),
                modifiers = modifiers ?? System.Array.Empty<StatModifier>(),
            };
        }

        static Vector2 Direction(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x);
    }
}
