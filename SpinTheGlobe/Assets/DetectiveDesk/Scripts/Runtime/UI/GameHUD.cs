using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Runtime-built HUD in the noir-ledger style (<see cref="UITheme"/>):
    /// top-left bar (settings | money | income per minute | gold), top-right UPGRADES button,
    /// bottom bar with the pins, case file, whiskey, desk and lootbox sections, a toast line and a "+€x" payout pop.
    /// Replaces the authored top-left HUD and buttons of the legacy canvas (they are hidden at start).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHUD : MonoBehaviour
    {
        GameManager game;
        Canvas canvas;
        Text moneyText, incomeText, goldText, upgradesHint;
        Text pinsText, caseText, whiskeyText, inventoryText, lootText, popText, toastText;
        CanvasGroup toastGroup;
        float toastTimer;
        Button fireButton, stampButton, sipButton, throwButton, lootButton;
        Image whiskeyBar;
        float popTimer;
        double popAmount;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Build();
            game.CashAwarded += OnCash;
        }

        void OnDestroy() { if (game) game.CashAwarded -= OnCash; if (canvas) Destroy(canvas.gameObject); }

        void OnCash(double amount) { popAmount = popTimer > 0 ? popAmount + amount : amount; popTimer = 1.2f; }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Game HUD", 40);
            BuildTopBar();
            BuildUpgradesButton();
            BuildBottomBar();

            popText = UIFactory.NewTitle("Payout pop", canvas.transform, "", 34, TextAnchor.MiddleCenter, UITheme.GoldBright);
            popText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 175), new Vector2(500, 50));
            var shadow = popText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.85f);

            var toast = UIFactory.NewPanel("Message", canvas.transform);
            toast.raycastTarget = false;
            toast.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 232), new Vector2(1100, 50));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false; toastGroup.interactable = false;
            toastText = UIFactory.NewText("Text", toast.transform, "", 21, TextAnchor.MiddleCenter, UITheme.Cream);
            toastText.rectTransform.Stretch(10);
            toast.gameObject.SetActive(false);

            HideLegacyHud();
        }

        void BuildTopBar()
        {
            var bar = UIFactory.NewPanel("Top bar", canvas.transform).rectTransform;
            bar.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(0, 66));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 26, 9, 9);
            h.spacing = 14;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            IconButton(bar, "gear", 44, () => { if (game.legacy) game.legacy.ToggleSettings(); });
            UIFactory.NewDivider(bar, 46);
            UIFactory.NewIcon("money", bar, 34);
            moneyText = Value(bar, 30, UITheme.Cream, true);
            UIFactory.NewDivider(bar, 46);
            UIFactory.NewIcon("chart", bar, 30);
            incomeText = Value(bar, 22, UITheme.Cream, false);
            UIFactory.NewDivider(bar, 46);
            UIFactory.NewIcon("coin", bar, 32);
            goldText = Value(bar, 28, UITheme.GoldBright, true);
        }

        static Text Value(Transform parent, int size, Color color, bool display)
        {
            var t = display ? UIFactory.NewTitle("Value", parent, "", size, TextAnchor.MiddleLeft, color)
                            : UIFactory.NewText("Value", parent, "", size, TextAnchor.MiddleLeft, color);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.gameObject.AddComponent<LayoutElement>().minWidth = 60;
            return t;
        }

        static void IconButton(Transform parent, string icon, float size, UnityEngine.Events.UnityAction action)
        {
            var hit = UIFactory.NewImage("Button " + icon, parent, new Color(0, 0, 0, 0));
            var le = hit.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = le.preferredHeight = le.minHeight = size;
            var button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
            button.onClick.AddListener(action);
            var img = UIFactory.NewIcon(icon, hit.transform, size);
            img.rectTransform.Stretch();
            var themed = hit.gameObject.AddComponent<ThemedButton>();
            themed.button = button; themed.icon = img;
        }

        void BuildUpgradesButton()
        {
            var b = UIFactory.NewButton("Upgrades", canvas.transform, "UPGRADES", 26, "tree", () => game.TreeView.Toggle(), out _);
            b.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(250, 66));
            upgradesHint = UIFactory.NewText("Hint", canvas.transform, "", 16, TextAnchor.UpperCenter, UITheme.CreamDim);
            upgradesHint.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -92), new Vector2(250, 24));
        }

        void BuildBottomBar()
        {
            var bar = UIFactory.NewPanel("Bottom bar", canvas.transform, true).rectTransform
                .Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 160));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 16, 16);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;

            pinsText = Section(bar, "PINS", "pin", "Fire pin  [Space]", () => game.FirePin(), out fireButton);
            UIFactory.NewDivider(bar, 120);
            caseText = Section(bar, "CASE FILE", "folder", "Stamp", () => game.Cases.TryStamp(), out stampButton);
            UIFactory.NewDivider(bar, 120);
            whiskeyText = Section(bar, "WHISKEY", "glass", "Pour", () => game.Whiskey.Sip(), out sipButton);
            whiskeyBar = Bar(whiskeyText.transform.parent);
            UIFactory.NewDivider(bar, 120);
            inventoryText = Section(bar, "DESK", "statue", "Throw paper ball", () => game.PaperBalls.StartAiming(), out throwButton);
            UIFactory.NewDivider(bar, 120);
            lootText = Section(bar, "LOOTBOX", "box", "Open", () => game.Lootbox.TryOpen(), out lootButton);
        }

        Text Section(RectTransform parent, string title, string icon, string buttonLabel, UnityEngine.Events.UnityAction action, out Button button)
        {
            var panel = UIFactory.NewRect(title, parent);
            panel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var head = UIFactory.NewRect("Heading", panel);
            head.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 28));
            var h = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            UIFactory.NewIcon(icon, head, 24);
            var t = UIFactory.NewTitle("Title", head, title, 19, TextAnchor.MiddleLeft, UITheme.Gold);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;

            var body = UIFactory.NewText("Body", panel, "", 16, TextAnchor.UpperLeft, UITheme.Cream);
            body.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(2, -32), new Vector2(-4, -72));
            button = UIFactory.NewButton("Button", panel, buttonLabel, 17, action);
            button.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 36));
            return body;
        }

        Image Bar(Transform panel)
        {
            var back = UIFactory.NewImage("Fill back", panel, new Color(0, 0, 0, 0.6f)).rectTransform
                .Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(0, 6));
            var fill = UIFactory.NewImage("Fill", back, new Color(0.86f, 0.55f, 0.18f));
            fill.rectTransform.Stretch();
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            return fill;
        }

        /// <summary>The authored legacy HUD (money, upgrades and archive buttons, hint line) is replaced by this HUD.</summary>
        void HideLegacyHud()
        {
            var legacy = game.legacy;
            if (!legacy) return;
            Transform root = null;
            if (legacy.moneyText) root = legacy.moneyText.canvas ? legacy.moneyText.canvas.rootCanvas.transform : legacy.moneyText.transform.root;
            else if (legacy.hintText) root = legacy.hintText.transform.root;
            if (!root) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Upper left HUD" || t.name == "UpgradesButton" || t.name == "ArchiveButton" || t.name == "Hint panel")
                    t.gameObject.SetActive(false);
        }

        /// <summary>Short message above the HUD bar (replaces the old hint line).</summary>
        public void Toast(string message)
        {
            if (!toastText || string.IsNullOrEmpty(message)) return;
            toastText.text = message;
            toastTimer = Mathf.Clamp(2.5f + message.Length * 0.03f, 3f, 6f);
            toastGroup.alpha = 1;
            toastGroup.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            if (!game || !canvas) return;
            if (Input.GetKeyDown(KeyCode.T) && !(game.PaperBalls && game.PaperBalls.IsAiming)) game.TreeView.Toggle();

            moneyText.text = NumberFormatter.Money(game.Wallet.Money);
            incomeText.text = "+" + NumberFormatter.Money(game.EstimatedIncomePerMinute()) + " / min";
            goldText.text = NumberFormatter.Format(game.Wallet.Gold);

            bool treeOwned = game.Skills.Owns(DefaultSkillTreeFactory.RootId);
            upgradesHint.text = treeOwned ? "" : "Unlock for " + UIFactory.Money(game.Tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0)) + "  [T]";

            var map = game.map;
            int unlocked = game.UnlockedPins;
            if (unlocked == 0) pinsText.text = "No pins yet.\nBuy Pin 1 under Upgrades.";
            else
            {
                string reload = map && map.CooldownRemaining > 0 ? "Reloading " + map.CooldownRemaining.ToString("0.0") + "s" : "Shooter ready";
                pinsText.text = (map ? map.ReadyCount : 0) + " / " + unlocked + " in the box · " + (map ? map.ActiveCount : 0) + " on map\n" + reload +
                                "\nLand " + Pct(game.Stats.Get(StatId.PinLandChance)) + " · City " + Pct(game.Stats.Get(StatId.PinCityChance));
            }
            fireButton.interactable = unlocked > 0 && map && map.ReadyCount > 0 && map.CooldownRemaining <= 0;

            var cases = game.Cases;
            if (cases.OnCooldown) caseText.text = "Case #" + cases.Level + " approved.\nNext case in " + UIFactory.Seconds(cases.Cooldown);
            else
                caseText.text = "Case #" + (cases.Level + 1) + ": " + cases.Evidence + " / " + cases.Required + " evidence\n" +
                                (cases.RequiredDurability > 0 ? "Pins with durability ≥ " + cases.RequiredDurability + "\n" : "Any pin counts\n") +
                                "Reward " + UIFactory.Money(cases.Reward);
            stampButton.interactable = !cases.OnCooldown && cases.IsFull;

            var w = game.Whiskey;
            whiskeyText.text = w.Boosting ? "BOOST ×" + game.Stats.Permanent(StatId.WhiskeyBoostStrength).ToString("0.##") + "  " + UIFactory.Seconds(w.BoostRemaining)
                : w.Cooldown > 0 ? "Empty glass\nRefill in " + UIFactory.Seconds(w.Cooldown)
                : "Filled " + Mathf.FloorToInt(w.Fill * 100) + "%" + (w.IceInGlass ? " (ice)" : game.Inventory.IceCubes > 0 ? "\nIce ready: " + game.Inventory.IceCubes : "");
            whiskeyBar.rectTransform.anchorMax = new Vector2(w.Boosting ? Mathf.Clamp01(w.BoostRemaining / Mathf.Max(0.01f, (float)game.Stats.Get(StatId.WhiskeyBoostDuration))) : w.Fill, 1);
            sipButton.interactable = !w.Boosting && w.Cooldown <= 0;

            var inv = game.Inventory;
            string statues = "";
            if (game.figurines != null)
                foreach (var f in game.figurines)
                    if (f) statues += (statues.Length > 0 ? " · " : "") + (f.Pieces == 0 ? "—" : !f.IsComplete ? f.Pieces + "/" + f.PiecesRequired : f.IsReadyToSmash ? "GOLD!" : Mathf.FloorToInt(f.GoldFill * 100) + "%");
            inventoryText.text = "Ice " + inv.IceCubes + " · Paper " + inv.PaperBalls + (inv.SparePieces > 0 ? " · Spare pieces " + inv.SparePieces : "") +
                                 "\nStatues: " + (statues.Length > 0 ? statues : "none");
            throwButton.interactable = inv.PaperBalls > 0;
            throwButton.GetComponentInChildren<Text>().text = game.PaperBalls.IsAiming ? "Click to throw…" : "Throw paper ball";

            lootText.text = "Price " + UIFactory.Money(game.Lootbox.Price) + "\nOpened " + game.Lootbox.Opened;
            lootButton.interactable = game.Wallet.CanAfford(Currency.Money, game.Lootbox.Price);

            if (toastTimer > 0)
            {
                toastTimer -= Time.deltaTime;
                toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.5f);
                if (toastTimer <= 0) toastGroup.gameObject.SetActive(false);
            }

            if (popTimer > 0)
            {
                popTimer -= Time.deltaTime;
                popText.text = "+" + UIFactory.Money(popAmount);
                var c = popText.color; c.a = Mathf.Clamp01(popTimer / 0.4f); popText.color = c;
                popText.rectTransform.anchoredPosition = new Vector2(0, 175 + (1.2f - popTimer) * 25);
            }
            else if (popText.text.Length > 0) popText.text = "";
        }

        static string Pct(double v) => Mathf.RoundToInt((float)(v * 100)) + "%";
    }
}
