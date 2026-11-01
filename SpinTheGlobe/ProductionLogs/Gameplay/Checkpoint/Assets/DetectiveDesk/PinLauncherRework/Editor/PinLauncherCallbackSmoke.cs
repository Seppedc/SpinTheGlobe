using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Opt-in review helper. Compiled in Edit Mode; survives the Play Mode domain reload.
[InitializeOnLoad]
internal static class PinLauncherCallbackSmoke
{
    private const string Request = "PinLauncherRework.CallbackSmoke";
    static PinLauncherCallbackSmoke() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (!SessionState.GetBool(Request, false) || !EditorApplication.isPlaying ||
            EditorApplication.isCompiling || Time.frameCount < 3) return;
        SessionState.SetBool(Request, false);
        try
        {
            var root = GameObject.Find("DetectiveOffice/GameplayObjects/ToyPinLauncher - aimed up at map");
            var interact = root.GetComponent("DeskInteractable");
            if (!interact) throw new Exception("Missing interactable");
            var data = new SerializedObject(interact);
            var action = data.FindProperty("action");
            var game = data.FindProperty("game").objectReferenceValue as Component;
            if (action.enumDisplayNames[action.enumValueIndex] != "Launcher" || !game)
                throw new Exception("Launcher binding missing");
            var hint = new SerializedObject(game).FindProperty("hintText").objectReferenceValue;
            game.SendMessage("FirePin", SendMessageOptions.RequireReceiver);
            var text = new SerializedObject(hint).FindProperty("m_Text").stringValue;
            if (!text.StartsWith("Toy launcher activated")) throw new Exception("Unexpected hint");
            File.WriteAllText("ProductionLogs/PinLauncherRework/runtime-callback.json",
                "{\"status\":\"PASS\",\"play_mode\":true,\"launcher_action_binding\":true,\"FirePin_invoked\":true,\"expected_hint_verified\":true,\"mouse_click_tested\":false}");
        }
        catch (Exception ex)
        {
            File.WriteAllText("ProductionLogs/PinLauncherRework/runtime-callback.json",
                "{\"status\":\"FAIL\",\"reason\":\""+ex.Message.Replace("\"","'")+"\"}");
        }
        finally { EditorApplication.isPlaying = false; }
    }
}
