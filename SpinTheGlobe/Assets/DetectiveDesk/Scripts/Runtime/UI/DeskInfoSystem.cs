using System;
using System.Collections.Generic;
using DetectiveDesk.Economy;
using TMPro;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Desk information as part of the scene: a small engraved brass name plate stands on the desk in front of
    /// each desk object and shows its state (case progress, glass fill, shooter reload, statue progress …).
    /// Always visible, no pop-ups. A plate hides itself when it has nothing to say.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeskInfoSystem : MonoBehaviour
    {
        [Tooltip("Plate size in world units (width × height).")]
        public Vector2 plateSize = new Vector2(0.5f, 0.12f);
        [Tooltip("How far the plate leans back from vertical, in degrees.")]
        public float tilt = 28f;
        public Color brass = new Color(0.78f, 0.58f, 0.3f);
        public Color engraving = new Color(0.13f, 0.08f, 0.035f);

        sealed class Tag
        {
            public string name;
            public Func<string> text;
            public GameObject go;
            public TextMeshPro label;
        }

        readonly List<Tag> tags = new List<Tag>();
        GameManager game;
        Transform root;
        Material brassMaterial, rimMaterial;

        public void Initialize(GameManager owner)
        {
            game = owner;
            root = new GameObject("Desk brass tags").transform;
            root.SetParent(transform, false);
            brassMaterial = MakeMaterial("Runtime brass tag", brass, 0.85f, 0.55f);
            rimMaterial = MakeMaterial("Runtime brass tag rim", brass * 0.45f, 0.8f, 0.4f);
            RegisterTags();
        }

        void OnDestroy()
        {
            if (brassMaterial) Destroy(brassMaterial);
            if (rimMaterial) Destroy(rimMaterial);
        }

        // ------------------------------------------------------------------ tags
        void RegisterTags()
        {
            var map = game.map;
            if (map && map.launcherVisual)
                Add("Shooter", InFront(map.launcherVisual), () =>
                {
                    int unlocked = game.UnlockedPins;
                    if (unlocked == 0) return "NO PINS";
                    if (map.CooldownRemaining > 0) return "RELOAD " + map.CooldownRemaining.ToString("0.0");
                    return map.ReadyCount > 0 ? map.ReadyCount + " / " + unlocked + " READY" : "BOX EMPTY";
                });

            var cases = game.Cases;
            if (cases && cases.FolderTransform)
            {
                // The folder reaches the front edge of the desk, so its plate stands beside it on the left.
                var b = ClickRouter.RendererBounds(cases.FolderTransform, cases.FolderTransform.position);
                var visual = cases.GetComponent<CaseFolderVisual>();
                if (visual && visual.Folder) b = ClickRouter.RendererBounds(visual.Folder, visual.Folder.position);
                Vector3 p = new Vector3(b.min.x - plateSize.x * 0.6f, b.min.y, b.min.z + plateSize.y * 2f);
                Add("Case file", p, () => cases.OnCooldown
                    ? "NEXT CASE " + UIFactory.Seconds(cases.Cooldown)
                    : "CASE " + (cases.Level + 1) + "  ·  " + cases.Evidence + " / " + cases.Required + "  ·  " + UIFactory.Money(cases.Reward));
            }
            if (cases && cases.StampTransform)
                Add("Stamp", InFront(cases.StampTransform), () => cases.IsFull && !cases.OnCooldown ? "STAMP!" : null);

            var w = game.Whiskey;
            if (w && w.GlassTransform)
                Add("Whiskey", InFront(w.GlassTransform), () =>
                    w.Boosting ? "BOOST " + UIFactory.Seconds(w.BoostRemaining)
                    : w.Cooldown > 0 ? "REFILL " + UIFactory.Seconds(w.Cooldown)
                    : Mathf.FloorToInt(w.Fill * 100) + " %");

            if (game.figurines != null)
                foreach (var f in game.figurines)
                {
                    if (!f) continue;
                    var fig = f;
                    Add("Statue", InFront(fig.transform), () =>
                        fig.Pieces == 0 ? "0 / " + fig.PiecesRequired
                        : !fig.IsComplete ? fig.Pieces + " / " + fig.PiecesRequired
                        : fig.IsReadyToSmash ? "SMASH" : Mathf.FloorToInt(fig.GoldFill * 100) + " % GOLD");
                }
        }

        Vector3 InFront(Transform t)
        {
            var b = ClickRouter.RendererBounds(t, t.position);
            var cam = game.GameCamera;
            Vector3 towardCamera = cam ? Vector3.ProjectOnPlane(cam.transform.position - b.center, Vector3.up).normalized : Vector3.back;
            float reach = Mathf.Abs(towardCamera.x) * b.extents.x + Mathf.Abs(towardCamera.z) * b.extents.z;
            return new Vector3(b.center.x, b.min.y, b.center.z) + towardCamera * (reach + plateSize.y * 0.8f);
        }

        void Add(string name, Vector3 deskPoint, Func<string> text)
        {
            var tag = new Tag { name = name, text = text };
            tag.go = new GameObject("Brass tag · " + name);
            var t = tag.go.transform;
            t.SetParent(root, false);

            // Stand on the desk, face the camera, lean back a little like a desk name plate.
            var cam = game.GameCamera;
            Vector3 away = cam ? Vector3.ProjectOnPlane(deskPoint - cam.transform.position, Vector3.up) : Vector3.forward;
            if (away.sqrMagnitude < 1e-6f) away = Vector3.forward;
            t.rotation = Quaternion.LookRotation(away.normalized, Vector3.up) * Quaternion.Euler(tilt, 0, 0);
            float h = plateSize.y;
            t.position = deskPoint + t.up * (h * 0.5f) + Vector3.up * 0.004f;

            // Dark rim behind a brass face.
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "Rim";
            Destroy(rim.GetComponent<Collider>());
            rim.transform.SetParent(t, false);
            rim.transform.localScale = new Vector3(plateSize.x + 0.016f, h + 0.016f, 0.012f);
            rim.transform.localPosition = new Vector3(0, 0, 0.004f);
            rim.GetComponent<Renderer>().sharedMaterial = rimMaterial;

            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            Destroy(face.GetComponent<Collider>());
            face.transform.SetParent(t, false);
            face.transform.localScale = new Vector3(plateSize.x, h, 0.01f);
            face.GetComponent<Renderer>().sharedMaterial = brassMaterial;

            var textGo = new GameObject("Engraving", typeof(RectTransform));
            textGo.transform.SetParent(t, false);
            textGo.transform.localPosition = new Vector3(0, 0, -0.0062f);
            var label = textGo.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(plateSize.x * 0.9f, h * 0.72f);
            label.font = UITheme.TmpDisplay;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.05f;
            label.fontSizeMax = 1.2f;
            label.color = engraving;
            label.text = "";
            tag.label = label;

            tags.Add(tag);
        }

        static Material MakeMaterial(string name, Color color, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader ? shader : Shader.Find("Standard")) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        // ------------------------------------------------------------------ update
        void LateUpdate()
        {
            if (!game) return;
            foreach (var tag in tags)
            {
                string s = tag.text();
                bool show = !string.IsNullOrEmpty(s);
                if (tag.go.activeSelf != show) tag.go.SetActive(show);
                if (show && tag.label.text != s) tag.label.text = s;
            }
        }
    }
}
