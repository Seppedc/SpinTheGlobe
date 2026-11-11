using DetectiveDesk.Gameplay;
using DetectiveDesk.Save;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Everyday developer controls, sharing grants with the F-key shortcuts.</summary>
    public static class SceneMaintenanceMenu
    {
        const string ResetMenu = "Tools/Spin The Globe/Reset Progress (Delete Save)";
        const string MoneyMenu = "Tools/Spin The Globe/Add EUR 10";
        const string GoldMenu = "Tools/Spin The Globe/Add 1 Gold";
        const string LargeMoneyMenu = "Tools/Spin The Globe/Add EUR 10,000";
        const string LargeGoldMenu = "Tools/Spin The Globe/Add 10 Gold";

        [MenuItem(ResetMenu, false, 0)]
        public static void DeleteSave()
        {
            var game = EditorApplication.isPlaying ? Object.FindAnyObjectByType<GameManager>() : null;
            if (EditorApplication.isPlaying && !game)
            {
                Debug.LogWarning("Stop Play mode before resetting without a running game.");
                return;
            }
            if (!EditorUtility.DisplayDialog("Reset all progress?",
                    "Delete the save and reset money, gold, skill purchases, retirement progress, statues and cases? Settings are kept.",
                    "Reset Progress", "Cancel")) return;
            if (game)
            {
                game.ResetProgress();
                return;
            }
            SaveStore.Default.Delete();
            Debug.Log("Save deleted, including its backup. The next Play session starts fresh.");
        }

        [MenuItem(MoneyMenu, false, 1)]
        static void AddMoney() => Cheats()?.GiveMoney();

        [MenuItem(GoldMenu, false, 2)]
        static void AddGold() => Cheats()?.GiveGold();

        [MenuItem(LargeMoneyMenu, false, 3)]
        static void AddLargeMoney() => Cheats()?.GiveLargeMoney();

        [MenuItem(LargeGoldMenu, false, 4)]
        static void AddLargeGold() => Cheats()?.GiveLargeGold();

        [MenuItem(MoneyMenu, true)]
        [MenuItem(GoldMenu, true)]
        [MenuItem(LargeMoneyMenu, true)]
        [MenuItem(LargeGoldMenu, true)]
        static bool CanGrant() => EditorApplication.isPlaying && Cheats();

        static DevCheats Cheats()
        {
            var game = Object.FindAnyObjectByType<GameManager>();
            return game ? game.GetComponent<DevCheats>() : null;
        }
    }
}
