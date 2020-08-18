using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using JetBrains.Annotations;
using OpenUniverse.Runtime.OpenLoader.Loaders;
using OpenUniverse.Runtime.OpenLoader.Views;
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
        public static OpenLoaderSystem Instance;

        [UsedImplicitly]
        public static Thread UnityThread;

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

        [UsedImplicitly, SuppressMessage("ReSharper", "InconsistentNaming")]
        public IOpenLoaderView _loaderView { get; set; }

        [NonSerialized]
        private bool _isInitialized;

        [NonSerialized]
        private bool _isRun;

        private void OnEnable()
        {
            IsEnabled = true;

            if (!Application.isPlaying) return;

            SubscribeEvents();
        }

        private void OnDisable()
        {
            IsEnabled = false;

            if (!Application.isPlaying) return;

            UnSubscribeEvents();
        }

        private void Awake()
        {
            UnityThread = Thread.CurrentThread;
            if (Instance == null) Instance = this;

            if (!Application.isPlaying) return;

            InitLoaders();

            SubscribeEvents();

            _isInitialized = true;
        }

        private void Update()
        {
            if (!Application.isPlaying || LoaderView == null) return;

            if (_isInitialized && !_isRun && _loaderView.IsShowScreenLoader)
            {
                _loaderView.HideScreenLoader();
                RunManifest();
            }
            else if (_isInitialized && !_isRun) RunManifest();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            UnSubscribeEvents();
        }

        private void RunManifest()
        {
            if (_isRun) return;
            _isRun = true;

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
