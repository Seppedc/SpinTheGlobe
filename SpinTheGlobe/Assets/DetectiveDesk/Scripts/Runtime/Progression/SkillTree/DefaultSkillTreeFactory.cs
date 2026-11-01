using System.Collections.Generic;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Builds the default skill tree layout: one root ("Skill Tree Unlock") with five branches
    /// radiating outward (Globe, Pins, Case File, Whiskey, Statues).
    /// The Pins branch is a Pin 1 → Pin 12 chain where every pin also gets its own
    /// Durability and Payout side nodes. Numbers are provisional; tune them in the asset.
    /// </summary>
    public static class DefaultSkillTreeFactory
    {
        public const string RootId = "root";
        public const int MaxPins = 12;
        const float Step = 170f;

        public static string PinId(int pin) => $"pin_{pin:00}";               // 1-based for readability
        public static string PinDurabilityId(int pin) => $"pin_{pin:00}_durability";
        public static string PinPayoutId(int pin) => $"pin_{pin:00}_payout";

        public static List<SkillNodeData> Create()
        {
            var nodes = new List<SkillNodeData>
            {
                Node(RootId, "Skill Tree Unlock", "Open the detective's ledger of upgrades.", SkillBranch.Root,
                    1, new CostCurve(25, 1), Vector2.zero, null),
            };

            BuildGlobe(nodes, Direction(90));
            BuildPins(nodes, Direction(18));
            BuildCaseFile(nodes, Direction(-54));
            BuildStatues(nodes, Direction(-126));
            BuildWhiskey(nodes, Direction(162));
            return nodes;
        }

        static void BuildGlobe(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            n.Add(Node("globe_max_speed", "Greased Bearings", "+15°/s maximum spin speed. Caps dragging and auto-rotate.",
                SkillBranch.Globe, 15, new CostCurve(15, 1.6), dir * Step, new[] { RootId },
                StatModifier.Add(StatId.GlobeMaxSpeed, 15)));
            n.Add(Node("globe_auto", "Clockwork Motor", "+6°/s auto-rotate. Can never exceed the maximum spin speed.",
                SkillBranch.Globe, 15, new CostCurve(40, 1.65), dir * Step * 2, new[] { "globe_max_speed" },
                StatModifier.Add(StatId.GlobeAutoSpeed, 6)));
            n[n.Count - 1].capStat = StatId.GlobeAutoSpeed;
            n[n.Count - 1].capByStat = StatId.GlobeMaxSpeed;
            n.Add(Node("globe_multiplier", "Cartographer's Cut", "+25% money from globe spins.",
                SkillBranch.Globe, 10, new CostCurve(60, 1.9), dir * Step * 3, new[] { "globe_auto" },
                StatModifier.Percent(StatId.GlobeMultiplier, 0.25)));
            n.Add(Node("globe_drag", "Firm Grip", "+20% speed gained from dragging.",
                SkillBranch.Globe, 5, new CostCurve(30, 1.8), dir * Step * 1.5f + side * Step, new[] { "globe_max_speed" },
                StatModifier.Percent(StatId.GlobeDragPower, 0.2)));
            n.Add(Node("globe_click", "Flick of the Wrist", "+25% speed gained from clicking.",
                SkillBranch.Globe, 5, new CostCurve(30, 1.8), dir * Step * 1.5f - side * Step, new[] { "globe_max_speed" },
                StatModifier.Percent(StatId.GlobeClickPower, 0.25)));
        }

        static void BuildPins(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            string previous = RootId;
            for (int pin = 1; pin <= MaxPins; pin++)
            {
                int index = pin - 1;
                Vector2 at = dir * Step * pin;
                n.Add(Node(PinId(pin), $"Pin {pin}", $"Adds pin #{pin} to the pin box.",
                    SkillBranch.Pins, 1, new CostCurve(20 * Mathf.Pow(2.4f, index), 1), at, new[] { previous },
                    StatModifier.Add(StatId.PinSlots, 1)));
                n.Add(Node(PinDurabilityId(pin), $"Pin {pin} · Durability", $"Pin #{pin} survives one more pass under the payout line.",
                    SkillBranch.Pins, 5, new CostCurve(15 * Mathf.Pow(2.2f, index), 1.7), at + side * Step * 0.6f, new[] { PinId(pin) },
                    StatModifier.Add(StatId.PinDurability, 1, index)));
                n.Add(Node(PinPayoutId(pin), $"Pin {pin} · Payout", $"+50% payout from pin #{pin}.",
                    SkillBranch.Pins, 5, new CostCurve(15 * Mathf.Pow(2.2f, index), 1.7), at - side * Step * 0.6f, new[] { PinId(pin) },
                    StatModifier.Percent(StatId.PinPayout, 0.5, index)));
                previous = PinId(pin);
            }
            // Shooter & landing upgrades sit beside the start of the pin chain.
            Vector2 extras = dir * Step * 1.5f + side * Step * 1.6f;
            n.Add(Node("shooter_cooldown", "Oiled Spring", "-8% pin shooter cooldown.",
                SkillBranch.Pins, 10, new CostCurve(35, 1.75), extras, new[] { PinId(1) },
                StatModifier.Percent(StatId.ShooterCooldown, -0.08)));
            n.Add(Node("pin_land_chance", "Steady Aim", "+8% chance a pin lands on land (land pays more).",
                SkillBranch.Pins, 5, new CostCurve(80, 2), extras + dir * Step, new[] { PinId(2) },
                StatModifier.Add(StatId.PinLandChance, 0.08)));
            n.Add(Node("pin_city_chance", "Big City Lights", "+5% chance a pin lands near a big city (cities pay more).",
                SkillBranch.Pins, 5, new CostCurve(150, 2.1), extras + dir * Step * 2, new[] { "pin_land_chance" },
                StatModifier.Add(StatId.PinCityChance, 0.05)));
        }

        static void BuildCaseFile(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            n.Add(Node("case_payout", "Bigger Retainers", "+25% money from approved cases.",
                SkillBranch.CaseFile, 10, new CostCurve(50, 1.8), dir * Step, new[] { RootId },
                StatModifier.Percent(StatId.CasePayout, 0.25)));
            n.Add(Node("case_cooldown", "Efficient Paperwork", "-7% wait before the next case opens.",
                SkillBranch.CaseFile, 10, new CostCurve(70, 1.8), dir * Step * 2 + side * Step * 0.6f, new[] { "case_payout" },
                StatModifier.Percent(StatId.CaseCooldown, -0.07)));
            n.Add(Node("case_autostamp", "Rubber-Stamp Clerk", "+5% chance a full case stamps itself.",
                SkillBranch.CaseFile, 10, new CostCurve(120, 1.9), dir * Step * 2 - side * Step * 0.6f, new[] { "case_payout" },
                StatModifier.Add(StatId.CaseAutoStampChance, 0.05)));
        }

        static void BuildWhiskey(List<SkillNodeData> n, Vector2 dir)
        {
            Vector2 side = Perpendicular(dir);
            n.Add(Node("whiskey_duration", "Slow Sipper", "+2 s boost duration.",
                SkillBranch.Whiskey, 10, new CostCurve(60, 1.8), dir * Step, new[] { RootId },
                StatModifier.Add(StatId.WhiskeyBoostDuration, 2)));
            n.Add(Node("whiskey_cooldown", "Quick Refill", "-6% whiskey cooldown.",
                SkillBranch.Whiskey, 10, new CostCurve(80, 1.8), dir * Step * 2 + side * Step * 0.6f, new[] { "whiskey_duration" },
                StatModifier.Percent(StatId.WhiskeyCooldown, -0.06)));
            n.Add(Node("whiskey_strength", "Top Shelf", "+0.25× boost strength.",
                SkillBranch.Whiskey, 5, new CostCurve(200, 2.2), dir * Step * 2 - side * Step * 0.6f, new[] { "whiskey_duration" },
                StatModifier.Add(StatId.WhiskeyBoostStrength, 0.25)));
        }

        static void BuildStatues(List<SkillNodeData> n, Vector2 dir)
        {
            n.Add(Node("statue_gold_fill", "Molten Pour", "+20% gold fill speed from globe spins.",
                SkillBranch.Statues, 10, new CostCurve(100, 1.85), dir * Step, new[] { RootId },
                StatModifier.Percent(StatId.StatueGoldFillRate, 0.2)));
            n.Add(Node("statue_payout", "Heavier Gold", "+25% gold from smashed statues.",
                SkillBranch.Statues, 10, new CostCurve(150, 1.9), dir * Step * 2, new[] { "statue_gold_fill" },
                StatModifier.Percent(StatId.StatuePayout, 0.25)));
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
