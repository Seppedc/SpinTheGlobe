using System;
using System.Collections.Generic;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Fair pin rotation and reload timing, including time left over between rendered frames.</summary>
    public sealed class PinShooterCycle
    {
        int nextPin;
        double remaining;

        public double CooldownRemaining => Math.Max(0, remaining);
        public bool Ready => remaining <= 0;

        public void Advance(double seconds) => remaining -= Math.Max(0, seconds);
        public void Reload(double seconds) => remaining += seconds;
        // Idle time cannot be saved up for a burst when a pin finally returns.
        public void DiscardOverdueTime() => remaining = Math.Max(0, remaining);

        public int TakeNextReady(int unlocked, ISet<int> busy)
        {
            for (int offset = 0; offset < unlocked; offset++)
            {
                int index = (nextPin + offset) % unlocked;
                if (busy.Contains(index)) continue;
                nextPin = (index + 1) % unlocked;
                return index;
            }
            return -1;
        }
    }
}
