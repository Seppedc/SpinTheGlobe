using System.Collections.Generic;
using DetectiveDesk.Save;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// The banker's lamp on the desk: grab the pull chain and drag it down (or just click it) to switch the lamp
    /// on or off. The chain follows the mouse, clicks over at the bottom and springs back. Off turns down the
    /// lamp's lights and the bulb's glow. Purely cosmetic; the state is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeskLamp : MonoBehaviour
    {
        [Tooltip("How far the chain can be pulled, as a fraction of its length.")]
        [Range(0.05f, 0.6f)] public float maxPull = 0.28f;
        [Tooltip("Pull (fraction of max) at which the switch clicks.")]
        [Range(0.2f, 1f)] public float clickAt = 0.7f;
        [Tooltip("Screen pixels of mouse drag for a full pull.")]
        public float dragPixels = 90f;
        public float fadeSeconds = 0.12f;

        sealed class Part { public Transform t; public Vector3 basePos; }
        sealed class Glow { public Renderer r; public int index; public Color on; }

        readonly List<Part> chain = new List<Part>();
        readonly List<(Light light, float on)> lights = new List<(Light, float)>();
        readonly List<Glow> glows = new List<Glow>();
        GameManager game;
        Transform root;
        Collider handle;
        MaterialPropertyBlock block;
        float chainLength, pull, pullVelocity, level = 1, dragStartY;
        bool isOn = true, dragging, switchedThisPull;

        public bool IsOn => isOn;
        public Transform Root => root;
        /// <summary>Clickable/hoverable area around the chain.</summary>
        public Transform Handle => handle ? handle.transform : null;

        public void Initialize(GameManager owner)
        {
            game = owner;
            block = new MaterialPropertyBlock();
            var pivot = FindActive("PullChain_Pivot");
            root = pivot ? LampRoot(pivot) : null;
            if (!root) return;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool moving = n.StartsWith("PullChain_Bead") || n.StartsWith("PullChain_Finial") || n.StartsWith("PullChain_Core");
                if (moving && t.GetComponent<Renderer>()) chain.Add(new Part { t = t, basePos = t.position });
            }
            // Children of moving parts move with them already.
            chain.RemoveAll(p => chain.Exists(o => o != p && p.t.IsChildOf(o.t)));
            if (chain.Count == 0) return;

            var b = new Bounds(chain[0].t.GetComponent<Renderer>().bounds.center, Vector3.zero);
            foreach (var p in chain) b.Encapsulate(p.t.GetComponent<Renderer>().bounds);
            chainLength = Mathf.Max(0.05f, b.size.y);

            // A generous invisible grab box: the chain itself is far too thin to hit with the mouse.
            var go = new GameObject("Lamp pull chain (grab)");
            go.transform.SetParent(root, true);
            go.transform.position = b.center - Vector3.up * chainLength * 0.1f;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            float w = Mathf.Max(0.08f, chainLength * 0.35f);
            var s = go.transform.lossyScale;
            box.size = new Vector3(w / Mathf.Max(1e-4f, s.x), chainLength * 1.3f / Mathf.Max(1e-4f, s.y), w / Mathf.Max(1e-4f, s.z));
            handle = box;

            foreach (var l in root.GetComponentsInChildren<Light>(true)) lights.Add((l, l.intensity));
            var sceneLight = ClickRouter.FindByName("Warm banker's lamp");
            if (sceneLight && sceneLight.TryGetComponent(out Light extra) && !lights.Exists(x => x.light == extra)) lights.Add((extra, extra.intensity));

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (!m || !m.HasProperty("_EmissionColor")) continue;
                    if (m.name.IndexOf("Bulb", System.StringComparison.OrdinalIgnoreCase) < 0 && m.name.IndexOf("Opal", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    glows.Add(new Glow { r = r, index = i, on = m.GetColor("_EmissionColor") });
                }
            }
            Apply(level);
        }

        static Transform FindActive(string name)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>The lamp's top object: the highest ancestor in an unbroken run of objects named "…Lamp…".</summary>
        static Transform LampRoot(Transform t)
        {
            Transform best = null;
            for (var p = t.parent; p; p = p.parent)
            {
                if (p.name.IndexOf("Lamp", System.StringComparison.OrdinalIgnoreCase) < 0) break;
                best = p;
            }
            return best ? best : t.parent;
        }

        public void Toggle()
        {
            isOn = !isOn;
            if (game && game.audioManager) game.audioManager.Click();
            if (game) game.Notify();
        }

        // ------------------------------------------------------------------ input + animation
        void Update()
        {
            if (!handle || chain.Count == 0) return;
            var cam = game ? game.GameCamera : Camera.main;

            if (!dragging && Input.GetMouseButtonDown(0) && cam && !(game && game.WorldInputBlocked)
                && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()) && HitsHandle(cam))
            {
                dragging = true;
                switchedThisPull = false;
                dragStartY = Input.mousePosition.y;
            }

            if (dragging)
            {
                float target = Mathf.Clamp01((dragStartY - Input.mousePosition.y) / dragPixels);
                pull = Mathf.Lerp(pull, target, 1 - Mathf.Exp(-Time.deltaTime * 30));
                if (!switchedThisPull && pull >= clickAt) { switchedThisPull = true; Toggle(); }
                if (!Input.GetMouseButton(0))
                {
                    dragging = false;
                    if (!switchedThisPull) { pullVelocity = 6f; Toggle(); }   // a plain click: give it a tug
                }
            }
            else
            {
                // Spring back with a little bounce.
                float force = -pull * 160f - pullVelocity * 14f;
                pullVelocity += force * Time.deltaTime;
                pull += pullVelocity * Time.deltaTime;
                if (Mathf.Abs(pull) < 0.001f && Mathf.Abs(pullVelocity) < 0.01f) { pull = 0; pullVelocity = 0; }
            }

            float offset = Mathf.Clamp(pull, -0.15f, 1f) * maxPull * chainLength;
            foreach (var p in chain) if (p.t) p.t.position = p.basePos + Vector3.down * offset;

            float goal = isOn ? 1 : 0;
            if (!Mathf.Approximately(level, goal))
            {
                level = Mathf.MoveTowards(level, goal, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
                Apply(level);
            }
        }

        bool HitsHandle(Camera cam)
        {
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(Input.mousePosition), 100f, ~0, QueryTriggerInteraction.Collide);
            foreach (var h in hits) if (h.collider == handle) return true;
            return false;
        }

        void Apply(float k)
        {
            foreach (var (light, on) in lights) if (light) { light.intensity = on * k; light.enabled = k > 0.001f || on <= 0; }
            foreach (var g in glows)
            {
                if (!g.r) continue;
                g.r.GetPropertyBlock(block, g.index);
                block.SetColor("_EmissionColor", g.on * k);
                g.r.SetPropertyBlock(block, g.index);
            }
        }

        public void LoadState(SaveData d) { isOn = !d.lampOff; level = isOn ? 1 : 0; Apply(level); }
        public void SaveState(SaveData d) { d.lampOff = !isOn; }
    }
}
