using System;
using System.Collections.Generic;

namespace DetectiveDesk.Save
{
    /// <summary>
    /// Everything persisted between sessions. Serialized with JsonUtility, so only public fields /
    /// [Serializable] types and Lists (no dictionaries). New fields default safely when an older
    /// save is loaded; bump <see cref="CurrentVersion"/> and add a migration in SaveStore only when
    /// a field changes meaning.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public long savedAtUnixSeconds;
        public double playTimeSeconds;
        public List<string> completedGuidance = new List<string>();

        // Economy
        public double money;
        public double gold;
        public double lifetimeMoney;
        public double lifetimeGold;

        // Prestige ("Retire the agency"): money earned in the current run, retirements so far, Gold shop ranks.
        public double runMoney;
        public int prestiges;
        public bool gameCompleted;
        public List<SkillRank> goldShop = new List<SkillRank>();

        // Progression
        public List<SkillRank> skills = new List<SkillRank>();

        // Inventory & lootbox
        public int iceCubes;
        public int paperBalls;
        // Lifetime side-game progress; kept through retirement for future achievements.
        public long paperShotsScored;
        public int sparePieces;
        public int lootboxesOpened;

        // Case file
        public int caseLevel;
        public int caseEvidence;
        public float caseCooldown;

        // Whiskey glass
        public float whiskeyFill;
        public float whiskeyCooldown;
        public int whiskeyIceCount;
        public float whiskeyIceMeltTimer;

        // Desk
        public bool lampOff;

        // Statues (one entry per pedestal, in scene order)
        public List<StatueState> statues = new List<StatueState>();
    }

    [Serializable]
    public struct SkillRank
    {
        public string id;
        public int rank;
    }

    [Serializable]
    public struct StatueState
    {
        public int pieces;
        public float goldFill;
    }
}
