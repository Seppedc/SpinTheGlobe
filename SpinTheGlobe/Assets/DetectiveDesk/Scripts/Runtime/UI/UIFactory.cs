using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Small helpers to build uGUI hierarchies from code (HUD, skill tree, popups) in the <see cref="UITheme"/> style.</summary>
    public static class UIFactory
    {
        /// <summary>Body text font (serif).</summary>
        public static Font Font => UITheme.BodyFont;

        public static Color Ink => UITheme.Cream;
        public static Color InkDim => UITheme.CreamDim;
        public static Color Brass => UITheme.Gold;
        public static Color Panel => UITheme.PanelColor;
        public static Color ButtonColor => UITheme.ButtonFill;

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current || UnityEngine.Object.FindAnyObjectByType<EventSystem>()) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = position; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image NewImage(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text NewText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = value; t.fontSize = size; t.alignment = anchor; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Heading text in the display serif (titles, labels in capitals).</summary>
        public static Text NewTitle(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            var t = NewText(name, parent, value, size, anchor, color);
            t.font = UITheme.DisplayFont;
            return t;
        }

        /// <summary>Thin gold double border drawn over (and stretched to) <paramref name="parent"/>.</summary>
        public static Image AddFrame(Transform parent, bool ornate = false, float thickness = 1f)
        {
            var sprite = ornate ? UITheme.FrameOrnate : UITheme.Frame;
            var frame = NewImage("Frame", parent, UITheme.Gold);
            frame.raycastTarget = false;
            frame.rectTransform.Stretch();
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (sprite)
            {
                frame.sprite = sprite;
                frame.type = Image.Type.Sliced;
                frame.fillCenter = false;
                frame.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, thickness);
            }
            else
            {
                // Fallback without the sprite: a plain outline.
                frame.color = Color.clear;
                var o = frame.gameObject.AddComponent<Outline>();
                o.effectColor = UITheme.Gold;
            }
            return frame;
        }

        /// <summary>Dark panel with the gold double border.</summary>
        public static Image NewPanel(string name, Transform parent, bool ornate = false, Color? color = null)
        {
            var img = NewImage(name, parent, color ?? UITheme.PanelColor);
            AddFrame(img.transform, ornate);
            return img;
        }

        /// <summary>Gold line-art icon (gear, money, chart, coin, tree, pin, folder, glass, box, paper, statue, close, stamp).</summary>
        public static Image NewIcon(string iconName, Transform parent, float size, Color? color = null)
        {
            var img = NewImage("Icon " + iconName, parent, color ?? UITheme.Gold);
            img.sprite = UITheme.Icon(iconName);
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (!img.sprite) img.color = Color.clear;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = size;
            le.preferredHeight = le.minHeight = size;
            return img;
        }

        /// <summary>Thin gold line that fades at the ends (vertical by default) for separating values.</summary>
        public static Image NewDivider(Transform parent, float length, bool horizontal = false)
        {
            var img = NewImage("Divider", parent, UITheme.GoldDim);
            img.raycastTarget = false;
            img.sprite = UITheme.Divider;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(img.sprite ? 8 : 1, length);
            if (horizontal) rt.localRotation = Quaternion.Euler(0, 0, 90);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = horizontal ? length : (img.sprite ? 8 : 1);
            le.preferredHeight = horizontal ? 8 : length;
            return img;
        }

        public static Button NewButton(string name, Transform parent, string label, int fontSize, UnityAction onClick, out Text labelText) =>
            NewButton(name, parent, label, fontSize, null, onClick, out labelText);

        /// <summary>Dark, gold-framed button with an optional icon in front of the label.</summary>
        public static Button NewButton(string name, Transform parent, string label, int fontSize, string iconName, UnityAction onClick, out Text labelText)
        {
            var img = NewImage(name, parent, UITheme.ButtonFill);
            var frame = AddFrame(img.transform);
            var button = img.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
            if (onClick != null) button.onClick.AddListener(onClick);

            Image icon = null;
            if (!string.IsNullOrEmpty(iconName))
            {
                var row = NewRect("Content", img.transform);
                row.Stretch(6);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleCenter;
                h.spacing = 10;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                icon = NewIcon(iconName, row, fontSize * 1.5f);
                labelText = NewTitle("Label", row, label, fontSize, TextAnchor.MiddleLeft, Ink);
                labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            else
            {
                labelText = NewTitle("Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, Ink);
                labelText.rectTransform.Stretch(4);
            }

            var themed = img.gameObject.AddComponent<ThemedButton>();
            themed.button = button; themed.background = img; themed.frame = frame; themed.icon = icon; themed.label = labelText;
            return button;
        }

        public static Button NewButton(string name, Transform parent, string label, int fontSize, UnityAction onClick) =>
            NewButton(name, parent, label, fontSize, null, onClick, out _);

        public static string Money(double v) => DetectiveDesk.Economy.NumberFormatter.Money(v);
        public static string Num(double v) => DetectiveDesk.Economy.NumberFormatter.Format(v);
        public static string Seconds(float s) => s >= 60 ? $"{Mathf.FloorToInt(s / 60)}m {Mathf.FloorToInt(s % 60):00}s" : $"{Mathf.CeilToInt(s)}s";
    }
}
