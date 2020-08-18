using System;
using System.Threading;
using JetBrains.Annotations;
using OpenUniverse.Runtime.OpenLoader.Loaders;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUniverse.Runtime.OpenLoader
{
    // TODO:: - AssetBundle FS cache
    // TODO:: - AssetBundle from editor
    // TODO:: - manifest loader
    // TODO:: - OpenLoader Settings (+SettingsEditor drawable config)
    // TODO:: - OpenLoader Browser
    // TODO:: - OpenLoader AssetBuilder
    public partial class OpenLoaderSystem : BaseOpenLoader
    {
        [UsedImplicitly]
        public const string OpenLoaderSceneMagicWords = "OpenLoader";

        [UsedImplicitly]
        public new static Thread UnityThread;

        [UsedImplicitly]
        public new static OpenLoaderSystem Instance => BaseOpenLoader.Instance as OpenLoaderSystem;

        public GameObject loaderView;
        public EventSystem eventSystem;

        [UsedImplicitly]
        public EventSystem EventSystem
        {
            get => eventSystem;
            set => eventSystem = value;
        }

        [UsedImplicitly]
        public GameObject LoaderView
        {
            get => loaderView;
            set => loaderView = value;
        }

        protected override void RunManifest()
        {
            if (IsRunManifest) return;
            IsRunManifest = true;

            LoadScene(new Uri("stream://localhost/mainmodule"), "MainModule", () =>
            {
                const string resourceTag = "__openLoader";
                /*
                 * LoadByteArrayFromUrl(new Uri("resource://open-loader")... for Assets/Resources/open-loader.bytes
                 * LoadByteArrayFromUrl(new Uri("stream://localhost/open-loader.bytes")... for Assets/StreamingAssets/open-loader.bytes
                 */
                LoadByteArrayFromUrl(new Uri("stream://localhost/open-loader.bytes"), resourceTag,
                    (resourceKey, bytes) =>
                    {
                        if (bytes == null)
                        {
                            Debug.LogError("Bytes is empty");
                            return;
                        }

                        // Debug.Log(resourceKey + "::" + Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                        UnLoadResourceByTag(resourceTag);
                        // UnLoadResourceByKey(resourceKey, resourceTag);
                    }
                );
            });
        }
    }
}
