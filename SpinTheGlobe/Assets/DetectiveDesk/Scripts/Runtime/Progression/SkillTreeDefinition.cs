using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public enum SkillEffect { Upgrade, Restoration }
    public enum SkillNodeState { Locked, Available, Unaffordable, Owned, Unavailable }

    [Serializable]
    public sealed class SkillNode
    {
        public string id, title;
        [TextArea] public string description;
        public SkillEffect effect;
        public UpgradeKind upgrade;
        public int figurineIndex;
        [Min(1)] public int rank = 1;
        public string[] prerequisites = Array.Empty<string>();
        public Vector2 position;
        public int tab;
    }

    [CreateAssetMenu(menuName = "Detective Desk/Gameplay/Skill Tree")]
    public sealed class SkillTreeDefinition : ScriptableObject
    {
        public SkillNode[] nodes = Array.Empty<SkillNode>();
        public SkillNode Find(string id)
        {
            if (nodes != null) foreach (var node in nodes) if (node != null && node.id == id) return node;
            return null;
        }
        public SkillNode UpgradeNode(UpgradeKind kind, int rank)
        {
            if (nodes != null) foreach (var node in nodes) if (node != null && node.effect == SkillEffect.Upgrade && node.upgrade == kind && node.rank == rank) return node;
            return null;
        }
        public SkillNode RestorationNode(int figure, int rank)
        {
            if (nodes != null) foreach (var node in nodes) if (node != null && node.effect == SkillEffect.Restoration && node.figurineIndex == figure && node.rank == rank) return node;
            return null;
        }
        public int CurrentRank(GameManager game, SkillNode node)
        {
            if (!game || node == null) return -1;
            if (node.effect == SkillEffect.Upgrade) return game.Level(node.upgrade);
            return game.figurines != null && node.figurineIndex >= 0 && node.figurineIndex < game.figurines.Length && game.figurines[node.figurineIndex]
                ? game.figurines[node.figurineIndex].Tier : -1;
        }
        public bool IsOwned(GameManager game, SkillNode node) => node != null && CurrentRank(game, node) >= node.rank;
        public double Cost(GameManager game, SkillNode node)
        {
            if (!game || !game.config || node == null || node.rank < 1) return double.PositiveInfinity;
            return node.effect == SkillEffect.Upgrade ? game.config.Cost(node.upgrade, node.rank - 1) : game.config.RestorationCost(node.rank - 1);
        }
        public SkillNodeState State(GameManager game, SkillNode node, out string reason)
        {
            reason = "";
            if (!game || !game.config || node == null || node.rank < 1 || string.IsNullOrEmpty(node.id) || Find(node.id) != node ||
                !Enum.IsDefined(typeof(SkillEffect), node.effect) ||
                (node.effect == SkillEffect.Upgrade && !Enum.IsDefined(typeof(UpgradeKind), node.upgrade)) || CurrentRank(game, node) < 0)
            { reason = "This skill is not configured."; return SkillNodeState.Unavailable; }
            // Existing progress remains authoritative, including progress earned before this tree.
            if (IsOwned(game, node)) { reason = "Already owned."; return SkillNodeState.Owned; }
            int maximum = node.effect == SkillEffect.Upgrade ? game.config.maximumUpgradeLevel : 3;
            if (node.rank > maximum) { reason = "This rank is outside the configured limit."; return SkillNodeState.Unavailable; }
            if (node.rank != CurrentRank(game, node) + 1)
            { reason = "Purchase the preceding rank first."; return SkillNodeState.Locked; }
            if (node.prerequisites != null) foreach (var id in node.prerequisites)
            {
                var required = Find(id);
                if (required == null || !IsOwned(game, required))
                { reason = "Requires " + (required == null ? id : required.title) + "."; return SkillNodeState.Locked; }
            }
            double cost = Cost(game, node);
            if (!GameplayMath.Finite(cost) || cost < 0 || !GameplayMath.Finite(game.Money))
            { reason = "This skill has an invalid cost or balance."; return SkillNodeState.Unavailable; }
            if (game.Money + 1e-9 < cost) { reason = "Need €" + cost.ToString("N0") + "."; return SkillNodeState.Unaffordable; }
            reason = "Ready to unlock."; return SkillNodeState.Available;
        }
        public string Requirements(GameManager game, SkillNode node)
        {
            if (node == null) return "Unavailable";
            if (node.prerequisites == null || node.prerequisites.Length == 0) return "Workshop foundation — no prerequisites.";
            var text = new StringBuilder();
            foreach (var id in node.prerequisites)
            {
                var required = Find(id);
                text.Append(IsOwned(game, required) ? "OWNED  " : "NEEDED  ").Append(required == null ? id : required.title).Append('\n');
            }
            return text.ToString().TrimEnd();
        }
        public string EffectText(GameManager game, SkillNode node)
        {
            if (!game || !game.config || node == null) return "";
            var config = game.config;
            if (node.effect == SkillEffect.Restoration)
            {
                string[] tiers = { "Broken rubble", "Glued stone", "Kintsugi gold seams", "Polished gold" };
                double[] multipliers = { 1, 1.5, 2, 3 };
                int rank = Mathf.Clamp(node.rank, 1, 3);
                return tiers[rank] + "\nFigurine multiplier: ×" + multipliers[rank].ToString("0.0") + "\nAll three figurines add to your global multiplier.";
            }
            switch (node.upgrade)
            {
                case UpgradeKind.Speed:
                    double speed = config.baseMaximumSpeed + node.rank * config.maximumSpeedPerLevel;
                    return "+" + config.maximumSpeedPerLevel.ToString("0.#") + "°/s maximum speed\nRank ceiling: " + speed.ToString("0.#") + "°/s (" + (speed / 6).ToString("0.#") + " RPM)";
                case UpgradeKind.AutoMotor:
                    return "Automatic spin: " + (node.rank * config.autoSpeedPerLevel).ToString("0.#") + "°/s\nThe motor floor remains limited by your maximum speed.";
                default:
                    return (1 + node.rank) + " durability charges per new pin\nEach wire strike uses one charge. Pins already fired keep their original charges.";
            }
        }

        public bool Validate(out string error)
        {
            error = ""; var ids = new Dictionary<string, SkillNode>(); var effects = new HashSet<string>();
            if (nodes == null || nodes.Length == 0) { error = "The skill tree has no nodes."; return false; }
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id) || ids.ContainsKey(node.id) || node.rank < 1)
                { error = "Null node, duplicate/empty ID or invalid rank."; return false; }
                if (!Enum.IsDefined(typeof(SkillEffect), node.effect) || (node.effect == SkillEffect.Upgrade && !Enum.IsDefined(typeof(UpgradeKind), node.upgrade)))
                { error = "Invalid effect type."; return false; }
                if (node.effect == SkillEffect.Restoration && (node.figurineIndex < 0 || node.figurineIndex >= 3 || node.rank > 3))
                { error = "Invalid figurine or restoration rank."; return false; }
                string effect = node.effect + ":" + (node.effect == SkillEffect.Upgrade ? (int)node.upgrade : node.figurineIndex) + ":" + node.rank;
                if (!effects.Add(effect)) { error = "Two nodes grant the same rank."; return false; }
                ids.Add(node.id, node);
            }
            var marks = new Dictionary<string, byte>();
            foreach (var node in nodes) if (!Visit(node, ids, marks, ref error)) return false;
            return true;
        }
        static bool Visit(SkillNode node, Dictionary<string, SkillNode> ids, Dictionary<string, byte> marks, ref string error)
        {
            byte mark; if (marks.TryGetValue(node.id, out mark))
            { if (mark == 1) { error = "Prerequisite cycle at " + node.id; return false; } return true; }
            marks[node.id] = 1;
            if (node.prerequisites != null) foreach (var id in node.prerequisites)
            {
                SkillNode required;
                if (string.IsNullOrEmpty(id) || !ids.TryGetValue(id, out required)) { error = "Missing prerequisite " + id; return false; }
                if (!Visit(required, ids, marks, ref error)) return false;
            }
            marks[node.id] = 2; return true;
        }

        public static SkillNode[] DefaultNodes(int maximumRank = 10)
        {
            var all = new List<SkillNode>(); maximumRank = Mathf.Clamp(maximumRank, 1, 20);
            string[] keys = { "speed", "motor", "durability" }, names = { "Momentum", "Auto-Motor", "Pincraft" };
            for (int branch = 0; branch < 3; branch++) for (int rank = 1; rank <= maximumRank; rank++)
            {
                var requirements = new List<string>();
                if (rank > 1) requirements.Add(keys[branch] + "_" + (rank - 1).ToString("00"));
                if (branch == 1 && rank >= 3) requirements.Add("speed_" + Mathf.Min(maximumRank, (rank - 1) / 2).ToString("00"));
                if (branch == 2 && rank >= 3) requirements.Add("motor_" + Mathf.Min(maximumRank, rank / 3).ToString("00"));
                all.Add(new SkillNode { id = keys[branch] + "_" + rank.ToString("00"), title = names[branch] + " " + rank,
                    effect = SkillEffect.Upgrade, upgrade = (UpgradeKind)branch, rank = rank, prerequisites = requirements.ToArray(),
                    description = branch == 0 ? "Raise the mechanical globe's speed ceiling." : branch == 1 ? "Build a motor that sustains forward rotation." : "Keep each country pin earning for more revolutions.",
                    position = new Vector2((branch - 1) * 260, -190 - (rank - 1) * 140), tab = 0 });
            }
            string[] figures = { "Detective", "Archivist", "Commissioner" }, tiers = { "Reassembly", "Kintsugi", "Masterpiece" };
            for (int figure = 0; figure < 3; figure++) for (int rank = 1; rank <= 3; rank++)
            {
                var requirements = new List<string>();
                if (rank > 1) requirements.Add("figure_" + figure + "_" + (rank - 1));
                requirements.Add(rank == 1 ? "durability_01" : rank == 2 ? "motor_01" : "speed_" + Mathf.Min(3, maximumRank).ToString("00"));
                all.Add(new SkillNode { id = "figure_" + figure + "_" + rank, title = figures[figure] + ": " + tiers[rank - 1],
                    effect = SkillEffect.Restoration, figurineIndex = figure, rank = rank, prerequisites = requirements.ToArray(),
                    description = "Restore this collectible without altering the other figurines.",
                    position = new Vector2((figure - 1) * 260, -190 - (rank - 1) * 165), tab = 1 });
            }
            return all.ToArray();
        }
    }
}
