using System;
using System.Collections.Generic;
using DetectiveDesk.Gameplay;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Editor
{
    /// <summary>Creates the same layouts used in play, with persistent scene objects and visual resources.</summary>
    public static class ScenePresentationAuthoring
    {
        public const string RootName = "Scene presentation";
        const string ResourcePath = "Assets/DetectiveDesk/Art/UI/ScenePresentationResources.asset";

        public static void EnsureLayout(GameManager game)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring layouts.");
            Undo.RegisterFullObjectHierarchyUndo(game.gameObject, "Create scene presentation");
            game.PrepareScenePresentation();
            var root = game.transform.Find(RootName);
            if (!root)
            {
                root = new GameObject(RootName).transform;
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Create scene presentation");
                root.SetParent(game.transform, false);
            }
            Parent(game.Hud.CreateLayout(game).transform, root);
            Parent(game.TreeView.CreateLayout(game).transform, root);
            Parent(game.Settings.CreateLayout(game).transform, root);
            Parent(game.ShopView.CreateLayout(game).transform, root);
            Parent(game.LootboxView.CreateLayout(game).transform, root);
            var paper = game.GetComponent<PaperBallThrower>();
            if (paper)
            {
                Parent(paper.CreateLayout(game).transform, root);
                if (paper.ScoreCanvas) Parent(paper.ScoreCanvas.transform, root);
            }
            Parent(game.DeskInfo.CreateLayout(game), root);
            if (game.DeskInfo.ReloadCanvas) Parent(game.DeskInfo.ReloadCanvas.transform, root);
            game.Hud.PreviewInitialState();
            game.TreeView.PreviewLayout();
            game.ShopView.PreviewLayout();
            game.LootboxView.PreviewLayout();
            game.DeskInfo.PreviewLayout();
            Preview(game, "Desk");
            PersistResources(root);
            foreach (var component in game.GetComponents<MonoBehaviour>()) EditorUtility.SetDirty(component);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }

        static void Parent(Transform child, Transform parent)
        {
            if (child.parent != parent) Undo.SetTransformParent(child, parent, "Organize scene presentation");
        }

        public static void Preview(GameManager game, string panel)
        {
            var root = game.transform.Find(RootName);
            if (!root) return;
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                bool menu = canvas.name == "Skill Tree" || canvas.name == "Settings" || canvas.name == "Gold Shop" || canvas.name == "Lootbox Opening" || canvas.name == "Paper Ball Aim";
                canvas.gameObject.SetActive(!menu || canvas.name == panel);
            }
            Canvas.ForceUpdateCanvases();
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            SceneView.RepaintAll();
        }

        // Runtime-created sprites, fonts and materials otherwise disappear after an editor reload.
        static void PersistResources(Transform root)
        {
            var container = AssetDatabase.LoadMainAssetAtPath(ResourcePath);
            if (!container)
            {
                container = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Scene presentation resources" };
                AssetDatabase.CreateAsset(container, ResourcePath);
            }
            var pending = new Queue<UnityEngine.Object>();
            var visited = new HashSet<int>();
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component) pending.Enqueue(component);
            // Include serialized material fields held by the desk view on the scene root.
            foreach (var component in root.parent.GetComponents<MonoBehaviour>()) pending.Enqueue(component);
            while (pending.Count > 0)
            {
                var obj = pending.Dequeue();
                if (!obj || !visited.Add(obj.GetInstanceID())) continue;
                var serialized = new SerializedObject(obj);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    if (!reference || reference is GameObject || reference is Component || EditorUtility.IsPersistent(reference)) continue;
                    if (!(reference is Material || reference is Texture2D || reference is Sprite || reference is TMP_FontAsset)) continue;
                    if (reference is TMP_FontAsset font)
                    {
                        // Bake the game's current alphabet; gameplay popups can still use dynamic fonts.
                        var characters = new System.Text.StringBuilder();
                        for (int i = 32; i <= 255; i++) characters.Append((char)i);
                        characters.Append("€★▸→…‹›∞−✓✕");
                        font.TryAddCharacters(characters.ToString(), out _);
                        font.atlasPopulationMode = AtlasPopulationMode.Static;
                        var fontData = new SerializedObject(font);
                        var sourceFont = fontData.FindProperty("m_SourceFontFile");
                        if (sourceFont != null) { sourceFont.objectReferenceValue = null; fontData.ApplyModifiedPropertiesWithoutUndo(); }
                    }
                    reference.hideFlags = HideFlags.None;
                    AssetDatabase.AddObjectToAsset(reference, container);
                    EditorUtility.SetDirty(reference);
                    pending.Enqueue(reference);
                }
            }
            AssetDatabase.SaveAssetIfDirty(container);
        }
    }
}
