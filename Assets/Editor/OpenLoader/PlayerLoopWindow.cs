using System.Collections.Generic;
using UnityEditor;
using UnityEngine.LowLevel;
using UnityEngine.UIElements;

namespace OpenUniverse.Editor.OpenLoader
{
    public class PlayerLoopWindow : EditorWindow
    {
        [MenuItem("OpenLoader/Player Loop")]
        private static void ShowWindow()
        {
            GetWindow<PlayerLoopWindow>(false, "Player Loop")?.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            rootVisualElement.Clear();
            rootVisualElement.Add(new Button(Refresh) {text = "Refresh"});
            var scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            ShowSystems(scrollView.contentContainer, loop.subSystemList, 0);
        }

        private static void ShowSystems(VisualElement root, IEnumerable<PlayerLoopSystem> systems, int indent)
        {
            foreach (var playerLoopSystem in systems)
            {
                if (playerLoopSystem.subSystemList != null)
                {
                    var foldout = new Foldout {text = playerLoopSystem.type.Name, style = {left = indent * 15}};
                    root.Add(foldout);
                    ShowSystems(foldout, playerLoopSystem.subSystemList, indent + 1);
                }
                else
                {
                    root.Add(new Label(playerLoopSystem.type.Name) {style = {left = indent * 15}});
                }
            }
        }
    }
}
