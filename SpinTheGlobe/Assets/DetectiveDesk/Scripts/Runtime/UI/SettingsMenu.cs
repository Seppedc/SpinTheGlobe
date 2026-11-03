using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Settings menu (gear button or Esc): language, master / effects / music volume, display options
    /// (window mode, resolution, frame-rate limit), Save &amp; Quit, and
    /// Restart game behind two confirmations. Built at runtime in the noir-ledger style.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsMenu : MonoBehaviour
    {
        const float RowHeight = 42, LabelWidth = 250, PanelWidth = 760;

        GameManager game;
        Canvas canvas;
        RectTransform panel, dialog;
        TextMeshProUGUI dialogTitle, dialogText, dialogConfirmLabel;
        Button dialogConfirm;
        int restartStep;
        Coroutine countdown;
        readonly List<Action> refreshers = new List<Action>();

        public bool IsOpen => canvas && canvas.gameObject.activeSelf;

        public void Initialize(GameManager owner)
        {
            game = owner;
            GameSettings.Apply();
            Build();
            canvas.gameObject.SetActive(false);
        }

        void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (!canvas) return;
            if (game.TreeView && game.TreeView.IsOpen) game.TreeView.Close();
            foreach (var r in refreshers) r();
            HideDialog();
            canvas.gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!canvas) return;
            HideDialog();
            canvas.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // Keep the panel on screen at large interface sizes.
            if (!IsOpen || !panel) return;
            float h = ((RectTransform)canvas.transform).rect.height;
            float k = panel.rect.height > 1 ? Mathf.Min(1f, h * 0.95f / panel.rect.height) : 1f;
            panel.localScale = new Vector3(k, k, 1);
        }

        void Update()
        {
            if (!game || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (dialog && dialog.gameObject.activeSelf) { HideDialog(); return; }
            if (IsOpen) { Close(); return; }
            // Esc that just closed the skill tree should not also open the settings.
            if (game.TreeView && (game.TreeView.IsOpen || game.TreeView.ClosedThisFrame)) return;
            Open();
        }

        // ------------------------------------------------------------------ build
        void Build()
        {
            canvas = UIFactory.CreateCanvas("Settings", 120);
            var dim = UIFactory.NewImage("Dim", canvas.transform, new Color(0, 0, 0, 0.72f));
            dim.rectTransform.Stretch();

            panel = UIFactory.NewPanel("Settings panel", canvas.transform, true, new Color(0.03f, 0.027f, 0.024f, 0.99f)).rectTransform;
            panel.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, 0));
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(36, 36, 22, 26);
            v.spacing = 6;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title row with a close button.
            var titleRow = Row("Title");
            var title = UIFactory.NewTitle("Title", titleRow, "SETTINGS", 30, TextAnchor.MiddleLeft, UITheme.Gold);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var close = UIFactory.NewButton("Close", titleRow, "", 16, "close", Close, out _);
            Fixed(close.gameObject, 46, 40);

            Section("GENERAL");
            Selector("Language", GameSettings.Languages, () => GameSettings.Language, i => { GameSettings.Language = i; GameSettings.Apply(); });

            Section("AUDIO");
            SliderRow("Master volume", () => GameSettings.Master, x => { GameSettings.Master = x; GameSettings.Apply(); });
            SliderRow("Sound effects", () => GameSettings.Effects, x => { GameSettings.Effects = x; GameSettings.Apply(); });
            SliderRow("Background music", () => GameSettings.Music, x => { GameSettings.Music = x; GameSettings.Apply(); });

            Section("DISPLAY");
            Selector("Window mode", GameSettings.WindowModes, () => GameSettings.WindowMode, i => { GameSettings.WindowMode = i; GameSettings.Apply(); }, EditorNote);
            var sizes = GameSettings.Resolutions();
            var sizeNames = new string[sizes.Count];
            for (int i = 0; i < sizes.Count; i++) sizeNames[i] = sizes[i].x + " × " + sizes[i].y;
            Selector("Resolution", sizeNames, () => Mathf.Max(0, sizes.IndexOf(GameSettings.Resolution)) , i => { GameSettings.Resolution = sizes[i]; GameSettings.Apply(); }, EditorNote);
            var fps = new string[GameSettings.FrameRates.Length];
            for (int i = 0; i < fps.Length; i++) fps[i] = GameSettings.FrameRates[i] < 0 ? "Unlimited" : GameSettings.FrameRates[i] + " FPS";
            Selector("Frame-rate limit", fps, () => GameSettings.FrameRateIndex, i => { GameSettings.FrameRateIndex = i; GameSettings.Apply(); });

            // Bottom buttons.
            var spacer = UIFactory.NewRect("Spacer", panel);
            spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;
            var buttons = Row("Buttons", 50);
            buttons.GetComponent<HorizontalLayoutGroup>().spacing = 14;
            var restart = UIFactory.NewButton("Restart", buttons, "RESTART GAME", 18, AskRestart, out var restartLabel);
            restartLabel.color = UITheme.Bad;
            Flexible(restart.gameObject);
            Flexible(UIFactory.NewButton("Save and quit", buttons, "SAVE & QUIT", 18, SaveAndQuit, out _).gameObject);
            Flexible(UIFactory.NewButton("Back", buttons, "BACK TO DESK", 18, Close, out _).gameObject);

            BuildDialog();
        }

        RectTransform Row(string name, float height = RowHeight)
        {
            var row = UIFactory.NewRect(name, panel);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        void Section(string title)
        {
            var spacer = UIFactory.NewRect("Gap", panel);
            spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 8;
            var t = UIFactory.NewTitle(title, panel, title, 18, TextAnchor.LowerLeft, UITheme.Gold);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;
            var rule = UIFactory.NewImage("Rule", panel, UITheme.GoldDim);
            rule.raycastTarget = false;
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 1;
        }

        TextMeshProUGUI Label(RectTransform row, string text)
        {
            var label = UIFactory.NewText("Label", row, text, 20, TextAnchor.MiddleLeft, UITheme.Cream);
            Fixed(label.gameObject, LabelWidth, RowHeight);
            return label;
        }

        /// <summary>A "‹ value ›" picker that cycles through options.</summary>
        void Selector(string label, string[] options, Func<int> get, Action<int> set, Func<string> note = null)
        {
            var row = Row(label);
            Label(row, label);
            int count = Mathf.Max(1, options.Length);
            TextMeshProUGUI value = null;
            void Show()
            {
                int i = Mathf.Clamp(get(), 0, count - 1);
                string n = note != null ? note() : null;
                value.text = (options.Length > 0 ? options[i] : "—") + (string.IsNullOrEmpty(n) ? "" : "  <size=70%><color=#a89a80>(" + n + ")</color></size>");
            }
            var prev = UIFactory.NewButton("Previous", row, "‹", 24, () => { set(((get() - 1) % count + count) % count); RefreshAll(); }, out _);
            Fixed(prev.gameObject, 44, 38);
            value = UIFactory.NewTitle("Value", row, "", 20, TextAnchor.MiddleCenter, UITheme.Cream);
            value.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var next = UIFactory.NewButton("Next", row, "›", 24, () => { set((get() + 1) % count); RefreshAll(); }, out _);
            Fixed(next.gameObject, 44, 38);
            refreshers.Add(Show);
            Show();
        }

        void SliderRow(string label, Func<float> get, Action<float> set)
        {
            var row = Row(label);
            Label(row, label);

            var root = UIFactory.NewRect("Slider", row);
            root.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var slider = root.gameObject.AddComponent<Slider>();
            var track = UIFactory.NewImage("Track", root, new Color(0, 0, 0, 0.6f));
            track.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 6));
            var fillArea = UIFactory.NewRect("Fill area", root);
            fillArea.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 6));
            var fill = UIFactory.NewImage("Fill", fillArea, UITheme.Gold);
            fill.rectTransform.Stretch();
            var handleArea = UIFactory.NewRect("Handle area", root);
            handleArea.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18, 0));
            var handle = UIFactory.NewImage("Handle", handleArea, UITheme.GoldBright);
            handle.rectTransform.sizeDelta = new Vector2(16, 22);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1;
            var nav = slider.navigation; nav.mode = Navigation.Mode.None; slider.navigation = nav;

            var value = UIFactory.NewTitle("Value", row, "", 20, TextAnchor.MiddleRight, UITheme.Cream);
            Fixed(value.gameObject, 70, RowHeight);
            slider.value = get();
            value.text = Mathf.RoundToInt(get() * 100) + "%";
            slider.onValueChanged.AddListener(x => { set(x); value.text = Mathf.RoundToInt(x * 100) + "%"; });
            refreshers.Add(() => { slider.SetValueWithoutNotify(get()); value.text = Mathf.RoundToInt(get() * 100) + "%"; });
        }

        // The editor's Game view can't resize its own window; these two apply in the built game.
        static string EditorNote() => Application.isEditor ? "in the built game" : null;

        void RefreshAll() { foreach (var r in refreshers) r(); }

        static void Fixed(GameObject go, float width, float height)
        {
            if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.minHeight = le.preferredHeight = height;
            le.flexibleWidth = 0;
        }

        static void Flexible(GameObject go)
        {
            if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 48;
        }

        // ------------------------------------------------------------------ save & quit
        void SaveAndQuit()
        {
            game.SaveGame();
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ restart (two confirmations)
        void BuildDialog()
        {
            var shade = UIFactory.NewImage("Dialog", canvas.transform, new Color(0, 0, 0, 0.6f));
            shade.rectTransform.Stretch();
            dialog = shade.rectTransform;

            var box = UIFactory.NewPanel("Box", dialog, true, new Color(0.035f, 0.025f, 0.022f, 1f)).rectTransform;
            box.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 0));
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(32, 32, 24, 26);
            v.spacing = 14;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            dialogTitle = UIFactory.NewTitle("Title", box, "", 26, TextAnchor.MiddleCenter, UITheme.Bad);
            dialogText = UIFactory.NewText("Text", box, "", 19, TextAnchor.MiddleCenter, UITheme.Cream);

            var row = UIFactory.NewRect("Buttons", box);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            UIFactory.NewButton("Cancel", row, "CANCEL", 18, HideDialog, out _);
            dialogConfirm = UIFactory.NewButton("Confirm", row, "", 18, ConfirmRestart, out dialogConfirmLabel);
            dialog.gameObject.SetActive(false);
        }

        void AskRestart()
        {
            restartStep = 1;
            dialogTitle.text = "RESTART THE GAME?";
            dialogText.text = "This starts a brand-new game. All money, gold, upgrades, cases and statues will be lost.\n(Your settings are kept.)";
            dialogConfirmLabel.text = "YES, RESTART";
            dialogConfirm.interactable = true;
            dialog.gameObject.SetActive(true);
        }

        void ConfirmRestart()
        {
            if (restartStep == 1)
            {
                // Second confirmation, with a short wait so it can't be clicked through by accident.
                restartStep = 2;
                dialogTitle.text = "ARE YOU SURE?";
                dialogText.text = "This cannot be undone. Your current save will be deleted.";
                if (countdown != null) StopCoroutine(countdown);
                countdown = StartCoroutine(Countdown(3));
                return;
            }
            if (restartStep == 2 && dialogConfirm.interactable)
            {
                restartStep = 0;
                Time.timeScale = 1;
                game.ResetProgress();
            }
        }

        IEnumerator Countdown(int seconds)
        {
            dialogConfirm.interactable = false;
            for (int s = seconds; s > 0; s--)
            {
                dialogConfirmLabel.text = "DELETE EVERYTHING (" + s + ")";
                yield return new WaitForSecondsRealtime(1f);
            }
            dialogConfirmLabel.text = "DELETE EVERYTHING";
            dialogConfirm.interactable = true;
            countdown = null;
        }

        void HideDialog()
        {
            restartStep = 0;
            if (countdown != null) { StopCoroutine(countdown); countdown = null; }
            if (dialog) dialog.gameObject.SetActive(false);
        }
    }
}
