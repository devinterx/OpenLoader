using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUniverse.Editor.OpenLoader
{
    internal class OpenLoaderWindow : EditorWindow
    {
        private static OpenLoaderWindow _currentWindow;

        private static Texture2D _icon;

        private static Texture _connectionIcon;
        private static Texture _disConnectIcon;
        private static Texture _settingsIcon;

        private bool _connectionToggleState;
        private bool _settingsToggleState;

        private bool _firstEnterAfterFocus;

        private Vector2 _bodyScroll = Vector2.zero;

        private Object _scene;

        private Object _settings;

        private string _currentServer = "anonymous@localhost";

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
                    image = _icon
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

        [MenuItem("OpenLoader/IMGUI Debugger")]
        public static void GuiViewDebuggerWindow()
        {
            var type = Type.GetType("UnityEditor.GUIViewDebuggerWindow,UnityEditor");
            GetWindow(type).Show();
        }

        [MenuItem("OpenLoader/TreeViewTestWindow")]
        public static void TreeViewTestWindow()
        {
            var type = Type.GetType("UnityEditor.TreeViewExamples.TreeViewTestWindow,UnityEditor");
            GetWindow(type).Show();
        }

        [MenuItem("OpenLoader/HackUnityHiddenWindow")]
        private static void HackUnity()
        {
            var windows = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.IsSubclassOf(typeof(EditorWindow)))
                .ToArray();

            var windowNames = windows.Select(c => c.FullName).ToArray();

            Debug.Log(string.Join("\n", windowNames));
        }
        
        [MenuItem("OpenLoader/HackUnity2")]
        private static void HackUnity2()
        {
            //var settings = GetAllInstances<OpenLoaderSettings>();

            //Debug.Log(settings);
        }
        
        

        private static T[] GetAllInstances<T>() where T : ScriptableObject
        {
            var assets = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var resultAssets = new T[assets.Length];
            for (var i = 0; i < assets.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assets[i]);
                resultAssets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return resultAssets;
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
                image = _icon
            };
        }

        private void OnFocus()
        {
            _firstEnterAfterFocus = true;
        }

        private static void LoadTextures()
        {
            if (_connectionIcon != null
                && _disConnectIcon != null
                && _settingsIcon != null
            ) return;
            const string packagePath = "Assets/Editor/OpenLoader/";

            var fullPackagePath = System.IO.Path.GetFullPath(packagePath);
            if (!System.IO.Directory.Exists(fullPackagePath)) return;
            _connectionIcon = (Texture2D) AssetDatabase.LoadAssetAtPath(
                $"{packagePath}OpenLoader_link{(EditorGUIUtility.isProSkin ? "_d" : "")}.png", typeof(Texture2D));
            _disConnectIcon = (Texture2D) AssetDatabase.LoadAssetAtPath(
                $"{packagePath}OpenLoader_unlink{(EditorGUIUtility.isProSkin ? "_d" : "")}.png", typeof(Texture2D));
            _settingsIcon = (Texture2D) AssetDatabase.LoadAssetAtPath(
                $"{packagePath}OpenLoader_settings{(EditorGUIUtility.isProSkin ? "_d" : "")}.png", typeof(Texture2D));
        }

        private void OnGUI()
        {
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
            if (_firstEnterAfterFocus)
            {
                GUIUtility.keyboardControl = 0;
                _firstEnterAfterFocus = false;
            }

            LoadTextures();
            OnGuiTopToolbar();
            OnGuiBody();
            OnGuiBottomStatusBar();
        }

        private void OnGuiTopToolbar()
        {
            var toolbarStyle = new GUIStyle(EditorStyles.toolbar) {padding = new RectOffset(0, 0, 0, 0)};
            GUILayout.BeginHorizontal(toolbarStyle);
            OnGuiToolbarServers();
            GUILayout.EndHorizontal();
        }

        private void OnGuiToolbarServers()
        {
            var labelContent = new GUIContent {text = " Server"};
            var labelStyle = new GUIStyle(EditorStyles.label) {margin = new RectOffset(1, 0, 3, 0)};
            var labelRect = GUILayoutUtility.GetRect(labelContent, labelStyle, GUILayout.Width(48f));
            EditorGUI.LabelField(labelRect, labelContent, labelStyle);
            var modeContent = new GUIContent {text = $" {_currentServer}"};

            var modeRect = GUILayoutUtility.GetRect(modeContent, EditorStyles.toolbarDropDown, GUILayout.Width(240f));
            if (EditorGUI.DropdownButton(modeRect, modeContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                var rect = GUILayoutUtility.GetLastRect();
                Debug.Log("DropdownButton");
                //PopupWindow.Show(rect, new SceneRenderModeWindow(this));
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();
            var connectionText = _connectionToggleState ? "Disconnect" : "Connect";
            var connectionToggleContent = new GUIContent {text = connectionText, image = _connectionIcon};

            var connectionStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedWidth = 38f + connectionText.Length * 6f,
                padding = new RectOffset(0, 4, 0, 0)
            };

            _connectionToggleState = GUILayout.Toggle(_connectionToggleState, connectionToggleContent, connectionStyle);
            var settingsToggleContent = new GUIContent {image = _settingsIcon};
            var settingsStyle = new GUIStyle(EditorStyles.toolbarButton) {fixedWidth = 24f};
            _settingsToggleState = GUILayout.Toggle(_settingsToggleState, settingsToggleContent, settingsStyle);
        }

        private void OnGuiBody()
        {
            GUILayout.BeginVertical();
            {
                // var bodyStyle = new GUIStyle(GUI.skin.GetStyle("FrameBox"));
                var bodyStyle = new GUIStyle(GUI.skin.GetStyle("GroupBox"));
                bodyStyle.margin = new RectOffset(4, 4, 4, 4);
                bodyStyle.padding = new RectOffset(5, 5, 5, 5);

                _bodyScroll = GUILayout.BeginScrollView(_bodyScroll, bodyStyle);

                // var labelContent = new GUIContent {text = " OpenLoader Scene"};
                // var labelStyle = new GUIStyle(EditorStyles.label)
                // {
                //     fixedWidth = 180f,
                //     margin = new RectOffset(1, 0, 3, 0)
                // };
                // var labelRect = GUILayoutUtility.GetRect(labelContent, labelStyle, GUILayout.Width(48f));
                // EditorGUI.LabelField(labelRect, labelContent, labelStyle);

                GUILayout.BeginHorizontal();
                {
                    _scene =
                        EditorGUILayout.ObjectField("OpenLoader Scene", _scene, typeof(SceneAsset),
                            false) as SceneAsset;
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(true);
                    // _settings = EditorGUILayout.ObjectField("OpenLoader Settings", _settings,
                    //     typeof(OpenLoaderSettings), false);
                    EditorGUI.EndDisabledGroup();
                }

                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private void OnGuiBottomStatusBar()
        {
            var toolbarStyle = new GUIStyle(EditorStyles.toolbar);

            GUILayout.BeginHorizontal(toolbarStyle);
            var labelContent = new GUIContent {text = "Connecting..."};
            var labelStyle = new GUIStyle(EditorStyles.label) {fixedWidth = 200f};
            var labelRect = GUILayoutUtility.GetRect(labelContent, labelStyle, GUILayout.Width(48f));
            EditorGUI.LabelField(labelRect, labelContent, labelStyle);

            EditorGUILayout.Space();

            GUILayout.EndHorizontal();
        }
    }
}
