using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Physical paper ball instance that rests on the desk, can be launched into 3D space,
    /// bounces off scene objects, triggers basket scores, and fades out after landing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PaperBallInstance : MonoBehaviour
    {
        public enum State { InPile, Grabbed, Thrown, Settled, Despawning }

        [SerializeField] Rigidbody rb;
        [SerializeField] SphereCollider sphereCollider;

        public PaperBallThrower Owner { get; set; }
        public State CurrentState => state;
        public bool HasScored { get; private set; }
        public bool IsThrown => state == State.Thrown || state == State.Settled;
        public bool IsGrabbed => state == State.Grabbed;
        public Rigidbody Rigidbody => rb ? rb : (rb = GetComponent<Rigidbody>());
        public Vector3 Velocity => rb ? rb.linearVelocity : (TryGetComponent<Rigidbody>(out var r) ? r.linearVelocity : Vector3.zero);

        State state = State.InPile;
        Camera activeCamera;
        float grabPlaneDistance;
        Vector3 initialScale;
        float settleTimer;
        float flightTimer;
        float lastBounceTime;

        struct MouseSample
        {
            public Vector2 screenPos;
            public float time;
        }

        readonly List<MouseSample> samples = new List<MouseSample>(12);

        void Awake()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
            if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();

            // Natural 1.0x scale for clear visibility without blocking the hoop
            if (transform.localScale.x > 1.25f || transform.localScale.x < 0.75f)
            {
                transform.localScale = Vector3.one * 1.0f;
            }
            initialScale = transform.localScale;
            ConfigurePhysics();
        }

        void ConfigurePhysics()
        {
            if (!rb) return;
            rb.mass = 0.05f;
            rb.linearDamping = 0.25f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (sphereCollider && !sphereCollider.sharedMaterial)
            {
                var mat = new PhysicsMaterial("PaperPhysicMaterial")
                {
                    bounciness = 0.38f,
                    dynamicFriction = 0.65f,
                    staticFriction = 0.70f,
                    bounceCombine = PhysicsMaterialCombine.Average,
                    frictionCombine = PhysicsMaterialCombine.Average
                };
                sphereCollider.sharedMaterial = mat;
            }
        }

        public void MarkScored()
        {
            HasScored = true;
        }

        public void Launch(Vector3 initialVelocity, Vector3 initialAngularVelocity)
        {
            state = State.Thrown;
            settleTimer = 0f;
            flightTimer = 0f;

            if (!rb) rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.linearVelocity = initialVelocity;
                rb.angularVelocity = initialAngularVelocity;
            }

            Owner?.OnBallThrown(this);
        }

        public void BeginGrab(Camera camera)
        {
            if (state == State.Thrown || state == State.Despawning) return;
            activeCamera = camera ? camera : Camera.main;

            state = State.Grabbed;
            if (!rb) rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (activeCamera)
            {
                grabPlaneDistance = Vector3.Dot(transform.position - activeCamera.transform.position, activeCamera.transform.forward);
                grabPlaneDistance = Mathf.Clamp(grabPlaneDistance, 2.5f, 6.0f);
            }

            samples.Clear();
            samples.Add(new MouseSample { screenPos = Input.mousePosition, time = Time.unscaledTime });
        }

        public void CancelGrab()
        {
            if (state != State.Grabbed) return;
            state = State.InPile;
            if (rb)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }
            samples.Clear();
        }

        void Update()
        {
            if (state == State.Grabbed)
            {
                UpdateGrabbed();
            }
            else if (state == State.Thrown)
            {
                UpdateFlight();
            }
            else if (state == State.Despawning)
            {
                UpdateDespawn();
            }
        }

        void UpdateGrabbed()
        {
            if (!activeCamera) activeCamera = Camera.main;
            if (!activeCamera) { CancelGrab(); return; }

            // Right-click or Escape cancels grab
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelGrab();
                return;
            }

            // Track sample
            float now = Time.unscaledTime;
            Vector2 mousePos = Input.mousePosition;
            samples.Add(new MouseSample { screenPos = mousePos, time = now });
            while (samples.Count > 10 && (now - samples[0].time) > 0.15f)
            {
                samples.RemoveAt(0);
            }

            // Smoothly move slightly toward cursor so player feels holding the ball
            var ray = activeCamera.ScreenPointToRay(mousePos);
            Vector3 targetPos = ray.GetPoint(grabPlaneDistance);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.unscaledDeltaTime * 20f);

            // On mouse button release: compute flick
            if (Input.GetMouseButtonUp(0))
            {
                ReleaseGrab(mousePos, now);
            }
        }

        void ReleaseGrab(Vector2 releasePos, float releaseTime)
        {
            if (samples.Count < 2)
            {
                CancelGrab();
                return;
            }

            // Find sample from ~80ms to 120ms ago
            MouseSample referenceSample = samples[0];
            for (int i = samples.Count - 1; i >= 0; i--)
            {
                if (releaseTime - samples[i].time >= 0.06f)
                {
                    referenceSample = samples[i];
                    break;
                }
            }

            Vector2 deltaScreen = releasePos - referenceSample.screenPos;
            float dt = Mathf.Max(0.001f, releaseTime - referenceSample.time);
            Vector2 screenVelocity = deltaScreen / dt;

            // Check if flick was fast enough and directed generally upwards/forwards
            float flickSpeed = screenVelocity.magnitude;
            if (flickSpeed >= 220f && screenVelocity.y > -150f)
            {
                Throw(screenVelocity);
            }
            else
            {
                CancelGrab();
            }
        }

        public void Throw(Vector2 screenVelocity)
        {
            if (!activeCamera) activeCamera = Camera.main;
            state = State.Thrown;
            settleTimer = 0f;
            flightTimer = 0f;

            Vector3 aimDir;
            if (activeCamera && activeCamera.pixelWidth > 0 && activeCamera.pixelHeight > 0)
            {
                Vector2 aimScreen = (Vector2)Input.mousePosition + screenVelocity * 0.18f;
                var aimRay = activeCamera.ScreenPointToRay(aimScreen);
                Vector3 aimTarget = aimRay.GetPoint(6.5f);
                aimDir = (aimTarget - transform.position).normalized;
            }
            else
            {
                aimDir = new Vector3(screenVelocity.x * 0.001f, 0.4f, 1.0f).normalized;
            }

            if (aimDir.sqrMagnitude < 0.001f) aimDir = (Vector3.forward + Vector3.up * 0.3f).normalized;
            aimDir.y = Mathf.Max(aimDir.y, 0.12f) + 0.24f;
            aimDir.Normalize();

            float tSpeed = Mathf.Clamp01((screenVelocity.magnitude - 250f) / 1600f);
            float speed = Mathf.Lerp(7.5f, 15.5f, tSpeed);

            if (!rb) rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.linearVelocity = aimDir * speed;
                rb.angularVelocity = Random.insideUnitSphere * 16f;
            }

            Owner?.OnBallThrown(this);
        }

        void UpdateFlight()
        {
            flightTimer += Time.deltaTime;

            if (flightTimer > 0.4f && rb)
            {
                if (rb.linearVelocity.sqrMagnitude < 0.12f)
                {
                    settleTimer += Time.deltaTime;
                    if (settleTimer >= 3.0f)
                    {
                        state = State.Despawning;
                    }
                }
                else
                {
                    settleTimer = 0f;
                }
            }

            if (flightTimer >= 8.5f)
            {
                state = State.Despawning;
            }
        }

        void UpdateDespawn()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 5f);
            if (transform.localScale.x <= 0.02f)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (state == State.Thrown && Time.unscaledTime - lastBounceTime > 0.12f)
            {
                lastBounceTime = Time.unscaledTime;
                if (collision.relativeVelocity.magnitude > 1.2f)
                {
                    Owner?.PlayBounceSound(transform.position);
                }
            }
        }
    }
}
