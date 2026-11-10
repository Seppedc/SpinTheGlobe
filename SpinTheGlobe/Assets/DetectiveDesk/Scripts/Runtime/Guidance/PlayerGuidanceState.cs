using System;
using System.Collections.Generic;
using DetectiveDesk.Save;

namespace DetectiveDesk.Progression
{
    /// <summary>Lifetime first-use prompts. Stable ids survive sessions and retirement.</summary>
    public sealed class PlayerGuidanceState
    {
        public const string GlobeSpin = "globe_spin";
        public const string PinIntroStarted = "pin_intro_started";
        public const string PinBoxOpened = "pin_box_opened";
        public const string PinFire = "pin_fire";
        readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);

        public bool NeedsGlobe => !completed.Contains(GlobeSpin);
        public bool NeedsPinBox => !completed.Contains(PinBoxOpened);
        public bool NeedsPinShooter => !completed.Contains(PinFire);
        public bool ShowPinShooter(bool pinsUnlocked) => pinsUnlocked && !NeedsPinBox && NeedsPinShooter;
        public bool Complete(string id) => !string.IsNullOrEmpty(id) && completed.Add(id);
        public List<string> ToSave() => new List<string>(completed);

        public void Load(SaveData data)
        {
            completed.Clear();
            if (data == null) return;
            if (data.completedGuidance != null)
                foreach (string id in data.completedGuidance) Complete(id);
            // Returning players from before prompts existed should not be taught the globe again.
            if (data.lifetimeMoney > 0 || data.prestiges > 0 || (data.skills != null && data.skills.Exists(s => s.rank > 0)))
                Complete(GlobeSpin);
            // A new first-pin purchase records its introduction before saving. Older progressed
            // saves without that marker have already used pins and should not replay the tutorial.
            if (!completed.Contains(PinIntroStarted) && (data.prestiges > 0 ||
                (data.skills != null && data.skills.Exists(s => s.id == "pin_01" && s.rank > 0))))
            {
                Complete(PinBoxOpened);
                Complete(PinFire);
            }
        }

        public static float PulseOpacity(double seconds, float period, float maximum)
        {
            if (period <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return 0;
            return (float)((.5 - .5 * Math.Cos(seconds / period * Math.PI * 2)) * Math.Max(0, Math.Min(1, maximum)));
        }
    }
}
