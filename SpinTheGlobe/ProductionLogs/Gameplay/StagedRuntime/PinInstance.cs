using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public sealed class PinInstance : MonoBehaviour
    {
        public CountryData Country { get; private set; }
        public GlobePinMarker Marker { get; private set; }
        public int RemainingPasses { get; private set; }
        public int InitialPasses { get; private set; }
        public float PinMultiplier { get; private set; }
        public double NextPass { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsActive => active;
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        WallMapManager owner;
        Quaternion rest;
        float wobble;
        bool active;
        Renderer[] renderers;

        public void Initialize(WallMapManager map, CountryData country, float lat, float lon,
            GlobePinMarker marker, int charges, float multiplier, double nextPass)
        {
            owner = map; Country = country; Latitude = lat; Longitude = lon; Marker = marker;
            InitialPasses = RemainingPasses = Mathf.Max(1, charges);
            PinMultiplier = Mathf.Max(0, multiplier); NextPass = nextPass;
            active = true; rest = transform.rotation; renderers = GetComponentsInChildren<Renderer>();
        }

        public bool CrossIfDue(double currentPhase)
        {
            if (!active || currentPhase + 1e-7 < NextPass) return false;
            // Re-arming is tied to unwrapped phase, never merely leaving an epsilon band.
            IsLocked = true;
            NextPass += 360;
            RemainingPasses--;
            owner.game.Credit(Country.baseValue * PinMultiplier * owner.game.GlobalMultiplier);
            owner.EmitDust(transform.position);
            if (RemainingPasses <= 0) Detach();
            else wobble = 1;
            return true;
        }

        void Update()
        {
            if (!active || RemainingPasses == InitialPasses) return;
            wobble = Mathf.MoveTowards(wobble, 0, Time.deltaTime * 1.6f);
            float tilt = Mathf.Lerp(30, 45, 1 - RemainingPasses / (float)InitialPasses);
            transform.rotation = rest * Quaternion.Euler(tilt + Mathf.Sin(Time.time * 39) * wobble * 14,
                Mathf.Cos(Time.time * 33) * wobble * 8, 0);
        }

        public void UpdateLock(double phase) { if (active && NextPass - phase < 330) IsLocked = false; }
        void Detach()
        {
            if (!active) return;
            active = false;
            if (Marker) Destroy(Marker.gameObject);
            owner.Unregister(this);
            var bounds = new Bounds(transform.position, Vector3.one * .015f);
            if (renderers != null) foreach (var r in renderers) if (r) bounds.Encapsulate(r.bounds);
            // A new small drop collider is local to the runtime clone; authored colliders stay untouched.
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(bounds.center);
            Vector3 scale = transform.lossyScale;
            box.size = new Vector3(bounds.size.x / Mathf.Max(.00001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(.00001f, Mathf.Abs(scale.y)), bounds.size.z / Mathf.Max(.00001f, Mathf.Abs(scale.z)));
            var body = gameObject.GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            body.isKinematic = false; body.detectCollisions = true; body.mass = .02f;
            body.linearVelocity = owner.mapNormal * .20f;
            body.angularVelocity = new Vector3(2, 3, 1);
            owner.TrackDropped(gameObject);
            Destroy(gameObject, 8);
        }

        public void RemoveWithoutReward()
        {
            if (!active) return;
            active = false; if (Marker) Destroy(Marker.gameObject); owner.Unregister(this); Destroy(gameObject);
        }
        void OnDestroy() { if (Marker) Destroy(Marker.gameObject); if (active && owner) owner.Unregister(this); }
    }
}
