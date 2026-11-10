using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Screen HUD in the noir-ledger style (<see cref="UITheme"/>):
    /// top-left bar (settings | money | income per minute | gold) with the desk items under it
    /// (lootbox, paper balls, ice), the UPGRADES button top-right, a toast line and a toast line (payout numbers: PayoutPopups).
    /// Everything about the desk objects themselves lives on the desk (<see cref="DeskInfoSystem"/>).
    /// Replaces the authored top-left HUD and buttons of the legacy canvas (hidden at start).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHUD : MonoBehaviour
    {
        GameManager game;
        [SerializeField] Canvas canvas;
        [SerializeField] TextMeshProUGUI moneyText, incomeText, goldText, upgradesHint, toastText;
        [SerializeField] TextMeshProUGUI lootLabel, paperLabel, iceLabel;
        [SerializeField] Button lootButton, paperButton;
        [SerializeField] GameObject iceChip;
        [SerializeField] CanvasGroup toastGroup;
        float toastTimer, incomeRefresh;
        double shownIncome = -1;

        public Canvas Canvas => canvas;

        public void Initialize(GameManager owner)
        {
            game = owner;
            CreateLayout(owner);
            BindActions();
            HideLegacyHud();
        }

        public Canvas CreateLayout(GameManager owner)
        {
            game = owner;
            if (!canvas) Build();
            return canvas;
        }

        void BindActions()
        {
            SceneViewBindings.Button(canvas.transform, "Top bar/Button gear", () => game.Settings.Toggle());
            SceneViewBindings.Button(canvas.transform, "Items/Chip box", () => game.Lootbox.TryOpen());
            SceneViewBindings.Button(canvas.transform, "Items/Chip paper", () => game.PaperBalls.Use());
            SceneViewBindings.Button(canvas.transform, "Upgrades", () => game.TreeView.Toggle());
        }

        public void PreviewInitialState()
        {
            moneyText.text = NumberFormatter.Money(game.Balance.startingMoney);
            goldText.text = NumberFormatter.Format(game.Balance.startingGold);
            incomeText.text = "+" + NumberFormatter.Money(0) + " / min";
            lootLabel.text = "Lootbox  " + UIFactory.Money(game.Balance.lootboxCost.Cost(0));
            upgradesHint.text = "Unlock for " + UIFactory.Money(game.Tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0)) + "  [T]";
            paperButton.gameObject.SetActive(false);
            iceChip.SetActive(false);
            toastGroup.gameObject.SetActive(false);
            HideLegacyHud();
        }

        void OnDestroy() { if (Application.isPlaying && canvas) Destroy(canvas.gameObject); }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Game HUD", 40);
            BuildTopBar();
            BuildItems();
            BuildUpgradesButton();


            var toast = UIFactory.NewPanel("Message", canvas.transform);
            toast.raycastTarget = false;
            toast.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1100, 52));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false; toastGroup.interactable = false;
            toastText = UIFactory.NewText("Text", toast.transform, "", 22, TextAnchor.MiddleCenter, UITheme.Cream);
            toastText.rectTransform.Stretch(12);
            toast.gameObject.SetActive(false);

        }

        static HorizontalLayoutGroup Row(RectTransform rt, int left, int right, int vertical, float spacing)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(left, right, vertical, vertical);
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            rt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return h;
        }

        void BuildTopBar()
        {
            var bar = UIFactory.NewPanel("Top bar", canvas.transform).rectTransform;
            bar.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(0, 66));
            Row(bar, 12, 26, 9, 14);

            IconButton(bar, "gear", 44, () => { if (game.Settings) game.Settings.Toggle(); });
            UIFactory.NewDivider(bar, 46);
            UIFactory.NewIcon("money", bar, 34);
            moneyText = Value(bar, 30, UITheme.Cream, true, 150);
            UIFactory.NewDivider(bar, 46);
            UIFactory.NewIcon("chart", bar, 30);
            incomeText = Value(bar, 22, UITheme.Cream, false, 170);
            UIFactory.NewDivider(bar, 46);
            UIFactory.NewIcon("coin", bar, 32);
            goldText = Value(bar, 28, UITheme.GoldBright, true, 80);
        }

        /// <summary>Desk items under the top bar: lootbox (buy), paper balls (throw), ice cubes (used by the glass).</summary>
        void BuildItems()
        {
            var row = UIFactory.NewRect("Items", canvas.transform);
            row.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -96), new Vector2(0, 46));
            Row(row, 0, 0, 0, 10);

            lootButton = Chip(row, "box", out lootLabel, () => game.Lootbox.TryOpen());
            paperButton = Chip(row, "paper", out paperLabel, () => game.PaperBalls.Use());
            Chip(row, "ice", out iceLabel, null);
            iceChip = iceLabel.transform.parent.parent.gameObject;
        }

        Button Chip(RectTransform parent, string icon, out TextMeshProUGUI label, UnityEngine.Events.UnityAction action)
        {
            var b = UIFactory.NewButton("Chip " + icon, parent, "", 19, icon, action, out label);
            var row = b.transform.Find("Content");
            if (row) { var r = (RectTransform)row; r.Stretch(); var h = row.GetComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(12, 14, 6, 6); h.spacing = 8; }
            label.font = UITheme.TmpBody;
            var fitter = b.gameObject.AddComponent<HorizontalLayoutGroup>();
            fitter.childControlWidth = fitter.childControlHeight = true;
            fitter.childForceExpandWidth = fitter.childForceExpandHeight = true;
            b.gameObject.AddComponent<LayoutElement>().minHeight = 46;
            return b;
        }

        static void SetValue(TextMeshProUGUI t, string value) { if (t.text != value) t.text = value; }

        /// <summary>
        /// Top-bar value in a fixed-width slot so the bar never changes length; long numbers shrink to fit.
        /// </summary>
        static TextMeshProUGUI Value(Transform parent, int size, Color color, bool display, float width)
        {
            var t = display ? UIFactory.NewTitle("Value", parent, "", size, TextAnchor.MiddleLeft, color)
                            : UIFactory.NewText("Value", parent, "", size, TextAnchor.MiddleLeft, color);
            t.NoWrap();
            t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.6f;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0;
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
            upgradesHint = UIFactory.NewText("Hint", canvas.transform, "", 17, TextAnchor.UpperCenter, UITheme.CreamDim);
            upgradesHint.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -92), new Vector2(250, 24));
        }

        /// <summary>The authored legacy HUD (money, upgrades and archive buttons, hint line) is replaced by this HUD.</summary>
        void HideLegacyHud()
        {
            var legacy = game.legacy;
            if (!legacy) return;
            if (legacy.settingsPanel) legacy.settingsPanel.SetActive(false);
            Transform root = null;
            if (legacy.moneyText) root = legacy.moneyText.canvas ? legacy.moneyText.canvas.rootCanvas.transform : legacy.moneyText.transform.root;
            else if (legacy.hintText) root = legacy.hintText.transform.root;
            if (!root) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Upper left HUD" || t.name == "UpgradesButton" || t.name == "ArchiveButton" || t.name == "Hint panel")
                    t.gameObject.SetActive(false);
        }

        /// <summary>Short message at the bottom of the screen (replaces the old hint line).</summary>
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

            SetValue(moneyText, NumberFormatter.Money(game.Wallet.Money));
            SetValue(goldText, NumberFormatter.Format(game.Wallet.Gold));

            // Income per minute follows the globe's speed, which changes every frame: smooth it and
            // refresh the number a few times per second so it reads calmly instead of flickering.
            double target = game.EstimatedIncomePerMinute();
            shownIncome = shownIncome < 0 ? target : shownIncome + (target - shownIncome) * (1 - System.Math.Exp(-Time.unscaledDeltaTime * 1.5));
            incomeRefresh -= Time.unscaledDeltaTime;
            if (incomeRefresh <= 0)
            {
                incomeRefresh = 0.4f;
                SetValue(incomeText, "+" + NumberFormatter.Money(shownIncome < 100 ? System.Math.Round(shownIncome) : shownIncome) + " / min");
            }

            bool treeOwned = game.Skills.Owns(DefaultSkillTreeFactory.RootId);
            upgradesHint.text = treeOwned ? "" : "Unlock for " + UIFactory.Money(game.Tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0)) + "  [T]";

            var inv = game.Inventory;
            lootLabel.text = "Lootbox  " + UIFactory.Money(game.Lootbox.Price);
            lootButton.interactable = game.Wallet.CanAfford(Currency.Money, game.Lootbox.Price);
            paperLabel.text = "× " + inv.PaperBalls;
            paperButton.gameObject.SetActive(inv.PaperBalls > 0);
            iceLabel.text = "× " + inv.IceCubes;
            iceChip.SetActive(inv.IceCubes > 0);

            if (toastTimer > 0)
            {
                toastTimer -= Time.deltaTime;
                toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.5f);
                if (toastTimer <= 0) toastGroup.gameObject.SetActive(false);
            }

        }
    }
}
