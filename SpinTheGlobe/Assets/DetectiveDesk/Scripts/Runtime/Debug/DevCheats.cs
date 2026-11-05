using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Small testing grants: F1 adds EUR 10; F2 adds 1 gold. Editor/development only.</summary>
    public sealed class DevCheats : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const double MoneyAmount = 10;
        public const double GoldAmount = 1;
        GameManager game;

        void Awake() { game = GetComponent<GameManager>(); }

        void Update()
        {
            if (!game) return;
            if (Input.GetKeyDown(KeyCode.F1)) GiveMoney();
            if (Input.GetKeyDown(KeyCode.F2)) GiveGold();
        }

        public void GiveMoney()
        {
            if (!game) return;
            game.Credit(MoneyAmount);
            game.ShowHint("[dev] +EUR 10");
        }

        public void GiveGold()
        {
            if (!game) return;
            game.CreditGold(GoldAmount);
            game.ShowHint("[dev] +1 gold");
        }
#endif
    }
}
