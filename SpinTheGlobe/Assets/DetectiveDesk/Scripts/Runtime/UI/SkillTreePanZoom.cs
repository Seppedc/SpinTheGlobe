using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Drag-to-pan and wheel-to-zoom for the skill tree content.</summary>
    public sealed class SkillTreePanZoom : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
    {
        public RectTransform content;
        public float minZoom = 0.3f, maxZoom = 1.6f;

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (!content) return;
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas ? canvas.scaleFactor : 1f;
            content.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scale);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!content) return;
            float current = content.localScale.x;
            float next = Mathf.Clamp(current * (1f + eventData.scrollDelta.y * 0.1f), minZoom, maxZoom);
            // Zoom around the content centre, keeping the visible point roughly in place.
            content.anchoredPosition *= next / current;
            content.localScale = Vector3.one * next;
        }
    }
}
