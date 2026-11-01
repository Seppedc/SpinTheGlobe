using System;
using UnityEngine;

namespace DetectiveDesk
{
    /// <summary>One authored pin socket; pins retain their original model orientation and individual skin.</summary>
    [DisallowMultipleComponent]
    public sealed class PinCofferSlot : MonoBehaviour
    {
        [SerializeField, Range(1, 12)] int slotNumber = 1;
        [SerializeField] PinSkin pinPrefab;
        [SerializeField] PinSkin occupant;

        public int SlotNumber => slotNumber;
        public PinSkin PinPrefab => pinPrefab;
        public PinSkin Occupant => occupant;
        public bool IsEmpty => !occupant;
        public event Action<PinCofferSlot> Selected;

        public PinSkin PlacePin(int skinIndex)
        {
            if (occupant) throw new InvalidOperationException("This pin socket is occupied.");
            if (!pinPrefab) throw new InvalidOperationException("No collectible pin prefab is assigned.");
            if (skinIndex < 0 || skinIndex >= PinSkin.SkinCount) throw new ArgumentOutOfRangeException(nameof(skinIndex));
            var pin = Instantiate(pinPrefab, transform, false);
            pin.transform.localPosition = Vector3.zero;
            pin.transform.localRotation = pinPrefab.transform.localRotation;
            pin.transform.localScale = pinPrefab.transform.localScale;
            pin.SetSkin(skinIndex);
            occupant = pin;
            return pin;
        }

        public PinSkin ReleasePin(Transform newParent = null)
        {
            var pin = occupant;
            if (pin) pin.transform.SetParent(newParent, true);
            occupant = null;
            return pin;
        }

        public void Select()
        {
            var interaction = GetComponentInParent<DeskInteractable>();
            if (interaction && interaction.game && interaction.action == DeskInteractable.Action.PinBox)
                interaction.game.SelectPin(this);
            Selected?.Invoke(this);
        }

        void OnMouseDown()
        {
            if (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            Select();
        }
    }
}
