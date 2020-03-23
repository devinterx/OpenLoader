using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUniverse.Editor.OpenLoader
{
    [CustomEditor(typeof(Runtime.OpenLoader.OpenLoader)), CanEditMultipleObjects]
    public class OpenLoaderEditor : UnityEditor.Editor
    {
        private SerializedProperty _eventSystem;
        private SerializedProperty _debug;
        private SerializedProperty _useAssetDatabase;

        private void OnEnable()
        {
            _eventSystem = serializedObject.FindProperty("eventSystem");
            _debug = serializedObject.FindProperty("debug");
            _useAssetDatabase = serializedObject.FindProperty("useAssetDatabase");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.ObjectField(_eventSystem, typeof(EventSystem), new GUIContent("Event System"));
            EditorGUILayout.PropertyField(_useAssetDatabase, new GUIContent("Use Asset Database"));
            EditorGUILayout.PropertyField(_debug, new GUIContent("Debug Messages"));

            GUILayout.Space(10);

            if (GUILayout.Button("OpenLoader Settings"))
            {
                OpenLoaderWindow.ShowWindow();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
