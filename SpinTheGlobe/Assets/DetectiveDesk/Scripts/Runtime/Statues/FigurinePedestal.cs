using System.Collections.Generic;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// One statue pedestal. Starts empty; statue pieces (from lootboxes) rebuild the statue part by
    /// part. A complete statue fills with gold as the globe turns; once full it can be smashed with
    /// the hammer for gold coins, and the pedestal is empty again.
    /// The authored bust parts are reused as the "pieces": they are shown progressively.
    /// </summary>
    public sealed class FigurinePedestal : MonoBehaviour
    {
        public GameManager game;
        public Transform bust;
        public Renderer[] bustRenderers;
        [Tooltip("Old restoration seam lines; hidden by the piece system.")]
        public LineRenderer[] restorationSeams;
        public Material glueMaterial, goldSeamMaterial;
        public GameObject[] authoredTierVisuals;
        public Transform scatterFrame;
        public Vector3 scatterCenter;
        public float scatterRadius = .16f;

        public int Index { get; private set; }
        public int Pieces { get; private set; }
        public float GoldFill { get; private set; }
        public int PiecesRequired => game
            ? Mathf.Max(2, game.Balance.statueBasePieces + game.Balance.statuePiecesPerIndex * Index - Mathf.RoundToInt((float)game.Stats.Get(StatId.StatuePieceReduction)))
            : 5;
        public bool IsComplete => Pieces >= PiecesRequired;
        /// <summary>Statues are bought in the Gold shop before lootboxes drop pieces for them.</summary>
        public bool Unlocked => !game || game.Shop.Owns(DetectiveDesk.Progression.GoldShopCatalog.StatueUnlockId(Index));
        public string UnlockTitle => DetectiveDesk.Progression.GoldShopCatalog.Find(DetectiveDesk.Progression.GoldShopCatalog.StatueUnlockId(Index))?.title ?? "a pedestal";
        public bool IsReadyToSmash => IsComplete && GoldFill >= 1f;
        public bool IsBusy { get; set; }
        public Bounds VisualBounds => ClickRouter.RendererBounds(bust ? bust : transform, transform.position);
        public string StatusText =>
            !Unlocked ? "Locked. Buy " + UnlockTitle + " in the Gold shop to build this statue."
            : Pieces == 0 ? "Empty pedestal — statue pieces come from lootboxes."
            : !IsComplete ? "Statue " + Pieces + " / " + PiecesRequired + " pieces."
            : !IsReadyToSmash ? "Filling with gold: " + Mathf.FloorToInt(GoldFill * 100) + "% (spin the globe)."
            : "Solid gold! Click to smash it with the hammer.";

        readonly List<Renderer> parts = new List<Renderer>();
        MaterialPropertyBlock properties;
        static readonly Color Stone = new Color(0.62f, 0.60f, 0.55f);
        static readonly Color Gold = new Color(0.86f, 0.62f, 0.18f);

        public void Bind(GameManager owner, int index)
        {
            game = owner;
            Index = index;
            parts.Clear();
            if (bustRenderers != null) foreach (var r in bustRenderers) if (r) parts.Add(r);
            if (parts.Count == 0 && bust) parts.AddRange(bust.GetComponentsInChildren<Renderer>(true));
            // Pieces appear from the bottom up: base and body first, the head last.
            parts.Sort((a, b) =>
            {
                int c = a.bounds.min.y.CompareTo(b.bounds.min.y);
                return c != 0 ? c : a.bounds.center.y.CompareTo(b.bounds.center.y);
            });
            if (authoredTierVisuals != null) foreach (var v in authoredTierVisuals) if (v) v.SetActive(false);
            if (restorationSeams != null) foreach (var s in restorationSeams) if (s) s.enabled = false;
            properties = new MaterialPropertyBlock();
            FitClickBox();
            Apply();
        }

        /// <summary>
        /// Shrinks the click box to this statue's own models, so a statue standing behind another one can still be
        /// clicked over the front one's shoulder.
        /// </summary>
        void FitClickBox()
        {
            var box = GetComponent<BoxCollider>();
            if (!box) return;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
            var s = transform.lossyScale;
            box.center = transform.InverseTransformPoint(b.center);
            box.size = new Vector3(b.size.x / Mathf.Max(1e-4f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(1e-4f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(1e-4f, Mathf.Abs(s.z)));
        }

        public bool AddPiece()
        {
            if (IsComplete) return false;
            Pieces++;
            Apply();
            return true;
        }

        public void AddGold(float amount)
        {
            if (!IsComplete || IsReadyToSmash || amount <= 0) return;
            bool wasReady = IsReadyToSmash;
            GoldFill = Mathf.Min(1f, GoldFill + amount);
            Apply();
            if (!wasReady && IsReadyToSmash && game) game.ShowHint("Statue " + (Index + 1) + " is solid gold. Smash it with the hammer!");
        }

        public double GoldReward => game ? game.Balance.statueBaseGold * (Index + 1) * game.Stats.Get(StatId.StatuePayout) : 0;

        /// <summary>Resets the pedestal after smashing and returns the gold earned.</summary>
        public double Shatter()
        {
            double reward = GoldReward;
            Pieces = 0;
            GoldFill = 0;
            Apply();
            return reward;
        }

        public void Click()
        {
            if (!game) return;
            if (IsReadyToSmash && !IsBusy && game.Hammer) { game.Hammer.Smash(this); return; }
            game.ShowHint("Statue " + (Index + 1) + ": " + StatusText);
        }

        void Update()
        {
            if (IsReadyToSmash && properties != null) Tint(Gold, 0.25f + 0.15f * Mathf.Sin(Time.time * 4f));
        }

        void Apply()
        {
            if (!Application.isPlaying) return; // the authored desk stays untouched in Edit Mode
            int visible = parts.Count == 0 ? 0 : Mathf.CeilToInt(parts.Count * Mathf.Clamp01(Pieces / (float)Mathf.Max(1, PiecesRequired)));
            for (int i = 0; i < parts.Count; i++) if (parts[i]) parts[i].enabled = i < visible;
            Tint(Color.Lerp(Stone, Gold, IsComplete ? GoldFill : 0), IsComplete ? GoldFill * 0.15f : 0);
        }

        void Tint(Color color, float glow)
        {
            if (properties == null) return;
            float metal = IsComplete ? Mathf.Lerp(0.05f, 0.95f, GoldFill) : 0.05f;
            foreach (var r in parts)
            {
                if (!r) continue;
                r.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                properties.SetFloat("_Metallic", metal);
                properties.SetTexture("_MetallicGlossMap", metal > 0.5f ? Texture2D.whiteTexture : Texture2D.blackTexture);
                properties.SetFloat("_Smoothness", Mathf.Lerp(0.3f, 0.85f, metal));
                properties.SetColor("_EmissionColor", Gold * glow);
                r.SetPropertyBlock(properties);
            }
        }

        void OnMouseDown()
        {
            // Pedestals with a DeskInteractable are routed through it instead.
            if (GetComponent<DeskInteractable>()) return;
            if (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (game && game.WorldInputBlocked) return;
            Click();
        }

        public void LoadState(StatueState s) { Pieces = Mathf.Max(0, s.pieces); GoldFill = Mathf.Clamp01(s.goldFill); Apply(); }
        public StatueState SaveState() => new StatueState { pieces = Pieces, goldFill = GoldFill };
    }
}
