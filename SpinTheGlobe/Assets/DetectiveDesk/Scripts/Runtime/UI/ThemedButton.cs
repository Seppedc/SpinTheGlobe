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
        public Text label;
        bool hover;
        int lastState = -1;

        public void OnPointerEnter(PointerEventData e) => hover = true;
        public void OnPointerExit(PointerEventData e) => hover = false;
        void OnDisable() { hover = false; lastState = -1; }

        void LateUpdate()
        {
            int state = button && !button.IsInteractable() ? 0 : hover ? 2 : 1;
            if (state == lastState) return;
            lastState = state;
            Color f = state == 0 ? new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, 0.35f) : state == 2 ? UITheme.GoldBright : UITheme.Gold;
            if (background) background.color = state == 2 ? UITheme.ButtonHover : UITheme.ButtonFill;
            if (frame) frame.color = f;
            if (icon) icon.color = f;
            if (label) label.color = state == 0 ? new Color(UITheme.CreamDim.r, UITheme.CreamDim.g, UITheme.CreamDim.b, 0.6f) : state == 2 ? UITheme.GoldBright : UITheme.Cream;
        }
    }
}
