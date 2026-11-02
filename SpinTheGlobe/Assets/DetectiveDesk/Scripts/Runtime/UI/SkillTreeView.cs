using System.Collections.Generic;
using System.Text;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Full-screen radial skill tree in the style of Bills Must Be Paid: compact square nodes around
    /// one root, hover a node for a tooltip card (what it does, stat now ▸ after, price), click to buy.
    /// Nodes appear once one of their prerequisites is owned. Drag to pan, scroll to zoom.
    /// Built at runtime from the SkillTreeAsset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillTreeView : MonoBehaviour
    {
        const float NodeSize = 78, RootSize = 96, TooltipWidth = 360;

        sealed class NodeView
        {
            public SkillNodeData data;
            public RectTransform rect;
            public Image fill, border;
            public TextMeshProUGUI label, rank;
            public readonly List<Image> links = new List<Image>();
            public float pulse;
        }

        GameManager game;
        Canvas canvas;
        RectTransform canvasRect, content, tooltip;
        TextMeshProUGUI moneyText, tipTitle, tipBody, tipStats, tipCost;
        readonly Dictionary<string, NodeView> views = new Dictionary<string, NodeView>();
        NodeView hovered;
        float tooltipAge;
        bool dirty = true;
        bool revealAll;

        public bool IsOpen => canvas && canvas.gameObject.activeSelf;

        /// <summary>Testing: show every node, including ones not discovered yet (F6 in the Editor).</summary>
        public bool RevealAll { get => revealAll; set { revealAll = value; dirty = true; } }

        public void Initialize(GameManager owner)
        {
            game = owner;
            Build();
            canvas.gameObject.SetActive(false);
            game.Changed += () => dirty = true;
        }

        void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (!canvas) return;
            if (game.legacy && game.legacy.upgradesPanel) game.legacy.upgradesPanel.SetActive(false);
            canvas.gameObject.SetActive(true);
            dirty = true;
        }

        public void Close()
        {
            if (!canvas) return;
            HideTooltip();
            canvas.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Skill Tree", 100);
            canvasRect = (RectTransform)canvas.transform;
            var back = UIFactory.NewImage("Backdrop", canvas.transform, new Color(0.03f, 0.027f, 0.024f, 0.97f)).rectTransform.Stretch();

            var viewport = UIFactory.NewImage("Viewport", back, new Color(0, 0, 0, 0)).rectTransform.Stretch();
            viewport.offsetMax = new Vector2(0, -64);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UIFactory.NewRect("Content", viewport);
            content.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            content.localScale = Vector3.one * 0.85f;
            var pan = viewport.gameObject.AddComponent<SkillTreePanZoom>();
            pan.content = content;

            var linkLayer = UIFactory.NewRect("Links", content);
            var nodeLayer = UIFactory.NewRect("Nodes", content);

            foreach (var node in game.Tree.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                views[node.id] = CreateNode(node, nodeLayer);
            }
            foreach (var v in views.Values)
            {
                if (v.data.prerequisites == null) continue;
                foreach (var p in v.data.prerequisites)
                {
                    var parent = game.Tree.Find(p);
                    if (parent != null) v.links.Add(Link(linkLayer, parent.position, v.data.position));
                }
            }

            // Header
            var header = UIFactory.NewPanel("Header", back, true, new Color(0.035f, 0.031f, 0.028f, 1)).rectTransform
                .Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 64));
            var titleIcon = UIFactory.NewIcon("tree", header, 34);
            titleIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(34, 34));
            UIFactory.NewTitle("Title", header, "THE DETECTIVE'S LEDGER", 28, TextAnchor.MiddleLeft, UITheme.Gold).rectTransform
                .Place(new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0.5f), new Vector2(70, 0), Vector2.zero);
            moneyText = UIFactory.NewTitle("Money", header, "", 24, TextAnchor.MiddleRight, UITheme.Cream);
            moneyText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(-190, 0));
            UIFactory.NewButton("Close", header, "CLOSE", 18, "close", Close, out _).GetComponent<RectTransform>()
                .Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(140, 44));
            UIFactory.NewText("Help", back, "Hover a node to inspect · click to buy · drag to move · scroll to zoom", 16, TextAnchor.LowerLeft, UIFactory.InkDim).rectTransform
                .Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 14), new Vector2(900, 24));

            BuildTooltip();
        }

        NodeView CreateNode(SkillNodeData node, RectTransform parent)
        {
            float size = node.IsRoot ? RootSize : NodeSize;
            var v = new NodeView { data = node };
            v.fill = UIFactory.NewImage(node.id, parent, Color.black);
            v.rect = v.fill.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), node.position, new Vector2(size, size));
            v.border = UIFactory.AddFrame(v.rect, node.IsRoot || node.currency == Currency.Gold, 0.85f);

            v.label = UIFactory.NewText("Label", v.rect, ShortLabel(node), node.IsRoot ? 14 : 12, TextAnchor.MiddleCenter, UIFactory.Ink);
            v.label.rectTransform.Stretch(9);
            v.rank = UIFactory.NewText("Rank", v.rect, "", 11, TextAnchor.LowerCenter, UIFactory.InkDim);
            v.rank.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 7), new Vector2(0, 16));

            if (node.IsRoot) v.rect.localRotation = Quaternion.Euler(0, 0, 45);           // diamond like the reference root
            if (node.IsRoot) { v.label.rectTransform.localRotation = Quaternion.Euler(0, 0, -45); v.rank.rectTransform.localRotation = Quaternion.Euler(0, 0, -45); }

            var widget = v.rect.gameObject.AddComponent<SkillNodeWidget>();
            widget.nodeId = node.id;
            widget.Entered = w => ShowTooltip(views[w.nodeId]);
            widget.Exited = w => { if (hovered != null && hovered.data.id == w.nodeId) HideTooltip(); };
            widget.Clicked = w => Buy(views[w.nodeId]);
            return v;
        }

        static string ShortLabel(SkillNodeData node)
        {
            string t = node.title ?? node.id;
            int dot = t.IndexOf(" · ", System.StringComparison.Ordinal);
            return dot >= 0 ? t.Substring(dot + 3) : t;
        }

        static Image Link(RectTransform parent, Vector2 a, Vector2 b)
        {
            var img = UIFactory.NewImage("Link", parent, Color.white);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            Vector2 d = b - a;
            rt.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), (a + b) * 0.5f, new Vector2(d.magnitude, 3));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return img;
        }

        void BuildTooltip()
        {
            var card = UIFactory.NewPanel("Tooltip", canvas.transform, true, new Color(0.03f, 0.027f, 0.024f, 0.98f));
            card.raycastTarget = false;
            tooltip = card.rectTransform;
            tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            tooltip.pivot = new Vector2(0, 0.5f);
            tooltip.sizeDelta = new Vector2(TooltipWidth, 0);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(6, -6);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 18);
            layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            tipTitle = UIFactory.NewTitle("Title", tooltip, "", 24, TextAnchor.MiddleCenter, UITheme.Gold);
            tipTitle.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            var rule = UIFactory.NewImage("Rule", tooltip, UITheme.GoldDim);
            rule.raycastTarget = false;
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 1;

            tipBody = Line("Description", 18, UIFactory.Ink, FontStyles.Bold);
            tipStats = Line("Stats", 16, UIFactory.InkDim, FontStyles.Normal);
            tipCost = Line("Cost", 26, UIFactory.Brass, FontStyles.Normal);
            tipCost.font = UITheme.TmpDisplay;
            tooltip.gameObject.SetActive(false);
        }

        TextMeshProUGUI Line(string name, int size, Color color, FontStyles style)
        {
            var t = UIFactory.NewText(name, tooltip, "", size, TextAnchor.MiddleCenter, color);
            t.fontStyle = style;
            var pad = t.gameObject.AddComponent<LayoutElement>();
            pad.minHeight = size + 6;
            return t;
        }

        // ------------------------------------------------------------------ tooltip
        void ShowTooltip(NodeView v)
        {
            hovered = v;
            tooltipAge = 0;
            tooltip.gameObject.SetActive(true);
            FillTooltip();
            PlaceTooltip();
        }

        void HideTooltip()
        {
            hovered = null;
            if (tooltip) tooltip.gameObject.SetActive(false);
        }

        void FillTooltip()
        {
            if (hovered == null) return;
            var node = hovered.data;
            var status = game.Skills.Status(node, game.Wallet, game.Stats, out string reason);
            int rank = game.Skills.Rank(node.id);
            bool maxed = status == NodeStatus.Maxed;

            tipTitle.text = node.title;
            tipBody.text = node.description;

            var sb = new StringBuilder();
            if (node.modifiers != null)
                foreach (var m in node.modifiers)
                {
                    if (m.stat == StatId.None) continue;
                    double now = game.Stats.Permanent(m.stat, m.pinIndex);
                    sb.Append(StatInfo.Name(m.stat)).Append("     ").Append(StatInfo.Format(m.stat, now));
                    if (!maxed) sb.Append("  →  ").Append(StatInfo.Format(m.stat, game.Stats.Preview(m.stat, node.modifiers, 1, m.pinIndex)));
                    sb.Append('\n');
                }
            if (node.maxRank >= DefaultSkillTreeFactory.MasteryRanks) sb.Append("Rank ").Append(rank).Append(" · endless");
            else if (node.maxRank > 1) sb.Append("Rank ").Append(rank).Append(" / ").Append(node.maxRank);
            tipStats.text = sb.ToString().TrimEnd('\n');

            string price = node.currency == Currency.Gold
                ? NumberFormatter.Format(game.Skills.NextCost(node)) + " gold"
                : NumberFormatter.Money(game.Skills.NextCost(node));
            switch (status)
            {
                case NodeStatus.Maxed: tipCost.text = rank > 0 && node.maxRank == 1 ? "OWNED" : "MAXED"; tipCost.color = new Color(0.55f, 0.8f, 0.45f); tipCost.fontSize = 22; break;
                case NodeStatus.Available: tipCost.text = price; tipCost.color = new Color(0.55f, 0.85f, 0.45f); tipCost.fontSize = 26; break;
                case NodeStatus.Unaffordable: tipCost.text = price; tipCost.color = new Color(0.92f, 0.28f, 0.3f); tipCost.fontSize = 26; break;
                default: tipCost.text = price + "\n<size=15>" + reason + "</size>"; tipCost.color = new Color(0.75f, 0.55f, 0.35f); tipCost.fontSize = 22; break;
            }
        }

        void PlaceTooltip()
        {
            if (hovered == null) return;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, hovered.rect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            float half = hovered.rect.rect.width * 0.5f * content.localScale.x + 18;
            Vector2 size = canvasRect.rect.size;
            bool right = local.x + half + TooltipWidth < size.x * 0.5f - 10;
            tooltip.pivot = new Vector2(right ? 0 : 1, 0.5f);
            float h = tooltip.rect.height;
            float y = Mathf.Clamp(local.y, -size.y * 0.5f + h * 0.5f + 10, size.y * 0.5f - h * 0.5f - 74);
            tooltip.anchoredPosition = new Vector2(local.x + (right ? half : -half), y);
        }

        // ------------------------------------------------------------------ buying
        void Buy(NodeView v)
        {
            var status = game.Skills.Status(v.data, game.Wallet, game.Stats, out string reason);
            if (status != NodeStatus.Available)
            {
                v.pulse = -1; // short red flash
                return;
            }
            if (game.PurchaseSkill(v.data.id)) { v.pulse = 1; dirty = true; }
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (dirty) { dirty = false; Refresh(); if (hovered != null) FillTooltip(); }

            if (hovered != null)
            {
                // Pop-in: slight scale and tilt that settles, like a card being dealt.
                tooltipAge += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(tooltipAge / 0.12f);
                float e = 1 - (1 - k) * (1 - k);
                tooltip.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);
                tooltip.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(2.5f, 0f, e));
                PlaceTooltip();
            }

            foreach (var v in views.Values)
            {
                if (v.pulse == 0) continue;
                float s = v.pulse > 0 ? 1 + 0.18f * v.pulse : 1 + 0.06f * Mathf.Sin(v.pulse * 40);
                v.rect.localScale = Vector3.one * s;
                v.pulse = v.pulse > 0 ? Mathf.Max(0, v.pulse - Time.unscaledDeltaTime * 4) : Mathf.Min(0, v.pulse + Time.unscaledDeltaTime * 3);
                if (v.pulse == 0) v.rect.localScale = Vector3.one;
                if (v.pulse < 0) v.border.color = new Color(0.9f, 0.25f, 0.25f);
                else if (v.pulse == 0) dirty = true;
            }
        }

        void Refresh()
        {
            moneyText.text = NumberFormatter.Money(game.Wallet.Money) + "   <color=#8a7550>|</color>   <color=#ffdb8f>" + NumberFormatter.Format(game.Wallet.Gold) + " gold</color>";
            foreach (var v in views.Values)
            {
                var status = game.Skills.Status(v.data, game.Wallet, game.Stats, out _);
                bool visible = revealAll || status != NodeStatus.Hidden;
                v.rect.gameObject.SetActive(visible);
                int rank = game.Skills.Rank(v.data.id);
                Color branch = BranchColor(v.data.branch);
                bool owned = rank > 0;

                Color fill, border;
                if (owned) { fill = Color.Lerp(branch, Color.black, 0.72f); border = Color.Lerp(branch, UITheme.Gold, 0.45f); }
                else
                    switch (status)
                    {
                        case NodeStatus.Available: fill = new Color(0.075f, 0.064f, 0.054f); border = UITheme.Gold; break;
                        case NodeStatus.Capped: fill = new Color(0.07f, 0.055f, 0.045f); border = new Color(0.85f, 0.45f, 0.15f); break;
                        case NodeStatus.Unaffordable: fill = new Color(0.05f, 0.046f, 0.042f); border = new Color(0.45f, 0.38f, 0.27f); break;
                        default: fill = new Color(0.035f, 0.032f, 0.03f); border = new Color(0.22f, 0.2f, 0.17f); break;
                    }
                if (owned && status == NodeStatus.Available) border = UITheme.GoldBright; // more ranks affordable
                v.fill.color = fill;
                if (v.pulse >= 0) v.border.color = border;
                v.label.color = owned || status == NodeStatus.Available ? UIFactory.Ink : status == NodeStatus.Unaffordable ? UIFactory.InkDim : new Color(0.45f, 0.42f, 0.38f);
                v.rank.text = v.data.maxRank >= DefaultSkillTreeFactory.MasteryRanks ? (rank > 0 ? "Lv " + rank : "∞")
                            : v.data.maxRank > 1 ? rank + "/" + v.data.maxRank : "";

                foreach (var link in v.links)
                {
                    link.gameObject.SetActive(visible);
                    link.color = owned ? new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, 0.9f) : new Color(0.3f, 0.27f, 0.22f, 0.6f);
                }
            }
        }

        static Color BranchColor(SkillBranch b)
        {
            switch (b)
            {
                case SkillBranch.Globe: return new Color(0.45f, 0.62f, 0.9f);
                case SkillBranch.Pins: return new Color(0.85f, 0.32f, 0.32f);
                case SkillBranch.CaseFile: return new Color(0.88f, 0.78f, 0.5f);
                case SkillBranch.Whiskey: return new Color(0.95f, 0.6f, 0.2f);
                case SkillBranch.Statues: return new Color(0.4f, 0.75f, 0.5f);
                case SkillBranch.Lootbox: return new Color(0.7f, 0.5f, 0.85f);
                default: return UIFactory.Brass;
            }
        }
    }
}
