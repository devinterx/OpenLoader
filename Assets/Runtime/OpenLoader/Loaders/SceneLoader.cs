using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;
using UnityEngine;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    public abstract partial class BaseOpenLoader : IOpenSceneLoader
    {
        private const string OpenLoaderScene = "OpenLoader";
        private const string OpenLoaderTag = "__openLoader";

        private const string SceneLoadException = "Failed to load scene {1} because asset bundle {0} it was not loaded";

        private const string NoStreamedSceneException =
            "Failed to load scene {1} because asset bundle {0} not have streamed scenes";

        public delegate void AdditiveSceneEventHandler(string sceneName, Scene scene);

        protected event AdditiveSceneEventHandler OnSceneLoadedInvokable;
        protected event AdditiveSceneEventHandler OnSceneUnLoadedInvokable;

        public event AdditiveSceneEventHandler OnSceneLoaded
        {
            add
            {
                if (OnSceneLoadedInvokable == null || !OnSceneLoadedInvokable.GetInvocationList().Contains(value))
                {
                    OnSceneLoadedInvokable += value;
                }
            }
            remove => OnSceneLoadedInvokable -= value;
        }

        public event AdditiveSceneEventHandler OnSceneUnLoaded
        {
            add
            {
                if (OnSceneUnLoadedInvokable == null || !OnSceneUnLoadedInvokable.GetInvocationList().Contains(value))
                {
                    OnSceneUnLoadedInvokable += value;
                }
            }
            remove => OnSceneUnLoadedInvokable -= value;
        }

        private readonly IDictionary<string, Scene> _loadedScenes = new Dictionary<string, Scene>(0);

        protected void SubscribeEvents()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedCallback;
            SceneManager.sceneUnloaded -= OnSceneUnLoadedCallback;
            SceneManager.sceneLoaded += OnSceneLoadedCallback;
            SceneManager.sceneUnloaded += OnSceneUnLoadedCallback;
        }

        protected void UnSubscribeEvents()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedCallback;
            SceneManager.sceneUnloaded -= OnSceneUnLoadedCallback;
        }

        [UsedImplicitly]
        public bool IsSceneLoaded(string sceneName)
        {
            return _loadedScenes.ContainsKey(sceneName);
        }

        [UsedImplicitly]
        public void LoadScene(string sceneName, Action callback = null)
        {
            if (sceneName == OpenLoaderScene) return;

            if (!_loadedScenes.ContainsKey(sceneName)) StartCoroutine(LoadSceneAsync(sceneName, callback));
        }

        [UsedImplicitly]
        public void LoadScene(Uri assetBundleUrl, string sceneName, Action callback = null,
            bool autoUnloadAssetBundle = false)
        {
            if (_loadedScenes.ContainsKey(sceneName)) return;
#if UNITY_EDITOR
            if (useAssetDatabase && assetBundleUrl.Scheme == "stream" || assetBundleUrl.Scheme == "file")
            {
                var localPath = assetBundleUrl.LocalPath;
                var scenePath = new List<string>(AssetDatabase.GetAssetPathsFromAssetBundle(
                    Path.GetFileNameWithoutExtension(localPath)
                )).Find(path => string.CompareOrdinal(sceneName, Path.GetFileNameWithoutExtension(path)) == 0);

                if (!string.IsNullOrEmpty(scenePath))
                {
                    LoadSceneFromAssetDatabase(scenePath, callback);
                    return;
                }
            }
#endif
            LoadAssetBundle(assetBundleUrl, OpenLoaderTag, (assetBundleKey, assetBundle) =>
            {
                if (assetBundle != null)
                {
                    LoadSceneFromAssetBundle(assetBundleUrl, assetBundle, sceneName, callback, autoUnloadAssetBundle);
                }
                else
                {
                    Debug.LogError(string.Format(SceneLoadException, assetBundleUrl, sceneName));

                    throw new SceneLoadException(string.Format(SceneLoadException, assetBundleUrl, sceneName));
                }
            });
        }

        [UsedImplicitly]
        public void UnLoadScene(string sceneName)
        {
            if (sceneName == OpenLoaderScene || !_loadedScenes.ContainsKey(sceneName)) return;

            SceneManager.UnloadSceneAsync(sceneName);
        }
#if UNITY_EDITOR
        private void LoadSceneFromAssetDatabase(string scenePath, Action callback)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            LoadScene(sceneAsset.name, () => { callback?.Invoke(); });
        }
#endif
        private void LoadSceneFromAssetBundle(Uri assetBundleUrl, AssetBundle assetBundle, string sceneName,
            Action callback = null, bool unloadAssetBundle = false)
        {
            if (!assetBundle.isStreamedSceneAssetBundle)
            {
                throw new SceneLoadException(string.Format(NoStreamedSceneException, sceneName, assetBundleUrl));
            }

            var assetBundleKey = md5(assetBundleUrl.ToString());
            var scenePaths = assetBundle.GetAllScenePaths();
            foreach (var scenePath in scenePaths)
            {
                if (!scenePath.Contains(sceneName)) continue;

                LoadScene(Path.GetFileNameWithoutExtension(scenePath), () =>
                {
                    if (unloadAssetBundle && assetBundleKey != "")
                        UnLoadAssetBundleByKey(assetBundleKey, OpenLoaderTag);

                    callback?.Invoke();
                });
                break;
            }
        }

        private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == OpenLoaderScene) return;

            if (debug) Debug.Log("Scene is loaded: " + scene.name);
            _loadedScenes.Add(scene.name, scene);
            OnSceneLoadedInvokable?.Invoke(scene.name, scene);
        }

        private void OnSceneUnLoadedCallback(Scene scene)
        {
            if (scene.name == OpenLoaderScene) return;

            if (debug) Debug.Log("Scene is unloaded: " + scene.name);
            OnSceneUnLoadedInvokable?.Invoke(scene.name, scene);
            _loadedScenes.Remove(scene.name);
        }

        private static IEnumerator LoadSceneAsync(string sceneName, Action callback = null)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncOperation.allowSceneActivation = true;

            while (!asyncOperation.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            callback?.Invoke();
        }
    }

    public class SceneLoadException : Exception
    {
        public SceneLoadException(string message) : base(message)
        {
        }
    }
}
