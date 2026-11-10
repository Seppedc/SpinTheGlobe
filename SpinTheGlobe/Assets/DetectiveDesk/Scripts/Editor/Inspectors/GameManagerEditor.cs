using DetectiveDesk.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    [CustomEditor(typeof(GameManager))]
    public sealed class GameManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene presentation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Layouts live under Scene presentation in the hierarchy. Move and style them there. Preview buttons show menus without running the game or loading your save.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                var game = (GameManager)target;
                if (GUILayout.Button("Create missing scene layouts")) ScenePresentationAuthoring.EnsureLayout(game);
                using (new EditorGUILayout.HorizontalScope())
                    foreach (var panel in new[] { "Desk", "Skill Tree", "Settings", "Gold Shop" })
                        if (GUILayout.Button(panel)) ScenePresentationAuthoring.Preview(game, panel);
            }
        }
    }
}
