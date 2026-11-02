using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// One pin stuck in the wall map. Its globe marker pays out every time it passes the payout line;
    /// each pass costs one strength. At zero strength the pin pops off the board and falls to the desk.
    /// </summary>
    public sealed class PinInstance : MonoBehaviour
    {
        public int Index { get; private set; }
        public PinLanding Landing { get; private set; }
        public GlobePinMarker Marker { get; private set; }
        public int RemainingPasses { get; private set; }
        public int InitialPasses { get; private set; }
        public double NextPass { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsActive => active;
        public float Latitude => Landing.Latitude;
        public float Longitude => Landing.Longitude;

        WallMapManager owner;
        Quaternion rest;
        float wobble;
        bool active;

        public void Initialize(WallMapManager map, int index, PinLanding landing, GlobePinMarker marker, int passes, double nextPass)
        {
            owner = map; Index = index; Landing = landing; Marker = marker;
            InitialPasses = RemainingPasses = Mathf.Max(1, passes);
            NextPass = nextPass;
            active = true; rest = transform.rotation;
        }

        public bool CrossIfDue(double currentPhase)
        {
            if (!active || currentPhase + 1e-7 < NextPass) return false;
            // Re-arming is tied to the unwrapped phase, never merely leaving an epsilon band.
            IsLocked = true;
            NextPass += 360;
            RemainingPasses--;
            owner.OnPinPassed(this);
            owner.EmitDust(transform.position);
            if (RemainingPasses <= 0) Detach();
            else wobble = 1;
            return true;
        }

        void Update()
        {
            if (!active || RemainingPasses == InitialPasses) return;
            wobble = Mathf.MoveTowards(wobble, 0, Time.deltaTime * 1.6f);
            float tilt = Mathf.Lerp(12, 40, 1 - RemainingPasses / (float)InitialPasses);
            transform.rotation = rest * Quaternion.Euler(tilt + Mathf.Sin(Time.time * 39) * wobble * 14,
                Mathf.Cos(Time.time * 33) * wobble * 8, 0);
        }

        public void UpdateLock(double phase) { if (active && NextPass - phase < 330) IsLocked = false; }

        void Detach()
        {
            if (!active) return;
            active = false;
            if (Marker) Destroy(Marker.gameObject);
            owner.OnPinFell(this);
            var bounds = new Bounds(Vector3.zero, Vector3.one * .001f);
            foreach (var mesh in GetComponentsInChildren<MeshFilter>())
            {
                if (!mesh.sharedMesh) continue;
                var local = mesh.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = local.center + Vector3.Scale(local.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    bounds.Encapsulate(transform.InverseTransformPoint(mesh.transform.TransformPoint(corner)));
                }
            }
            // A small drop collider local to this runtime clone; authored colliders stay untouched.
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            var body = gameObject.GetComponent<Rigidbody>();
            if (!body) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = false; body.detectCollisions = true; body.useGravity = true; body.mass = .02f;
            // Release the clone clear of the board's backing collider, then pop it toward the desk.
            transform.position += owner.mapNormal * .12f;
            body.position = transform.position;
            body.WakeUp();
            body.linearVelocity = owner.mapNormal * owner.dropOutwardSpeed;
            body.angularVelocity = new Vector3(2, 3, 1);
            owner.TrackDropped(gameObject);
            Destroy(gameObject, 8);
        }

        public void RemoveWithoutReward()
        {
            if (!active) return;
            active = false;
            if (Marker) Destroy(Marker.gameObject);
            owner.Unregister(this);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Marker) Destroy(Marker.gameObject);
            if (active && owner) owner.Unregister(this);
        }
    }
}
