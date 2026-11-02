using System;
using System.Collections.Generic;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Progression
{
    /// <summary>Explicit values: serialized. Append only.</summary>
    public enum SkillBranch
    {
        Root = 0,
        Globe = 1,
        Pins = 2,
        CaseFile = 3,
        Whiskey = 4,
        Statues = 5,
        Lootbox = 6,
    }

    [Serializable]
    public sealed class SkillNodeData
    {
        [Tooltip("Stable unique id. Saved in player saves: never rename a shipped id.")]
        public string id;
        public string title;
        [TextArea(2, 4)] public string description;
        public SkillBranch branch;
        [Min(1)] public int maxRank = 1;
        public Currency currency = Currency.Money;
        public CostCurve cost = new CostCurve(10, 1.5);

        [Tooltip("Every listed node must be owned (rank ≥ 1) before this one can be bought. " +
                 "The node becomes visible as soon as ANY of them is owned (Spellbound-style reveal).")]
        public string[] prerequisites = Array.Empty<string>();

        public StatModifier[] modifiers = Array.Empty<StatModifier>();

        [Tooltip("Optional cap: buying a rank is blocked if it would push this stat above the stat below " +
                 "(e.g. Auto-rotate speed can never exceed Max speed).")]
        public StatId capStat;
        public StatId capByStat;

        [Tooltip("Position in skill-tree space (root at 0,0). Used by the UI only.")]
        public Vector2 position;

        public bool IsRoot => prerequisites == null || prerequisites.Length == 0;
    }

    /// <summary>
    /// Data definition of the whole skill tree: one root node with branches growing outward.
    /// Edit in the Inspector or regenerate the defaults via Tools ▸ Spin The Globe ▸ Create Default Data.
    /// </summary>
    [CreateAssetMenu(menuName = "Spin The Globe/Skill Tree", fileName = "SkillTree")]
    public sealed class SkillTreeAsset : ScriptableObject
    {
        [SerializeField] List<SkillNodeData> nodes = new List<SkillNodeData>();

        Dictionary<string, SkillNodeData> lookup;

        public IReadOnlyList<SkillNodeData> Nodes => nodes;

        public SkillNodeData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null || lookup.Count != nodes.Count) RebuildLookup();
            return lookup.TryGetValue(id, out var node) ? node : null;
        }

        public void SetNodes(IEnumerable<SkillNodeData> newNodes)
        {
            nodes = new List<SkillNodeData>(newNodes);
            lookup = null;
        }

        void OnValidate() => lookup = null;

        void RebuildLookup()
        {
            lookup = new Dictionary<string, SkillNodeData>(StringComparer.Ordinal);
            foreach (var n in nodes)
                if (n != null && !string.IsNullOrEmpty(n.id) && !lookup.ContainsKey(n.id)) lookup.Add(n.id, n);
        }

        /// <summary>Structural checks: unique ids, prerequisites exist, exactly one root, no cycles, sane ranks.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int roots = 0;
            foreach (var n in nodes)
            {
                if (n == null) { errors.Add("Null node entry."); continue; }
                if (string.IsNullOrEmpty(n.id)) { errors.Add($"Node '{n.title}' has no id."); continue; }
                if (!seen.Add(n.id)) errors.Add($"Duplicate id '{n.id}'.");
                if (n.maxRank < 1) errors.Add($"'{n.id}' maxRank must be ≥ 1.");
                if (n.IsRoot) roots++;
                if (n.prerequisites != null)
                    foreach (var p in n.prerequisites)
                    {
                        if (p == n.id) errors.Add($"'{n.id}' requires itself.");
                        else if (Find(p) == null) errors.Add($"'{n.id}' requires missing node '{p}'.");
                    }
                if ((n.capStat == StatId.None) != (n.capByStat == StatId.None))
                    errors.Add($"'{n.id}' must set both capStat and capByStat, or neither.");
            }
            if (roots != 1) errors.Add($"Expected exactly one root node, found {roots}.");

            // Cycle detection (depth-first, three-colour).
            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var n in nodes)
                if (n != null && !string.IsNullOrEmpty(n.id) && HasCycle(n, state)) { errors.Add($"Prerequisite cycle through '{n.id}'."); break; }
            return errors;
        }

        bool HasCycle(SkillNodeData node, Dictionary<string, int> state)
        {
            state.TryGetValue(node.id, out int s);
            if (s == 1) return true;
            if (s == 2) return false;
            state[node.id] = 1;
            if (node.prerequisites != null)
                foreach (var p in node.prerequisites)
                {
                    var parent = Find(p);
                    if (parent != null && HasCycle(parent, state)) return true;
                }
            state[node.id] = 2;
            return false;
        }
    }
}
