using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>One-off desk layout commands (undoable; save the scene afterwards).</summary>
    public static class DeskLayoutMenu
    {
        const string FolderName = "Generated_CaseFiles", ClickName = "CaseFiles";

        /// <summary>
        /// Turns the case folder a quarter turn on the desk (portrait ⇄ landscape) around its centre, together with the
        /// clickable case-file object and the SOLVED mark, then keeps its front edge on the desk.
        /// </summary>
        [MenuItem("Tools/Spin The Globe/Layout/Rotate Case Folder 90°")]
        public static void RotateCaseFolder()
        {
            if (Application.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            var folder = GameObject.Find(FolderName);
            var click = GameObject.Find(ClickName);
            if (!folder) { Debug.LogError(FolderName + " not found in the open scene."); return; }

            var before = Bounds(folder.transform);
            Vector3 pivot = before.center;
            Undo.RecordObject(folder.transform, "Rotate case folder");
            folder.transform.RotateAround(pivot, Vector3.up, 90);
            if (click)
            {
                Undo.RecordObject(click.transform, "Rotate case folder");
                click.transform.RotateAround(pivot, Vector3.up, 90);
            }

            // Keep the folder's front edge (towards the camera, −Z) where it was so it doesn't hang off the desk.
            var after = Bounds(folder.transform);
            float shift = before.min.z - after.min.z;
            var move = new Vector3(0, 0, shift);
            folder.transform.position += move;
            if (click) click.transform.position += move;

            after = Bounds(folder.transform);
            var desk = GameObject.Find("Desk slab");
            string fit = "";
            if (desk)
            {
                var d = Bounds(desk.transform);
                fit = $"  Desk top x {d.min.x:0.00}..{d.max.x:0.00}, z {d.min.z:0.00}..{d.max.z:0.00}.";
                if (after.max.x > d.max.x || after.min.x < d.min.x || after.min.z < d.min.z) fit += "  ⚠ The folder overhangs the desk.";
            }
            EditorSceneManager.MarkSceneDirty(folder.scene);
            Debug.Log($"Case folder rotated. Now x {after.min.x:0.00}..{after.max.x:0.00}, z {after.min.z:0.00}..{after.max.z:0.00}.{fit} Save the scene to keep it.");
        }

        static Bounds Bounds(Transform root)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(root.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
