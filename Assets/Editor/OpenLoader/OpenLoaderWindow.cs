using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUniverse.Editor.OpenLoader
{
    public class OpenLoaderWindow : EditorWindow
    {
        private static OpenLoaderWindow _currentWindow;
        private static OpenLoaderSettings _settings;

        private readonly IDictionary<OpenLoaderSettingsMode, string> _settingsModes =
            new Dictionary<OpenLoaderSettingsMode, string>
            {
                {OpenLoaderSettingsMode.Account, "Manage accounts"},
                {OpenLoaderSettingsMode.Builder, "Builder"},
                {OpenLoaderSettingsMode.Others, "Others"}
            };

        private OpenLoaderSettingsMode _settingsMode = OpenLoaderSettingsMode.None;

        private static Texture2D _icon;

        private static Texture _connectionIcon;
        private static Texture _disConnectIcon;
        private static Texture _settingsIcon;

        private bool _connectionToggleState;
        private bool _settingsToggleState;

        private Vector2 _bodyScroll = Vector2.zero;

        private Object _scene;

        public OpenLoaderAccount CurrentAccount { get; set; }

        private ReorderableList _reorderableList;

        private List<OpenLoaderAccount> _accounts = null;

        #region Menu

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

                if (_settings == null)
                {
                    _settings = OpenLoaderSettings.Load();
                    _currentWindow.CurrentAccount = _settings.SavedAccounts.Count > 0
                        ? _settings.SavedAccounts[0]
                        : null;
                }

                _currentWindow.Show();
                // _currentWindow.maximized = true;
            }
            else
            {
                if (_settings == null)
                {
                    _settings = OpenLoaderSettings.Load();
                    _currentWindow.CurrentAccount = _settings.SavedAccounts.Count > 0
                        ? _settings.SavedAccounts[0]
                        : null;
                }

                _currentWindow.Show();
                // _currentWindow.maximized = true;
            }
        }

        [MenuItem("OpenLoader/Settings", false, 0)]
        public static void OpenLoaderWindowShow()
        {
            ShowWindow();
            //_currentWindow.Focus();
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

        [MenuItem("OpenLoader/GameObject/Show WireFrame %w")]
        private static void ShowWireFrame()
        {
            foreach (var selected in Selection.gameObjects)
            {
                var renderers = selected.GetComponentsInChildren<Renderer>();
                if (renderers == null) return;

                foreach (var renderer in renderers)
                {
                    EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Hidden);
                }
            }
        }

        [MenuItem("OpenLoader/GameObject/Show WireFrame %w", true)]
        private static bool ShowWireFrameValidate()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("OpenLoader/GameObject/Hide WireFrame %h")]
        private static void HideWireFrame()
        {
            foreach (var selected in Selection.gameObjects)
            {
                var renderers = selected.GetComponentsInChildren<Renderer>();
                if (renderers == null) return;

                foreach (var renderer in renderers)
                {
                    EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Wireframe);
                }
            }
        }

        [MenuItem("OpenLoader/GameObject/Hide WireFrame %h", true)]
        private static bool HideWireFrameValidate()
        {
            return Selection.activeGameObject != null;
        }

        #endregion

        #region API

        public void OpenSettings(OpenLoaderSettingsMode mode = OpenLoaderSettingsMode.None)
        {
            _settingsToggleState = true;
            _settingsMode = mode;
        }

        // private static T[] GetAllInstances<T>() where T : ScriptableObject
        // {
        //     var assets = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        //     var resultAssets = new T[assets.Length];
        //     for (var i = 0; i < assets.Length; i++)
        //     {
        //         var path = AssetDatabase.GUIDToAssetPath(assets[i]);
        //         resultAssets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        //     }
        //
        //     return resultAssets;
        // }

        #endregion

        #region Render

        private void OnEnable()
        {
            if (_currentWindow == null)
            {
                var type = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor.dll");
                _currentWindow = GetWindow<OpenLoaderWindow>(type);
            }

            if (_settings == null)
            {
                _settings = OpenLoaderSettings.Load();
                _currentWindow.CurrentAccount = _settings.SavedAccounts.Count > 0 ? _settings.SavedAccounts[0] : null;

                _accounts = _settings.savedAccounts;
            }

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
            if (_settings == null)
            {
                _settings = OpenLoaderSettings.Load();
                CurrentAccount = _settings.SavedAccounts.Count > 0 ? _settings.SavedAccounts[0] : null;
            }
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

            LoadTextures();
            OnGuiTopToolbar();

            if (_settingsToggleState)
            {
                OnGuiBodySettings();
            }
            else
            {
                OnGuiBody();
            }

            OnGuiBottomStatusBar();
        }

        private void OnGuiTopToolbar()
        {
            var toolbarStyle = new GUIStyle(EditorStyles.toolbar)
            {
                padding = new RectOffset(0, 0, 0, 0),
                contentOffset = new Vector2(0, 0),
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            GUILayout.BeginHorizontal(toolbarStyle);
            OnGuiToolbarServers();
            GUILayout.EndHorizontal();
        }

        private void OnGuiToolbarServers()
        {
            // Server label
            var labelContent = new GUIContent {text = " Server"};
            var labelStyle = new GUIStyle(EditorStyles.label) {margin = new RectOffset(1, 0, 3, 0)};
            var labelRect = GUILayoutUtility.GetRect(labelContent, labelStyle, GUILayout.Width(48f));
            EditorGUI.LabelField(labelRect, labelContent, labelStyle);
            var accountContent = new GUIContent
                {text = CurrentAccount == null ? "None" : $"{CurrentAccount.login}@{CurrentAccount.host}"};

            // Account dropdown list
            const float toolbarDropDownWidth = 240f;
            var accountRect = GUILayoutUtility.GetRect(
                accountContent,
                EditorStyles.toolbarDropDown,
                GUILayout.Width(toolbarDropDownWidth)
            );
            var accountToolbarStyle = new GUIStyle(EditorStyles.toolbarDropDown)
            {
                padding = new RectOffset(5, 5, 0, 0)
            };

            if (EditorGUI.DropdownButton(accountRect, accountContent, FocusType.Passive, accountToolbarStyle))
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect = new Rect(rect.x + 48, rect.y + 20, rect.width, rect.height);
                // PopupWindow.Show(rect, new SceneRenderModeWindow(this));
                PopupWindow.Show(
                    rect,
                    new OpenLoaderAccountsPopup(
                        this,
                        toolbarDropDownWidth,
                        _settings.SavedAccounts,
                        CurrentAccount,
                        _settings.SavedAccountsLimit
                    )
                );
                GUIUtility.ExitGUI();
            }

            // Free space
            EditorGUILayout.Space();

            // Connection button
            if (CurrentAccount != null)
            {
                var connectionText = _connectionToggleState ? "Disconnect" : "Connect";
                var connectionToggleContent = new GUIContent {text = connectionText, image = _connectionIcon};
                var connectionStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedWidth = 38f + connectionText.Length * 6f,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(5, 5, 0, 0)
                };
                _connectionToggleState = GUILayout.Toggle(
                    _connectionToggleState,
                    connectionToggleContent,
                    connectionStyle
                );
            }

            // Settings button
            var settingsToggleContent = new GUIContent {image = _settingsIcon};
            var settingsStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedWidth = 24f,
                margin = new RectOffset(5, 3, 0, 0)
            };
            _settingsToggleState = GUILayout.Toggle(_settingsToggleState, settingsToggleContent, settingsStyle);
        }

        private void OnGuiBody()
        {
            GUILayout.BeginVertical();
            {
                var bodyStyle = new GUIStyle();
                bodyStyle.padding = new RectOffset(10, 10, 10, 10);

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
                    // _scene = EditorGUILayout.ObjectField(
                    //     "OpenLoader Scene",
                    //     _scene,
                    //     typeof(SceneAsset),
                    //     false
                    // ) as SceneAsset;
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

        private void OnGuiBodySettings()
        {
            GUILayout.BeginVertical();
            {
                var bodyStyle = new GUIStyle
                {
                    margin = new RectOffset(4, 4, 4, 4),
                    padding = new RectOffset(5, 5, 5, 5)
                };

                _bodyScroll = GUILayout.BeginScrollView(_bodyScroll, bodyStyle);

                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        foreach (var mode in _settingsModes)
                        {
                            var style = new GUIStyle(EditorStyles.helpBox) {padding = {left = 4}};
                            EditorGUILayout.BeginVertical(style);
                            var foldoutStyle = new GUIStyle("Foldout")
                            {
                                padding = new RectOffset(16, 5, 3, 3),
                                fontStyle = FontStyle.Normal,
                            };
                            foldoutStyle.onNormal = foldoutStyle.normal = new GUIStyleState
                            {
                                textColor = EditorGUIUtility.isProSkin
                                    ? new Color(0.8f, 0.8f, 0.8f)
                                    : new Color(0, 0, 0)
                            };
                            foldoutStyle.onFocused = foldoutStyle.onActive = foldoutStyle.onHover =
                                foldoutStyle.focused = foldoutStyle.active = foldoutStyle.hover = new GUIStyleState
                                {
                                    textColor = EditorGUIUtility.isProSkin
                                        ? new Color(0.8f, 0.8f, 0.8f)
                                        : new Color(0, 0, 0)
                                };

                            if (EditorGUILayout.BeginFoldoutHeaderGroup(_settingsMode == mode.Key, new GUIContent
                            {
                                text = mode.Value
                            }, foldoutStyle))
                            {
                                _settingsMode = mode.Key;
                            }

                            if (_settingsMode != OpenLoaderSettingsMode.None && _settingsMode == mode.Key)
                            {
                                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                                switch (_settingsMode)
                                {
                                    case OpenLoaderSettingsMode.Account:
                                        OnGuiSettingsAccount();
                                        break;
                                    case OpenLoaderSettingsMode.Builder:
                                        OnGuiSettingsBuilder();
                                        break;
                                    case OpenLoaderSettingsMode.Others:
                                        OnGuiSettingsOthers();
                                        break;
                                }
                            }

                            EditorGUILayout.EndFoldoutHeaderGroup();
                            EditorGUILayout.EndVertical();
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.EndScrollView();

                DrawOutline(GUILayoutUtility.GetLastRect(), 1f);
            }

            GUILayout.EndVertical();
        }

        private void OnGuiSettingsAccount()
        {
            var style = new GUIStyle
            {
                padding = new RectOffset(5, 5, 5, 5)
            };

            EditorGUILayout.BeginVertical(style);

            // TODO:: accounts
            if (_reorderableList == null)
            {
                _reorderableList = new ReorderableList(_accounts, typeof(OpenLoaderAccount))
                {
                    drawHeaderCallback = DrawHeader,
                    elementHeightCallback = ElementHeight,
                    drawElementCallback = DrawElement,
                    drawElementBackgroundCallback = DrawElementBackground,
                    onAddCallback = OnAdd,
                    onRemoveCallback = OnRemove
                };
            }

            _reorderableList?.DoLayoutList();

            EditorGUILayout.EndVertical();
        }

        private void OnAdd(ReorderableList list)
        {
            //Undo.RecordObject(todoList, "Task Added");
            //todoList.list.Add(new ToDoElement());
        }

        private void OnRemove(ReorderableList list)
        {
            //Undo.RecordObject(todoList, "Task Removed");
            //todoList.list.RemoveAt(list.index);
        }

        private float ElementHeight(int index)
        {
            // ToDoElement element = todoList.list[index];
            //
            // float width = GetWidth();
            // if (style_textArea == null) {
            //     style_textArea = new GUIStyle(GUI.skin.label);
            //     style_textArea.alignment = TextAnchor.UpperLeft;
            //     style_textArea.wordWrap = true;
            // }
            // if (element.editing) {
            //     style_textArea.richText = false;
            // }
            //
            // // Height
            // float height = style_textArea.CalcHeight(new GUIContent(element.text), width) + 5;
            // style_textArea.richText = true;

            // if (element.objectReference != null) {
            //     height += EditorGUIUtility.singleLineHeight + 4;
            // }
            // height = Mathf.Max(EditorGUIUtility.singleLineHeight + 4, height);
            // return height;

            return EditorGUIUtility.singleLineHeight;
        }

        private void DrawHeader(Rect rect)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = {textColor = Color.grey}
            };

            EditorGUI.LabelField(rect, "2 / 10", style);

            var textColor = style.normal.textColor;

            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true,
                normal = {textColor = textColor},
                focused = {textColor = textColor},
                hover = {textColor = textColor}
            };

            EditorGUI.TextField(rect, "Saved accounts", style);

            rect = new Rect(rect.x, rect.y, rect.width, rect.height + 20);
        }

        private void DrawElementBackground(Rect rect, int index, bool active, bool focus)
        {
            if (index < 0) return;

            var elementRect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 1);

            if (focus || active) EditorGUI.DrawRect(elementRect, new Color(0.12f, 0.12f, 0.12f, 0.5f));
        }

        private void DrawElement(Rect rect, int index, bool active, bool focus)
        {
            var element = _accounts[index];

            // Toggle
            // var h = EditorGUIUtility.singleLineHeight;

            // This prevents text area from highlighting all text on focus
            var preventSelection = Event.current.type == EventType.MouseDown;
            var cursorColor = GUI.skin.settings.cursorColor;
            if (preventSelection)
            {
                GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
            }

            // // Text Colours
            // if (completed) {
            //     style_textArea.normal.textColor = completedTextColor;
            //     style_textArea.focused.textColor = completedTextColor;
            //     style_textArea.hover.textColor = completedTextColor;
            // } else {
            //     style_textArea.normal.textColor = textColor;
            //     style_textArea.focused.textColor = textColor;
            //     style_textArea.hover.textColor = textColor;
            // }

            // // If editing, turn off richText
            // if (element.editing) {
            //     style_textArea.richText = false;
            // }

            // Text Area
            // float x = h + 5;
            // float textHeight = rect.height;
            // if (element.objectReference) {
            //     textHeight -= 25;
            // }
            // EditorGUI.BeginChangeCheck();
            // GUI.SetNextControlName("TextArea");
            // string text = EditorGUI.TextArea(
            //     new Rect(rect.x + x, rect.y + 2, rect.width - x, textHeight),
            //     element.text, style_textArea);
            //
            // element.editing = (GUI.GetNameOfFocusedControl() == "TextArea");
            // style_textArea.richText = true;
            //
            // if (EditorGUI.EndChangeCheck()) {
            //     Undo.RecordObject(todoList, "Edited Task Text");
            //     element.text = text;
            // }

            // Reset Cursor Color
            if (preventSelection)
            {
                GUI.skin.settings.cursorColor = cursorColor;
            }

            // // Object Field
            // if (element.objectReference) {
            //     EditorGUI.BeginChangeCheck();
            //     EditorGUI.LabelField(
            //         new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - 27, h),
            //         "Linked Object : ",
            //         style_textArea);
            //     x += EditorGUIUtility.labelWidth;
            //     Object obj = EditorGUI.ObjectField(
            //         new Rect(rect.x + x, rect.y + rect.height + 5 - 25, rect.width - x, h),
            //         element.objectReference,
            //         typeof(Object), true);
            //     if (EditorGUI.EndChangeCheck()) {
            //         Undo.RecordObject(todoList, "Changed Task Object");
            //         element.objectReference = obj;
            //     }
            // }

            // Handle Drag & Drop Object onto Element
            var currentEvent = Event.current;
            if (rect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        var obj = DragAndDrop.objectReferences[0];
                        // Undo.RecordObject(todoList, "Changed Task Object");
                        // element.objectReference = obj;
                    }

                    currentEvent.Use();
                }
            }
        }

        private void OnGuiSettingsBuilder()
        {
            var style = new GUIStyle
            {
                padding = new RectOffset(5, 5, 5, 5)
            };

            EditorGUILayout.BeginVertical(style);

            _scene = EditorGUILayout.ObjectField(
                "OpenLoader Scene",
                _scene,
                typeof(SceneAsset),
                false
            ) as SceneAsset;

            EditorGUILayout.EndVertical();
        }

        private void OnGuiSettingsOthers()
        {
            var style = new GUIStyle
            {
                padding = new RectOffset(5, 5, 5, 5)
            };

            EditorGUILayout.BeginVertical(style);


            EditorGUILayout.EndVertical();
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

        private static void DrawOutline(Rect rect, float size)
        {
            var color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if (EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            var orgColor = GUI.color;
            GUI.color *= color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size),
                EditorGUIUtility.whiteTexture);

            GUI.color = orgColor;
        }

        #endregion
    }
}
