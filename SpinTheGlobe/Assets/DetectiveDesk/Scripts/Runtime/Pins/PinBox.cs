using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// The pin coffer. Shows the player's pins in its 12 sockets (a socket holds a pin when that pin
    /// is unlocked and in the box, not flying or stuck in the map). The lid stays closed until the
    /// first pin is unlocked, then swings open.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PinBox : MonoBehaviour
    {
        [Tooltip("Degrees the lid hinge rotates (around its local X) from the authored open pose to closed.")]
        public float closedAngle = -108f;
        [Min(0.05f)] public float lidSeconds = 0.8f;

        GameManager game;
        Transform lid;
        Quaternion lidOpenRotation;
        float lidOpen; // 0 closed .. 1 open

        public bool IsOpen => game && game.UnlockedPins > 0;

        public void Initialize(GameManager owner)
        {
            game = owner;
            FindLid();
            lidOpen = IsOpen && !game.Guidance.NeedsPinBox ? 1 : 0;
            ApplyLid();
            Refresh();
        }

        void FindLid()
        {
            if (game.pinSlots == null || game.pinSlots.Length == 0 || !game.pinSlots[0]) return;
            var interaction = game.pinSlots[0].GetComponentInParent<DeskInteractable>();
            var root = interaction ? interaction.transform : game.pinSlots[0].transform.root;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Lid") { lid = t; lidOpenRotation = t.localRotation; return; }
        }

        void Update()
        {
            AdvanceLid(Time.deltaTime, game && !game.WorldInputBlocked && Application.isFocused);
        }

        /// <summary>The first opening waits until the desk is visible and pauses if it is hidden again.</summary>
        public void AdvanceLid(float seconds, bool deskVisible)
        {
            if (!game || !lid) return;
            if (IsOpen && game.Guidance.NeedsPinBox && !deskVisible) return;
            float target = IsOpen ? 1 : 0;
            if (Mathf.Approximately(lidOpen, target)) return;
            lidOpen = Mathf.MoveTowards(lidOpen, target, Mathf.Max(0, seconds) / Mathf.Max(.05f, lidSeconds));
            ApplyLid();
            if (IsOpen && lidOpen >= 1) game.Guidance.Complete(DetectiveDesk.Progression.PlayerGuidanceState.PinBoxOpened);
        }

        void ApplyLid()
        {
            if (!lid) return;
            float e = lidOpen * lidOpen * (3 - 2 * lidOpen);
            lid.localRotation = lidOpenRotation * Quaternion.Euler(closedAngle * (1 - e), 0, 0);
        }

        public void Refresh()
        {
            if (!game || game.pinSlots == null) return;
            int unlocked = game.UnlockedPins;
            foreach (var slot in game.pinSlots)
            {
                if (!slot) continue;
                int index = slot.SlotNumber - 1;
                bool show = index < unlocked && !(game.map && game.map.IsBusy(index));
                if (show && slot.IsEmpty && slot.PinPrefab) slot.PlacePin(0);
                else if (!show && !slot.IsEmpty)
                {
                    var pin = slot.ReleasePin();
                    if (pin) Destroy(pin.gameObject);
                }
                if (show && slot.Occupant) game.ApplyPinAppearance(slot.Occupant, index);
            }
        }

        void OnDisable() { if (lid) lid.localRotation = lidOpenRotation; }
    }
}
