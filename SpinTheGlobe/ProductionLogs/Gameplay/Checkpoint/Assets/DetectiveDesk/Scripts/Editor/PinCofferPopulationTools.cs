using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Play Mode test helpers; no scene or prefab assets are edited.</summary>
    [InitializeOnLoad]
    public static class PinCofferPopulationTools
    {
        static readonly Dictionary<PinCofferSlot, PinSkin> testPins = new Dictionary<PinCofferSlot, PinSkin>();
        const string MenuRoot = "Tools/Detective Desk/Test Pins/";

        static PinCofferPopulationTools()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    testPins.Clear();
            };
        }

        static PinCofferSlot[] Slots()
        {
            var selected = Selection.activeGameObject
                ? Selection.activeGameObject.GetComponentInParent<DeskInteractable>() : null;
            var game = Object.FindAnyObjectByType<DetectiveGame>();
            var slots = selected && selected.action == DeskInteractable.Action.PinBox
                ? selected.GetComponentsInChildren<PinCofferSlot>()
                : game ? game.GetComponentsInChildren<PinCofferSlot>() : new PinCofferSlot[0];
            System.Array.Sort(slots, (a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            return slots;
        }

        [MenuItem(MenuRoot + "Populate 12 Skins")]
        static void PopulateMenu() { Populate(); }

        [MenuItem(MenuRoot + "Populate 12 Skins", true)]
        static bool CanPopulate() { return Application.isPlaying && Slots().Length == 12; }

        public static int Populate()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to populate test pins."); return 0; }
            var slots = Slots();
            if (slots.Length != 12) { Debug.LogWarning("Select a coffer with twelve sockets."); return 0; }
            foreach (var slot in slots)
                if (slot.IsEmpty && !slot.PinPrefab) { Debug.LogWarning("A socket has no collectible pin prefab assigned.", slot); return 0; }

            int added = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty) continue;
                var pin = slot.PlacePin(slot.SlotNumber - 1);
                testPins[slot] = pin;
                added++;
            }
            Debug.Log("Added " + added + " test pins. Slots 01–12 use Ruby through Midnight. Existing occupants were retained.");
            return added;
        }

        [MenuItem(MenuRoot + "Validate Population")]
        static void ValidateMenu() { ValidateCurrentPopulation(); }

        [MenuItem(MenuRoot + "Validate Population", true)]
        static bool CanValidate() { return Application.isPlaying && Slots().Length == 12; }

        public static bool ValidateCurrentPopulation()
        {
            if (!Application.isPlaying) return false;
            var slots = Slots();
            var issues = new List<string>();
            var heads = new HashSet<Renderer>();
            if (slots.Length != 12) issues.Add("Expected twelve sockets.");
            foreach (var slot in slots)
            {
                var pin = slot.Occupant;
                if (!pin) { issues.Add(slot.name + " is empty."); continue; }
                int expected = slot.SlotNumber - 1;
                if (pin.SkinIndex != expected || !pin.EnamelHeadRenderer ||
                    pin.EnamelHeadRenderer.sharedMaterial != pin.SkinMaterials[expected])
                    issues.Add(slot.name + " has an unexpected skin.");
                if (pin.EnamelHeadRenderer && !heads.Add(pin.EnamelHeadRenderer)) issues.Add("A head renderer is shared by two sockets.");
                if (pin.transform.parent != slot.transform || pin.transform.localPosition.sqrMagnitude > .00000001f)
                    issues.Add(slot.name + " has an unseated pin.");
            }
            if (issues.Count > 0) { Debug.LogWarning("Pin population check:\n" + string.Join("\n", issues)); return false; }
            Debug.Log("PASS: twelve seated, independent pins with the twelve expected skins.");
            return true;
        }

        [MenuItem(MenuRoot + "Clear Test Pins")]
        static void ClearMenu() { ClearTestPins(); }

        [MenuItem(MenuRoot + "Clear Test Pins", true)]
        static bool CanClear() { return Application.isPlaying && testPins.Count > 0; }

        public static int ClearTestPins()
        {
            if (!Application.isPlaying) return 0;
            int cleared = 0;
            foreach (var entry in testPins)
            {
                // Never remove pre-existing occupants or pins moved elsewhere by gameplay.
                if (!entry.Key || !entry.Value || entry.Key.Occupant != entry.Value) continue;
                var pin = entry.Key.ReleasePin();
                pin.gameObject.SetActive(false);
                Object.Destroy(pin.gameObject);
                cleared++;
            }
            testPins.Clear();
            Debug.Log("Cleared " + cleared + " test pins; other occupants retained.");
            return cleared;
        }
    }
}
