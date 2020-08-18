using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using OpenUniverse.Runtime.OpenLoader.Views;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    public abstract partial class BaseOpenLoader : MonoBehaviour
    {
        [UsedImplicitly]
        protected static BaseOpenLoader Instance { get; set; }

        [UsedImplicitly]
        public static Thread UnityThread;

        [NonSerialized, UsedImplicitly]
        protected bool IsEnabled;

        [FormerlySerializedAs("Debug")]
        public bool debug;

        [FormerlySerializedAs("UseAssetDatabase")]
        public bool useAssetDatabase;

        [NonSerialized]
        protected bool IsRunManifest;

        [UsedImplicitly, SuppressMessage("ReSharper", "InconsistentNaming")]
        protected IOpenLoaderView _loaderView { get; set; }

        [NonSerialized]
        private bool _isInitialized;

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

        private void OnDestroy()
        {
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
            if (!Application.isPlaying || _loaderView == null) return;

            if (_isInitialized && !IsRunManifest && _loaderView.IsShowScreenLoader)
            {
                _loaderView.HideScreenLoader();
                Instance.RunManifest();
            }
            else if (_isInitialized && !IsRunManifest) RunManifest();
        }

        private void InitLoaders()
        {
            if (_assetBundleLoaderQueue == null)
                _assetBundleLoaderQueue = new CoroutineQueue(StartCoroutine, AssetBundleMaxConcurrentConnectionsCount);

            if (_resourceLoaderQueue == null)
                _resourceLoaderQueue = new CoroutineQueue(StartCoroutine, ResourceMaxConcurrentConnectionsCount);
        }

        [UsedImplicitly]
        protected virtual void RunManifest()
        {
        }

        [UsedImplicitly]
        public void UnLoadAll()
        {
            var loadedScenesKeys = _loadedScenes.Keys.ToList();
            foreach (var scene in loadedScenesKeys) UnLoadScene(scene);

            var loadedAssetBundlesKeys = _loadedAssetBundles.Keys.ToList();
            foreach (var assetBundleKey in loadedAssetBundlesKeys) UnLoadAssetBundleByKey(assetBundleKey, "*");
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static string md5(string inputString)
        {
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(inputString);
            var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(inputBytes);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var temp in hash) stringBuilder.Append(temp.ToString("x2"));

            return stringBuilder.ToString();
        }
    }

    public class CoroutineQueue
    {
        private uint _maxConcurrentCoroutinesCount;

        public uint MaxConcurrentCoroutinesCount
        {
            get => _maxConcurrentCoroutinesCount;
            set => _maxConcurrentCoroutinesCount = value == 0 ? 1 : value;
        }

        private readonly Func<IEnumerator, Coroutine> _coroutineStarter;

        private readonly Queue<IEnumerator> _iEnumeratorsQueue = new Queue<IEnumerator>();

        private uint _concurrentCoroutinesCount;

        public CoroutineQueue(Func<IEnumerator, Coroutine> coroutineStarter, uint maxConcurrentCoroutinesCount = 1)
        {
            if (maxConcurrentCoroutinesCount == 0) maxConcurrentCoroutinesCount = 1;

            _maxConcurrentCoroutinesCount = maxConcurrentCoroutinesCount;
            _coroutineStarter = coroutineStarter;
        }

        public void Enqueue(IEnumerator coroutine)
        {
            if (_concurrentCoroutinesCount < MaxConcurrentCoroutinesCount)
            {
                _coroutineStarter(CoroutineRunner(coroutine));
            }
            else
            {
                _iEnumeratorsQueue.Enqueue(coroutine);
            }
        }

        private IEnumerator CoroutineRunner(IEnumerator coroutine)
        {
            _concurrentCoroutinesCount++;
            while (coroutine.MoveNext()) yield return coroutine.Current;
            _concurrentCoroutinesCount--;

            if (_iEnumeratorsQueue.Count <= 0) yield break;

            Enqueue(_iEnumeratorsQueue.Dequeue());
        }
    }
}
