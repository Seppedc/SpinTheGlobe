using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Shows the player's pins in the 12 coffer sockets: a socket holds a pin when that pin is
    /// unlocked and currently in the box (not flying or stuck in the map).
    /// The box stays open for now; a closed lid until the first unlock is planned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PinBox : MonoBehaviour
    {
        GameManager game;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Refresh();
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
                if (show && slot.IsEmpty && slot.PinPrefab) slot.PlacePin(index % PinSkin.SkinCount);
                else if (!show && !slot.IsEmpty)
                {
                    var pin = slot.ReleasePin();
                    if (pin) Destroy(pin.gameObject);
                }
            }
        }
    }
}
