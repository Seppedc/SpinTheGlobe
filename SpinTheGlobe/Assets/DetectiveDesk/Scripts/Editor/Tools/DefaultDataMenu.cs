using System.IO;
using DetectiveDesk.Progression;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Creates (or refreshes) the default balance and skill-tree assets in Data/.</summary>
    public static class DefaultDataMenu
    {
        public const string BalancePath = "Assets/DetectiveDesk/Data/Balance/GameBalance.asset";
        public const string SkillTreePath = "Assets/DetectiveDesk/Data/SkillTree/SkillTree.asset";

        public static void CreateDefaults()
        {
            var balance = LoadOrCreate<GameBalance>(BalancePath, out bool balanceIsNew);
            var tree = LoadOrCreate<SkillTreeAsset>(SkillTreePath, out bool treeIsNew);

            if (!treeIsNew && !EditorUtility.DisplayDialog("Skill tree exists",
                    "Overwrite " + SkillTreePath + " with the default layout? Hand-made changes will be lost.",
                    "Overwrite", "Keep current"))
            {
                Debug.Log("Kept existing skill tree.");
            }
            else
            {
                Undo.RecordObject(tree, "Default skill tree");
                tree.SetNodes(DefaultSkillTreeFactory.Create());
                tree.SetSections(DefaultSkillTreeFactory.CreateSections());
                EditorUtility.SetDirty(tree);
            }

            var errors = tree.Validate();
            foreach (var e in errors) Debug.LogError("Skill tree: " + e, tree);

            AssetDatabase.SaveAssets();
            Selection.activeObject = tree;
            Debug.Log($"Default data ready. Balance {(balanceIsNew ? "created" : "kept")}, " +
                      $"skill tree has {tree.Nodes.Count} nodes, {errors.Count} validation errors.");
        }

        static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
