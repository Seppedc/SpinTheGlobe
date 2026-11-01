using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    public sealed class GameplayPanel : MonoBehaviour
    {
        public GameManager game;
        public Text[] upgradeLabels, figurineLabels;
        public Button[] upgradeButtons, figurineButtons;
        public Text summary;
        void OnEnable() { if (game) game.Changed += Refresh; Refresh(); }
        void OnDisable() { if (game) game.Changed -= Refresh; }
        public void Refresh()
        {
            if (!game || !game.config) return;
            if (summary) summary.text = "GLOBAL ×" + game.GlobalMultiplier.ToString("0.0") + "   /   " + PinSkin.SkinNames[game.SelectedSkin] + " selected";
            for (int i = 0; i < 3; i++)
            {
                var kind = (UpgradeKind)i; int level = game.Level(kind); bool maximum = level >= game.config.maximumUpgradeLevel;
                double cost = game.config.Cost(kind, level);
                if (upgradeLabels != null && i < upgradeLabels.Length && upgradeLabels[i]) upgradeLabels[i].text =
                    (i == 0 ? "Terminal speed" : i == 1 ? "Auto-motor" : "Pin durability") + "  L" + level + (maximum ? " / MAX" : " / €" + cost.ToString("N0"));
                if (upgradeButtons != null && i < upgradeButtons.Length && upgradeButtons[i]) upgradeButtons[i].interactable = !maximum && game.Money >= cost;
                var f = game.figurines != null && i < game.figurines.Length ? game.figurines[i] : null;
                if (figurineLabels != null && i < figurineLabels.Length && figurineLabels[i]) figurineLabels[i].text = f ?
                    (i + 1) + ". " + f.TierName + " ×" + f.Multiplier.ToString("0.0") + (f.Tier < 3 ? " / €" + game.config.RestorationCost(f.Tier).ToString("N0") : " / MAX") : "Restoration slot unavailable";
                if (figurineButtons != null && i < figurineButtons.Length && figurineButtons[i]) figurineButtons[i].interactable = f && f.Tier < 3 && game.Money >= game.config.RestorationCost(f.Tier);
            }
        }
        public void BuySpeed() => game.BuyUpgrade(UpgradeKind.Speed);
        public void BuyMotor() => game.BuyUpgrade(UpgradeKind.AutoMotor);
        public void BuyDurability() => game.BuyUpgrade(UpgradeKind.Durability);
        public void RestoreFirst() => game.RestoreFigurine(0);
        public void RestoreSecond() => game.RestoreFigurine(1);
        public void RestoreThird() => game.RestoreFigurine(2);
    }
}
