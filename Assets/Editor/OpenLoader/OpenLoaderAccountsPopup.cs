using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OpenUniverse.Editor.OpenLoader
{
    public class OpenLoaderAccountsPopup : PopupWindowContent
    {
        private readonly OpenLoaderWindow _view;
        private readonly List<OpenLoaderAccount> _savedAccounts;
        private readonly OpenLoaderAccount _currentAccount;
        private readonly int _accountsLimit;

        private readonly float _width;
        private float _height = 18f;

        public OpenLoaderAccountsPopup(OpenLoaderWindow view, float width, List<OpenLoaderAccount> savedAccounts,
            OpenLoaderAccount currentAccount, int accountsLimit)
        {
            _width = width;
            _view = view;
            _savedAccounts = savedAccounts;
            _currentAccount = currentAccount;
            _accountsLimit = accountsLimit;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width, _height);
        }

        public override void OnGUI(Rect rect)
        {
            if (_view == null) return;

            Draw(rect.width);

            if (Event.current.type == EventType.MouseMove) Event.current.Use();
            if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.Escape) return;
            editorWindow.Close();
            GUIUtility.ExitGUI();
        }

        private void Draw(float listElementWidth)
        {
            var rect = new Rect(0.0f, 2.0f, listElementWidth, 18f);

            DrawHeader(ref rect, new GUIContent {text = $" Saved accounts ({_savedAccounts.Count}/{_accountsLimit})"});


            foreach (var account in _savedAccounts)
            {
                DrawMenuAccountItem(ref rect, account);
            }

            DrawSeparator(ref rect);
            DrawHeader(ref rect, new GUIContent {text = " Want to add new account or edit existing?"});

            DrawMenuManageButton(ref rect, new GUIContent {text = "Manage accounts"});

            if (Math.Abs(_height - rect.y + 2) > 0.1) _height = rect.y + 2;
        }

        private void DrawHeader(ref Rect rect, GUIContent label)
        {
            var position = rect;
            position.y += 3f;
            position.x += 5f;
            position.width = EditorStyles.miniLabel.CalcSize(label).x;
            position.height = EditorStyles.miniLabel.CalcSize(label).y;

            GUI.Label(position, label, EditorStyles.miniLabel);
            rect.y += 20f;
        }

        private void DrawSeparator(ref Rect rect)
        {
            var position = rect;
            position.x += 5f;
            position.y += 5f;
            position.width -= 10f;
            position.height = 3f;
            GUI.Label(position, GUIContent.none, "sv_iconselector_sep");
            rect.y += 5f;
        }

        private void DrawMenuAccountItem(ref Rect rect, OpenLoaderAccount account)
        {
            var position = rect;
            position.x += 2f;

            var isActive = _currentAccount != null
                           && _currentAccount.host == account.host
                           && _currentAccount.login == account.login;
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(position, isActive, new GUIContent {text = $"{account.login}@{account.host}"}, "MenuItem");

            if (EditorGUI.EndChangeCheck() && !isActive)
            {
                _view.CurrentAccount = account;

                editorWindow.Close();
                GUIUtility.ExitGUI();
                return;
            }

            rect.y += 18f;
        }


        private void DrawMenuManageButton(ref Rect rect, GUIContent label)
        {
            var position = rect;
            position.x += 2f;

            EditorGUI.BeginChangeCheck();
            if (GUI.Toggle(position, false, label, "MenuItem"))
            {
                _view.OpenSettings(OpenLoaderSettingsMode.Account);

                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            rect.y += 18f;
        }
    }
}
