using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Gold-framed button states: the frame brightens on hover, everything dims when not interactable.</summary>
    [DisallowMultipleComponent]
    public sealed class ThemedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;
        public Image background, frame, icon;
        public TextMeshProUGUI label;
        bool hover;
        int lastState = -1;
        bool usesPurchaseColors;
        PurchaseVisualState purchaseState;

        public void SetPurchaseState(PurchaseVisualState state)
        {
            if (!usesPurchaseColors || purchaseState != state) lastState = -1;
            usesPurchaseColors = true;
            purchaseState = state;
            ApplyColors();
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; ApplyColors(); }
        public void OnPointerExit(PointerEventData e) { hover = false; ApplyColors(); }
        void OnDisable() { hover = false; lastState = -1; }

        void LateUpdate() => ApplyColors();

        void ApplyColors()
        {
            int state = button && !button.IsInteractable() ? 0 : hover ? 2 : 1;
            int key = state + (usesPurchaseColors ? 3 + (int)purchaseState * 3 : 0);
            if (key == lastState) return;
            lastState = key;
            if (usesPurchaseColors)
            {
                var colors = PurchasePalette.For(purchaseState);
                bool highlight = state == 2 && purchaseState == PurchaseVisualState.Available;
                if (background) background.color = highlight ? Color.Lerp(colors.fill, Color.white, .08f) : colors.fill;
                if (frame) frame.color = highlight ? Color.Lerp(colors.border, colors.text, .15f) : colors.border;
                if (icon) icon.color = colors.icon;
                if (label) label.color = colors.text;
                return;
            }
            Color f = state == 0 ? new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, 0.35f) : state == 2 ? UITheme.GoldBright : UITheme.Gold;
            if (background) background.color = state == 2 ? UITheme.ButtonHover : UITheme.ButtonFill;
            if (frame) frame.color = f;
            if (icon) icon.color = f;
            if (label) label.color = state == 0 ? new Color(UITheme.CreamDim.r, UITheme.CreamDim.g, UITheme.CreamDim.b, 0.6f) : state == 2 ? UITheme.GoldBright : UITheme.Cream;
        }
    }
}
