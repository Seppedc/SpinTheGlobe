using System;
using System.Collections.Generic;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Information lives on the desk instead of in a HUD bar:
    /// a small always-on gold label above each desk object (case progress, glass fill, reload, statue state),
    /// and a detailed card in the skill-tree tooltip style while hovering the object. Clicking the object acts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeskInfoSystem : MonoBehaviour
    {
        const float CardWidth = 330;

        sealed class Spot
        {
            public string title, icon;
            public Transform root;
            public Func<Vector3> anchor;
            public Func<string> label;        // null or empty = no label
            public Func<string> body;
            public Func<string> hint;
            public RectTransform chip;
            public TextMeshProUGUI chipText;
        }

        readonly List<Spot> spots = new List<Spot>();
        GameManager game;
        Canvas canvas;
        RectTransform canvasRect, card;
        TextMeshProUGUI cardTitle, cardBody, cardHint;
        Image cardIcon;
        Spot hovered;
        float cardAge;
        Camera cam;

        public void Initialize(GameManager owner)
        {
            game = owner;
            cam = game.GameCamera;
            canvas = UIFactory.CreateCanvas("Desk Info", 35);
            canvasRect = (RectTransform)canvas.transform;
            canvas.GetComponent<GraphicRaycaster>().enabled = false; // purely informational, never blocks desk clicks
            BuildCard();
            RegisterSpots();
        }

        void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }

        // ------------------------------------------------------------------ spots
        void RegisterSpots()
        {
            var globe = game.globe;
            if (globe)
                Add("GLOBE", "chart", globe.dragCollider ? globe.dragCollider.transform : globe.transform, () => globe.Center + Vector3.up * 0.2f, null,
                    () =>
                    {
                        double perTurn = game.Balance.revolutionReward * game.IncomeScale + (game.map ? game.map.ActiveValuePerRevolution : 0);
                        return "Speed  <b>" + Rpm(globe.CurrentVelocity) + "</b> / " + Rpm(globe.MaximumVelocity) +
                               (globe.AutoFloor > 0 ? "\nAuto-rotate  " + Rpm(globe.AutoFloor) : "") +
                               "\nEach turn pays  <b>" + UIFactory.Money(perTurn) + "</b>" +
                               (game.map && game.map.ActiveCount > 0 ? "\n" + game.map.ActiveCount + " pins on the globe" : "");
                    },
                    () => "Drag to spin fast · click to nudge");

            var map = game.map;
            Func<string> pinBody = () =>
            {
                int unlocked = game.UnlockedPins;
                if (unlocked == 0) return "No pins yet.\nBuy <b>Pin 1</b> under Upgrades.";
                return (map ? map.ReadyCount : 0) + " of " + unlocked + " pins in the box · " + (map ? map.ActiveCount : 0) + " on the map" +
                       "\n" + (map && map.CooldownRemaining > 0 ? "Reloading  " + map.CooldownRemaining.ToString("0.0") + "s" : "Shooter ready") +
                       "\nLand " + Pct(game.Stats.Get(StatId.PinLandChance)) + " · City " + Pct(game.Stats.Get(StatId.PinCityChance));
            };
            Func<string> pinLabel = () =>
            {
                int unlocked = game.UnlockedPins;
                if (unlocked == 0 || !map) return null;
                if (map.CooldownRemaining > 0) return "Reload " + map.CooldownRemaining.ToString("0.0") + "s";
                return map.ReadyCount > 0 ? map.ReadyCount + " / " + unlocked + " ready" : "Box empty";
            };
            if (map && map.launcherVisual)
                Add("PIN SHOOTER", "pin", map.launcherVisual, Top(map.launcherVisual), pinLabel, pinBody, () => "Click or press Space to fire");

            var cases = game.Cases;
            if (cases && cases.FolderTransform)
                Add("CASE FILE", "folder", cases.FolderTransform, () => cases.FolderLabelAnchor,
                    () => cases.OnCooldown ? "Next case " + UIFactory.Seconds(cases.Cooldown) : cases.Evidence + " / " + cases.Required,
                    () => cases.OnCooldown
                        ? "Case #" + cases.Level + " approved.\nNext case in " + UIFactory.Seconds(cases.Cooldown)
                        : "Case #" + (cases.Level + 1) + "  ·  <b>" + cases.Evidence + " / " + cases.Required + "</b> evidence\n" +
                          (cases.RequiredDurability > 0 ? "Only pins with durability ≥ " + cases.RequiredDurability + " count" : "Every fallen pin counts") +
                          "\nReward  <b>" + UIFactory.Money(cases.Reward) + "</b>" + (cases.GoldReward > 0 ? "  +" + NumberFormatter.Format(cases.GoldReward) + " gold" : ""),
                    () => "Pins that fall off the map become evidence");
            if (cases && cases.StampTransform)
                Add("STAMP", "stamp", cases.StampTransform, Top(cases.StampTransform),
                    () => cases.IsFull && !cases.OnCooldown ? "STAMP!" : null,
                    () => cases.OnCooldown ? "Waiting for the next case (" + UIFactory.Seconds(cases.Cooldown) + ")."
                        : cases.IsFull ? "Approve case #" + (cases.Level + 1) + " for <b>" + UIFactory.Money(cases.Reward) + "</b>."
                        : "The case file is not complete yet (" + cases.Evidence + " / " + cases.Required + ").",
                    () => "Click to stamp the case");

            var w = game.Whiskey;
            if (w && w.GlassTransform)
                Add("WHISKEY", "glass", w.GlassTransform, Top(w.GlassTransform),
                    () => w.Boosting ? "Boost " + UIFactory.Seconds(w.BoostRemaining) : w.Cooldown > 0 ? "Refill " + UIFactory.Seconds(w.Cooldown) : Mathf.FloorToInt(w.Fill * 100) + "%",
                    () => (w.Boosting ? "<b>BOOST ×" + game.Stats.Permanent(StatId.WhiskeyBoostStrength).ToString("0.##") + "</b> for " + UIFactory.Seconds(w.BoostRemaining) + "\nGlobe earnings and spin speed boosted"
                           : w.Cooldown > 0 ? "Empty glass. Refill in " + UIFactory.Seconds(w.Cooldown)
                           : "Filled " + Mathf.FloorToInt(w.Fill * 100) + "%" + (w.IceInGlass ? " (ice: fills faster)" : "")) +
                          "\nBoost ×" + game.Stats.Permanent(StatId.WhiskeyBoostStrength).ToString("0.##") + " for " + UIFactory.Seconds((float)game.Stats.Permanent(StatId.WhiskeyBoostDuration)) +
                          (game.Inventory.IceCubes > 0 ? "\nIce cubes: " + game.Inventory.IceCubes : ""),
                    () => "Click to pour");

            if (game.figurines != null)
                for (int i = 0; i < game.figurines.Length; i++)
                {
                    var f = game.figurines[i];
                    if (!f) continue;
                    Add("STATUE " + (i + 1), "statue", f.transform, () => { var b = f.VisualBounds; return b.center + Vector3.up * (b.extents.y + 0.04f); },
                        () => f.Pieces == 0 ? null : !f.IsComplete ? f.Pieces + " / " + f.PiecesRequired : f.IsReadyToSmash ? "SMASH!" : Mathf.FloorToInt(f.GoldFill * 100) + "% gold",
                        () => f.StatusText + "\nSmashing pays <b>" + NumberFormatter.Format(f.GoldReward) + " gold</b>" +
                              (game.Inventory.SparePieces > 0 ? "\nSpare pieces: " + game.Inventory.SparePieces : ""),
                        () => f.IsReadyToSmash ? "Click to smash" : "Pieces come from lootboxes");
                }

            var lamp = game.Lamp;
            if (lamp && lamp.Handle)
                Add("DESK LAMP", "lamp", lamp.Handle, () => lamp.Handle.position + Vector3.up * 0.15f, null,
                    () => lamp.IsOn ? "The lamp is on." : "The lamp is off.",
                    () => "Drag the pull chain down to switch it");
        }

        void Add(string title, string icon, Transform root, Func<Vector3> anchor, Func<string> label, Func<string> body, Func<string> hint)
        {
            if (!root) return;
            ClickRouter.EnsureCollider(root);
            var s = new Spot { title = title, icon = icon, root = root, anchor = anchor, label = label, body = body, hint = hint };
            if (label != null) BuildChip(s);
            spots.Add(s);
        }

        static Func<Vector3> Top(Transform t)
        {
            var b = ClickRouter.RendererBounds(t, t.position);
            Vector3 p = b.center + Vector3.up * (b.extents.y + 0.03f);
            return () => p;
        }

        void BuildChip(Spot s)
        {
            var chip = UIFactory.NewPanel("Label " + s.title, canvas.transform, false, new Color(0.03f, 0.027f, 0.024f, 0.82f));
            chip.raycastTarget = false;
            var frame = chip.transform.Find("Frame").GetComponent<Image>();
            frame.pixelsPerUnitMultiplier = 1.6f;
            frame.color = UITheme.GoldDim;
            s.chip = chip.rectTransform;
            s.chip.anchorMin = s.chip.anchorMax = new Vector2(0.5f, 0.5f);
            s.chip.pivot = new Vector2(0.5f, 0);
            var h = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(9, 11, 4, 4);
            h.spacing = 6;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var fit = chip.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.NewIcon(s.icon, chip.transform, 18);
            s.chipText = UIFactory.NewTitle("Text", chip.transform, "", 17, TextAnchor.MiddleLeft, UITheme.Cream).NoWrap();
            s.chip.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ card
        void BuildCard()
        {
            var panel = UIFactory.NewPanel("Desk card", canvas.transform, true, new Color(0.03f, 0.027f, 0.024f, 0.97f));
            panel.raycastTarget = false;
            card = panel.rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0, 0.5f);
            card.sizeDelta = new Vector2(CardWidth, 0);
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 12, 16);
            v.spacing = 8;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var head = UIFactory.NewRect("Heading", card);
            var h = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            cardIcon = UIFactory.NewIcon("folder", head, 26);
            cardTitle = UIFactory.NewTitle("Title", head, "", 23, TextAnchor.MiddleLeft, UITheme.Gold).NoWrap();

            var rule = UIFactory.NewImage("Rule", card, UITheme.GoldDim);
            rule.raycastTarget = false;
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 1;

            cardBody = UIFactory.NewText("Body", card, "", 19, TextAnchor.MiddleCenter, UITheme.Cream);
            cardBody.lineSpacing = 8;
            cardHint = UIFactory.NewText("Hint", card, "", 16, TextAnchor.MiddleCenter, UITheme.Gold);
            cardHint.fontStyle = FontStyles.Italic;
            card.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ update
        void LateUpdate()
        {
            if (!game || !canvas) return;
            if (!cam) cam = game.GameCamera;
            bool hidden = !cam || (game.TreeView && game.TreeView.IsOpen) || (game.legacy && game.legacy.settingsPanel && game.legacy.settingsPanel.activeInHierarchy);
            if (canvas.enabled == hidden) canvas.enabled = !hidden;
            if (hidden) return;

            foreach (var s in spots)
            {
                if (!s.chip) continue;
                string text = s.root.gameObject.activeInHierarchy ? s.label() : null;
                bool show = !string.IsNullOrEmpty(text) && ToCanvas(s.anchor(), out Vector2 local);
                if (s.chip.gameObject.activeSelf != show) s.chip.gameObject.SetActive(show);
                if (!show) continue;
                if (s.chipText.text != text) s.chipText.text = text;
                ToCanvas(s.anchor(), out local);
                s.chip.anchoredPosition = local + new Vector2(0, 6);
            }

            var now = FindHovered();
            if (now != hovered) { hovered = now; cardAge = 0; card.gameObject.SetActive(now != null); }
            if (hovered == null) return;

            cardTitle.text = hovered.title;
            cardIcon.sprite = UITheme.Icon(hovered.icon);
            cardBody.text = hovered.body();
            string hint = hovered.hint != null ? hovered.hint() : null;
            cardHint.text = hint ?? "";
            cardHint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            PlaceCard();

            cardAge += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(cardAge / 0.12f), e = 1 - (1 - k) * (1 - k);
            card.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
        }

        Spot FindHovered()
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || game.WorldInputBlocked) return null;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return null;
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(Input.mousePosition), 100f, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
                foreach (var s in spots)
                    if (s.root && s.root.gameObject.activeInHierarchy && (hit.transform == s.root || hit.transform.IsChildOf(s.root))) return s;
            return null;
        }

        void PlaceCard()
        {
            Canvas.ForceUpdateCanvases();
            if (!ToCanvas(hovered.anchor(), out Vector2 local))
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out local);
            Vector2 size = canvasRect.rect.size;
            float h = card.rect.height;
            bool right = local.x + 40 + CardWidth < size.x * 0.5f - 10;
            card.pivot = new Vector2(right ? 0 : 1, 0.5f);
            float y = Mathf.Clamp(local.y, -size.y * 0.5f + h * 0.5f + 70, size.y * 0.5f - h * 0.5f - 150);
            card.anchoredPosition = new Vector2(local.x + (right ? 40 : -40), y);
        }

        bool ToCanvas(Vector3 world, out Vector2 local)
        {
            local = default;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
        }

        static string Rpm(double degreesPerSecond) => (degreesPerSecond / 6.0).ToString("0.0") + " RPM";
        static string Pct(double v) => Mathf.RoundToInt((float)(v * 100)) + "%";
    }
}
