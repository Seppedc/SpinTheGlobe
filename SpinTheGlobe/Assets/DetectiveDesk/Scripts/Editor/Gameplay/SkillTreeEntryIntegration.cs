using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using DetectiveDesk.Gameplay;

namespace DetectiveDesk.Editor
{
    /// <summary>Adds one visible entry next to Archive; does not move existing HUD controls.</summary>
    [InitializeOnLoad]
    public static class SkillTreeEntryIntegration
    {
        const string Log = "ProductionLogs/SkillTree/";
        const string Request = "DetectiveDesk.SkillTreeEntryValidation";
        static SkillTreeEntryIntegration() { EditorApplication.playModeStateChanged += State; }

        public static void Snapshot(string file)
        {
            var rows = new Dictionary<string, object>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        if (!component) continue;
                        var references = new Dictionary<string, string>();
                        var serialized = new SerializedObject(component); var p = serialized.GetIterator();
                        while (p.Next(true)) if (p.propertyType == SerializedPropertyType.ObjectReference)
                            references[p.propertyPath] = p.objectReferenceValue ? GlobalObjectId.GetGlobalObjectIdSlow(p.objectReferenceValue).ToString() : "";
                        rows[GlobalObjectId.GetGlobalObjectIdSlow(component).ToString()] = new
                        { name = component.name, type = component.GetType().FullName, json = EditorJsonUtility.ToJson(component), references };
                    }
            File.WriteAllText(file, JsonConvert.SerializeObject(rows));
        }
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
            var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var parent = game.legacy.upgradesPanel.transform.parent;
            if (parent.Find("UpgradesButton")) throw new Exception("Entry already installed; inspect before changing");
            var archive = parent.Find("ArchiveButton"); if (!archive) throw new Exception("Archive reference missing");
            var scene = SceneManager.GetActiveScene();
            if (scene.isDirty) throw new Exception("Preserve unsaved edits before installation");
            Snapshot(Log + "entry-before.json");
            File.Copy(scene.path, Log + "BeforeEntryButton.unity", false);
            var button = UnityEngine.Object.Instantiate(archive.gameObject, parent, false);
            button.name = "UpgradesButton"; Undo.RegisterCreatedObjectUndo(button, "Add skill tree entry");
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition += new Vector2(-178, 0);
            var control = button.GetComponent<Button>(); control.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(control.onClick, game.legacy.ToggleUpgrades);
            var image = button.GetComponent<Image>(); image.color = new Color(.18f, .13f, .07f, .98f); image.raycastTarget = true;
            var label = button.GetComponentInChildren<Text>(true); label.text = "UPGRADES"; label.raycastTarget = false;
            button.SetActive(true); EditorUtility.SetDirty(control);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            Snapshot(Log + "entry-after.json");
        }
        public static void Arm()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new Exception("Save clean Edit Mode first");
            SessionState.SetBool(Request, true); SessionState.SetBool(Request + ".background", Application.runInBackground);
            Application.runInBackground = true; EditorApplication.isPlaying = true;
        }
        static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Request, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Temporary skill entry validation").AddComponent<SkillTreeEntryRunner>();
            if (state == PlayModeStateChange.EnteredEditMode)
            { Application.runInBackground = SessionState.GetBool(Request + ".background", false); SessionState.SetBool(Request, false); }
        }
    }

    public sealed class SkillTreeEntryRunner : MonoBehaviour
    {
        readonly List<string> checks = new List<string>();
        void Check(bool condition, string text) { if (!condition) throw new Exception(text); checks.Add(text); }
        System.Collections.IEnumerator Start()
        {
            yield return null; yield return null;
            string failure = null;
            try
            {
                var game = UnityEngine.Object.FindAnyObjectByType<GameManager>(); var parent = game.legacy.upgradesPanel.transform.parent;
                var entry = parent.Find("UpgradesButton").GetComponent<Button>(); var archive = parent.Find("ArchiveButton").GetComponent<Button>();
                double cash = game.Money;
                Check(game.skillTree.nodes.Length == 39 && !game.legacy.upgradesPanel.activeSelf, "Tree starts closed with all 39 nodes");
                Check(parent.GetComponentsInChildren<Button>(true).Count(b => b.name == "UpgradesButton") == 1, "Exactly one visible upgrade entry");
                Check(entry.onClick.GetPersistentEventCount() == 1 && entry.onClick.GetPersistentMethodName(0) == "ToggleUpgrades", "Entry uses original persistent open/close callback");
                Canvas.ForceUpdateCanvases();
                Vector3[] points = new Vector3[4]; ((RectTransform)entry.transform).GetWorldCorners(points);
                var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, (points[0] + points[2]) * .5f) };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
                Check(hits.Count > 0 && (hits[0].gameObject == entry.gameObject || hits[0].gameObject.transform.IsChildOf(entry.transform)), "Real UI ray reaches upgrade entry");
                Rect a = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, entry.transform).ToRect();
                Rect b = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, archive.transform).ToRect();
                Check(!a.Overlaps(b), "Upgrade and Archive controls do not overlap");
                entry.onClick.Invoke();
                Check(game.legacy.upgradesPanel.activeSelf && game.WorldInputBlocked && game.skillTreePanel.game == game, "Entry opens bound skill tree and blocks world input");
                var close = game.legacy.upgradesPanel.transform.Find("Button_CLOSE").GetComponent<Button>(); close.onClick.Invoke();
                Check(!game.legacy.upgradesPanel.activeSelf && !game.WorldInputBlocked, "Original close control closes tree");
                archive.onClick.Invoke(); Check(game.archive.IsArchiveOpen, "Original Archive control still works"); game.archive.CloseArchive();
                Check(game.Money == cash && game.SpeedLevel == 0 && game.MotorLevel == 0 && game.DurabilityLevel == 0, "Entry controls do not alter money or progress");
            }
            catch (Exception error) { failure = error.ToString(); }
            File.WriteAllText("ProductionLogs/SkillTree/entry-runtime-validation.json", JsonConvert.SerializeObject(new { status = failure == null ? "PASS" : "FAIL", checks, failure, actualPlayMode = true, actualUIRay = true, physicalMouseSynthesized = false }, Formatting.Indented));
            EditorApplication.isPlaying = false;
        }
    }
    static class SkillTreeBoundsExtensions
    {
        public static Rect ToRect(this Bounds bounds) => new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
    }
}
