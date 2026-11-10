using DetectiveDesk.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    public sealed partial class SkillTreeView
    {
        [SerializeField] int purchaseAmount = 1;
        public int PurchaseAmount => purchaseAmount;

        void EnsurePurchaseControls()
        {
            var back = canvas.transform.Find("Backdrop");
            var row = back.Find("Purchase Amount");
            if (!row)
            {
                var rect = UIFactory.NewRect("Purchase Amount", back);
                rect.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                    new Vector2(-20, 14), new Vector2(324, 38));
                row = rect;
                int index = 0;
                foreach (int amount in new[] { 1, 5, 10 })
                {
                    var button = UIFactory.NewButton("Buy " + amount, row, "BUY " + amount, 16, null, out _);
                    ((RectTransform)button.transform).Place(Vector2.zero, Vector2.zero, Vector2.zero,
                        new Vector2(index++ * 110, 0), new Vector2(104, 38));
                }
            }
            if (purchaseAmount != 1 && purchaseAmount != 5 && purchaseAmount != 10) purchaseAmount = 1;
            foreach (int amount in new[] { 1, 5, 10 })
            {
                int selected = amount;
                SceneViewBindings.Button(row, "Buy " + amount, () => SetPurchaseAmount(selected));
            }
            RefreshPurchaseControls();
        }

        public void SetPurchaseAmount(int amount)
        {
            if (amount != 1 && amount != 5 && amount != 10)
                throw new System.ArgumentOutOfRangeException(nameof(amount));
            purchaseAmount = amount;
            RefreshPurchaseControls();
            dirty = true;
            if (hovered != null) FillTooltip();
        }

        void RefreshPurchaseControls()
        {
            var row = canvas.transform.Find("Backdrop/Purchase Amount");
            if (!row) return;
            foreach (int amount in new[] { 1, 5, 10 })
            {
                var button = row.Find("Buy " + amount);
                button.GetComponentInChildren<TextMeshProUGUI>(true).text = amount == purchaseAmount
                    ? "<color=#ffdb8f><b>BUY " + amount + "</b></color>" : "BUY " + amount;
            }
        }

        int PurchaseQuote(SkillNodeData node, out double totalCost) =>
            game.Skills.QuotePurchase(node, game.Wallet, game.Stats, purchaseAmount, out totalCost);
    }
}
