using System.Collections.Generic;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// The Gold shop: a plain list menu (not a tree) for everything bought with gold, plus "Retire the agency"
    /// (prestige) at the top. Nothing in here resets when retiring. Opened from the GOLD SHOP button (or G).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoldShopView : MonoBehaviour
    {
        const float PanelWidth = 960, PanelHeight = 900;

        sealed class Row
        {
            public GoldShopItem item;
            public TextMeshProUGUI title, description, buttonLabel;
            public Button button;
            public Image background;
        }

        GameManager game;
        Canvas canvas;
        RectTransform dialog;
        Button hudButton, retireButton;
        TextMeshProUGUI goldText, retireInfo, retireLabel, prestigeText, dialogText, hudRank;
        readonly List<Row> rows = new List<Row>();
        float refreshTimer;
        int closedFrame = -1;

        public bool IsOpen => canvas && canvas.gameObject.activeSelf;
        public bool ClosedThisFrame => closedFrame == Time.frameCount;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Build();
            BuildHudButton();
            canvas.gameObject.SetActive(false);
        }

        void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (!canvas) return;
            if (game.TreeView && game.TreeView.IsOpen) game.TreeView.Close();
            if (game.Settings && game.Settings.IsOpen) game.Settings.Close();
            HideDialog();
            canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!canvas || !IsOpen) return;
            HideDialog();
            canvas.gameObject.SetActive(false);
            closedFrame = Time.frameCount;
        }

        void Update()
        {
            if (!game) return;
            UpdateHudButton();
            if (Input.GetKeyDown(KeyCode.G) && !(game.Settings && game.Settings.IsOpen)) { Toggle(); return; }
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { if (dialog.gameObject.activeSelf) HideDialog(); else Close(); return; }
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0) Refresh();
        }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Gold Shop", 110);
            var dim = UIFactory.NewImage("Dim", canvas.transform, new Color(0, 0, 0, 0.72f));
            dim.rectTransform.Stretch();

            var panel = UIFactory.NewPanel("Gold shop panel", canvas.transform, true, new Color(0.03f, 0.027f, 0.024f, 0.99f)).rectTransform;
            panel.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(34, 34, 22, 26);
            v.spacing = 10;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            // Header: title, gold, close.
            var header = HRow(panel, "Header", 48);
            var title = UIFactory.NewTitle("Title", header, "GOLD SHOP", 30, TextAnchor.MiddleLeft, UITheme.Gold);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            prestigeText = UIFactory.NewText("Prestiges", header, "", 18, TextAnchor.MiddleRight, UITheme.CreamDim);
            Fixed(prestigeText.gameObject, 340, 48);
            Fixed(UIFactory.NewIcon("coin", header, 30).gameObject, 30, 30);
            goldText = UIFactory.NewTitle("Gold", header, "", 28, TextAnchor.MiddleLeft, UITheme.GoldBright);
            Fixed(goldText.gameObject, 110, 48);
            Fixed(UIFactory.NewButton("Close", header, "", 16, "close", Close, out _).gameObject, 46, 40);

            BuildRetireCard(panel);

            // Scrolling list of items, grouped by category.
            var scrollRoot = UIFactory.NewRect("List", panel);
            scrollRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var viewport = UIFactory.NewImage("Viewport", scrollRoot, new Color(0, 0, 0, 0.001f)).rectTransform;
            viewport.Stretch();
            viewport.offsetMax = new Vector2(-14, 0);   // room for the scrollbar
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIFactory.NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var cv = content.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 8; cv.padding = new RectOffset(0, 6, 0, 10);
            cv.childControlWidth = cv.childControlHeight = true;
            cv.childForceExpandWidth = true; cv.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            scroll.verticalScrollbar = BuildScrollbar(scrollRoot);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            Section(content, "UNLOCKS", "New things for the desk.", GoldShopCategory.Unlock);
            Section(content, "PERMANENT", "Bonuses that are always active.", GoldShopCategory.Permanent);
            Section(content, "FRESH START", "Head starts for every new run after retiring.", GoldShopCategory.FreshStart);

            BuildDialog();
        }

        Scrollbar BuildScrollbar(RectTransform parent)
        {
            var track = UIFactory.NewImage("Scrollbar", parent, new Color(0, 0, 0, 0.5f));
            track.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(6, 0));
            var area = UIFactory.NewRect("Sliding area", track.transform);
            area.Stretch();
            var handle = UIFactory.NewImage("Handle", area, UITheme.GoldDim);
            handle.rectTransform.Stretch();
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            var nav = bar.navigation; nav.mode = Navigation.Mode.None; bar.navigation = nav;
            return bar;
        }

        void BuildRetireCard(RectTransform panel)
        {
            var card = UIFactory.NewPanel("Retire", panel, false, new Color(0.09f, 0.06f, 0.035f, 1f)).rectTransform;
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 118;
            var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(22, 18, 14, 14);
            h.spacing = 16;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            var text = UIFactory.NewRect("Text", card);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var tv = text.gameObject.AddComponent<VerticalLayoutGroup>();
            tv.spacing = 4; tv.childControlWidth = tv.childControlHeight = true; tv.childForceExpandWidth = true; tv.childForceExpandHeight = false;
            UIFactory.NewTitle("Title", text, "RETIRE THE AGENCY", 24, TextAnchor.MiddleLeft, UITheme.Gold);
            retireInfo = UIFactory.NewText("Info", text, "", 17, TextAnchor.UpperLeft, UITheme.Cream);

            retireButton = UIFactory.NewButton("Retire", card, "", 20, "coin", AskRetire, out retireLabel);
            Fixed(retireButton.gameObject, 250, 60);
        }

        void Section(RectTransform content, string title, string subtitle, GoldShopCategory category)
        {
            var head = UIFactory.NewRect(title, content);
            head.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            var t = UIFactory.NewTitle("Title", head, title + "   <size=70%><color=#a89a80>" + subtitle + "</color></size>", 20, TextAnchor.LowerLeft, UITheme.Gold);
            t.rectTransform.Stretch();
            t.rectTransform.offsetMin = new Vector2(4, 6);
            var rule = UIFactory.NewImage("Rule", head, UITheme.GoldDim);
            rule.raycastTarget = false;
            rule.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1));

            foreach (var item in GoldShopCatalog.Items)
                if (item.category == category) rows.Add(BuildRow(content, item));
        }

        Row BuildRow(RectTransform content, GoldShopItem item)
        {
            var row = new Row { item = item };
            row.background = UIFactory.NewImage(item.id, content, new Color(1, 1, 1, 0.035f));
            var rt = row.background.rectTransform;
            rt.gameObject.AddComponent<LayoutElement>().minHeight = 76;
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 14, 10, 10);
            h.spacing = 16;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            var text = UIFactory.NewRect("Text", rt);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var tv = text.gameObject.AddComponent<VerticalLayoutGroup>();
            tv.spacing = 2; tv.childControlWidth = tv.childControlHeight = true; tv.childForceExpandWidth = true; tv.childForceExpandHeight = false;
            row.title = UIFactory.NewTitle("Title", text, item.title, 20, TextAnchor.MiddleLeft, UITheme.Cream);
            row.description = UIFactory.NewText("Description", text, item.description, 16, TextAnchor.UpperLeft, UITheme.CreamDim);

            string id = item.id;
            row.button = UIFactory.NewButton("Buy", rt, "", 18, () => { if (game.PurchaseShopItem(id)) Refresh(); }, out row.buttonLabel);
            Fixed(row.button.gameObject, 190, 50);
            return row;
        }

        RectTransform HRow(RectTransform parent, string name, float height)
        {
            var row = UIFactory.NewRect(name, parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        static void Fixed(GameObject go, float width, float height)
        {
            if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.minHeight = le.preferredHeight = height;
            le.flexibleWidth = 0;
        }

        // ------------------------------------------------------------------ HUD button
        void BuildHudButton()
        {
            var parent = game.Hud && game.Hud.Canvas ? game.Hud.Canvas.transform : canvas.transform;
            hudButton = UIFactory.NewButton("Gold shop", parent, "GOLD SHOP", 24, "coin", Toggle, out _);
            hudButton.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-284, -20), new Vector2(240, 66));
            hudButton.gameObject.SetActive(false);

            // Detective rank under the button, like the unlock hint under UPGRADES.
            hudRank = UIFactory.NewText("Rank", parent, "", 17, TextAnchor.UpperCenter, UITheme.CreamDim);
            hudRank.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-284, -92), new Vector2(240, 24));
            hudRank.raycastTarget = false;
            hudRank.gameObject.SetActive(false);
        }

        /// <summary>"Inspector · +50% income" (the rank bonus is left out while it is zero).</summary>
        string RankLine()
        {
            var r = game.Rank;
            return r.title + (r.incomeBonus > 0 ? "  ·  +" + Mathf.RoundToInt(r.incomeBonus * 100) + "% income" : "");
        }

        /// <summary>The button appears once the shop matters: the skill tree is open, or there is gold or a retirement.</summary>
        void UpdateHudButton()
        {
            if (!hudButton) return;
            bool show = game.Skills.Owns(DefaultSkillTreeFactory.RootId) || game.Wallet.Gold > 0 || game.Prestiges > 0 || game.CanRetire;
            if (hudButton.gameObject.activeSelf != show) hudButton.gameObject.SetActive(show);
            if (hudRank)
            {
                if (hudRank.gameObject.activeSelf != show) hudRank.gameObject.SetActive(show);
                if (show) { string line = RankLine(); if (hudRank.text != line) hudRank.text = line; }
            }
        }

        // ------------------------------------------------------------------ refresh
        void Refresh()
        {
            refreshTimer = 0.25f;
            if (!game) return;
            goldText.text = NumberFormatter.Format(game.Wallet.Gold);
            string next = game.TryNextRank(out var nextRank)
                ? "  <color=#a89a80>next: " + nextRank.title + " (" + (nextRank.retirements - game.Prestiges) + " more)</color>" : "";
            prestigeText.text = "<color=#e8d5a8>" + RankLine() + "</color>" + next;

            double gain = game.PrestigeGold;
            if (game.CanRetire)
            {
                retireInfo.text = "Earned this run: " + NumberFormatter.Money(game.RunEarnings) + ". Money, the skill tree, pins, cases and items reset. " +
                                  "Gold, the Gold shop and your statues are kept.";
                retireLabel.text = "RETIRE  +" + NumberFormatter.Format(gain);
                retireButton.interactable = true;
            }
            else
            {
                retireInfo.text = "Earn " + NumberFormatter.Money(game.Balance.prestigeThreshold) + " in one run to retire for gold. " +
                                  "This run: " + NumberFormatter.Money(game.RunEarnings) + ".";
                retireLabel.text = "NOT YET";
                retireButton.interactable = false;
            }

            var shop = game.Shop;
            foreach (var r in rows)
            {
                var item = r.item;
                int rank = shop.Rank(item.id);
                bool maxed = shop.IsMaxed(item), locked = shop.IsLocked(item);
                r.title.text = item.title + (item.maxRank > 1 ? "  <size=75%><color=#a89a80>" + rank + " / " + item.maxRank + "</color></size>" : "");
                string requires = locked ? GoldShopCatalog.Find(item.requires)?.title : null;
                r.description.text = item.description + (requires != null ? "  <color=#c0643c>Requires " + requires + ".</color>" : "");
                if (maxed)
                {
                    r.buttonLabel.text = item.maxRank > 1 ? "MAXED" : "OWNED";
                    r.button.interactable = false;
                }
                else if (locked)
                {
                    r.buttonLabel.text = "LOCKED";
                    r.button.interactable = false;
                }
                else
                {
                    double cost = shop.NextCost(item);
                    r.buttonLabel.text = NumberFormatter.Format(cost) + " GOLD";
                    r.button.interactable = game.Wallet.CanAfford(Currency.Gold, cost);
                }
                r.title.color = maxed ? UITheme.Gold : locked ? UITheme.CreamDim : UITheme.Cream;
                r.background.color = maxed ? new Color(0.85f, 0.65f, 0.25f, 0.07f) : new Color(1, 1, 1, 0.035f);
            }
        }

        // ------------------------------------------------------------------ retire confirmation
        void BuildDialog()
        {
            var shade = UIFactory.NewImage("Dialog", canvas.transform, new Color(0, 0, 0, 0.6f));
            shade.rectTransform.Stretch();
            dialog = shade.rectTransform;

            var box = UIFactory.NewPanel("Box", dialog, true, new Color(0.035f, 0.025f, 0.022f, 1f)).rectTransform;
            box.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 0));
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(32, 32, 24, 26);
            v.spacing = 14;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIFactory.NewTitle("Title", box, "RETIRE THE AGENCY?", 26, TextAnchor.MiddleCenter, UITheme.Gold);
            dialogText = UIFactory.NewText("Text", box, "", 19, TextAnchor.MiddleCenter, UITheme.Cream);

            var row = UIFactory.NewRect("Buttons", box);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            UIFactory.NewButton("Cancel", row, "CANCEL", 18, HideDialog, out _);
            UIFactory.NewButton("Confirm", row, "RETIRE", 18, () => { HideDialog(); game.Retire(); }, out _);
            dialog.gameObject.SetActive(false);
        }

        void AskRetire()
        {
            if (!game.CanRetire) return;
            dialogText.text = "You receive <color=#f2c75c>+" + NumberFormatter.Format(game.PrestigeGold) + " gold</color>.\n" +
                              "Money, the skill tree, pins, cases, the whiskey glass and your items start over.\n" +
                              "Gold, the Gold shop, statues and settings are kept.";
            dialog.gameObject.SetActive(true);
        }

        void HideDialog() { if (dialog) dialog.gameObject.SetActive(false); }
    }
}
