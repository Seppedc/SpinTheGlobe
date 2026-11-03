using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>One-off desk layout commands (undoable; save the scene afterwards).</summary>
    public static class DeskLayoutMenu
    {
        const string FolderName = "Generated_CaseFiles";

        /// <summary>
        /// Turns the case folder a quarter turn on the desk (portrait ⇄ landscape) around its centre, together with the
        /// SOLVED mark (a sibling under CaseFiles stays put; the click box is fitted to the folder at runtime),
        /// then keeps its front edge where it was.
        /// </summary>
        [MenuItem("Tools/Spin The Globe/Layout/Rotate Case Folder 90°")]
        public static void RotateCaseFolder()
        {
            if (Application.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            var folder = GameObject.Find(FolderName);
            if (!folder) { Debug.LogError(FolderName + " not found in the open scene."); return; }

            var before = Bounds(folder.transform);
            Vector3 pivot = before.center;
            Undo.RecordObject(folder.transform, "Rotate case folder");
            folder.transform.RotateAround(pivot, Vector3.up, 90);

            // Keep the folder's front edge (towards the camera, −Z) where it was so it doesn't hang off the desk.
            var after = Bounds(folder.transform);
            float shift = before.min.z - after.min.z;
            var move = new Vector3(0, 0, shift);
            folder.transform.position += move;

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

        /// <summary>World bounds from the meshes and the current transforms (Renderer.bounds can lag a frame in the editor).</summary>
        static Bounds Bounds(Transform root)
        {
            bool any = false;
            var b = new Bounds(root.position, Vector3.zero);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mf.sharedMesh || !mr || !mr.enabled) continue;
                var lb = mf.sharedMesh.bounds;
                var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var w = m.MultiplyPoint3x4(c);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
                }
            }
            return b;
        }
    }
}
