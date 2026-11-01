using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    [CustomEditor(typeof(PinSkin)), CanEditMultipleObjects]
    public sealed class PinSkinEditor : UnityEditor.Editor
    {
        bool showMaterials;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var index = serializedObject.FindProperty("skinIndex");
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = index.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Skin", index.intValue, PinSkin.SkinNames);
            if (EditorGUI.EndChangeCheck()) index.intValue = selected;
            EditorGUI.showMixedValue = false;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enamelHeadRenderer"), new GUIContent("Enamel head"));

            showMaterials = EditorGUILayout.Foldout(showMaterials, "Skin materials", true);
            if (showMaterials)
            {
                var materials = serializedObject.FindProperty("skinMaterials");
                if (materials.arraySize != PinSkin.SkinCount) materials.arraySize = PinSkin.SkinCount;
                for (int i = 0; i < PinSkin.SkinCount; i++)
                    EditorGUILayout.PropertyField(materials.GetArrayElementAtIndex(i), new GUIContent(PinSkin.SkinNames[i]));
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (PinSkin pin in targets)
                    if (pin.EnamelHeadRenderer) Undo.RecordObject(pin.EnamelHeadRenderer, "Change pin skin");
                serializedObject.ApplyModifiedProperties();
                foreach (PinSkin pin in targets)
                {
                    pin.SetSkin(pin.SkinIndex);
                    EditorUtility.SetDirty(pin);
                    if (pin.EnamelHeadRenderer)
                    {
                        EditorUtility.SetDirty(pin.EnamelHeadRenderer);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(pin.EnamelHeadRenderer);
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pin);
                    if (!Application.isPlaying && pin.gameObject.scene.IsValid() && pin.gameObject.scene.isLoaded)
                        EditorSceneManager.MarkSceneDirty(pin.gameObject.scene);
                }
            }
            else serializedObject.ApplyModifiedProperties();
        }
    }
}
