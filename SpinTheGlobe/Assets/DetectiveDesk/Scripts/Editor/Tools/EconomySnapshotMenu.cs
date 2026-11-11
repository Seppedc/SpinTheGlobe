using System;
using System.IO;
using System.Linq;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Exports the actual player assets for the offline pacing simulators.</summary>
    public static class EconomySnapshotMenu
    {
        [Serializable]
        sealed class ShopEntry
        {
            public string id, requires;
            public int maxRank;
            public CostCurve cost;
            public StatModifier[] modifiers;
        }

        [Serializable]
        sealed class Snapshot
        {
            public int version = 1;
            public int layoutRevision;
            public SkillNodeData[] skills;
            public SkillSectionData[] sections;
            public ShopEntry[] shop;
            public int startingSpinLimitRanks;
            public int startingMotorRanks;
            public GameBalance.StatBase[] bases;
        }

        public static void Export()
        {
            var balance = AssetDatabase.LoadAssetAtPath<GameBalance>(DefaultDataMenu.BalancePath);
            var tree = AssetDatabase.LoadAssetAtPath<SkillTreeAsset>(DefaultDataMenu.SkillTreePath);
            if (!balance || !tree) throw new InvalidOperationException("Balance and skill-tree assets are required.");
            var stats = new StatSheet();
            balance.ApplyTo(stats);
            var snapshot = new Snapshot
            {
                layoutRevision = tree.layoutRevision,
                skills = tree.Nodes.ToArray(),
                sections = tree.Sections.ToArray(),
                shop = balance.goldShopItems.Select(i => new ShopEntry
                {
                    id = i.id, requires = i.requires, maxRank = i.maxRank,
                    cost = i.id == GoldShopCatalog.Whiskey ? new CostCurve(balance.whiskeyUnlockGoldCost, 1) : i.cost,
                    modifiers = i.modifiers,
                }).ToArray(),
                startingSpinLimitRanks = GoldShopCatalog.StartingSpinLimitRanks, startingMotorRanks = GoldShopCatalog.WoundClockworkRanks,
                bases = Enum.GetValues(typeof(StatId)).Cast<StatId>().Where(s => s != StatId.None)
                    .Select(s => new GameBalance.StatBase(s, stats.GetBase(s))).ToArray(),
            };
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Tools", "economy_snapshot.json"));
            string json = JsonUtility.ToJson(snapshot, true);
            json = json.Substring(0, json.Length - 1) + ",\n\"balance\":" + JsonUtility.ToJson(balance, true) + "\n}";
            File.WriteAllText(path, json);
            Debug.Log("Exported current economy to " + path);
        }
    }
}
