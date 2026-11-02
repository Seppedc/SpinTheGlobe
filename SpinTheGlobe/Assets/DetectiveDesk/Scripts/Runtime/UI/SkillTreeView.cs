using System.Collections.Generic;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Full-screen radial skill tree (Bills Must Be Paid style): one root in the middle, branches
    /// outward. Nodes appear when a prerequisite is owned. Drag to pan, scroll to zoom, click a node
    /// to inspect, Buy to purchase. Built at runtime from the SkillTreeAsset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillTreeView : MonoBehaviour
    {
        const float NodeW = 150, NodeH = 62;

        sealed class NodeView
        {
            public SkillNodeData data;
            public RectTransform rect;
            public Image fill;
            public Outline outline;
            public Text label;
            public readonly List<Image> links = new List<Image>();
        }

        GameManager game;
        Canvas canvas;
        RectTransform content;
        Text detailTitle, detailBody, detailCost, moneyText;
        Button buyButton;
        Text buyLabel;
        readonly Dictionary<string, NodeView> views = new Dictionary<string, NodeView>();
        string selected;
        bool dirty = true;

        public bool IsOpen => canvas && canvas.gameObject.activeSelf;

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
            if (selected == null) Select(DefaultSkillTreeFactory.RootId);
            dirty = true;
        }

        public void Close() { if (canvas) canvas.gameObject.SetActive(false); }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Skill Tree", 100);
            var back = UIFactory.NewImage("Backdrop", canvas.transform, new Color(0.04f, 0.03f, 0.025f, 0.94f)).rectTransform.Stretch();

            var viewport = UIFactory.NewImage("Viewport", back, new Color(0, 0, 0, 0)).rectTransform.Stretch();
            viewport.offsetMax = new Vector2(-420, -70);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UIFactory.NewRect("Content", viewport);
            content.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            content.localScale = Vector3.one * 0.7f;
            var pan = viewport.gameObject.AddComponent<SkillTreePanZoom>();
            pan.content = content;

            var linkLayer = UIFactory.NewRect("Links", content);
            var nodeLayer = UIFactory.NewRect("Nodes", content);

            foreach (var node in game.Tree.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                var v = new NodeView { data = node };
                var img = UIFactory.NewImage(node.id, nodeLayer, UIFactory.ButtonColor);
                v.rect = img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), node.position, new Vector2(NodeW, NodeH));
                v.fill = img;
                v.outline = img.gameObject.AddComponent<Outline>();
                v.outline.effectDistance = new Vector2(2, -2);
                var button = img.gameObject.AddComponent<Button>();
                string id = node.id;
                button.onClick.AddListener(() => Select(id));
                v.label = UIFactory.NewText("Label", img.transform, node.title, 15, TextAnchor.MiddleCenter, UIFactory.Ink);
                v.label.rectTransform.Stretch(4);
                views[node.id] = v;
            }
            foreach (var v in views.Values)
            {
                if (v.data.prerequisites == null) continue;
                foreach (var p in v.data.prerequisites)
                {
                    var parent = game.Tree.Find(p);
                    if (parent == null) continue;
                    v.links.Add(Link(linkLayer, parent.position, v.data.position));
                }
            }

            // Header
            var header = UIFactory.NewImage("Header", back, new Color(0.10f, 0.07f, 0.05f, 1)).rectTransform
                .Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 70));
            UIFactory.NewText("Title", header, "THE DETECTIVE'S LEDGER", 30, TextAnchor.MiddleLeft, UIFactory.Brass).rectTransform
                .Place(new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0.5f), new Vector2(24, 0), Vector2.zero);
            moneyText = UIFactory.NewText("Money", header, "", 24, TextAnchor.MiddleRight, UIFactory.Ink);
            moneyText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(-170, 0));
            UIFactory.NewButton("Close", header, "Close  [T]", 18, Close).GetComponent<RectTransform>()
                .Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(120, 44));
            UIFactory.NewText("Help", back, "Drag to move · scroll to zoom · click a node", 16, TextAnchor.LowerLeft, UIFactory.InkDim).rectTransform
                .Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 14), new Vector2(600, 24));

            // Detail panel
            var panel = UIFactory.NewImage("Details", back, new Color(0.12f, 0.085f, 0.06f, 1)).rectTransform;
            panel.anchorMin = new Vector2(1, 0); panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(1, 0.5f);
            panel.offsetMin = new Vector2(-410, 10); panel.offsetMax = new Vector2(-10, -80);
            detailTitle = UIFactory.NewText("Title", panel, "", 26, TextAnchor.UpperLeft, UIFactory.Brass);
            detailTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(-36, 70));
            detailBody = UIFactory.NewText("Body", panel, "", 19, TextAnchor.UpperLeft, UIFactory.Ink);
            detailBody.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(-36, 300));
            detailCost = UIFactory.NewText("Cost", panel, "", 20, TextAnchor.LowerLeft, UIFactory.Ink);
            detailCost.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(-36, 80));
            buyButton = UIFactory.NewButton("Buy", panel, "Buy", 22, Buy, out buyLabel);
            buyButton.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(-36, 56));
        }

        static Image Link(RectTransform parent, Vector2 a, Vector2 b)
        {
            var img = UIFactory.NewImage("Link", parent, Color.white);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            Vector2 d = b - a;
            rt.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), (a + b) * 0.5f, new Vector2(d.magnitude, 5));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return img;
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (dirty) { dirty = false; Refresh(); }
        }

        void Refresh()
        {
            moneyText.text = NumberFormatter.Money(game.Wallet.Money) + "   ·   " + NumberFormatter.Format(game.Wallet.Gold) + " gold";
            foreach (var v in views.Values)
            {
                var status = game.Skills.Status(v.data, game.Wallet, game.Stats, out _);
                bool visible = status != NodeStatus.Hidden;
                v.rect.gameObject.SetActive(visible);
                Color border = BranchColor(v.data.branch);
                Color fill;
                switch (status)
                {
                    case NodeStatus.Maxed: fill = new Color(0.20f, 0.30f, 0.16f, 1); break;
                    case NodeStatus.Available: fill = new Color(0.42f, 0.30f, 0.12f, 1); break;
                    case NodeStatus.Unaffordable: fill = new Color(0.22f, 0.17f, 0.12f, 1); break;
                    case NodeStatus.Capped: fill = new Color(0.36f, 0.20f, 0.10f, 1); break;
                    default: fill = new Color(0.13f, 0.12f, 0.11f, 1); border *= 0.5f; break;
                }
                v.fill.color = fill;
                v.outline.effectColor = v.data.id == selected ? Color.white : border;
                int rank = game.Skills.Rank(v.data.id);
                v.label.text = v.data.title + (v.data.maxRank > 1 ? "\n" + rank + " / " + v.data.maxRank : rank > 0 ? "\n✓" : "");
                v.label.color = status == NodeStatus.Locked ? UIFactory.InkDim : UIFactory.Ink;
                foreach (var link in v.links)
                {
                    link.gameObject.SetActive(visible);
                    link.color = game.Skills.Owns(v.data.id) ? new Color(border.r, border.g, border.b, 0.9f) : new Color(0.4f, 0.35f, 0.3f, 0.5f);
                }
            }
            RefreshDetails();
        }

        void Select(string id) { selected = id; dirty = true; }

        void RefreshDetails()
        {
            var node = selected != null ? game.Tree.Find(selected) : null;
            if (node == null) { detailTitle.text = ""; detailBody.text = "Select a node."; detailCost.text = ""; buyButton.gameObject.SetActive(false); return; }
            var status = game.Skills.Status(node, game.Wallet, game.Stats, out string reason);
            int rank = game.Skills.Rank(node.id);
            detailTitle.text = node.title;
            detailBody.text = node.description + "\n\n" + BranchName(node.branch) + "\nRank " + rank + " / " + node.maxRank;
            if (status == NodeStatus.Maxed) detailCost.text = "Fully upgraded.";
            else detailCost.text = "Cost: " + (node.currency == Currency.Gold ? NumberFormatter.Format(game.Skills.NextCost(node)) + " gold" : NumberFormatter.Money(game.Skills.NextCost(node))) +
                                   (status == NodeStatus.Available ? "" : "\n" + reason);
            buyButton.gameObject.SetActive(status != NodeStatus.Maxed);
            buyButton.interactable = status == NodeStatus.Available;
            buyLabel.text = rank == 0 ? "Unlock" : "Upgrade";
        }

        void Buy()
        {
            if (selected == null) return;
            if (game.PurchaseSkill(selected)) dirty = true;
        }

        static Color BranchColor(SkillBranch b)
        {
            switch (b)
            {
                case SkillBranch.Globe: return new Color(0.35f, 0.65f, 0.95f);
                case SkillBranch.Pins: return new Color(0.92f, 0.30f, 0.28f);
                case SkillBranch.CaseFile: return new Color(0.90f, 0.82f, 0.55f);
                case SkillBranch.Whiskey: return new Color(0.95f, 0.58f, 0.15f);
                case SkillBranch.Statues: return new Color(0.95f, 0.80f, 0.25f);
                default: return UIFactory.Brass;
            }
        }

        static string BranchName(SkillBranch b) =>
            b == SkillBranch.Root ? "Root" : b == SkillBranch.CaseFile ? "Case file branch" : b + " branch";
    }

}
