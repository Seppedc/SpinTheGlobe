using DetectiveDesk.Progression;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>White surface prompts for first-use objects and cases ready to claim.</summary>
    [DisallowMultipleComponent]
    public sealed class FirstUseGuidance : MonoBehaviour
    {
        [Tooltip("The globe's rotating visible geometry. Its stand and other desk objects are excluded.")]
        public Transform globeTarget;
        [Tooltip("The complete pin shooter model. Resolved from the launcher's model hierarchy if not assigned.")]
        public Transform shooterTarget;
        [Tooltip("The complete stamp model, highlighted whenever a case can be claimed.")]
        public Transform stampTarget;
        GameManager game;
        ObjectPulseOverlay overlay;
        ObjectPulseOverlay shooterOverlay;
        ObjectPulseOverlay stampOverlay;
        double elapsed;
        double shooterElapsed;
        double stampElapsed;

        public void Initialize(GameManager owner)
        {
            game = owner;
            if (!globeTarget && game.globe) globeTarget = game.globe.rotor;
            overlay = new ObjectPulseOverlay(globeTarget, game.Balance.guidanceOverlayMaterial);
            if (!shooterTarget && game.map)
            {
                shooterTarget = game.map.launcherVisual;
                for (var t = shooterTarget; t; t = t.parent)
                    if (t.name == "PinLauncher") { shooterTarget = t; break; }
            }
            shooterOverlay = new ObjectPulseOverlay(shooterTarget, game.Balance.guidanceOverlayMaterial);
            if (!stampTarget && game.Cases) stampTarget = game.Cases.StampTransform;
            stampOverlay = new ObjectPulseOverlay(stampTarget, game.Balance.guidanceOverlayMaterial);
            elapsed = 0;
            shooterElapsed = 0;
            stampElapsed = 0;
        }

        void LateUpdate()
        {
            if (!game || game.WorldInputBlocked || !Application.isFocused) return;
            if (overlay != null && game.Guidance.NeedsGlobe)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.Draw(PlayerGuidanceState.PulseOpacity(elapsed, game.Balance.guidancePulseSeconds,
                    game.Balance.guidanceOverlayOpacity));
            }
            if (shooterOverlay != null && game.Guidance.ShowPinShooter(game.UnlockedPins > 0))
            {
                shooterElapsed += Time.unscaledDeltaTime;
                shooterOverlay.Draw(PlayerGuidanceState.PulseOpacity(shooterElapsed, game.Balance.guidancePulseSeconds,
                    game.Balance.guidanceOverlayOpacity));
            }
            if (stampOverlay != null && game.Cases && game.Cases.CanClaim)
            {
                stampElapsed += Time.unscaledDeltaTime;
                stampOverlay.Draw(PlayerGuidanceState.PulseOpacity(stampElapsed, game.Balance.guidancePulseSeconds,
                    game.Balance.guidanceOverlayOpacity));
            }
            else stampElapsed = 0;
        }
    }
}
