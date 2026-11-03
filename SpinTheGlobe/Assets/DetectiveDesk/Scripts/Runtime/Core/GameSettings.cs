using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Player settings (language, audio, display, UI scale). Stored per machine in PlayerPrefs, separate from the
    /// game save, so restarting the game keeps them. <see cref="Apply"/> pushes them into Unity.
    /// </summary>
    public static class GameSettings
    {
        public static readonly string[] Languages = { "English", "Nederlands", "Français", "Deutsch", "Español" };
        public static readonly string[] WindowModes = { "Fullscreen", "Borderless window", "Windowed" };
        public static readonly int[] FrameRates = { 30, 60, 120, 144, 240, -1 };
        public static readonly float[] UiScales = { 0.8f, 0.9f, 1f, 1.1f, 1.25f, 1.4f };

        public static event Action Changed;

        public static int Language { get => Get("lang", 0); set => Set("lang", Mathf.Clamp(value, 0, Languages.Length - 1)); }
        public static float Master { get => GetF("vol.master", 1f); set => SetF("vol.master", Mathf.Clamp01(value)); }
        public static float Effects { get => GetF("vol.sfx", 1f); set => SetF("vol.sfx", Mathf.Clamp01(value)); }
        public static float Music { get => GetF("vol.music", 0.7f); set => SetF("vol.music", Mathf.Clamp01(value)); }
        public static int WindowMode { get => Get("win.mode", 1); set => Set("win.mode", Mathf.Clamp(value, 0, WindowModes.Length - 1)); }
        public static int ResolutionWidth { get => Get("win.w", 0); set => Set("win.w", value); }
        public static int ResolutionHeight { get => Get("win.h", 0); set => Set("win.h", value); }
        public static int FrameRateIndex { get => Get("fps", 1); set => Set("fps", Mathf.Clamp(value, 0, FrameRates.Length - 1)); }
        public static int UiScaleIndex { get => Get("ui.scale", 2); set => Set("ui.scale", Mathf.Clamp(value, 0, UiScales.Length - 1)); }

        /// <summary>Effective volumes (master × channel) for sound effects and music.</summary>
        public static float EffectsVolume => Master * Effects;
        public static float MusicVolume => Master * Music;
        public static float UiScale => UiScales[UiScaleIndex];

        /// <summary>Distinct screen sizes the monitor supports, largest last.</summary>
        public static List<Vector2Int> Resolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!list.Contains(size)) list.Add(size);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        public static Vector2Int Resolution
        {
            get
            {
                int w = ResolutionWidth, h = ResolutionHeight;
                if (w <= 0 || h <= 0) { var cur = Screen.currentResolution; return new Vector2Int(cur.width, cur.height); }
                return new Vector2Int(w, h);
            }
            set { ResolutionWidth = value.x; ResolutionHeight = value.y; }
        }

        static FullScreenMode UnityMode(int mode) =>
            mode == 0 ? FullScreenMode.ExclusiveFullScreen : mode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        /// <summary>Pushes every setting into Unity. Safe to call any time (display changes only apply in a build).</summary>
        public static void Apply()
        {
            AudioListener.volume = Master;
            // VSync off so the frame-rate limit is what decides the frame rate.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = FrameRates[FrameRateIndex];
#if !UNITY_EDITOR
            var res = Resolution;
            Screen.SetResolution(res.x, res.y, UnityMode(WindowMode));
#endif
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        static int Get(string key, int fallback) => PlayerPrefs.GetInt("settings." + key, fallback);
        static void Set(string key, int value) => PlayerPrefs.SetInt("settings." + key, value);
        static float GetF(string key, float fallback) => PlayerPrefs.GetFloat("settings." + key, fallback);
        static void SetF(string key, float value) => PlayerPrefs.SetFloat("settings." + key, value);
    }
}
