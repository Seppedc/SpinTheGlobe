using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Connects authored controls to the current game's actions after scene loading.</summary>
    public static class SceneViewBindings
    {
        public static void Bind(Button button, UnityAction action)
        {
            if (!button) throw new MissingReferenceException("An authored UI button is missing.");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public static void Button(Transform root, string path, UnityAction action)
        {
            var target = root ? root.Find(path) : null;
            if (!target) throw new MissingReferenceException("Authored UI button missing: " + path);
            Bind(target.GetComponent<Button>(), action);
        }
    }
}
