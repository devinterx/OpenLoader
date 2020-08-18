using System;
using System.IO;
using OpenUniverse.Runtime.OpenLoader.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace OpenUniverse.Runtime.OpenLoader
{
    [ExecuteAlways]
    public partial class OpenLoaderSystem
    {
        private const string EventSystemName = "EventSystem";
        private const string LoaderSystemName = "LoaderSystem";
        private const string MoreThenOneEventSystemException = "Found more then one EventSystem component.";
        private const string MoreThenOneLoaderSystemException = "Found more then one LoaderSystem component.";

        [NonSerialized]
        private static bool _isInitializedLoaderHook;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SetupHook()
        {
            if (!Application.isPlaying) return;

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (var i = 0; i < loop.subSystemList.Length; ++i)
            {
                if (loop.subSystemList[i].type == typeof(Initialization))
                    loop.subSystemList[i].updateDelegate += InitOpenLoader;
            }

            PlayerLoop.SetPlayerLoop(loop);
        }

        private static void InitOpenLoader()
        {
            if (_isInitializedLoaderHook || !Application.isPlaying) return;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.name.Contains(OpenLoaderSceneMagicWords)) return;

            try
            {
                var eventSystem = FindOrCreateEventSystem();
                var loaderSystem = FindOrCreateLoaderSystem();

                var loaderSystemConfig = loaderSystem != null ? loaderSystem.GetComponent<OpenLoaderSystem>() : null;
                if (loaderSystemConfig == null) return;

                if (loaderSystemConfig.EventSystem)
                    loaderSystemConfig.EventSystem = eventSystem.GetComponent<EventSystem>();

                GameObject loaderView = null;
                if (loaderSystemConfig.LoaderView == null)
                {
                    loaderView = OpenLoaderView.InstantiateView();

                    loaderSystemConfig.LoaderView = loaderView;
                    loaderSystemConfig._loaderView = loaderView.GetComponent<IOpenLoaderView>();
                }

                if (loaderView != null)
                {
                    DontDestroyOnLoad(loaderView);
                }

                DontDestroyOnLoad(loaderSystem);
                DontDestroyOnLoad(eventSystem);
            }
            catch (FileNotFoundException exception)
            {
                Abort(exception);
            }
            catch (LoaderException exception)
            {
                Abort(exception);
            }
            finally
            {
                _isInitializedLoaderHook = true;
            }
        }

        private static void Abort(Exception exception)
        {
            Debug.LogError(exception);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static GameObject FindOrCreateEventSystem()
        {
            GameObject eventSystem;

            var eventSystemsObjects = FindObjectsOfType<EventSystem>();

            switch (eventSystemsObjects.Length)
            {
                case 0:
                    eventSystem = CreateNewEventSystem();
                    break;
                case 1:
                    eventSystem = eventSystemsObjects[0].gameObject;
                    break;
                default:
                    throw new LoaderException(MoreThenOneEventSystemException);
            }

            return eventSystem;
        }

        private static GameObject FindOrCreateLoaderSystem()
        {
            GameObject loaderSystem;

            var eventSystemsObjects = FindObjectsOfType<OpenLoaderSystem>();

            switch (eventSystemsObjects.Length)
            {
                case 0:
                    loaderSystem = CreateNewLoaderSystem();
                    break;
                case 1:
                    loaderSystem = eventSystemsObjects[0].gameObject;
                    break;
                default:
                    throw new LoaderException(MoreThenOneLoaderSystemException);
            }

            return loaderSystem;
        }

        private static GameObject CreateNewEventSystem()
        {
            var eventSystem = new GameObject(EventSystemName);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            return eventSystem;
        }

        private static GameObject CreateNewLoaderSystem()
        {
            var loaderSystem = new GameObject(LoaderSystemName);
            loaderSystem.AddComponent<OpenLoaderSystem>();
            return loaderSystem;
        }
    }

    public class LoaderException : Exception
    {
        public LoaderException(string message) : base(message)
        {
        }
    }
}
