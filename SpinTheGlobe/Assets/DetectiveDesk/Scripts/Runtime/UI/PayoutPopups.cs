using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Cookie Clicker style payout numbers: every money (and gold) payout floats up as its own "+€12" above the globe,
    /// drifts a little sideways and fades out. Mid to late game there is a constant stream of them.
    /// Pooled; when payouts arrive faster than <see cref="minimumGap"/> they are added to the newest number instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PayoutPopups : MonoBehaviour
    {
        [Tooltip("Seconds a number stays on screen.")]
        public float lifetime = 1.4f;
        [Tooltip("Pixels a number rises during its life.")]
        public float rise = 120f;
        [Tooltip("Random horizontal spread around the top of the globe, in pixels.")]
        public float spread = 90f;
        [Tooltip("Payouts closer together than this are merged into one number (keeps it readable late game).")]
        public float minimumGap = 0.06f;
        [Tooltip("Most numbers on screen at once.")]
        public int maxActive = 40;

        sealed class Pop
        {
            public TextMeshProUGUI text;
            public RectTransform rect;
            public float age, life, drift, size;
            public Vector2 start;
            public double amount;
            public bool gold, located;
        }

        GameManager game;
        Canvas canvas;
        RectTransform root;
        Material outlined;
        readonly List<Pop> active = new List<Pop>();
        readonly Stack<Pop> pool = new Stack<Pop>();
        Pop newest;
        float sinceLast = 99;

        public void Initialize(GameManager owner)
        {
            game = owner;
            canvas = UIFactory.CreateCanvas("Payout popups", 35);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            root = (RectTransform)canvas.transform;

            // One shared material with a dark outline so the numbers read on the bright map and the dark desk.
            outlined = new Material(UITheme.TmpDisplay.material) { name = "Payout popup (outlined)" };
            outlined.EnableKeyword("OUTLINE_ON");
            outlined.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            outlined.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.07f, 0.04f, 0.02f, 1f));
            outlined.EnableKeyword("UNDERLAY_ON");
            outlined.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.6f));
            outlined.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            outlined.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.4f);

            game.CashAwarded += OnCash;
            game.GoldAwarded += OnGold;
        }

        void OnDestroy()
        {
            if (game) { game.CashAwarded -= OnCash; game.GoldAwarded -= OnGold; }
            if (canvas) Destroy(canvas.gameObject);
            if (outlined) Destroy(outlined);
        }

        void OnCash(double amount) => Spawn(amount, false);
        void OnGold(double amount) => Spawn(amount, true);

        void Spawn(double amount, bool gold)
        {
            if (!root || amount <= 0) return;
            Vector3? origin = game.PayoutOrigin;
            // Late game: merge rapid globe payouts into the newest money number rather than flooding the screen.
            if (!gold && origin == null && newest != null && !newest.gold && !newest.located && newest.age < 0.25f && (sinceLast < minimumGap || active.Count >= maxActive))
            {
                newest.amount += amount;
                newest.text.text = Label(newest.amount, false);
                sinceLast = 0;
                return;
            }
            if (active.Count >= maxActive) Recycle(active[0]);

            var p = pool.Count > 0 ? pool.Pop() : Create();
            p.amount = amount;
            p.gold = gold;
            p.located = origin != null;
            p.age = 0;
            p.life = lifetime * (gold ? 1.5f : 1f) * Random.Range(0.9f, 1.1f);
            p.drift = Random.Range(-25f, 25f);
            // Big payouts (a case, a jackpot: several seconds of income at once) get a bigger number.
            double perSecond = game.IncomeRate;
            bool big = gold || (perSecond > 0 && amount >= perSecond * 4);
            p.size = gold ? 40 : big ? 38 : 28;
            p.text.fontSize = p.size;
            p.text.color = gold ? UITheme.GoldBright : big ? new Color(1f, 0.86f, 0.45f) : new Color(0.98f, 0.93f, 0.78f);
            p.text.text = Label(amount, gold);
            p.start = origin != null
                ? AnchorPoint(origin.Value) + new Vector2(Random.Range(-20f, 20f), gold ? 46 : 0)
                : AnchorPoint() + new Vector2(Random.Range(-spread, spread), Random.Range(-12f, 18f));
            p.rect.anchoredPosition = p.start;
            p.text.gameObject.SetActive(true);
            p.rect.SetAsLastSibling();
            active.Add(p);
            newest = p;
            sinceLast = 0;
        }

        static string Label(double amount, bool gold) =>
            gold ? "+" + UIFactory.Num(amount) + " gold" : "+" + UIFactory.Money(amount);

        Pop Create()
        {
            var t = UIFactory.NewTitle("Payout", root, "", 28, TextAnchor.MiddleCenter, UITheme.Cream);
            t.NoWrap();
            t.raycastTarget = false;
            t.fontSharedMaterial = outlined;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(320, 60);
            return new Pop { text = t, rect = t.rectTransform };
        }

        void Recycle(Pop p)
        {
            active.Remove(p);
            if (newest == p) newest = null;
            p.text.gameObject.SetActive(false);
            pool.Push(p);
        }

        /// <summary>A world point (e.g. the case file) in canvas coordinates, a little above it.</summary>
        Vector2 AnchorPoint(Vector3 world)
        {
            var cam = game.GameCamera;
            if (cam)
            {
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local))
                    return local + new Vector2(0, 30);
            }
            return AnchorPoint();
        }

        /// <summary>Just above the top of the globe, in canvas coordinates (falls back to upper middle).</summary>
        Vector2 AnchorPoint()
        {
            var cam = game.GameCamera;
            if (cam && game.globe)
            {
                var b = ClickRouter.RendererBounds(game.globe.rotor ? game.globe.rotor : game.globe.transform, game.globe.Center);
                Vector3 top = new Vector3(b.center.x, b.max.y, b.center.z);
                Vector3 screen = cam.WorldToScreenPoint(top);
                if (screen.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local))
                    return local + new Vector2(0, 30);
            }
            return new Vector2(0, root.rect.height * 0.15f);
        }

        void Update()
        {
            sinceLast += Time.unscaledDeltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.age += Time.unscaledDeltaTime;
                float k = p.age / p.life;
                if (k >= 1) { Recycle(p); continue; }
                // Ease out upward, slight sideways drift, pop-in scale, fade over the last 40%.
                float up = 1 - (1 - k) * (1 - k);
                p.rect.anchoredPosition = p.start + new Vector2(p.drift * k, rise * up);
                float pop = p.age < 0.12f ? Mathf.Lerp(1.35f, 1f, p.age / 0.12f) : 1f;
                p.rect.localScale = new Vector3(pop, pop, 1);
                var c = p.text.color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f; p.text.color = c;
            }
        }
    }
}
