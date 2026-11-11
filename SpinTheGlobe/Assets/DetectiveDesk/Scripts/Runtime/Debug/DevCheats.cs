using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Testing grants: F1 adds EUR 10; F2 adds 1 gold; F3 adds EUR 10,000; F4 adds 10 gold; F5 / P adds 1 paper ball; F6 adds 5 paper balls. Editor/development only.</summary>
    public sealed class DevCheats : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const double MoneyAmount = 10;
        public const double LargeMoneyAmount = 10_000;
        public const double GoldAmount = 1;
        public const double LargeGoldAmount = 10;
        public const int PaperBallAmount = 1;
        public const int LargePaperBallAmount = 5;
        GameManager game;

        void Awake() { game = GetComponent<GameManager>(); }

        void Update()
        {
            if (!game) return;
            if (Input.GetKeyDown(KeyCode.F1)) GiveMoney();
            if (Input.GetKeyDown(KeyCode.F2)) GiveGold();
            if (Input.GetKeyDown(KeyCode.F3)) GiveLargeMoney();
            if (Input.GetKeyDown(KeyCode.F4)) GiveLargeGold();
            if (Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.P)) GivePaperBall();
            if (Input.GetKeyDown(KeyCode.F6)) GiveLargePaperBall();
        }

        public void GiveMoney()
        {
            if (!game) return;
            game.Credit(MoneyAmount);
            game.ShowHint("[dev] +EUR 10");
        }

        public void GiveLargeMoney()
        {
            if (!game) return;
            game.Credit(LargeMoneyAmount);
            game.ShowHint("[dev] +EUR 10,000");
        }

        public void GiveGold()
        {
            if (!game) return;
            game.CreditGold(GoldAmount);
            game.ShowHint("[dev] +1 gold");
        }

        public void GiveLargeGold()
        {
            if (!game) return;
            game.CreditGold(LargeGoldAmount);
            game.ShowHint("[dev] +10 gold");
        }

        [ContextMenu("Give 1 Paper Ball (P / F5)")]
        public void GivePaperBall()
        {
            if (!game || game.Inventory == null) return;
            game.Inventory.AddPaperBall(PaperBallAmount);
            game.ShowHint("[dev] +1 Paper Ball (P / F5)");
        }

        [ContextMenu("Give 5 Paper Balls (F6)")]
        public void GiveLargePaperBall()
        {
            if (!game || game.Inventory == null) return;
            game.Inventory.AddPaperBall(LargePaperBallAmount);
            game.ShowHint("[dev] +5 Paper Balls (F6)");
        }
#endif
    }
}
