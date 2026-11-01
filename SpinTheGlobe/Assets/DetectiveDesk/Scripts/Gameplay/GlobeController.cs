using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    [DefaultExecutionOrder(-80), DisallowMultipleComponent]
    public sealed class GlobeController : MonoBehaviour
    {
        public Transform rotor;
        public Camera gameCamera;
        public GameManager game;
        public Collider dragCollider;
        public Vector3 markerLocalCenter;
        public float markerLocalRadius = .683f;
        public double TotalDegrees { get; private set; }
        public double CurrentVelocity { get; private set; }
        public double FirstRevolutionAngle { get; private set; } = 360;
        public double MaximumVelocity => game && game.config ? game.config.baseMaximumSpeed + game.SpeedLevel * game.config.maximumSpeedPerLevel : 30;
        public double AutoFloor => game && game.config ? Math.Min(MaximumVelocity, game.MotorLevel * game.config.autoSpeedPerLevel) : 0;
        public bool IsRedline => CurrentVelocity >= MaximumVelocity * .95;
        public Vector3 Axis => rotor && rotor.parent ? rotor.parent.up : Vector3.up;
        public Vector3 Center => rotor ? rotor.TransformPoint(markerLocalCenter) : transform.position;
        public event Action<double, double> Advanced;
        public event Action<long> RevolutionsCompleted;
        bool dragging;
        Vector2 pointer;

        void Start()
        {
            if (!rotor || !gameCamera) return;
            Vector3 prime = Vector3.ProjectOnPlane(rotor.TransformPoint(MarkerPosition(0, 0)) - Center, Axis).normalized;
            Vector3 wire = Vector3.ProjectOnPlane(gameCamera.transform.position - Center, Axis).normalized;
            double offset = GameplayMath.Repeat(-Vector3.SignedAngle(prime, wire, Axis), 360);
            // Skip the first partial turn: only returning to the origin after a full turn pays.
            FirstRevolutionAngle = offset < .00001 ? 360 : 360 + offset;
        }

        void Update()
        {
            if (game && game.WorldInputBlocked) { dragging = false; }
            else if (gameCamera && dragCollider)
            {
                if (Input.GetMouseButtonDown(0) && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
                {
                    dragging = CanBeginDrag(gameCamera.ScreenPointToRay(Input.mousePosition));
                    pointer = Input.mousePosition;
                }
                if (dragging && Input.GetMouseButton(0))
                {
                    Vector2 next = Input.mousePosition;
                    AddSwipe(next.x - pointer.x);
                    pointer = next;
                }
                if (Input.GetMouseButtonUp(0)) dragging = false;
            }
            Advance(Time.deltaTime);
        }

        public bool CanBeginDrag(Ray ray)
        {
            if (!rotor || !dragCollider || (game && game.WorldInputBlocked)) return false;
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 100) || hit.collider != dragCollider) return false;
            // Preserve the original generous interaction box, but start a spin only
            // over the visible ball. A visible map country inside that box must not
            // simultaneously acquire a drag when the player clicks its country.
            Vector3 direction = ray.direction.normalized, offset = ray.origin - Center;
            float radius = markerLocalRadius * Mathf.Abs(rotor.lossyScale.x);
            float along = Vector3.Dot(offset, direction);
            float discriminant = along * along - (offset.sqrMagnitude - radius * radius);
            return discriminant >= 0 && -along + Mathf.Sqrt(discriminant) > 0;
        }

        public void AddSwipe(float horizontalPixels)
        {
            if (float.IsNaN(horizontalPixels) || float.IsInfinity(horizontalPixels) || horizontalPixels <= 0 || IsRedline) return;
            double gain = game && game.config ? game.config.swipeAcceleration : .35;
            CurrentVelocity = Math.Min(MaximumVelocity, Math.Max(AutoFloor, CurrentVelocity) + horizontalPixels * gain);
        }

        public void EnforceLimits() { CurrentVelocity = Math.Max(AutoFloor, Math.Min(MaximumVelocity, CurrentVelocity)); }
        public void Advance(double seconds)
        {
            double velocity = CurrentVelocity;
            double delta = GameplayMath.Integrate(ref velocity, seconds, game && game.config ? game.config.frictionAt60Hz : .985, AutoFloor, MaximumVelocity);
            CurrentVelocity = velocity;
            if (game && game.legacy) game.legacy.globeSpeed = (float)CurrentVelocity;
            if (delta <= 0 || !rotor) return;
            double previous = TotalDegrees;
            TotalDegrees += delta;
            // Longitude +east points +X from the front -Z: negative Unity Y is physical west-to-east.
            rotor.Rotate(Vector3.up, -(float)(delta % 360), Space.Self);
            long completed = GameplayMath.Crossings(previous, TotalDegrees, FirstRevolutionAngle);
            if (completed > 0) RevolutionsCompleted?.Invoke(completed);
            Advanced?.Invoke(previous, TotalDegrees);
        }

        public Vector3 MarkerPosition(float latitude, float longitude) => markerLocalCenter + GameplayMath.Spherical(latitude, longitude, markerLocalRadius);
        void OnDisable() { dragging = false; }
    }
}
