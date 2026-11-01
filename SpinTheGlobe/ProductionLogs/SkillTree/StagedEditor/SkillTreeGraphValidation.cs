using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DetectiveDesk.Gameplay;

namespace DetectiveDesk.Editor
{
    /// <summary>Exercises real graph/purchase code in a disposable scene, not the player's balance.</summary>
    public static class SkillTreeGraphValidation
    {
        static readonly List<string> checks = new List<string>();
        static void Check(bool value, string message) { if (!value) throw new Exception(message); checks.Add(message); }
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run graph checks in Edit Mode.");
            checks.Clear(); var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UpgradeConfig config = null; SkillTreeDefinition tree = null, invalid = null;
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("Disposable skill fixture");
                var legacy = root.AddComponent<DetectiveGame>();
                var game = root.AddComponent<GameManager>();
                var globe = root.AddComponent<GlobeController>();
                config = ScriptableObject.CreateInstance<UpgradeConfig>();
                tree = ScriptableObject.CreateInstance<SkillTreeDefinition>(); tree.nodes = SkillTreeDefinition.DefaultNodes(10);
                game.legacy = legacy; game.config = config; game.globe = globe; game.skillTree = tree; globe.game = game;
                game.figurines = new FigurinePedestal[3];
                for (int i = 0; i < 3; i++) { var figure = new GameObject("Fixture figure " + i); figure.transform.SetParent(root.transform); game.figurines[i] = figure.AddComponent<FigurinePedestal>(); }
                string reason;
                Check(tree.Validate(out reason) && tree.nodes.Length == 39, "Default graph has 39 unique acyclic nodes");
                Check(tree.State(game, tree.Find("speed_01"), out reason) == SkillNodeState.Available &&
                    tree.State(game, tree.Find("motor_01"), out reason) == SkillNodeState.Available &&
                    tree.State(game, tree.Find("durability_01"), out reason) == SkillNodeState.Available, "All existing first upgrades remain accessible");
                double balance = game.Money;
                Check(!game.PurchaseSkill("speed_03") && game.Money == balance, "Cannot skip ranks or spend for a locked node");
                Check(!game.PurchaseSkill("figure_0_1") && game.figurines[0].Tier == 0 && game.Money == balance, "Restoration requires Pincraft I");
                Check(game.PurchaseSkill("motor_01") && game.MotorLevel == 1 && game.Money == balance - 100, "First motor uses unchanged cash cost and grants one rank");
                balance = game.Money;
                Check(!game.PurchaseSkill("motor_01") && game.Money == balance && game.MotorLevel == 1, "Owned node cannot be charged twice");
                Check(game.PurchaseSkill("motor_02") && game.MotorLevel == 2, "Sequential motor rank is purchasable");
                legacy.money = 100000;
                balance = game.Money;
                Check(!game.BuyUpgrade(UpgradeKind.AutoMotor) && game.MotorLevel == 2 && game.Money == balance, "Direct legacy upgrade call cannot bypass cross-branch prerequisite");
                Check(tree.State(game, tree.Find("motor_03"), out reason) == SkillNodeState.Locked && reason.Contains("Momentum 1"), "Lock explains the missing branch requirement");
                Check(game.PurchaseSkill("speed_01") && game.PurchaseSkill("motor_03") && game.MotorLevel == 3, "Owned speed prerequisite unlocks the next motor node");
                balance = game.Money;
                Check(game.PurchaseSkill("durability_01") && game.DurabilityLevel == 1 && game.Money == balance - 60, "Pincraft retains original effect and fee");
                balance = game.Money;
                Check(game.PurchaseSkill("figure_0_1") && game.figurines[0].Tier == 1 && game.Money == balance - 120 && game.GlobalMultiplier == 3.5, "Restoration charges once and changes the existing sum multiplier");
                Check(game.figurines[1].Tier == 0 && game.figurines[2].Tier == 0, "Figurine paths progress independently");
                Check(game.PurchaseSkill("figure_0_2") && game.figurines[0].Tier == 2, "Kintsugi accepts owned first motor prerequisite");
                balance = game.Money;
                Check(!game.RestoreFigurine(0) && game.figurines[0].Tier == 2 && game.Money == balance, "Direct restoration cannot bypass Momentum III requirement");
                Check(game.PurchaseSkill("speed_02") && game.PurchaseSkill("speed_03") && game.PurchaseSkill("figure_0_3") && game.figurines[0].Tier == 3, "Cross-branch prerequisite unlocks gold masterpiece");
                balance = game.Money;
                Check(!game.PurchaseSkill("missing") && !game.PurchaseSkill(null) && !game.BuyUpgrade((UpgradeKind)99) && game.Money == balance, "Unknown skills and enum values never spend cash");
                legacy.money = 0;
                Check(tree.State(game, tree.Find("durability_02"), out reason) == SkillNodeState.Unaffordable &&
                    !game.PurchaseSkill("durability_02") && game.Money == 0, "Unaffordable skill is distinct from locked and cannot make balance negative");
                legacy.money = 100000;
                double originalCost = config.speedBaseCost; config.speedBaseCost = double.NaN; balance = game.Money;
                Check(tree.State(game, tree.Find("speed_04"), out reason) == SkillNodeState.Unavailable && !game.PurchaseSkill("speed_04") && game.Money == balance, "Invalid configured cost fails closed");
                config.speedBaseCost = originalCost;
                game.skillTree = null;
                Check(game.BuyUpgrade(UpgradeKind.AutoMotor) && game.MotorLevel == 4, "Unconfigured legacy game keeps its original purchase behaviour");
                game.skillTree = tree;
                Check(tree.IsOwned(game, tree.Find("motor_04")) && tree.State(game, tree.Find("motor_04"), out reason) == SkillNodeState.Owned, "Existing pre-tree progress is grandfathered and never reset");
                invalid = ScriptableObject.CreateInstance<SkillTreeDefinition>(); invalid.nodes = SkillTreeDefinition.DefaultNodes(10);
                invalid.nodes[1].id = invalid.nodes[0].id;
                Check(!invalid.Validate(out reason), "Duplicate skill IDs rejected");
                invalid.nodes = SkillTreeDefinition.DefaultNodes(10); invalid.nodes[0].prerequisites = new[] { "speed_02" };
                Check(!invalid.Validate(out reason), "Prerequisite cycle rejected");
                invalid.nodes = SkillTreeDefinition.DefaultNodes(10); invalid.nodes[0].prerequisites = new[] { "missing" };
                Check(!invalid.Validate(out reason), "Dangling prerequisite rejected");
                invalid.nodes = SkillTreeDefinition.DefaultNodes(10); invalid.nodes[1].rank = 1;
                Check(!invalid.Validate(out reason), "Duplicate effect rank rejected");
                Directory.CreateDirectory("ProductionLogs/SkillTree");
                File.WriteAllText("ProductionLogs/SkillTree/graph-validation.json", JsonUtility.ToJson(new Result { status = "PASS", assertions = checks.Count, checks = checks.ToArray(), playerSceneChanged = false }, true));
                Debug.Log("Skill tree graph/purchase checks PASS: " + checks.Count);
            }
            finally
            {
                if (config) UnityEngine.Object.DestroyImmediate(config);
                if (tree) UnityEngine.Object.DestroyImmediate(tree);
                if (invalid) UnityEngine.Object.DestroyImmediate(invalid);
                SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true);
            }
        }
        [Serializable] sealed class Result { public string status; public int assertions; public string[] checks; public bool playerSceneChanged; }
    }
}
