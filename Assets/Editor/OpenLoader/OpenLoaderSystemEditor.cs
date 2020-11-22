using OpenUniverse.Runtime.OpenLoader;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUniverse.Editor.OpenLoader
{
    [CustomEditor(typeof(OpenLoaderSystem)), CanEditMultipleObjects]
    public class OpenLoaderSystemEditor : UnityEditor.Editor
    {
        private SerializedProperty _loaderView;
        private SerializedProperty _eventSystem;
        private SerializedProperty _debug;
        private SerializedProperty _useAssetDatabase;

        private void OnEnable()
        {
            _loaderView = serializedObject.FindProperty("loaderView");
            _eventSystem = serializedObject.FindProperty("eventSystem");
            _debug = serializedObject.FindProperty("debug");
            _useAssetDatabase = serializedObject.FindProperty("useAssetDatabase");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.ObjectField(_loaderView, typeof(GameObject), new GUIContent("Loader View"));
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
