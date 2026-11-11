using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Detects when a thrown physics paper ball passes downward through the basketball hoop rim.
    /// Also provides a gentle inward assist (soft rim) so near-misses rattle into the hoop.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class PaperHoopTrigger : MonoBehaviour
    {
        public PaperBallThrower thrower;
        [SerializeField] float assistRadius = 0.42f;
        [SerializeField] float assistStrength = 6.0f;

        void Awake()
        {
            var col = GetComponent<Collider>();
            if (col) col.isTrigger = true;
            if (!thrower) thrower = FindAnyObjectByType<PaperBallThrower>();
        }

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<PaperBallInstance>();
            if (!ball && other.attachedRigidbody) ball = other.attachedRigidbody.GetComponent<PaperBallInstance>();
            if (!ball || !ball.IsThrown || ball.HasScored) return;

            // Only score if the ball is moving downwards into the hoop
            if (ball.Velocity.y <= 0.45f)
            {
                ball.MarkScored();

                // Swish net physics: dampen horizontal speed and funnel ball straight down through the net
                var rb = ball.Rigidbody;
                if (rb)
                {
                    Vector3 center = transform.position;
                    Vector3 currentVel = rb.linearVelocity;
                    Vector3 funnel = new Vector3(center.x - rb.position.x, 0, center.z - rb.position.z) * 4.0f;
                    rb.linearVelocity = new Vector3(currentVel.x * 0.22f + funnel.x, Mathf.Clamp(currentVel.y * 0.6f, -2.4f, -0.9f), currentVel.z * 0.22f + funnel.z);
                    rb.angularVelocity *= 0.4f;
                }

                if (thrower) thrower.OnHoopScored(ball);
            }
        }

        void FixedUpdate()
        {
            // Soft rim assist: if any falling paper ball is near the rim, gently pull horizontally towards rim center
            Vector3 center = transform.position;
            var hits = Physics.OverlapSphere(center + Vector3.up * 0.15f, assistRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                ApplySoftRimAssist(hits[i]);
            }
        }

        public bool ApplySoftRimAssist(Collider other)
        {
            if (!other) return false;
            var rb = other.attachedRigidbody;
            if (!rb || rb.isKinematic) return false;
            var ball = rb.GetComponent<PaperBallInstance>();
            if (!ball || !ball.IsThrown) return false;

            // Only assist when ball is descending toward hoop
            if (rb.linearVelocity.y > 0.45f) return false;

            Vector3 center = transform.position;
            Vector3 diff = center - rb.position;
            diff.y = 0f; // horizontal inward nudge only
            float dist = diff.magnitude;

            if (dist > 0.02f && dist < assistRadius)
            {
                Vector3 nudge = diff.normalized * assistStrength;
                rb.AddForce(nudge, ForceMode.Acceleration);
                return true;
            }
            return false;
        }
    }
}
