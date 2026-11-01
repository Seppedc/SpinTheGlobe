using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public sealed class YarnRenderer : MonoBehaviour
    {
        public LineRenderer line;
        public void Rebuild(IReadOnlyList<PinInstance> pins, Vector3 normal)
        {
            if (!line) return;
            line.useWorldSpace = true;
            int count = 0; foreach (var pin in pins) if (pin && pin.IsActive) count++;
            line.positionCount = count >= 2 ? count : 0;
            if (count < 2) return;
            int i = 0; foreach (var pin in pins) if (pin && pin.IsActive) line.SetPosition(i++, pin.transform.position + normal * .018f);
        }
    }
}
