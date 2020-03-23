using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    public abstract partial class BaseOpenLoader : IOpenAssetBundleLoader
    {
        private CoroutineQueue _assetBundleLoaderQueue;

        private uint _assetBundleMaxConcurrentConnectionsCount = 3;

        public uint AssetBundleMaxConcurrentConnectionsCount
        {
            get => _assetBundleMaxConcurrentConnectionsCount;
            set
            {
                _assetBundleMaxConcurrentConnectionsCount = value <= 0 ? 1 : value;
                if (_assetBundleLoaderQueue != null)
                    _assetBundleLoaderQueue.MaxConcurrentCoroutinesCount = _assetBundleMaxConcurrentConnectionsCount;
            }
        }

        private struct TaggedAssetBundle
        {
            public AssetBundle AssetBundle;
            public Uri Url;
            public string[] Tags;
        }

        public delegate void AssetBundleEventHandler(string assetBundleKey, AssetBundle assetBundle);

        private readonly IDictionary<string, TaggedAssetBundle> _loadedAssetBundles =
            new Dictionary<string, TaggedAssetBundle>(0);

        private Coroutine _cleanAssetBundlesWithUnloadCoroutine;
        private Coroutine _cleanAssetBundlesWithoutUnloadCoroutine;
        private int _cleanAssetBundlesWithUnloadCoroutineCycles;
        private int _cleanAssetBundlesWithoutUnloadCoroutineCycles;

        public void LoadAssetBundle(Uri url, string assetTag, AssetBundleEventHandler callback)
        {
            LoadAssetBundle(url, new[] {assetTag}, callback);
        }

        public void LoadAssetBundle(Uri url, string[] assetTags, AssetBundleEventHandler callback)
        {
            var assetBundleKey = md5(url.ToString());

            if (_loadedAssetBundles.ContainsKey(assetBundleKey))
            {
                if (!assetTags.All(assetTag => _loadedAssetBundles[assetBundleKey].Tags.Contains(assetTag)))
                {
                    var taggedAssetBundle = _loadedAssetBundles[assetBundleKey];
                    taggedAssetBundle.Tags = taggedAssetBundle.Tags.Concat(assetTags).ToArray();
                    _loadedAssetBundles[assetBundleKey] = taggedAssetBundle;
                }

                callback.Invoke(assetBundleKey, _loadedAssetBundles[assetBundleKey].AssetBundle);
                return;
            }

            _assetBundleLoaderQueue?.Enqueue(AssetBundleLoader(url, assetTags, callback));
        }

        public void UnLoadAssetBundle(Uri url, string assetTag, bool unloadAllLoadedObjects = true)
        {
            UnLoadAssetBundle(url, new[] {assetTag}, unloadAllLoadedObjects);
        }

        public void UnLoadAssetBundleByKey(string assetBundleKey, string assetTag, bool unloadAllLoadedObjects = true)
        {
            UnLoadAssetBundleByKey(assetBundleKey, new[] {assetTag}, unloadAllLoadedObjects);
        }

        public void UnLoadAssetBundle(Uri url, string[] assetTags, bool unloadAllLoadedObjects = true)
        {
            var assetBundleKey = md5(url.ToString());

            UnLoadAssetBundleByKey(assetBundleKey, assetTags, unloadAllLoadedObjects);
        }

        public void UnLoadAssetBundleByKey(string assetBundleKey, string[] assetTags,
            bool unloadAllLoadedObjects = true)
        {
            if (!_loadedAssetBundles.ContainsKey(assetBundleKey)) return;

            if (assetTags.Contains("*"))
            {
                var taggedAssetBundle = _loadedAssetBundles[assetBundleKey];
                taggedAssetBundle.Tags = new string[] { };
                _loadedAssetBundles[assetBundleKey] = taggedAssetBundle;
            }
            else if (assetTags.Any(assetTag => _loadedAssetBundles[assetBundleKey].Tags.Contains(assetTag)))
            {
                var taggedAssetBundle = _loadedAssetBundles[assetBundleKey];
                taggedAssetBundle.Tags = taggedAssetBundle.Tags.Except(assetTags).ToArray();
                _loadedAssetBundles[assetBundleKey] = taggedAssetBundle;
            }

            if (_loadedAssetBundles[assetBundleKey].Tags.Length != 0) return;

            StartCoroutine(AssetBundleUnLoader(unloadAllLoadedObjects, assetBundleKey));
        }

        public void UnLoadAssetBundleByTag(string assetTag, bool unloadAllLoadedObjects = true)
        {
            UnLoadAssetBundleByTags(new[] {assetTag}, unloadAllLoadedObjects);
        }

        public void UnLoadAssetBundleByTags(string[] assetTags, bool unloadAllLoadedObjects = true)
        {
            foreach (var pair in _loadedAssetBundles.ToArray())
            {
                if (!assetTags.Any(assetTag => pair.Value.Tags.Contains(assetTag))) continue;

                var taggedAssetBundle = pair.Value;
                taggedAssetBundle.Tags = taggedAssetBundle.Tags.Except(assetTags).ToArray();
                _loadedAssetBundles[pair.Key] = taggedAssetBundle;
            }

            if (unloadAllLoadedObjects)
            {
                if (_cleanAssetBundlesWithUnloadCoroutine == null)
                {
                    _cleanAssetBundlesWithUnloadCoroutineCycles++;
                    _cleanAssetBundlesWithUnloadCoroutine = StartCoroutine(AssetBundleUnLoader());
                }
                else
                {
                    _cleanAssetBundlesWithUnloadCoroutineCycles++;
                }
            }
            else
            {
                if (_cleanAssetBundlesWithoutUnloadCoroutine == null)
                {
                    _cleanAssetBundlesWithoutUnloadCoroutineCycles++;
                    _cleanAssetBundlesWithoutUnloadCoroutine = StartCoroutine(AssetBundleUnLoader(false));
                }
                else
                {
                    _cleanAssetBundlesWithoutUnloadCoroutineCycles++;
                }
            }
        }

        public void UnLoadAssetBundle(AssetBundle assetBundle, string assetTag = "*",
            bool unloadAllLoadedObjects = true)
        {
            UnLoadAssetBundle(assetBundle, new[] {assetTag}, unloadAllLoadedObjects);
        }

        public void UnLoadAssetBundle(AssetBundle assetBundle, string[] assetTags, bool unloadAllLoadedObjects = true)
        {
            var assetsKeys = new List<string>();
            if (assetTags.Length > 0)
            {
                assetsKeys.AddRange(_loadedAssetBundles.Keys.ToList()
                    .Where(key => _loadedAssetBundles[key].Tags.Except(assetTags).Any())
                    .Where(key => _loadedAssetBundles[key].AssetBundle.GetHashCode() == assetBundle.GetHashCode()));
            }
            else
            {
                assetsKeys.AddRange(_loadedAssetBundles.Keys.ToList()
                    .Where(key => _loadedAssetBundles[key].AssetBundle.GetHashCode() == assetBundle.GetHashCode()));
            }

            foreach (var assetKey in assetsKeys)
            {
                UnLoadAssetBundleByKey(assetKey, assetTags, unloadAllLoadedObjects);
            }
        }

        // TODO:: error handler
        private IEnumerator AssetBundleLoader(Uri url, string[] tags, AssetBundleEventHandler callback)
        {
            if (!IsEnabled) yield return new WaitForEndOfFrame();

            var assetBundleKey = md5(url.ToString());

            AssetBundle assetBundle;

            if (url.Scheme == "stream" && (Application.isEditor
                                           || Application.platform == RuntimePlatform.LinuxPlayer
                                           || Application.platform == RuntimePlatform.WindowsPlayer
                                           || Application.platform == RuntimePlatform.OSXPlayer)
            )
            {
                var localPath = Path.Combine(
                    Application.streamingAssetsPath,
                    url.LocalPath.TrimStart('/').TrimEnd('/')
                ).Replace('\\', '/');

                if (File.Exists(localPath))
                {
                    using (var fs = File.Open(localPath, FileMode.Open))
                    {
                        if (fs.CanRead) url = new Uri($"file://{localPath}");
                    }
                }
            }

            if (url.Scheme == "file" && Application.isEditor
                || Application.platform == RuntimePlatform.LinuxPlayer
                || Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.OSXPlayer
            )
            {
                var request = AssetBundle.LoadFromFileAsync(url.LocalPath);
                yield return request;

                if (request.assetBundle == null)
                {
                    Debug.LogErrorFormat("Error request {0}", url);

                    callback.Invoke(assetBundleKey, null);
                    yield break;
                }

                assetBundle = request.assetBundle;
            }
            else
            {
                var request = UnityWebRequestAssetBundle.GetAssetBundle(url);

                yield return request.SendWebRequest();

                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogErrorFormat("Error request {0} \r\n {1}", url, request.error);

                    request.Dispose();
                    callback.Invoke(assetBundleKey, null);
                    yield break;
                }

                assetBundle = DownloadHandlerAssetBundle.GetContent(request);

                request.Dispose();
            }

            if (assetBundle == null)
            {
                callback.Invoke(assetBundleKey, null);
                yield break;
            }

            if (_loadedAssetBundles.ContainsKey(assetBundleKey))
            {
                if (!tags.All(assetTag => _loadedAssetBundles[assetBundleKey].Tags.Contains(assetTag)))
                {
                    var taggedAssetBundle = _loadedAssetBundles[assetBundleKey];
                    taggedAssetBundle.Tags = taggedAssetBundle.Tags.Concat(tags).ToArray();
                    _loadedAssetBundles[assetBundleKey] = taggedAssetBundle;
                }
            }
            else
            {
                _loadedAssetBundles.Add(assetBundleKey, new TaggedAssetBundle
                {
                    AssetBundle = assetBundle,
                    Url = url,
                    Tags = tags
                });
            }

            if (debug) Debug.Log("AssetBundle is loaded: " + url.ToString().TrimEnd('/'));

            callback.Invoke(assetBundleKey, assetBundle);
        }

        private IEnumerator AssetBundleUnLoader(bool unloadAllLoadedObjects = true, string assetBundleKey = "")
        {
            if (!IsEnabled) yield return new WaitForEndOfFrame();

            IEnumerator CleanUp()
            {
                var counter = 0;
                foreach (var key in _loadedAssetBundles.Keys.ToList()
                    .Where(key => _loadedAssetBundles[key].Tags.Length == 0
                                  && (assetBundleKey == "" || key == assetBundleKey))
                )
                {
                    var url = _loadedAssetBundles[key].Url;

                    _loadedAssetBundles[key].AssetBundle.Unload(unloadAllLoadedObjects);
                    _loadedAssetBundles.Remove(key);

                    if (debug) Debug.Log("AssetBundle is unloaded: " + url.ToString().TrimEnd('/'));

                    counter++;

                    if (counter <= 7) continue;

                    yield return new WaitForEndOfFrame();
                    counter = 0;
                }
            }

            if (assetBundleKey != "")
            {
                yield return CleanUp();
            }
            else if (unloadAllLoadedObjects)
            {
                while (_cleanAssetBundlesWithUnloadCoroutineCycles > 0)
                {
                    yield return CleanUp();
                    _cleanAssetBundlesWithUnloadCoroutineCycles--;
                }

                _cleanAssetBundlesWithUnloadCoroutine = null;
            }
            else
            {
                while (_cleanAssetBundlesWithoutUnloadCoroutineCycles > 0)
                {
                    yield return CleanUp();
                    _cleanAssetBundlesWithoutUnloadCoroutineCycles--;
                }

                _cleanAssetBundlesWithoutUnloadCoroutine = null;
            }
        }
    }
}
