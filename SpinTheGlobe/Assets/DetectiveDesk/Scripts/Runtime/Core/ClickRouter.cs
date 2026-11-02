using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Routes left clicks on desk objects (whiskey glass, paper bin …) to gameplay callbacks without
    /// adding scripts to authored prefabs. Systems register a root transform; any collider at or
    /// below it counts as a hit. Nearer unrelated colliders are skipped, because several authored
    /// interaction boxes are intentionally generous.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClickRouter : MonoBehaviour
    {
        struct Target { public Transform root; public Action onClick; }

        readonly List<Target> targets = new List<Target>();
        GameManager game;
        Camera cam;

        public void Initialize(GameManager owner, Camera camera) { game = owner; cam = camera; }

        public void Register(Transform root, Action onClick)
        {
            if (!root || onClick == null) return;
            EnsureCollider(root);
            targets.Add(new Target { root = root, onClick = onClick });
        }

        void Update()
        {
            if (!cam || targets.Count == 0 || !Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            if (game && game.WorldInputBlocked) return;
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(Input.mousePosition), 100f, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
                foreach (var t in targets)
                    if (t.root && (hit.transform == t.root || hit.transform.IsChildOf(t.root))) { t.onClick(); return; }
        }

        /// <summary>Adds a box collider fitted to the renderers when the object has no collider yet.</summary>
        public static void EnsureCollider(Transform root)
        {
            if (!root || root.GetComponentInChildren<Collider>()) return;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var box = root.gameObject.AddComponent<BoxCollider>();
            Vector3 scale = root.lossyScale;
            scale = new Vector3(Mathf.Max(1e-5f, Mathf.Abs(scale.x)), Mathf.Max(1e-5f, Mathf.Abs(scale.y)), Mathf.Max(1e-5f, Mathf.Abs(scale.z)));
            box.center = root.InverseTransformPoint(bounds.center);
            // Rotation-agnostic approximation; generous on purpose so small props are easy to click.
            box.size = Vector3.Scale(bounds.size * 1.1f, new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z));
        }

        /// <summary>Finds a scene object by exact name, falling back to a case-insensitive "contains" match.</summary>
        public static Transform FindByName(string exact, string contains = null)
        {
            Transform fallback = null;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name == exact) return t;
                if (fallback == null && contains != null && t.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0) fallback = t;
            }
            return fallback;
        }

        public static Bounds RendererBounds(Transform root, Vector3 fallbackCenter)
        {
            var renderers = root ? root.GetComponentsInChildren<Renderer>() : new Renderer[0];
            if (renderers.Length == 0) return new Bounds(root ? root.position : fallbackCenter, Vector3.one * 0.1f);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
