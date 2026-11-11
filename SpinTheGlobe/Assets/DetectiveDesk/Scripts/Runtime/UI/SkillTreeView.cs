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
    /// Uses the authored scene layout, with a builder fallback for scenes that have not been set up yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class SkillTreeView : MonoBehaviour
    {
        const float NodeSize = 78, RootSize = 96, TooltipWidth = 360;

        [System.Serializable]
        sealed class NodeView
        {
            public string id;
            [System.NonSerialized] public SkillNodeData data;
            public RectTransform rect;
            public Image fill, border;
            public TextMeshProUGUI label, rank;
            public Image icon;
            public List<Image> links = new List<Image>();
            public float pulse;
        }

        GameManager game;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform canvasRect, content, tooltip;
        [SerializeField] TextMeshProUGUI moneyText, tipTitle, tipBody, tipStats, tipCost, tipRank;
        readonly Dictionary<string, NodeView> views = new Dictionary<string, NodeView>();
        [SerializeField] List<NodeView> authoredNodes = new List<NodeView>();
        [SerializeField] int authoredLayoutRevision;
        NodeView hovered;
        float tooltipAge;
        bool dirty = true;
        bool revealAll;

        public bool IsOpen => canvas && canvas.gameObject.activeSelf;

        /// <summary>Authoring preview: show every node, including ones not discovered yet.</summary>
        public bool RevealAll { get => revealAll; set { revealAll = value; dirty = true; } }

        public void Initialize(GameManager owner)
        {
            game = owner;
            CreateLayout(owner);
            BindActions();
            canvas.gameObject.SetActive(false);
            game.Changed += () => dirty = true;
        }

        public Canvas CreateLayout(GameManager owner)
        {
            game = owner;
            if (!canvas)
            {
                Build();
                authoredNodes = new List<NodeView>(views.Values);
            }
            else
            {
                views.Clear();
                for (int i = authoredNodes.Count - 1; i >= 0; i--)
                {
                    var node = authoredNodes[i];
                    node.data = game.Tree.Find(node.id);
                    if (node.data != null) { views[node.id] = node; continue; }
                    if (hovered == node) HideTooltip();
                    var obsolete = new List<Component> { node.rect, node.label };
                    foreach (var link in node.links) obsolete.Add(link);
                    foreach (var component in obsolete)
                    {
                        if (!component) continue;
                        component.gameObject.SetActive(false);
                        if (Application.isPlaying) Destroy(component.gameObject);
                        else DestroyImmediate(component.gameObject);
                    }
                    authoredNodes.RemoveAt(i);
                }
            }
            RestoreSections();
            if (authoredLayoutRevision != game.Tree.layoutRevision)
            {
                foreach (var node in views.Values)
                {
                    node.rect.anchoredPosition = node.data.position;
                    float size = node.data.IsRoot ? RootSize : NodeSize;
                    float below = node.data.IsRoot ? size * .71f : size * .5f;
                    node.label.rectTransform.anchoredPosition = node.data.position - new Vector2(0, below + 5);
                    node.label.text = "<mark=#0b0a09E6 padding=\"6,6,2,2\">" + ShortLabel(node.data) + "</mark>";
                }
                foreach (var section in sectionViews.Values)
                {
                    section.rect.anchoredPosition = section.data.position;
                    PlaceSectionLabel(section);
                }
                foreach (var node in views.Values) RebuildNodeLinks(node);
                authoredLayoutRevision = game.Tree.layoutRevision;
            }
            EnsurePurchaseControls();
            return canvas;
        }

        void BindActions()
        {
            SceneViewBindings.Button(canvas.transform, "Backdrop/Header/Close", Close);
            foreach (var node in views.Values) BindNode(node.rect.GetComponent<SkillNodeWidget>());
        }

        void BindNode(SkillNodeWidget widget)
        {
            widget.Entered = w => ShowTooltip(views[w.nodeId]);
            widget.Exited = w => { if (hovered != null && hovered.data.id == w.nodeId) HideTooltip(); };
            widget.Clicked = w => Buy(views[w.nodeId]);
        }

        public void PreviewLayout()
        {
            bool previous = revealAll;
            revealAll = true;
            Refresh();
            revealAll = previous;
        }

        /// <summary>Update one authored branch after changing its data, leaving the other branches untouched.</summary>
        public void SynchronizeBranchLayout(GameManager owner, SkillBranch branch)
        {
            var ids = new List<string>();
            foreach (var node in owner.Tree.Nodes) if (node.branch == branch) ids.Add(node.id);
            SynchronizeNodeLayout(owner, ids.ToArray());
        }

        /// <summary>Update only the specified nodes, retaining other authored positions and styling.</summary>
        public void SynchronizeNodeLayout(GameManager owner, params string[] nodeIds)
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Update authored layouts outside Play mode.");
            CreateLayout(owner);
            var selected = new HashSet<string>(nodeIds);
            foreach (var data in game.Tree.Nodes)
            {
                if (!selected.Contains(data.id)) continue;
                if (!views.TryGetValue(data.id, out var node))
                {
                    NodeView style = null;
                    foreach (var existing in views.Values)
                        if (existing.data.branch == data.branch) { style = existing; break; }
                    node = CreateNode(data, content.Find("Nodes") as RectTransform, content.Find("Labels") as RectTransform);
                    if (style != null)
                    {
                        node.label.font = style.label.font;
                        node.rank.font = style.rank.font;
                        node.border.sprite = style.border.sprite;
                        node.icon.sprite = style.icon.sprite;
                    }
                    views[data.id] = node;
                }
                node.rect.anchoredPosition = data.position;
                float size = data.IsRoot ? RootSize : NodeSize;
                float below = data.IsRoot ? size * 0.71f : size * 0.5f;
                node.label.rectTransform.anchoredPosition = data.position - new Vector2(0, below + 5);
                node.label.text = "<mark=#0b0a09E6 padding=\"6,6,2,2\">" + ShortLabel(data) + "</mark>";
            }
            foreach (var node in views.Values)
                if (selected.Contains(node.id) || (node.data.prerequisites != null
                    && System.Array.Exists(node.data.prerequisites, selected.Contains))) RebuildNodeLinks(node);
            authoredNodes = new List<NodeView>(views.Values);
            RefreshSections();
        }

        // Entry nodes connect to the root; additional requirements stay in the tooltip.
        static bool DrawLink(SkillNodeData parent, SkillNodeData child) => parent != null
            && (parent.IsRoot || (parent.branch == child.branch
                && !System.Array.Exists(child.prerequisites, id => id == DefaultSkillTreeFactory.RootId)));

        void OnDestroy() { if (Application.isPlaying && canvas) Destroy(canvas.gameObject); }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (game && game.PaperBalls && game.PaperBalls.IsThrowing) return;
            if (game && game.PaperBalls) game.PaperBalls.CancelAim();
            if (game && game.LootboxView && game.LootboxView.IsOpen) return;
            if (!canvas) return;
            if (game.legacy && game.legacy.upgradesPanel) game.legacy.upgradesPanel.SetActive(false);
            if (game.ShopView && game.ShopView.IsOpen) game.ShopView.Close();
            canvas.gameObject.SetActive(true);
            Refresh();
            dirty = false;
        }

        /// <summary>True during the frame the tree was closed (so the same Esc press doesn't open the settings).</summary>
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        int closedFrame = -1;

        public void Close()
        {
            if (!canvas) return;
            if (IsOpen) closedFrame = Time.frameCount;
            HideTooltip();
            canvas.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Skill Tree", 100);
            canvasRect = (RectTransform)canvas.transform;
            var back = UIFactory.NewImage("Backdrop", canvas.transform, new Color(0.03f, 0.027f, 0.024f, 1f)).rectTransform.Stretch();

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
            var labelLayer = UIFactory.NewRect("Labels", content);   // above nodes and links, so names stay readable

            foreach (var node in game.Tree.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                views[node.id] = CreateNode(node, nodeLayer, labelLayer);
            }
            EnsureSections();
            foreach (var v in views.Values) RebuildNodeLinks(v);

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
            UIFactory.NewText("Help", back, "Hover: details  ·  Click: buy  ·  Drag: move  ·  Scroll: zoom", 16, TextAnchor.LowerLeft, UIFactory.InkDim).rectTransform
                .Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 14), new Vector2(900, 24));

            BuildTooltip();
        }

        NodeView CreateNode(SkillNodeData node, RectTransform parent, RectTransform labels)
        {
            float size = node.IsRoot ? RootSize : NodeSize;
            var v = new NodeView { id = node.id, data = node };
            v.fill = UIFactory.NewImage(node.id, parent, Color.black);
            v.rect = v.fill.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), node.position, new Vector2(size, size));
            v.border = UIFactory.AddFrame(v.rect, node.IsRoot || node.currency == Currency.Gold, 0.85f);

            // Inside the node: the branch icon (a coin for gold nodes) and the rank. The name sits underneath.
            v.icon = UIFactory.NewIcon(NodeIcon(node), v.rect, size * 0.42f);
            v.icon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(size * 0.42f, size * 0.42f));
            v.rank = UIFactory.NewText("Rank", v.rect, "", 11, TextAnchor.LowerCenter, UIFactory.InkDim);
            v.rank.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(0, 16));

            float below = node.IsRoot ? size * 0.71f : size * 0.5f;
            v.label = UIFactory.NewText("Label " + node.id, labels, "", 16, TextAnchor.UpperCenter, UIFactory.Ink);
            v.label.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), node.position - new Vector2(0, below + 5), new Vector2(150, 48));
            v.label.text = "<mark=#0b0a09E6 padding=\"6,6,2,2\">" + ShortLabel(node) + "</mark>";

            if (node.IsRoot)
            {
                v.rect.localRotation = Quaternion.Euler(0, 0, 45);           // diamond like the reference root
                v.icon.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
                v.rank.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            }

            var widget = v.rect.gameObject.AddComponent<SkillNodeWidget>();
            widget.nodeId = node.id;
            BindNode(widget);
            return v;
        }

        static string NodeIcon(SkillNodeData node)
        {
            if (node.IsRoot) return "tree";
            if (node.currency == Currency.Gold) return "coin";
            switch (node.branch)
            {
                case SkillBranch.Globe: return "globe";
                case SkillBranch.Pins: return "pin";
                case SkillBranch.CaseFile: return "folder";
                case SkillBranch.Whiskey: return "glass";
                case SkillBranch.Statues: return "statue";
                case SkillBranch.Lootbox: return "box";
                default: return "tree";
            }
        }

        static string ShortLabel(SkillNodeData node)
        {
            return (node.title ?? node.id).Replace(" · ", "\n");
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

            tipTitle = UIFactory.NewTitle("Title", tooltip, "", 24, TextAnchor.MiddleLeft, UITheme.Gold);
            tipTitle.gameObject.AddComponent<LayoutElement>().minHeight = 32;
            var rule = UIFactory.NewImage("Rule", tooltip, UITheme.GoldDim);
            rule.raycastTarget = false;
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 1;

            tipBody = Line("Description", 17, UITheme.CreamDim, FontStyles.Normal);
            tipStats = Line("Stats", 20, UIFactory.Ink, FontStyles.Bold);
            tipRank = Line("Level", 15, UITheme.CreamDim, FontStyles.Normal);
            tipCost = Line("Cost", 26, UIFactory.Brass, FontStyles.Normal);
            tipCost.font = UITheme.TmpDisplay;
            tooltip.gameObject.SetActive(false);
        }

        TextMeshProUGUI Line(string name, int size, Color color, FontStyles style)
        {
            var t = UIFactory.NewText(name, tooltip, "", size, TextAnchor.MiddleLeft, color);
            t.fontStyle = style;
            var pad = t.gameObject.AddComponent<LayoutElement>();
            pad.minHeight = size + 6;
            return t;
        }

        // ------------------------------------------------------------------ tooltip
        void ShowTooltip(NodeView v)
        {
            hoveredSection = null;
            hovered = v;
            tooltipAge = 0;
            tooltip.gameObject.SetActive(true);
            FillTooltip();
            PlaceTooltip();
        }

        void HideTooltip()
        {
            hovered = null;
            hoveredSection = null;
            if (tooltip) tooltip.gameObject.SetActive(false);
        }

        void FillTooltip()
        {
            if (hoveredSection != null) { FillSectionTooltip(); return; }
            if (hovered == null) return;
            tipCost.gameObject.SetActive(true);
            var node = hovered.data;
            var status = game.Skills.Status(node, game.Wallet, game.Stats, out string reason);
            int rank = game.Skills.Rank(node.id);
            int quantity = PurchaseQuote(node, out double totalCost);
            int previewRanks = quantity > 0 ? quantity : 1;
            bool maxed = status == NodeStatus.Maxed;
            var colors = PurchasePalette.For(status);

            tipTitle.text = node.title;
            tooltip.GetComponent<Image>().color = colors.fill;
            tooltip.Find("Frame").GetComponent<Image>().color = colors.border;
            tooltip.Find("Rule").GetComponent<Image>().color = colors.border;
            tipTitle.color = colors.text;
            tipBody.color = colors.text;
            tipStats.color = colors.text;
            tipRank.color = colors.text;
            tipCost.color = colors.price;
            tipBody.text = node.description;

            var sb = new StringBuilder();
            if (node.modifiers != null)
                foreach (var m in node.modifiers)
                {
                    if (m.stat == StatId.None) continue;
                    double now = game.Stats.Permanent(m.stat, m.pinIndex);
                    sb.Append(StatInfo.Name(m.stat)).Append("     ").Append(StatInfo.Format(m.stat, now));
                    if (!maxed) sb.Append("  →  ").Append(StatInfo.Format(m.stat, game.Stats.Preview(m.stat, node.modifiers, previewRanks, m.pinIndex)));
                    sb.Append('\n');
                }
            tipStats.text = sb.ToString().TrimEnd('\n');
            tipStats.gameObject.SetActive(sb.Length > 0);
            tipRank.gameObject.SetActive(node.maxRank > 1);
            tipRank.text = node.maxRank >= DefaultSkillTreeFactory.MasteryRanks
                ? "Level " + rank
                : "Level " + rank + " / " + node.maxRank;

            string price = node.currency == Currency.Gold
                ? NumberFormatter.Format(quantity > 0 ? totalCost : game.Skills.NextCost(node)) + " gold"
                : NumberFormatter.Money(quantity > 0 ? totalCost : game.Skills.NextCost(node));
            if (quantity > 0 && node.maxRank > 1 && purchaseAmount > 1)
                price = "Buy " + quantity + " level" + (quantity == 1 ? "" : "s") + " · " + price;
            switch (status)
            {
                case NodeStatus.Maxed: tipCost.text = rank > 0 && node.maxRank == 1 ? "OWNED" : "MAXED"; tipCost.fontSize = 22; break;
                case NodeStatus.Available: tipCost.text = price; tipCost.fontSize = 26; break;
                case NodeStatus.Unaffordable: tipCost.text = price; tipCost.fontSize = 26; break;
                default:
                    if (status == NodeStatus.Capped) reason = "Raise " + StatInfo.Name(node.capByStat) + " first.";
                    else if (status == NodeStatus.Locked && node.prerequisites != null)
                    {
                        var missing = new List<string>();
                        foreach (var id in node.prerequisites)
                            if (!game.Skills.Owns(id)) missing.Add(game.Tree.Find(id)?.title ?? "an earlier upgrade");
                        reason = "Needs " + string.Join(", ", missing) + ".";
                    }
                    tipCost.text = price + "\n<size=15>" + reason + "</size>";
                    tipCost.fontSize = 22; break;
            }
        }

        void PlaceTooltip()
        {
            var targetRect = hovered != null ? hovered.rect : hoveredSection?.rect;
            if (!targetRect) return;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, targetRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            float half = targetRect.rect.width * 0.5f * content.localScale.x + 18;
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
                v.pulse = -1; // short shake; retain the purchase-state colour
                return;
            }
            if (game.PurchaseSkill(v.data.id, purchaseAmount)) { v.pulse = 1; dirty = true; }
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (dirty) { dirty = false; Refresh(); if (hovered != null || hoveredSection != null) FillTooltip(); }

            if (hovered != null || hoveredSection != null)
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
                if (v.pulse == 0) dirty = true;
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
                v.label.gameObject.SetActive(visible);
                int rank = game.Skills.Rank(v.data.id);
                var colors = PurchasePalette.For(status);
                v.fill.color = colors.fill;
                v.border.color = colors.border;
                v.label.color = colors.text;
                v.rank.color = colors.text;
                v.icon.color = colors.icon;
                v.rank.text = v.data.maxRank >= DefaultSkillTreeFactory.MasteryRanks ? (rank > 0 ? "Lv " + rank : "∞")
                            : v.data.maxRank > 1 ? rank + "/" + v.data.maxRank : "";

                foreach (var link in v.links)
                {
                    link.gameObject.SetActive(visible);
                    link.color = new Color(colors.border.r, colors.border.g, colors.border.b, .65f);
                }
            }
            RefreshSections();
        }

    }
}
