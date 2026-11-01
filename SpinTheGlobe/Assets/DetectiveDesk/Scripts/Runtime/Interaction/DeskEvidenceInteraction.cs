using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk
{
    public sealed class DeskEvidenceInteraction : MonoBehaviour
    {
        public DeskAssetPresentation presentation;
        [Range(0, 5)] public int evidenceIndex;
        void OnMouseDown()
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            if (presentation) presentation.InspectEvidence(evidenceIndex);
        }
    }
}
