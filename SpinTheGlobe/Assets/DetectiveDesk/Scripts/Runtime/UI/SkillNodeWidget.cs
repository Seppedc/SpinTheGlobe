using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Pointer events of one skill-tree node: hover shows the tooltip, a click (not a drag) buys.</summary>
    public sealed class SkillNodeWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public string nodeId;
        public Action<SkillNodeWidget> Entered, Exited, Clicked;

        public void OnPointerEnter(PointerEventData e) => Entered?.Invoke(this);
        public void OnPointerExit(PointerEventData e) => Exited?.Invoke(this);

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || e.dragging) return;
            Clicked?.Invoke(this);
        }
    }
}
