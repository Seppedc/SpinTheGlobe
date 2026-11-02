using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Trigger inside the waste bin: a paper ball entering it scores once.</summary>
    public sealed class PaperBallBin : MonoBehaviour
    {
        public PaperBallThrower thrower;

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<PaperBallMarker>();
            if (!ball || ball.scored || !thrower) return;
            ball.scored = true;
            thrower.Scored(ball.gameObject);
        }
    }
}
