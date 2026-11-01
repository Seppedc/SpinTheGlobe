using System;
using System.Collections.Generic;

namespace DetectiveDesk.Save
{
    /// <summary>
    /// Everything persisted between sessions. Serialized with JsonUtility, so only public fields /
    /// [Serializable] types and Lists (no dictionaries). Bump <see cref="CurrentVersion"/> and add a
    /// migration in SaveStore when the shape changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long savedAtUnixSeconds;
        public double playTimeSeconds;

        // Economy
        public double money;
        public double gold;
        public double lifetimeMoney;
        public double lifetimeGold;

        // Progression
        public List<SkillRank> skills = new List<SkillRank>();

        // Feature systems append their own sections below as they are built
        // (pins, case file, whiskey, statues, inventory).
    }

    [Serializable]
    public struct SkillRank
    {
        public string id;
        public int rank;
    }
}
