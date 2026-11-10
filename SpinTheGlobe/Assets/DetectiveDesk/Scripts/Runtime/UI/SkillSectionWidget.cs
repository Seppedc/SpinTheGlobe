using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>A free information node. Pointer interaction never purchases a skill.</summary>
    public sealed class SkillSectionWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Entered, Exited;
        public void OnPointerEnter(PointerEventData e) => Entered?.Invoke();
        public void OnPointerExit(PointerEventData e) => Exited?.Invoke();
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && !e.dragging) Entered?.Invoke();
        }
    }
}
