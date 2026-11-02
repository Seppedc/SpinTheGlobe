using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// The "noir ledger" UI look: near-black panels, thin gold double borders, serif type in gold and cream,
    /// gold line-art icons. Sprites are white line art in Art/UI/Resources/UI and are tinted at runtime.
    /// Fonts: drop a Display.ttf / Body.ttf into Art/UI/Resources/UI/Fonts to override; otherwise the best
    /// installed serif (Palatino Linotype, Book Antiqua, Georgia …) is used, then Unity's built-in font.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Gold = new Color(0.84f, 0.68f, 0.40f);
        public static readonly Color GoldBright = new Color(1f, 0.86f, 0.56f);
        public static readonly Color GoldDim = new Color(0.84f, 0.68f, 0.40f, 0.45f);
        public static readonly Color Cream = new Color(0.94f, 0.89f, 0.78f);
        public static readonly Color CreamDim = new Color(0.68f, 0.62f, 0.52f);
        public static readonly Color PanelColor = new Color(0.035f, 0.031f, 0.028f, 0.93f);
        public static readonly Color ButtonFill = new Color(0.075f, 0.064f, 0.054f, 0.97f);
        public static readonly Color ButtonHover = new Color(0.16f, 0.13f, 0.09f, 0.98f);
        public static readonly Color Good = new Color(0.62f, 0.84f, 0.50f);
        public static readonly Color Bad = new Color(0.90f, 0.36f, 0.33f);

        /// <summary>9-slice borders in pixels of the 64 px frame sources.</summary>
        const float FrameBorder = 16, FillBorder = 8;

        static Font display, body;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        static readonly string[] DisplayFonts = { "Perpetua Titling MT", "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" };
        static readonly string[] BodyFonts = { "Palatino Linotype", "Book Antiqua", "Georgia", "Cambria", "Times New Roman" };

        public static Font DisplayFont => display ? display : (display = LoadFont("UI/Fonts/Display", DisplayFonts));
        public static Font BodyFont => body ? body : (body = LoadFont("UI/Fonts/Body", BodyFonts));

        static Font LoadFont(string resource, string[] osNames)
        {
            var f = Resources.Load<Font>(resource);
            if (f) return f;
            try
            {
                var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
                foreach (var n in osNames)
                    if (installed.Contains(n)) return Font.CreateDynamicFontFromOSFont(n, 32);
            }
            catch (System.Exception) { /* platform without OS font access */ }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Sprite Frame => Get("UI/Frames/frame", FrameBorder);
        public static Sprite FrameOrnate => Get("UI/Frames/frame_ornate", FrameBorder);
        public static Sprite Fill => Get("UI/Frames/fill", FillBorder);
        public static Sprite Divider => Get("UI/Frames/divider", 0);
        public static Sprite Icon(string name) => Get("UI/Icons/" + name, 0);

        static Sprite Get(string path, float border)
        {
            if (sprites.TryGetValue(path, out var s) && s) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (!tex) { sprites[path] = null; return null; }
            tex.wrapMode = TextureWrapMode.Clamp;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0,
                              SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.name = tex.name;
            sprites[path] = s;
            return s;
        }
    }
}
