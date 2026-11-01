using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public sealed class GlobePinMarker : MonoBehaviour
    {
        Material illumination;
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        public void Initialize(GlobeController globe, float latitude, float longitude)
        {
            Latitude = latitude; Longitude = longitude;
            transform.SetParent(globe.rotor, false);
            transform.localPosition = globe.MarkerPosition(latitude, longitude);
            // The visual is oriented independently below this stable geographic anchor.
        }
        public void Illuminate(PinSkin pin)
        {
            var head = pin ? pin.EnamelHeadRenderer : null;
            if (!head) return;
            var slots = head.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == pin.SkinMaterials[pin.SkinIndex])
                {
                    illumination = new Material(slots[i]);
                    illumination.name = "Runtime marker enamel";
                    illumination.SetColor("_EmissionColor", illumination.GetColor("_BaseColor") * .65f);
                    illumination.EnableKeyword("_EMISSION"); slots[i] = illumination;
                    head.sharedMaterials = slots; break;
                }
        }
        void OnDestroy() { if (illumination) Destroy(illumination); }
    }

    public static class PinVisual
    {
        public static PinSkin Create(PinSkin prefab, Transform parent, Vector3 position, Vector3 outward,
            float length, int skin)
        {
            var pin = Object.Instantiate(prefab, parent, false);
            pin.transform.position = position;
            pin.transform.rotation = prefab.transform.rotation;
            // Gameplay visuals never inherit socket, hover or other input hitboxes.
            foreach (var collider in pin.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in pin.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            foreach (var t in pin.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            var renderers = pin.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Vector3 direction = pin.EnamelHeadRenderer ? pin.EnamelHeadRenderer.bounds.center - bounds.center : pin.transform.up;
                if (direction.sqrMagnitude < 1e-9) direction = pin.transform.up;
                float biggest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (biggest > .00001f) pin.transform.localScale *= length / biggest;
                pin.transform.rotation = Quaternion.FromToRotation(direction.normalized, outward.normalized) * pin.transform.rotation;
            }
            pin.SetSkin(skin);
            return pin;
        }
    }
}
