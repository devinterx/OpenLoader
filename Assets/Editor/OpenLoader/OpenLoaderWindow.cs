using System;
using UnityEditor;
using UnityEngine;

namespace OpenUniverse.Editor.OpenLoader
{
    internal class OpenLoaderWindow : EditorWindow
    {
        private static OpenLoaderWindow _currentWindow;

        private static Texture2D _icon;

        [MenuItem("OpenLoader/Settings", false, 0)]
        public static void ShowWindow()
        {
            if (_icon == null)
                _icon = EditorGUIUtility.Load(
                    $"Assets/Editor/OpenLoader/OpenLoaderIcon{(EditorGUIUtility.isProSkin ? "_d" : "")}.png"
                ) as Texture2D;

            if (_currentWindow == null)
            {
                var type = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor.dll");
                _currentWindow = GetWindow<OpenLoaderWindow>(type);
                _currentWindow.titleContent = new GUIContent
                {
                    text = "OpenLoader",
                    image = _icon,
                    tooltip = "OpenLoader Browser"
                };
                _currentWindow.Show();
                // _currentWindow.maximized = true;
                _currentWindow.Focus();
            }
            else
            {
                _currentWindow.Show();
                // _currentWindow.maximized = true;
                _currentWindow.Focus();
            }
        }

        private void OnEnable()
        {
            if (_icon != null || _currentWindow == null) return;

            _icon = EditorGUIUtility.Load(
                $"Assets/Editor/OpenLoader/OpenLoaderIcon{(EditorGUIUtility.isProSkin ? "_d" : "")}.png"
            ) as Texture2D;
            _currentWindow.titleContent = new GUIContent
            {
                text = "OpenLoader",
                image = _icon,
                tooltip = "OpenLoader Settings"
            };
        }

        private void OnGUI()
        {
        }
    }
}
