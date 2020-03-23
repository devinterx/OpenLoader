using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    [SuppressMessage("ReSharper", "ParameterTypeCanBeEnumerable.Global")]
    public interface IOpenAssetBundleLoader
    {
        uint AssetBundleMaxConcurrentConnectionsCount { get; set; }

        void LoadAssetBundle(Uri assetBundleUrl, string assetTag, BaseOpenLoader.AssetBundleEventHandler callback);
        void LoadAssetBundle(Uri assetBundleUrl, string[] tags, BaseOpenLoader.AssetBundleEventHandler callback);

        void UnLoadAssetBundle(Uri assetBundleUrl, string assetTag, bool unloadAllLoadedObjects = true);
        void UnLoadAssetBundleByKey(string assetBundleKey, string assetTag, bool unloadAllLoadedObjects = true);

        void UnLoadAssetBundle(Uri assetBundleUrl, string[] tags, bool unloadAllLoadedObjects = true);
        void UnLoadAssetBundleByKey(string assetBundleKey, string[] tags, bool unloadAllLoadedObjects = true);

        void UnLoadAssetBundleByTag(string assetTag, bool unloadAllLoadedObjects = true);
        void UnLoadAssetBundleByTags(string[] assetTags, bool unloadAllLoadedObjects = true);

        void UnLoadAssetBundle(AssetBundle assetBundle, string assetTag = "*", bool unloadAllLoadedObjects = true);
        void UnLoadAssetBundle(AssetBundle assetBundle, string[] assetTags, bool unloadAllLoadedObjects = true);
    }
}
