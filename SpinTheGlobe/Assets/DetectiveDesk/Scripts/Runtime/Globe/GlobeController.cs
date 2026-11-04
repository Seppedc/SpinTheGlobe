using System;
using DetectiveDesk.Progression;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// The spinning globe: drag (strong) or click (small impulse) to add speed, one direction only,
    /// exponential friction down to the auto-rotate floor, hard cap at the max-speed stat.
    /// Raises <see cref="RevolutionsCompleted"/> each time the start line comes round again and
    /// <see cref="Advanced"/> every frame for pin payouts at the meridian.
    /// </summary>
    [DefaultExecutionOrder(-80), DisallowMultipleComponent]
    public sealed class GlobeController : MonoBehaviour
    {
        public Transform rotor;
        public Camera gameCamera;
        public GameManager game;
        public Collider dragCollider;
        public Vector3 markerLocalCenter;
        public float markerLocalRadius = .683f;
        [Tooltip("A press shorter than this (pixels of movement) counts as a click impulse.")]
        public float clickMaxPixels = 8f;

        public double TotalDegrees { get; private set; }
        public double CurrentVelocity { get; private set; }
        public double FirstRevolutionAngle { get; private set; } = 360;
        public double MaximumVelocity => game ? Math.Max(1, game.Stats.Get(StatId.GlobeMaxSpeed)) : 60;
        /// <summary>Auto-rotate floor: Clockwork Motor's share of the max speed (or a fixed speed, whichever is higher).</summary>
        public double AutoFloor => game
            ? Math.Max(0, Math.Min(MaximumVelocity, Math.Max(game.Stats.Get(StatId.GlobeAutoSpeed),
                                                             MaximumVelocity * Math.Min(1, game.Stats.Get(StatId.GlobeAutoShare)))))
            : 0;
        /// <summary>Hand spinning scales with the max speed, so dragging stays worthwhile on a fast globe.</summary>
        double ImpulseScale => game ? Math.Max(1, MaximumVelocity / Math.Max(1, game.Balance.impulseReferenceSpeed)) : 1;
        public bool IsRedline => CurrentVelocity >= MaximumVelocity * .95;
        public Vector3 Axis => rotor && rotor.parent ? rotor.parent.up : Vector3.up;
        public Vector3 Center => rotor ? rotor.TransformPoint(markerLocalCenter) : transform.position;
        public event Action<double, double> Advanced;
        public event Action<long> RevolutionsCompleted;
        /// <summary>Payout ticks (several per revolution, see GameBalance.globeTicksPerRevolution).</summary>
        public event Action<long> TicksCompleted;

        bool dragging;
        Vector2 pointer;
        float travelled;
        double dragAdded;   // speed requested by the current drag

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
                    travelled = 0;
                    dragAdded = 0;
                }
                if (dragging && Input.GetMouseButton(0))
                {
                    Vector2 next = Input.mousePosition;
                    float dx = next.x - pointer.x;
                    travelled += (next - pointer).magnitude;
                    AddSwipe(dx);
                    pointer = next;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    if (dragging && travelled <= clickMaxPixels) AddClick();
                    else if (dragging) FinishDrag();
                    dragging = false;
                }
            }
            Advance(Time.deltaTime);
        }

        public bool CanBeginDrag(Ray ray)
        {
            if (!rotor || !dragCollider || (game && game.WorldInputBlocked)) return false;
            if (!Physics.Raycast(ray, out RaycastHit hit, 100) || hit.collider != dragCollider) return false;
            // Keep the generous interaction box, but only start on the visible ball so a map click
            // inside that box never also grabs the globe.
            Vector3 direction = ray.direction.normalized, offset = ray.origin - Center;
            float radius = markerLocalRadius * Mathf.Abs(rotor.lossyScale.x);
            float along = Vector3.Dot(offset, direction);
            float discriminant = along * along - (offset.sqrMagnitude - radius * radius);
            return discriminant >= 0 && -along + Mathf.Sqrt(discriminant) > 0;
        }

        /// <summary>Drag input: positive (west → east) pixels only.</summary>
        public void AddSwipe(float horizontalPixels)
        {
            if (float.IsNaN(horizontalPixels) || float.IsInfinity(horizontalPixels) || horizontalPixels <= 0) return;
            double gain = game ? game.Balance.dragAcceleration * game.Stats.Get(StatId.GlobeDragPower) * ImpulseScale : .35;
            dragAdded += horizontalPixels * gain;
            AddVelocity(horizontalPixels * gain);
        }

        /// <summary>
        /// Rule: dragging is always more rewarding than clicking. A drag in the spin direction gives at least
        /// <see cref="GameBalance.dragOverClick"/> × a click's impulse, using the (always higher) drag power.
        /// </summary>
        void FinishDrag()
        {
            if (dragAdded <= 0 || !game) return;
            double minimum = game.Balance.clickImpulse * game.Balance.dragOverClick * game.Stats.Get(StatId.GlobeDragPower) * ImpulseScale;
            if (dragAdded < minimum) AddVelocity(minimum - dragAdded);
        }

        /// <summary>Click input: a fixed impulse, deliberately weaker than a good drag.</summary>
        public void AddClick()
        {
            double impulse = game ? game.Balance.clickImpulse * game.Stats.Get(StatId.GlobeClickPower) * ImpulseScale : 10;
            AddVelocity(impulse);
        }

        void AddVelocity(double amount)
        {
            if (amount <= 0 || IsRedline && CurrentVelocity >= MaximumVelocity) return;
            CurrentVelocity = Math.Min(MaximumVelocity, Math.Max(AutoFloor, CurrentVelocity) + amount);
        }

        public void EnforceLimits() { CurrentVelocity = Math.Max(AutoFloor, Math.Min(MaximumVelocity, CurrentVelocity)); }

        public void Advance(double seconds)
        {
            double velocity = CurrentVelocity;
            double friction = game ? game.Balance.frictionAt60Hz : .985;
            double delta = GameplayMath.Integrate(ref velocity, seconds, friction, AutoFloor, MaximumVelocity);
            CurrentVelocity = velocity;
            if (game && game.legacy) game.legacy.globeSpeed = (float)CurrentVelocity;
            if (delta <= 0 || !rotor) return;
            double previous = TotalDegrees;
            TotalDegrees += delta;
            // Longitude +east points +X from the front -Z: negative Unity Y is physical west-to-east.
            rotor.Rotate(Vector3.up, -(float)(delta % 360), Space.Self);
            long completed = GameplayMath.Crossings(previous, TotalDegrees, FirstRevolutionAngle);
            if (completed > 0) RevolutionsCompleted?.Invoke(completed);
            int ticks = game ? Math.Max(1, game.Balance.globeTicksPerRevolution) : 1;
            long tickCount = GameplayMath.Crossings(previous, TotalDegrees, FirstRevolutionAngle - 360 + 360.0 / ticks, 360.0 / ticks);
            if (tickCount > 0) TicksCompleted?.Invoke(tickCount);
            Advanced?.Invoke(previous, TotalDegrees);
        }

        public Vector3 MarkerPosition(float latitude, float longitude) => markerLocalCenter + GameplayMath.Spherical(latitude, longitude, markerLocalRadius);
        void OnDisable() { dragging = false; }
    }
}
