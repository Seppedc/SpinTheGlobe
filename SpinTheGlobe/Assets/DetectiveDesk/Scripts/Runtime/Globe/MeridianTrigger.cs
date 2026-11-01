using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MeridianTrigger : MonoBehaviour
    {
        public GlobeController globe;
        public WallMapManager map;
        public Material wireMaterial;
        public LineRenderer stationaryWire, primeRibbon;
        public Vector3 WireDirection { get; private set; }
        void OnEnable() { if (globe) globe.Advanced += CheckCrossings; }
        void OnDisable() { if (globe) globe.Advanced -= CheckCrossings; }
        void Start()
        {
            if (!globe || !globe.rotor || !globe.gameCamera) return;
            WireDirection = Vector3.ProjectOnPlane(globe.gameCamera.transform.position - globe.Center, globe.Axis).normalized;
            float radius = globe.markerLocalRadius * globe.rotor.lossyScale.x;
            if (stationaryWire)
            {
                stationaryWire.useWorldSpace = true; stationaryWire.positionCount = 2;
                Vector3 front = globe.Center + WireDirection * radius * 1.035f;
                stationaryWire.SetPosition(0, front + globe.Axis * radius * .98f);
                stationaryWire.SetPosition(1, front - globe.Axis * radius * .98f);
            }
            if (primeRibbon)
            {
                primeRibbon.transform.SetParent(globe.rotor, false); primeRibbon.useWorldSpace = false; primeRibbon.positionCount = 45;
                for (int i = 0; i < 45; i++) primeRibbon.SetPosition(i, globe.markerLocalCenter + GameplayMath.Spherical(-85 + i * 170f / 44, 0, globe.markerLocalRadius * 1.006f));
            }
        }

        public double NextPass(Vector3 markerWorld)
        {
            Vector3 wire = WireDirection.sqrMagnitude > .5f ? WireDirection : Vector3.ProjectOnPlane(globe.gameCamera.transform.position - globe.Center, globe.Axis).normalized;
            Vector3 marker = Vector3.ProjectOnPlane(markerWorld - globe.Center, globe.Axis).normalized;
            double forwardPhase = -Vector3.SignedAngle(marker, wire, globe.Axis);
            return GameplayMath.NextCrossing(globe.TotalDegrees, forwardPhase);
        }
        void CheckCrossings(double previous, double current)
        {
            if (!map) return;
            // Snapshot because consuming the final charge unregisters the pin during iteration.
            var pins = map.ActivePinsSnapshot();
            foreach (var pin in pins)
            {
                if (!pin) continue;
                // Handles frame skips/multiple revolutions without missing or duplicating a pass.
                while (pin.IsActive && pin.NextPass <= current + 1e-7) pin.CrossIfDue(current);
                pin.UpdateLock(current);
            }
        }
    }
}
