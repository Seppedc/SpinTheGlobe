using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Presentation only: rewards are committed by LootboxSystem before the reel starts.</summary>
    [DisallowMultipleComponent]
    public sealed class LootboxOpeningView : MonoBehaviour
    {
        const int CardCount = 36, Winner = 31;
        const float Stride = 180;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform reel;
        [SerializeField] TextMeshProUGUI heading, description, actionLabel;
        [SerializeField] Button action;
        [SerializeField] Image[] icons;
        [SerializeField] TextMeshProUGUI[] titles;
        GameManager game;
        LootOpened reward;
        float elapsed;
        int lastTick;
        readonly System.Random presentationRandom = new System.Random();
        public bool IsOpen => canvas && canvas.gameObject.activeSelf;
        public bool IsRolling { get; private set; }
        public LootOpened Reward => reward;
        public string HeadingText => heading ? heading.text : "";
        public Color HeadingColor => heading ? heading.color : Color.white;

        public Canvas CreateLayout(GameManager owner)
        {
            game = owner;
            if (canvas) return canvas;
            canvas = UIFactory.CreateCanvas("Lootbox Opening", 140);
            var backdrop = UIFactory.NewImage("Backdrop", canvas.transform, new Color(0, 0, 0, .88f));
            backdrop.rectTransform.Stretch();
            var panel = UIFactory.NewPanel("Opening", canvas.transform, true);
            Place(panel.rectTransform, 0, 0, 1120, 600);
            heading = UIFactory.NewTitle("Heading", panel.transform, "LOOTBOX", 36, TextAnchor.MiddleCenter, UITheme.Gold);
            Place(heading.rectTransform, 0, 235, 1000, 60);
            var viewport = UIFactory.NewRect("Reel window", panel.transform);
            Place(viewport, 0, 35, 1040, 250);
            viewport.gameObject.AddComponent<RectMask2D>();
            reel = UIFactory.NewRect("Reel", viewport);
            Place(reel, 0, 0, 0, 230);
            icons = new Image[CardCount];
            titles = new TextMeshProUGUI[CardCount];
            for (int i = 0; i < CardCount; i++)
            {
                var card = UIFactory.NewPanel("Reward " + i, reel);
                Place(card.rectTransform, i * Stride, 0, 166, 220);
                icons[i] = UIFactory.NewIcon("box", card.transform, 72);
                Place(icons[i].rectTransform, 0, 30, 72, 72);
                titles[i] = UIFactory.NewTitle("Reward name", card.transform, "Loot", 22, TextAnchor.MiddleCenter, UITheme.Cream);
                Place(titles[i].rectTransform, 0, -65, 150, 75);
            }
            var marker = UIFactory.NewImage("Winning marker", panel.transform, UITheme.Gold);
            marker.raycastTarget = false;
            Place(marker.rectTransform, 0, 35, 3, 262);
            description = UIFactory.NewText("Result", panel.transform, "Opening...", 25, TextAnchor.MiddleCenter, UITheme.Cream);
            Place(description.rectTransform, 0, -145, 990, 105);
            action = UIFactory.NewButton("Continue", panel.transform, "Skip", 24, AdvanceOrClose, out actionLabel);
            Place((RectTransform)action.transform, 0, -245, 240, 58);
            canvas.gameObject.SetActive(false);
            return canvas;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height) =>
            rect.Place(Vector2.one * .5f, Vector2.one * .5f, Vector2.one * .5f, new Vector2(x, y), new Vector2(width, height));

        public void Initialize(GameManager owner)
        {
            CreateLayout(owner);
            action.onClick.RemoveAllListeners();
            action.onClick.AddListener(AdvanceOrClose);
            IsRolling = false;
            canvas.gameObject.SetActive(false);
        }

        public void Show(LootOpened result)
        {
            reward = result;
            for (int i = 0; i < CardCount; i++)
            {
                if (i == Winner)
                {
                    SetCard(i, result.Kind, result.Tier);
                }
                else
                {
                    var kind = game.Lootbox.PreviewKind(presentationRandom);
                    int fakeTier = 0;
                    if (kind == LootKind.Money)
                    {
                        double fakeRoll = presentationRandom.NextDouble() * 47.0;
                        LootboxSystem.RollMoneyMultiplier(fakeRoll, presentationRandom.NextDouble(), out _, out fakeTier);
                    }
                    SetCard(i, kind, fakeTier);
                }
            }
            elapsed = 0;
            lastTick = 3;
            reel.anchoredPosition = new Vector2(-3 * Stride, 0);
            heading.text = "OPENING LOOTBOX";
            heading.color = UITheme.Gold;
            description.text = "";
            actionLabel.text = "Skip";
            IsRolling = true;
            canvas.gameObject.SetActive(true);
        }

        void SetCard(int i, LootKind kind, int tier = 0)
        {
            string icon;
            string title;
            Color iconColor = UITheme.Gold;
            Color titleColor = UITheme.Cream;

            if (kind == LootKind.Money)
            {
                switch (tier)
                {
                    case 4: // Jackpot: overflowing mountain of cash
                        icon = "money_jackpot";
                        title = "JACKPOT!";
                        iconColor = UITheme.GoldBright;
                        titleColor = UITheme.GoldBright;
                        break;
                    case 3: // Big win: thick bundle of cash
                        icon = "money_big";
                        title = "Big Win";
                        iconColor = UITheme.GoldBright;
                        titleColor = UITheme.Gold;
                        break;
                    case 2: // Nice win: stack of bills
                        icon = "money_medium";
                        title = "Nice Win";
                        iconColor = UITheme.Gold;
                        titleColor = UITheme.Cream;
                        break;
                    default: // Small win: single 1 bill
                        icon = "money_small";
                        title = "Cash";
                        iconColor = UITheme.Gold;
                        titleColor = UITheme.Cream;
                        break;
                }
            }
            else
            {
                icon = kind == LootKind.Gold ? "coin" :
                       kind.IsStatuePiece() ? "statue" :
                       kind == LootKind.PaperBall ? "paper" : "glass";
                title = kind == LootKind.Gold ? "Gold" :
                        kind == LootKind.StatuePiece2 ? "Statue 2 piece" :
                        kind == LootKind.StatuePiece3 ? "Statue 3 piece" :
                        kind.IsStatuePiece() ? "Statue 1 piece" :
                        kind == LootKind.PaperBall ? "Paper ball" : "Ice cube";
            }

            icons[i].sprite = UITheme.Icon(icon) ?? UITheme.Icon("money");
            icons[i].color = iconColor;
            titles[i].text = title;
            titles[i].color = titleColor;
        }

        void Update()
        {
            if (!IsOpen) return;
            AdvanceAnimation(Time.unscaledDeltaTime);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) AdvanceOrClose();
        }

        public void AdvanceAnimation(float seconds)
        {
            if (!IsRolling) return;
            elapsed += Mathf.Max(0, seconds);
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f, game.Balance.lootboxOpeningSeconds));
            float position = Mathf.Lerp(3, Winner, 1 - Mathf.Pow(1 - t, 4));
            reel.anchoredPosition = new Vector2(-position * Stride, 0);
            int tick = Mathf.RoundToInt(position);
            if (tick != lastTick && game.audioManager) game.audioManager.Click();
            lastTick = tick;
            if (t >= 1) Reveal();
        }

        public void Reveal()
        {
            if (!IsOpen || !IsRolling) return;
            IsRolling = false;
            reel.anchoredPosition = new Vector2(-Winner * Stride, 0);
            bool isJackpot = reward.Kind == LootKind.Money && reward.Tier == 4;
            heading.text = isJackpot ? "JACKPOT!" : "YOU FOUND";
            heading.color = isJackpot ? UITheme.GoldBright : UITheme.Gold;
            description.text = reward.Description;
            actionLabel.text = "Continue";
        }

        public void AdvanceOrClose()
        {
            if (IsRolling) Reveal();
            else if (canvas) canvas.gameObject.SetActive(false);
        }

        public void PreviewLayout()
        {
            for (int i = 0; i < CardCount; i++) SetCard(i, (LootKind)(i % 5), (i % 4) + 1);
            reel.anchoredPosition = new Vector2(-Winner * Stride, 0);
            heading.text = "LOOTBOX";
            heading.color = UITheme.Gold;
            description.text = "";
            actionLabel.text = "Continue";
        }
    }
}
