using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Paper balls from lootboxes. For now using one pays money straight away (paper-ball reward × income scale);
    /// the throw-into-the-bin mini-game comes back when the waste bin model exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PaperBallThrower : MonoBehaviour
    {
        GameManager game;

        /// <summary>Kept for the HUD/input checks; there is no aiming mode at the moment.</summary>
        public bool IsAiming => false;

        public void Initialize(GameManager owner) { game = owner; }

        /// <summary>Uses one paper ball for an instant payout.</summary>
        public void Use()
        {
            if (!game) return;
            if (!game.Inventory.TryUsePaperBall()) { game.ShowHint("No paper balls. They come from lootboxes."); return; }
            double reward = game.Balance.paperBallReward * game.IncomeScale;
            game.CreditIncome(reward);
            if (game.audioManager) game.audioManager.Click();
            game.ShowHint("Paper ball cashed in: +" + UIFactory.Money(reward));
        }
    }
}
