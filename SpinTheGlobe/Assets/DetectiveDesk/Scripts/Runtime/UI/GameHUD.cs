using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Runtime-built HUD bar along the bottom of the screen: pins/shooter, case file, whiskey glass,
    /// inventory + paper ball, lootbox and the skill-tree button. Money and gold stay in the
    /// existing top-left HUD (DetectiveGame). Also shows a short "+€x" pop for payouts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHUD : MonoBehaviour
    {
        GameManager game;
        Canvas canvas;
        Text pinsText, caseText, whiskeyText, inventoryText, statueText, lootText, treeText, popText, toastText;
        Image toastBack;
        float toastTimer;
        Button fireButton, stampButton, sipButton, throwButton, lootButton, treeButton;
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

        void Build()
        {
            canvas = UIFactory.CreateCanvas("Game HUD", 40);
            var bar = UIFactory.NewImage("Bottom bar", canvas.transform, UIFactory.Panel).rectTransform
                .Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 150));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 14;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            pinsText = Section(bar, "PINS", "Fire pin  [Space]", () => game.FirePin(), out fireButton);
            caseText = Section(bar, "CASE FILE", "Stamp", () => game.Cases.TryStamp(), out stampButton);
            whiskeyText = Section(bar, "WHISKEY", "Pour", () => game.Whiskey.Sip(), out sipButton);
            whiskeyBar = Bar(whiskeyText.transform.parent);
            inventoryText = Section(bar, "DESK", "Throw paper ball", () => game.PaperBalls.StartAiming(), out throwButton);
            statueText = inventoryText;
            lootText = Section(bar, "LOOTBOX", "Open", () => game.Lootbox.TryOpen(), out lootButton);
            treeText = Section(bar, "SKILL TREE", "Open  [T]", () => game.TreeView.Toggle(), out treeButton);

            popText = UIFactory.NewText("Payout pop", canvas.transform, "", 34, TextAnchor.MiddleCenter, UIFactory.Brass);
            popText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 165), new Vector2(500, 50));
            var shadow = popText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);

            toastBack = UIFactory.NewImage("Message", canvas.transform, new Color(0.07f, 0.05f, 0.04f, 0.88f));
            toastBack.raycastTarget = false;
            toastBack.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 222), new Vector2(1100, 46));
            toastText = UIFactory.NewText("Text", toastBack.transform, "", 21, TextAnchor.MiddleCenter, UIFactory.Ink);
            toastText.rectTransform.Stretch(6);
            toastBack.gameObject.SetActive(false);

            // The authored hint panel sits behind the new bottom bar; messages are shown here instead.
            if (game.legacy && game.legacy.hintText && game.legacy.hintText.transform.parent && game.legacy.hintText.transform.parent.name == "Hint panel")
                game.legacy.hintText.transform.parent.gameObject.SetActive(false);
        }

        /// <summary>Short message above the HUD bar (replaces the old hint line).</summary>
        public void Toast(string message)
        {
            if (!toastText || string.IsNullOrEmpty(message)) return;
            toastText.text = message;
            toastTimer = Mathf.Clamp(2.5f + message.Length * 0.03f, 3f, 6f);
            toastBack.gameObject.SetActive(true);
        }

        Text Section(RectTransform parent, string title, string buttonLabel, UnityEngine.Events.UnityAction action, out Button button)
        {
            var panel = UIFactory.NewImage(title, parent, new Color(0.16f, 0.11f, 0.08f, 0.85f)).rectTransform;
            UIFactory.NewText("Title", panel, title, 18, TextAnchor.UpperLeft, UIFactory.Brass).rectTransform
                .Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(-20, 24));
            var body = UIFactory.NewText("Body", panel, "", 17, TextAnchor.UpperLeft, UIFactory.Ink);
            body.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -30), new Vector2(-20, -74));
            button = UIFactory.NewButton("Button", panel, buttonLabel, 17, action);
            button.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(-16, 34));
            return body;
        }

        Image Bar(Transform panel)
        {
            var back = UIFactory.NewImage("Fill back", panel, new Color(0, 0, 0, 0.5f)).rectTransform
                .Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(-16, 8));
            var fill = UIFactory.NewImage("Fill", back, new Color(0.85f, 0.48f, 0.12f));
            fill.rectTransform.Stretch();
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            return fill;
        }

        void Update()
        {
            if (!game || !canvas) return;
            if (Input.GetKeyDown(KeyCode.T) && !(game.PaperBalls && game.PaperBalls.IsAiming)) game.TreeView.Toggle();

            var map = game.map;
            int unlocked = game.UnlockedPins;
            if (unlocked == 0) pinsText.text = "No pins yet.\nBuy Pin 1 in the skill tree.";
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

            lootText.text = "Price " + UIFactory.Money(game.Lootbox.Price) + "\nOpened " + game.Lootbox.Opened +
                            "\nGold " + NumberFormatter.Format(game.Wallet.Gold);
            lootButton.interactable = game.Wallet.CanAfford(Currency.Money, game.Lootbox.Price);

            bool treeOwned = game.Skills.Owns(DefaultSkillTreeFactory.RootId);
            double rootCost = game.Tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0);
            treeText.text = treeOwned ? "Upgrades for the globe, pins,\ncases, whiskey and statues."
                                      : "Locked. Unlock for " + UIFactory.Money(rootCost) + ".";

            if (toastTimer > 0)
            {
                toastTimer -= Time.deltaTime;
                float a = Mathf.Clamp01(toastTimer / 0.5f);
                toastBack.color = new Color(0.07f, 0.05f, 0.04f, 0.88f * a);
                toastText.color = new Color(UIFactory.Ink.r, UIFactory.Ink.g, UIFactory.Ink.b, a);
                if (toastTimer <= 0) toastBack.gameObject.SetActive(false);
            }

            if (popTimer > 0)
            {
                popTimer -= Time.deltaTime;
                popText.text = "+" + UIFactory.Money(popAmount);
                var c = popText.color; c.a = Mathf.Clamp01(popTimer / 0.4f); popText.color = c;
                popText.rectTransform.anchoredPosition = new Vector2(0, 165 + (1.2f - popTimer) * 25);
            }
            else if (popText.text.Length > 0) popText.text = "";
        }

        static string Pct(double v) => Mathf.RoundToInt((float)(v * 100)) + "%";
    }
}
