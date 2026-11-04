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
        public const int MaxPins = 12;
        public const int MasteryRanks = 999;
        const float Step = 190f;

        public static string PinId(int pin) => $"pin_{pin:00}";               // 1-based for readability
        public static string PinDurabilityId(int pin) => $"pin_{pin:00}_durability";
        public static string PinPayoutId(int pin) => $"pin_{pin:00}_payout";
        // Pin 1 is cheap so the first shot comes early; from pin 2 on: €20 × 3.1^(n-1).
        public static double PinPrice(int pin) => pin <= 1 ? 10 : 20 * System.Math.Pow(3.1, pin - 1);

        public static List<SkillNodeData> Create()
        {
            var nodes = new List<SkillNodeData>
            {
                Node(RootId, "Skill Tree Unlock", "Open the detective's ledger of upgrades.", SkillBranch.Root,
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

        /// <summary>
        /// Clean grid layout (cell coordinates; x right, y up). Every node sits at least one cell from every other
        /// node and no link runs through a node (checked with Tools/skilltree_layout.py).
        /// Root in the middle; Globe up, Pins to the right (pin chain with durability above and payout below,
        /// shooter upgrades on a track underneath), Case File left, Whiskey up-left, Lootbox down-left, Statues down.
        /// </summary>
        static void ApplyLayout(List<SkillNodeData> nodes)
        {
            var at = new Dictionary<string, Vector2>
            {
                [RootId] = new Vector2(0, 0),
                // Globe
                ["globe_auto"] = new Vector2(0, 1),
                ["globe_drag"] = new Vector2(-1, 2), ["globe_max_speed"] = new Vector2(0, 2), ["globe_click"] = new Vector2(1, 2),
                ["globe_multiplier"] = new Vector2(0, 3), ["globe_gilded_axis"] = new Vector2(0, 4),
                ["globe_mastery"] = new Vector2(0, 5),
                // Pins: shooter track
                ["shooter_cooldown"] = new Vector2(3, -2), ["pin_autoloader"] = new Vector2(3, -3),
                ["pin_land_chance"] = new Vector2(4, -2), ["pin_city_chance"] = new Vector2(5, -2),
                ["pin_mastery"] = new Vector2(MaxPins + 2, 0),
                // Case file
                ["case_payout"] = new Vector2(-1, 0),
                ["case_cooldown"] = new Vector2(-2, 0.5f), ["case_autostamp"] = new Vector2(-2, -0.5f),
                ["case_cold_cases"] = new Vector2(-3, 0.5f), ["case_mastery"] = new Vector2(-4, 0.5f),
                ["case_headlines"] = new Vector2(-3, -0.5f),
                // Whiskey
                ["whiskey_duration"] = new Vector2(-2, 2),
                ["whiskey_cooldown"] = new Vector2(-3, 2.5f), ["whiskey_strength"] = new Vector2(-3, 1.5f),
                ["whiskey_mastery"] = new Vector2(-4, 1.5f),
                // Statues
                ["statue_gold_fill"] = new Vector2(0, -1), ["statue_payout"] = new Vector2(0, -2),
                ["statue_mastery"] = new Vector2(0, -3),
                // Lootbox
                ["loot_bulk"] = new Vector2(-2, -2), ["loot_luck"] = new Vector2(-3, -2),
                ["loot_mastery"] = new Vector2(-4, -2),
            };
            for (int pin = 1; pin <= MaxPins; pin++)
            {
                at[PinId(pin)] = new Vector2(pin + 1, 0);
                at[PinDurabilityId(pin)] = new Vector2(pin + 1, 1);
                at[PinPayoutId(pin)] = new Vector2(pin + 1, -1);
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
            // Clockwork Motor comes first: it is meant to be the player's first purchase, so the globe keeps
            // turning (and earning) without constant clicking. It is capped by the maximum spin speed.
            var auto = Add(n, "globe_auto", "Clockwork Motor", "+2 RPM auto-rotate: the globe keeps turning by itself. Never faster than the maximum spin speed.",
                SkillBranch.Globe, 20, Money(8, 1.6), dir * Step, new[] { RootId }, StatModifier.Add(StatId.GlobeAutoSpeed, 12));
            Add(n, "globe_max_speed", "Greased Bearings", "+2 RPM maximum spin speed. Caps dragging and auto-rotate.",
                SkillBranch.Globe, 20, Money(10, 1.42), dir * Step * 2, new[] { "globe_auto" }, StatModifier.Add(StatId.GlobeMaxSpeed, 12));
            auto.capStat = StatId.GlobeAutoSpeed; auto.capByStat = StatId.GlobeMaxSpeed;
            Add(n, "globe_drag", "Firm Grip", "+30% speed gained from dragging. (Dragging always beats clicking at the same upgrade level.)",
                SkillBranch.Globe, 5, Money(20, 1.8), dir * Step * 1.5f + side * Step, new[] { "globe_max_speed" }, StatModifier.Percent(StatId.GlobeDragPower, 0.3));
            Add(n, "globe_click", "Flick of the Wrist", "+20% speed gained from clicking.",
                SkillBranch.Globe, 5, Money(20, 1.8), dir * Step * 1.5f - side * Step, new[] { "globe_max_speed" }, StatModifier.Percent(StatId.GlobeClickPower, 0.2));
            Add(n, "globe_multiplier", "Cartographer's Cut", "+25% globe earnings (turns and pins).",
                SkillBranch.Globe, 25, Money(150, 1.42), dir * Step * 3, new[] { "globe_max_speed" }, StatModifier.Percent(StatId.GlobeMultiplier, 0.25));
            Add(n, "globe_gilded_axis", "Gilded Axis", "Doubles all globe earnings.",
                SkillBranch.Globe, 1, Money(5e6, 1), dir * Step * 4, new[] { "globe_multiplier" }, StatModifier.Multiply(StatId.GlobeMultiplier, 2));
            Add(n, "globe_mastery", "Globe Mastery", "+5% globe earnings per rank, compounding. Never maxes out.",
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
                Add(n, PinId(pin), $"Pin {pin}", $"Adds pin #{pin} to the pin box.",
                    SkillBranch.Pins, 1, Money(price, 1), at, new[] { previous }, StatModifier.Add(StatId.PinSlots, 1));
                Add(n, PinDurabilityId(pin), $"Pin {pin} · Durability", $"Pin #{pin} survives one more pass under the payout line.",
                    SkillBranch.Pins, 5, Money(price * 1.5, 1.6), at + side * Step * 0.55f, new[] { PinId(pin) }, StatModifier.Add(StatId.PinDurability, 1, index));
                Add(n, PinPayoutId(pin), $"Pin {pin} · Payout", $"+50% payout from pin #{pin}.",
                    SkillBranch.Pins, 10, Money(price * 1.25, 1.45), at - side * Step * 0.55f, new[] { PinId(pin) }, StatModifier.Percent(StatId.PinPayout, 0.5, index));
                previous = PinId(pin);
            }
            // Shooter and landing upgrades run on a parallel track beside the pin chain.
            Vector2 track = side * Step * 1.45f;
            // 15 ranks × -6% = -90% at most (above -95% the stat floor would make the last ranks useless).
            Add(n, "shooter_cooldown", "Oiled Spring", "-6% pin shooter reload time.",
                SkillBranch.Pins, 15, Money(25, 1.45), dir * Step * 1.5f + track, new[] { PinId(1) }, StatModifier.Percent(StatId.ShooterCooldown, -0.06));
            Add(n, "pin_land_chance", "Steady Aim", "+8% chance a pin lands on land (land pays ×2).",
                SkillBranch.Pins, 5, Money(80, 1.9), dir * Step * 2.5f + track, new[] { PinId(2) }, StatModifier.Add(StatId.PinLandChance, 0.08));
            Add(n, "pin_city_chance", "Big City Lights", "+5% chance a pin lands near a big city (up to ×6).",
                SkillBranch.Pins, 5, Money(200, 2.0), dir * Step * 3.5f + track, new[] { "pin_land_chance" }, StatModifier.Add(StatId.PinCityChance, 0.05));
            Add(n, "pin_autoloader", "Auto-loader", "The shooter fires a pin by itself whenever one is ready.",
                SkillBranch.Pins, 1, Money(50000, 1), dir * Step * 4.5f + track, new[] { "shooter_cooldown" }, StatModifier.Add(StatId.PinAutoFire, 1));
            Add(n, "pin_mastery", "Pin Mastery", "+5% payout for every pin per rank, compounding. Never maxes out.",
                SkillBranch.Pins, MasteryRanks, Money(5e6, 1.25), dir * Step * (MaxPins + 1), new[] { PinId(MaxPins) }, StatModifier.Multiply(StatId.PinPayout, 1.05));
        }

        static void BuildCaseFile(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "case_payout", "Bigger Retainers", "+25% money from approved cases.",
                SkillBranch.CaseFile, 10, Money(80, 1.7), dir * Step, new[] { RootId }, StatModifier.Percent(StatId.CasePayout, 0.25));
            Add(n, "case_cooldown", "Efficient Paperwork", "-7% wait before the next case opens.",
                SkillBranch.CaseFile, 10, Money(100, 1.7), dir * Step * 2 + side * Step * 0.6f, new[] { "case_payout" }, StatModifier.Percent(StatId.CaseCooldown, -0.07));
            Add(n, "case_autostamp", "Rubber-Stamp Clerk", "+5% chance a full case stamps itself.",
                SkillBranch.CaseFile, 10, Money(250, 1.7), dir * Step * 2 - side * Step * 0.6f, new[] { "case_payout" }, StatModifier.Add(StatId.CaseAutoStampChance, 0.05));
            Add(n, "case_cold_cases", "Cold Cases", "Every case needs one less piece of evidence.",
                SkillBranch.CaseFile, 3, Money(1000, 3), dir * Step * 3 + side * Step * 0.6f, new[] { "case_cooldown" }, StatModifier.Add(StatId.CaseEvidenceReduction, 1));
            Add(n, "case_headlines", "Front Page News", "+1% chance an approved case drops gold (needs Press Coverage from the Gold shop). Never above 10%.",
                SkillBranch.CaseFile, 8, Money(5000, 1.9), dir * Step * 3 - side * Step * 0.6f, new[] { "case_autostamp" }, StatModifier.Add(StatId.CaseGoldChance, 0.01)).requiresUnlock = GoldShopCatalog.PressCoverage;
            Add(n, "case_mastery", "Case Mastery", "+5% case rewards per rank, compounding. Never maxes out.",
                SkillBranch.CaseFile, MasteryRanks, Money(1e6, 1.25), dir * Step * 4, new[] { "case_cold_cases" }, StatModifier.Multiply(StatId.CasePayout, 1.05));
        }

        static void BuildStatues(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "statue_gold_fill", "Molten Pour", "+20% gold fill speed from globe spins.",
                SkillBranch.Statues, 10, Money(150, 1.6), dir * Step, new[] { RootId }, StatModifier.Percent(StatId.StatueGoldFillRate, 0.2)).requiresUnlock = GoldShopCatalog.Statue1;
            Add(n, "statue_payout", "Heavier Gold", "+25% gold from smashed statues.",
                SkillBranch.Statues, 10, Money(250, 1.6), dir * Step * 2, new[] { "statue_gold_fill" }, StatModifier.Percent(StatId.StatuePayout, 0.25)).requiresUnlock = GoldShopCatalog.Statue1;
            Add(n, "statue_mastery", "Statue Mastery", "+5% statue gold per rank, compounding. Never maxes out.",
                SkillBranch.Statues, MasteryRanks, Money(1e6, 1.25), dir * Step * 3, new[] { "statue_payout" }, StatModifier.Multiply(StatId.StatuePayout, 1.05)).requiresUnlock = GoldShopCatalog.Statue1;
        }

        static void BuildLootbox(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "loot_bulk", "Bulk Order", "-6% lootbox price.",
                SkillBranch.Lootbox, 5, Money(300, 2), dir * Step, new[] { RootId }, StatModifier.Percent(StatId.LootboxPrice, -0.06));
            Add(n, "loot_luck", "Lucky Charm", "+20% odds for statue pieces and gold.",
                SkillBranch.Lootbox, 5, Money(500, 2), dir * Step * 2, new[] { "loot_bulk" }, StatModifier.Add(StatId.LootLuck, 0.2));
            Add(n, "loot_mastery", "Lootbox Mastery", "+5% lootbox luck per rank. Never maxes out.",
                SkillBranch.Lootbox, MasteryRanks, Money(1e6, 1.25), dir * Step * 3, new[] { "loot_luck" }, StatModifier.Add(StatId.LootLuck, 0.05));
        }

        static void BuildWhiskey(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            Add(n, "whiskey_duration", "Slow Sipper", "+2 s boost duration.",
                SkillBranch.Whiskey, 10, Money(100, 1.55), dir * Step, new[] { RootId }, StatModifier.Add(StatId.WhiskeyBoostDuration, 2));
            Add(n, "whiskey_cooldown", "Quick Refill", "-6% whiskey cooldown.",
                SkillBranch.Whiskey, 10, Money(120, 1.55), dir * Step * 2 + side * Step * 0.6f, new[] { "whiskey_duration" }, StatModifier.Percent(StatId.WhiskeyCooldown, -0.06));
            Add(n, "whiskey_strength", "Top Shelf", "+0.25× boost strength.",
                SkillBranch.Whiskey, 5, Money(400, 2.2), dir * Step * 2 - side * Step * 0.6f, new[] { "whiskey_duration" }, StatModifier.Add(StatId.WhiskeyBoostStrength, 0.25));
            Add(n, "whiskey_mastery", "Whiskey Mastery", "+1 s boost duration per rank. Never maxes out.",
                SkillBranch.Whiskey, MasteryRanks, Money(1e6, 1.25), dir * Step * 3 - side * Step * 0.6f, new[] { "whiskey_strength" }, StatModifier.Add(StatId.WhiskeyBoostDuration, 1));
        }

        // ------------------------------------------------------------------ helpers
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
