using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public sealed class FigurinePedestal : MonoBehaviour
    {
        public GameManager game;
        public Transform bust;
        public Renderer[] bustRenderers;
        public LineRenderer[] restorationSeams;
        public GameObject[] authoredTierVisuals;
        [SerializeField, Range(0, 3)] int tier;
        readonly Dictionary<Transform, Vector3> restPositions = new Dictionary<Transform, Vector3>();
        readonly Dictionary<Transform, Quaternion> restRotations = new Dictionary<Transform, Quaternion>();
        MaterialPropertyBlock properties;
        public int Tier => tier;
        public string TierName => new[] { "Broken rubble", "Glued stone", "Kintsugi", "Gold masterpiece" }[tier];
        public double Multiplier => new double[] { 1, 1.5, 2, 3 }[tier];

        void Awake()
        {
            properties = new MaterialPropertyBlock();
            if (bust) foreach (var t in bust.GetComponentsInChildren<Transform>(true))
            { restPositions[t] = t.localPosition; restRotations[t] = t.localRotation; }
            Apply();
        }
        public void SetTier(int value) { tier = Mathf.Clamp(value, 0, 3); Apply(); }
        void Apply()
        {
            if (!Application.isPlaying) return; // Scene stays as the approved authored desk in Edit Mode.
            if (authoredTierVisuals != null && authoredTierVisuals.Length == 4)
            {
                for (int i = 0; i < 4; i++) if (authoredTierVisuals[i]) authoredTierVisuals[i].SetActive(i == tier);
                return;
            }
            // Reuse authored bust parts, never rewrite their meshes or material assets.
            int index = 0;
            foreach (var entry in restPositions)
            {
                var t = entry.Key; if (!t) continue;
                t.localPosition = entry.Value; t.localRotation = restRotations[t];
                if (tier == 0 && t != bust && t.GetComponent<MeshFilter>())
                {
                    float a = index++ * 2.399963f;
                    t.localPosition = new Vector3(Mathf.Cos(a) * .022f, Mathf.Sin(a) * .018f, .035f + index % 3 * .002f);
                    // Imported bust source is Z-up beneath its axis-correction assembly.
                    t.localRotation = Quaternion.Euler(index * 37 % 160, index * 71 % 170, index * 29 % 180);
                }
            }
            if (properties == null) properties = new MaterialPropertyBlock();
            if (bustRenderers != null) foreach (var r in bustRenderers)
            {
                if (!r) continue; r.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", tier == 3 ? new Color(.83f, .59f, .18f) : tier == 0 ? new Color(.75f, .72f, .64f) : new Color(.48f, .47f, .42f));
                properties.SetFloat("_Metallic", tier == 3 ? .93f : .02f);
                // Existing metal-map keywords must not turn the stone tiers metallic.
                properties.SetTexture("_MetallicGlossMap", tier == 3 ? Texture2D.whiteTexture : Texture2D.blackTexture);
                properties.SetFloat("_Smoothness", tier == 3 ? .82f : .28f);
                r.SetPropertyBlock(properties);
            }
            if (restorationSeams != null) foreach (var seam in restorationSeams)
            {
                if (!seam) continue; seam.enabled = tier == 1 || tier == 2;
                seam.startColor = seam.endColor = tier == 2 ? new Color(1, .59f, .10f) : new Color(.035f, .03f, .025f);
            }
        }
        void OnMouseDown()
        {
            if (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            game?.InspectFigurine(this);
        }
    }
}
