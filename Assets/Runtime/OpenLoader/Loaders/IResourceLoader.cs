using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    [SuppressMessage("ReSharper", "ParameterTypeCanBeEnumerable.Global")]
    public interface IOpenResourceLoader
    {
        uint ResourceMaxConcurrentConnectionsCount { get; set; }

        void LoadResource<T>(Uri assetBundleUrl, string resourceName, string resourceTag,
            BaseOpenLoader.ResourceEventHandler<T> callback) where T : Object;

        void LoadResource<T>(Uri assetBundleUrl, string resourceName, string[] resourceT,
            BaseOpenLoader.ResourceEventHandler<T> callback) where T : Object;

        void LoadResourceFromUrl<T>(Uri resourceUrl, string resourceTag,
            BaseOpenLoader.ResourceEventHandler<T> callback) where T : Object;

        void LoadResourceFromUrl<T>(Uri resourceUrl, string[] resourceT,
            BaseOpenLoader.ResourceEventHandler<T> callback)
            where T : Object;

        void LoadAudioFromUrl<T>(Uri url, AudioType audioType, string resourceTag,
            BaseOpenLoader.ResourceEventHandler<T> callback) where T : Object;

        void LoadAudioFromUrl<T>(Uri url, AudioType audioType, string[] resourceTags,
            BaseOpenLoader.ResourceEventHandler<T> callback) where T : Object;

        void LoadByteArrayFromUrl(Uri url, string resourceTag, BaseOpenLoader.ResourceBytesEventHandler callback);
        void LoadByteArrayFromUrl(Uri url, string[] resourceTags, BaseOpenLoader.ResourceBytesEventHandler callback);

        void UnLoadResource(Uri assetBundleUrl, string resourceName, string resourceTag);
        void UnLoadResource(Uri assetBundleUrl, string resourceName, string[] resourceTags);

        void UnLoadResource(Uri resourceUrl, string resourceTag);
        void UnLoadResourceByKey(string resourceKey, string resourceTag);

        void UnLoadResource(Uri resourceUrl, string[] resourceTags);
        void UnLoadResourceByKey(string resourceKey, string[] resourceTags);

        void UnLoadResourceByTag(string resourceTag);
        void UnLoadResourceByTags(string[] resourceTag);

        void UnLoadResource<T>(T resource, string resourceTag = "*") where T : Object;
        void UnLoadResource<T>(T resource, string[] resourceTags) where T : Object;

        void UnLoadByteArrayResource(byte[] resource, string resourceTag = "*");
        void UnLoadByteArrayResource(byte[] resource, string[] resourceTags);
    }
}
