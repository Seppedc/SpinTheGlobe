using DetectiveDesk.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Small maintenance commands for the open scene and the save file.</summary>
    public static class SceneMaintenanceMenu
    {
        [MenuItem("Tools/Spin The Globe/Remove Missing Scripts In Open Scene")]
        public static void RemoveMissingScripts()
        {
            int removed = 0;
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (n == 0) continue;
                    Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove missing scripts");
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }
            if (removed > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Removed " + removed + " missing script component(s). Save the scene to keep the change.");
        }

        [MenuItem("Tools/Spin The Globe/Delete Save File")]
        public static void DeleteSave()
        {
            var store = SaveStore.Default;
            if (!store.Exists) { Debug.Log("No save file at " + store.FilePath); return; }
            if (!EditorUtility.DisplayDialog("Delete save", "Delete " + store.FilePath + "?", "Delete", "Cancel")) return;
            store.Delete();
            Debug.Log("Save deleted.");
        }

        [MenuItem("Tools/Spin The Globe/Reveal Save File")]
        public static void RevealSave() => EditorUtility.RevealInFinder(SaveStore.Default.FilePath);
    }
}
