using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Small helpers to build uGUI hierarchies from code (HUD, skill tree, popups).</summary>
    public static class UIFactory
    {
        static Font font;
        public static Font Font => font ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static readonly Color Ink = new Color(0.94f, 0.88f, 0.74f);
        public static readonly Color InkDim = new Color(0.70f, 0.64f, 0.52f);
        public static readonly Color Brass = new Color(0.86f, 0.66f, 0.29f);
        public static readonly Color Panel = new Color(0.07f, 0.055f, 0.045f, 0.86f);
        public static readonly Color ButtonColor = new Color(0.30f, 0.20f, 0.12f, 0.95f);

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

        public static Button NewButton(string name, Transform parent, string label, int fontSize, UnityAction onClick, out Text labelText)
        {
            var img = NewImage(name, parent, ButtonColor);
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            labelText = NewText("Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, Ink);
            labelText.rectTransform.Stretch(4);
            return button;
        }

        public static Button NewButton(string name, Transform parent, string label, int fontSize, UnityAction onClick) =>
            NewButton(name, parent, label, fontSize, onClick, out _);

        public static string Money(double v) => DetectiveDesk.Economy.NumberFormatter.Money(v);
        public static string Num(double v) => DetectiveDesk.Economy.NumberFormatter.Format(v);
        public static string Seconds(float s) => s >= 60 ? $"{Mathf.FloorToInt(s / 60)}m {Mathf.FloorToInt(s % 60):00}s" : $"{Mathf.CeilToInt(s)}s";
    }
}
