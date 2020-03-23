using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    public abstract partial class BaseOpenLoader : IOpenResourceLoader
    {
        private CoroutineQueue _resourceLoaderQueue;

        private uint _resourceMaxConcurrentConnectionsCount = 3;

        public uint ResourceMaxConcurrentConnectionsCount
        {
            get => _resourceMaxConcurrentConnectionsCount;
            set
            {
                _resourceMaxConcurrentConnectionsCount = value <= 0 ? 1 : value;
                if (_resourceLoaderQueue != null)
                    _resourceLoaderQueue.MaxConcurrentCoroutinesCount = _resourceMaxConcurrentConnectionsCount;
            }
        }

        private const string ResourceLoadException =
            "Failed to load resource {1} because asset bundle {0} it was not loaded";

        private struct TaggedResource
        {
            public Object Resource;
            public byte[] RawData;
            public Uri Url;
            public string[] Tags;
        }

        public delegate void ResourceEventHandler<in T>(string resourceKey, T resource) where T : Object;

        public delegate void ResourceBytesEventHandler(string resourceKey, byte[] resource);

        private Coroutine _cleanResourcesCoroutine;
        private int _cleanResourcesCoroutineCycles;

        private readonly IDictionary<string, TaggedResource> _loadedResources =
            new Dictionary<string, TaggedResource>(0);

        public void LoadResource<T>(Uri assetBundleUrl, string resourceName, string resourceTag,
            ResourceEventHandler<T> callback) where T : Object
        {
            LoadResource(assetBundleUrl, resourceName, new[] {resourceTag}, callback);
        }

        public void LoadResource<T>(Uri assetBundleUrl, string resourceName, string[] resourceTags,
            ResourceEventHandler<T> callback) where T : Object
        {
            var resourceKey = md5(assetBundleUrl + ":" + resourceName);

            if (_loadedResources.ContainsKey(resourceKey))
            {
                if (!resourceTags.All(resourceTag => _loadedResources[resourceKey].Tags.Contains(resourceTag)))
                {
                    var taggedResource = _loadedResources[resourceKey];
                    taggedResource.Tags = taggedResource.Tags.Concat(resourceTags).ToArray();
                    _loadedResources[resourceKey] = taggedResource;
                }

                callback(resourceKey, _loadedResources[resourceKey].Resource as T);
                return;
            }
#if UNITY_EDITOR
            if (useAssetDatabase && (assetBundleUrl.Scheme == "stream" || assetBundleUrl.Scheme == "file"))
            {
                var localPath = assetBundleUrl.LocalPath;
                var resourcePath = new List<string>(AssetDatabase.GetAssetPathsFromAssetBundle(
                    Path.GetFileNameWithoutExtension(localPath)
                )).Find(path => string.CompareOrdinal(resourceName, Path.GetFileNameWithoutExtension(path)) == 0);

                if (!string.IsNullOrEmpty(resourcePath))
                {
                    var resource = AssetDatabase.LoadAssetAtPath<T>(resourcePath);

                    _loadedResources.Add(resourceKey, new TaggedResource
                    {
                        Resource = resource,
                        Tags = resourceTags
                    });
                    callback(resourceKey, resource);
                    return;
                }
            }
#endif
            LoadAssetBundle(assetBundleUrl, resourceTags, (_, assetBundle) =>
            {
                if (assetBundle == null)
                {
                    Debug.LogError(string.Format(ResourceLoadException, assetBundleUrl, resourceKey));

                    throw new ResourceLoadException(
                        string.Format(ResourceLoadException, assetBundleUrl, resourceKey));
                }

                if (typeof(T).IsSubclassOf(typeof(Object)))
                {
                    var resources = new List<T>(assetBundle.LoadAllAssets<T>());

                    foreach (var resource in resources.Where(resource => resource.name.Equals(resourceKey)))
                    {
                        _loadedResources.Add(resourceKey, new TaggedResource
                        {
                            Resource = resource,
                            Tags = resourceTags
                        });
                        callback(resourceKey, resource);
                        break;
                    }
                }

                callback(resourceKey, null);
            });
        }

        public void LoadAudioFromUrl<T>(Uri url, AudioType audioType, string resourceTag,
            ResourceEventHandler<T> callback) where T : Object
        {
            LoadAudioFromUrl(url, audioType, new[] {resourceTag}, callback);
        }

        public void LoadByteArrayFromUrl(Uri url, string resourceTag,
            ResourceBytesEventHandler callback)
        {
            LoadByteArrayFromUrl(url, new[] {resourceTag}, callback);
        }

        public void LoadResourceFromUrl<T>(Uri url, string resourceTag,
            ResourceEventHandler<T> callback) where T : Object
        {
            LoadResourceFromUrl(url, new[] {resourceTag}, callback);
        }

        public void LoadResourceFromUrl<T>(Uri url, string[] resourceTags, ResourceEventHandler<T> callback)
            where T : Object
        {
            var resourceKey = md5(url.ToString());

            if (_loadedResources.ContainsKey(resourceKey))
            {
                if (!resourceTags.All(resourceTag => _loadedResources[resourceKey].Tags.Contains(resourceTag)))
                {
                    var taggedResource = _loadedResources[resourceKey];
                    taggedResource.Tags = taggedResource.Tags.Concat(resourceTags).ToArray();
                    _loadedResources[resourceKey] = taggedResource;
                }

                callback.Invoke(resourceKey, _loadedResources[resourceKey].Resource as T);

                return;
            }

            _resourceLoaderQueue?.Enqueue(ResourceLoader(url, resourceTags, callback));
        }

        public void LoadAudioFromUrl<T>(Uri url, AudioType audioType, string[] resourceTags,
            ResourceEventHandler<T> callback) where T : Object
        {
            var resourceKey = md5(url.ToString());

            if (_loadedResources.ContainsKey(resourceKey))
            {
                if (!resourceTags.All(resourceTag => _loadedResources[resourceKey].Tags.Contains(resourceTag)))
                {
                    var taggedResource = _loadedResources[resourceKey];
                    taggedResource.Tags = taggedResource.Tags.Concat(resourceTags).ToArray();
                    _loadedResources[resourceKey] = taggedResource;
                }

                callback.Invoke(resourceKey, _loadedResources[resourceKey].Resource as T);

                return;
            }

            _resourceLoaderQueue?.Enqueue(ResourceLoader(url, resourceTags, callback, audioType));
        }

        public void LoadByteArrayFromUrl(Uri url, string[] resourceTags, ResourceBytesEventHandler callback)
        {
            var resourceKey = md5(url.ToString());

            if (_loadedResources.ContainsKey(resourceKey))
            {
                if (!resourceTags.All(resourceTag => _loadedResources[resourceKey].Tags.Contains(resourceTag)))
                {
                    var taggedByteArrayResource = _loadedResources[resourceKey];
                    taggedByteArrayResource.Tags = taggedByteArrayResource.Tags.Concat(resourceTags).ToArray();
                    _loadedResources[resourceKey] = taggedByteArrayResource;
                }

                callback.Invoke(resourceKey, _loadedResources[resourceKey].RawData);

                return;
            }

            _resourceLoaderQueue?.Enqueue(ResourceByteArrayLoader(url, resourceTags, callback));
        }

        public void UnLoadResource(Uri assetUrl, string resourceName, string resourceTag)
        {
            UnLoadResource(assetUrl, resourceName, new[] {resourceTag});
        }

        public void UnLoadResource(Uri assetUrl, string resourceName, string[] resourceTags)
        {
            var resourceKey = md5(resourceName + ":" + resourceName);

            UnLoadResourceByKey(resourceKey, resourceTags);
        }

        public void UnLoadResource(Uri url, string resourceTag)
        {
            UnLoadResource(url, new[] {resourceTag});
        }

        public void UnLoadResourceByKey(string resourceKey, string resourceTag)
        {
            UnLoadResourceByKey(resourceKey, new[] {resourceTag});
        }

        public void UnLoadResource(Uri resourceUrl, string[] resourceTags)
        {
            var resourceKey = md5(resourceUrl.ToString());

            UnLoadResourceByKey(resourceKey, resourceTags);
        }

        public void UnLoadResourceByKey(string resourceKey, string[] resourceTags)
        {
            if (!_loadedResources.ContainsKey(resourceKey)) return;

            if (resourceTags.Contains("*"))
            {
                var taggedResource = _loadedResources[resourceKey];
                taggedResource.Tags = new string[] { };
                _loadedResources[resourceKey] = taggedResource;
            }
            else if (resourceTags.Any(assetTag => _loadedResources[resourceKey].Tags.Contains(assetTag)))
            {
                var taggedResource = _loadedResources[resourceKey];
                taggedResource.Tags = taggedResource.Tags.Except(resourceTags).ToArray();
                _loadedResources[resourceKey] = taggedResource;
            }

            if (_loadedResources[resourceKey].Tags.Length != 0) return;

            StartCoroutine(ResourceUnLoader(resourceKey));
        }

        public void UnLoadResourceByTag(string resourceTag)
        {
            UnLoadResourceByTags(new[] {resourceTag});
        }

        public void UnLoadResourceByTags(string[] resourceTags)
        {
            foreach (var pair in _loadedResources.ToArray())
            {
                if (!resourceTags.Any(assetTag => pair.Value.Tags.Contains(assetTag))) continue;

                var taggedResource = pair.Value;
                taggedResource.Tags = taggedResource.Tags.Except(resourceTags).ToArray();
                _loadedResources[pair.Key] = taggedResource;
            }

            if (_cleanResourcesCoroutine == null)
            {
                _cleanResourcesCoroutineCycles++;
                _cleanResourcesCoroutine = StartCoroutine(ResourceUnLoader());
            }
            else
            {
                _cleanResourcesCoroutineCycles++;
            }
        }

        public void UnLoadResource<T>(T resource, string resourceTag = "*") where T : Object
        {
            UnLoadResource(resource, new[] {resourceTag});
        }

        public void UnLoadResource<T>(T resource, string[] resourceTags) where T : Object
        {
            var resourceKeys = new List<string>();
            if (resourceTags.Length > 0)
            {
                resourceKeys.AddRange(_loadedResources.Keys.ToList()
                    .Where(key => _loadedResources[key].Tags.Except(resourceTags).Any())
                    .Where(key => _loadedResources[key].Resource.GetHashCode() == resource.GetHashCode()));
            }
            else
            {
                resourceKeys.AddRange(_loadedResources.Keys.ToList()
                    .Where(key => _loadedResources[key].Resource.GetHashCode() == resource.GetHashCode()));
            }

            foreach (var resourceKey in resourceKeys)
            {
                UnLoadResourceByKey(resourceKey, resourceTags);
            }
        }

        public void UnLoadByteArrayResource(byte[] resource, string resourceTag = "*")
        {
            UnLoadByteArrayResource(resource, new[] {resourceTag});
        }

        public void UnLoadByteArrayResource(byte[] resource, string[] resourceTags)
        {
            var resourceKeys = new List<string>();
            if (resourceTags.Length > 0)
            {
                resourceKeys.AddRange(_loadedResources.Keys.ToList()
                    .Where(key => _loadedResources[key].Tags.Except(resourceTags).Any())
                    .Where(key => _loadedResources[key].RawData.SequenceEqual(resource)));
            }
            else
            {
                resourceKeys.AddRange(_loadedResources.Keys.ToList()
                    .Where(key => _loadedResources[key].RawData.SequenceEqual(resource)));
            }

            foreach (var resourceKey in resourceKeys)
            {
                UnLoadResourceByKey(resourceKey, resourceTags);
            }
        }

        private IEnumerator ResourceLoader<T>(Uri url, string[] resourceTags, ResourceEventHandler<T> callback,
            AudioType audioType = AudioType.OGGVORBIS) where T : Object
        {
            if (!IsEnabled) yield return new WaitForEndOfFrame();

            var resourceKey = md5(url.ToString());
            T resource = null;

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (url.Scheme == "resource")
            {
                // Assets/Resources/
                resource = Resources.Load<T>(url.ToString().Substring("resource://".Length).TrimEnd('/'));
            }
            else if (url.Scheme == "stream" || url.Scheme == "file" || url.Scheme == "http" || url.Scheme == "https")
            {
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

                UnityWebRequest request;
                if (typeof(T) == typeof(Texture2D))
                {
                    request = UnityWebRequestTexture.GetTexture(url);
                }
                else if (typeof(T) == typeof(AudioClip))
                {
                    request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
                }
                else
                {
                    request = UnityWebRequest.Get(url);
                }

                yield return request.SendWebRequest();

                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogErrorFormat("Error request [{0}, {1}]", url, request.error);

                    request.Dispose();
                    callback.Invoke(resourceKey, null);
                    yield break;
                }

                if (typeof(T) == typeof(Texture2D))
                {
                    resource = DownloadHandlerTexture.GetContent(request) as T;
                }
                else if (typeof(T) == typeof(AudioClip))
                {
                    resource = DownloadHandlerAudioClip.GetContent(request) as T;
                }
                else
                {
                    resource = request.downloadHandler.data as T;
                }

                request.Dispose();
            }

            if (resource == null)
            {
                callback.Invoke(resourceKey, null);
                yield break;
            }

            if (_loadedResources.ContainsKey(resourceKey))
            {
                if (!resourceTags.All(resourceTag => _loadedResources[resourceKey].Tags.Contains(resourceTag)))
                {
                    var taggedResource = _loadedResources[resourceKey];
                    taggedResource.Tags = taggedResource.Tags.Concat(resourceTags).ToArray();
                    _loadedResources[resourceKey] = taggedResource;
                }
            }
            else
            {
                _loadedResources.Add(resourceKey, new TaggedResource
                {
                    Resource = resource,
                    RawData = null,
                    Url = url,
                    Tags = resourceTags
                });
            }

            if (debug) Debug.Log("Resource is loaded: " + url.ToString().TrimEnd('/'));

            callback.Invoke(resourceKey, resource);
        }

        private IEnumerator ResourceByteArrayLoader(Uri url, string[] resourceTags, ResourceBytesEventHandler callback)
        {
            if (!IsEnabled) yield return new WaitForEndOfFrame();

            var resourceKey = md5(url.ToString());
            byte[] resource = null;

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (url.Scheme == "resource")
            {
                var request = // resource://{path} = Assets/Resources/{path}
                    Resources.LoadAsync<TextAsset>(url.ToString().Substring("resource://".Length).TrimEnd('/'));
                yield return request;

                if (request.asset == null)
                {
                    Debug.LogError($"Error request {url.ToString().TrimEnd('/')}");

                    callback.Invoke(resourceKey, null);
                    yield break;
                }

                resource = (request.asset as TextAsset)?.bytes;
            }
            else if (url.Scheme == "stream" || url.Scheme == "file" || url.Scheme == "http" || url.Scheme == "https")
            {
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

                var request = UnityWebRequest.Get(url);

                yield return request.SendWebRequest();

                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogErrorFormat("Error request {0} \r\n {1}", url, request.error);

                    request.Dispose();
                    callback.Invoke(resourceKey, null);
                    yield break;
                }

                resource = request.downloadHandler.data;

                request.Dispose();
            }

            if (resource == null)
            {
                callback.Invoke(resourceKey, null);
                yield break;
            }

            if (_loadedResources.ContainsKey(resourceKey))
            {
                if (!resourceTags.All(resourceTag => _loadedResources[resourceKey].Tags.Contains(resourceTag)))
                {
                    var taggedByteArrayResource = _loadedResources[resourceKey];
                    taggedByteArrayResource.Tags = taggedByteArrayResource.Tags.Concat(resourceTags).ToArray();
                    _loadedResources[resourceKey] = taggedByteArrayResource;
                }
            }
            else
            {
                _loadedResources.Add(resourceKey, new TaggedResource()
                {
                    Resource = null,
                    RawData = resource,
                    Url = url,
                    Tags = resourceTags
                });
            }

            if (debug) Debug.Log("Resource is loaded: " + url.ToString().TrimEnd('/'));

            callback.Invoke(resourceKey, resource);
        }

        private IEnumerator ResourceUnLoader(string resourceKey = "")
        {
            if (!IsEnabled) yield return new WaitForEndOfFrame();

            IEnumerator CleanUp()
            {
                var counter = 0;
                foreach (var key in _loadedResources.Keys.ToList()
                    .Where(key => _loadedResources[key].Tags.Length == 0
                                  && (resourceKey == "" || key == resourceKey))
                )
                {
                    var url = _loadedResources[key].Url;

                    Destroy(_loadedResources[key].Resource);
                    _loadedResources.Remove(key);

                    if (debug) Debug.Log("Resource is unloaded: " + url.ToString().TrimEnd('/'));

                    counter++;

                    if (counter <= 7) continue;

                    yield return new WaitForEndOfFrame();
                    counter = 0;
                }
            }

            if (resourceKey != "")
            {
                yield return CleanUp();
            }
            else
            {
                while (_cleanResourcesCoroutineCycles > 0)
                {
                    yield return CleanUp();
                    _cleanResourcesCoroutineCycles--;
                }

                _cleanResourcesCoroutine = null;
            }
        }
    }

    public class ResourceLoadException : Exception
    {
        public ResourceLoadException(string message) : base(message)
        {
        }
    }
}
