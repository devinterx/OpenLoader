using System;
using System.Collections;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using OpenUniverse.Runtime.OpenLoader.Loaders;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenUniverse.Runtime.OpenLoader
{
    // TODO:: - AssetBundle FS cache
    // TODO:: - AssetBundle from editor
    // TODO:: - Async Task
    // TODO::   - Loaders as await promises
    // TODO::   - async recursive optimize unLoaders
    // TODO::   - async recursive optimize unLoaders
    // TODO:: - manifest loader
    // TODO:: - OpenLoader Settings (+SettingsEditor drawable config)
    // TODO:: - OpenLoader Browser
    // TODO:: - OpenLoader AssetBuilder
    [ExecuteAlways] // [RuntimeInitializeOnLoadMethod]
    public class OpenLoader : BaseOpenLoader
    {
        [UsedImplicitly]
        public static OpenLoader Instance;

        [UsedImplicitly]
        public static Thread UnityThread;

        private const string LoaderUiName = "LoaderUi";
        private const string ScreenName = "LoaderScreen";
        private const string VersionName = "Version";
        private const string ProgressBarName = "ProgressBar";
        private const string ProgressBarLabelName = "Label";

        [UsedImplicitly]
        public string Version
        {
            get => _version != null ? _version.text : "";
            set
            {
                if (_version != null) _version.text = value;
            }
        }

        [UsedImplicitly]
        public float Progress
        {
            get => _slider != null ? _slider.value : 0.0f;
            set
            {
                if (_slider != null) _slider.value = value;
            }
        }

        [UsedImplicitly]
        public string ProgressStatus
        {
            get => _sliderLabel != null ? _sliderLabel.text : "";
            set
            {
                if (_sliderLabel != null) _sliderLabel.text = value;
            }
        }

        [FormerlySerializedAs("EventSystem")]
        public EventSystem eventSystem;

        [NonSerialized, UsedImplicitly]
        private bool _isInitialized;

        [NonSerialized, UsedImplicitly]
        private bool _isRun;

        private Transform _openLoader;
        private Transform _openLoaderUi;
        private Image _screenLoader;
        private Color ScreenColor => _screenLoader != null ? _screenLoader.color : new Color(0, 0, 0, 1.0f);
        private Slider _slider;
        private TMP_Text _sliderLabel;
        private TMP_Text _version;

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
            if (UnityThread == null) UnityThread = Thread.CurrentThread;
            if (Instance == null) Instance = this;

            if (!Application.isPlaying) return;

            InitLoaders();

            if (_openLoader == null) _openLoader = transform;
            if (_openLoader != null)
            {
                if (_screenLoader == null) _screenLoader = _openLoader.Find(ScreenName)?.GetComponent<Image>();
                if (_screenLoader != null)
                {
#if UNITY_EDITOR
                    if (_screenLoader.gameObject.activeSelf) _screenLoader.gameObject.SetActive(false);
                    _screenLoader.color = new Color(ScreenColor.r, ScreenColor.g, ScreenColor.b, 0);
#else
                    if (!_screenLoader.gameObject.activeSelf) _screenLoader.gameObject.SetActive(true);
                    _screenLoader.color = new Color(ScreenColor.r, ScreenColor.g, ScreenColor.b, 1.0f);
#endif
                }

                if (_openLoaderUi == null) _openLoaderUi = _openLoader.Find(LoaderUiName);

                if (_openLoaderUi != null)
                {
                    if (_version == null)
                        _version = _openLoaderUi.Find(VersionName)?.GetComponent<TMP_Text>();
                    if (_slider == null)
                        _slider = _openLoaderUi.Find(ProgressBarName)?.GetComponent<Slider>();
                    if (_slider != null && _sliderLabel == null)
                        _sliderLabel = _slider.transform.Find(ProgressBarLabelName)?.GetComponent<TMP_Text>();
                }
            }

            DontDestroyOnLoad(gameObject);
            if (eventSystem != null) DontDestroyOnLoad(eventSystem.gameObject);

            SubscribeEvents();

            _isInitialized = true;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (_isInitialized && !_isRun && IsShowScreenLoader)
            {
                HideScreenLoader();
                Run();
            }
            else if (_isInitialized && !_isRun) Run();

            if (!debug || !Application.isPlaying) return;

            if (Progress < 100)
            {
                Progress += 0.5f;
            }
            else if (ProgressStatus != "Loaded")
            {
                ProgressStatus = "Loaded";
                Version += " (Core)";

                ShowScreenLoader();
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            UnSubscribeEvents();
        }

        private void Run()
        {
            if (_isRun) return;
            _isRun = true;

            LoadScene(new Uri("stream://localhost/main-module"), "MainModule", () =>
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

                        Debug.Log(resourceKey + "::" + Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                        UnLoadResourceByTag(resourceTag);
                        // UnLoadResourceByKey(resourceKey, resourceTag);
                    }
                );
            }, true);
        }

        [UsedImplicitly]
        public bool IsShowScreenUi => _openLoaderUi && _openLoaderUi.gameObject.activeSelf;

        [UsedImplicitly]
        public void ShowLoaderUi()
        {
            if (_openLoaderUi.gameObject.activeSelf) return;

            _openLoaderUi.gameObject.SetActive(true);
        }

        [UsedImplicitly]
        public void HideLoaderUi()
        {
            if (!_openLoaderUi.gameObject.activeSelf) return;

            _openLoaderUi.gameObject.SetActive(false);
        }

        [UsedImplicitly]
        public bool IsShowScreenLoader => _screenLoader && _screenLoader.gameObject.activeSelf && ScreenColor.a > 0f;

        [UsedImplicitly]
        public void ShowScreenLoader()
        {
            if (!_screenLoader.gameObject.activeSelf) _screenLoader.gameObject.SetActive(true);
            if (_screenLoader.gameObject.activeSelf && ScreenColor.a < 1) RunScreenLoaderEffect(true);
        }

        [UsedImplicitly]
        public void HideScreenLoader()
        {
            if (!_screenLoader.gameObject.activeSelf || ScreenColor.a <= 0) return;
            RunScreenLoaderEffect(false);
        }

        #region ScreenLoaderEffect

        private Coroutine _screenLoaderEffect;
        private bool _screenLoaderEffectDirection;

        private void RunScreenLoaderEffect(bool direction)
        {
            _screenLoaderEffectDirection = direction;

            if (_screenLoaderEffect == null)
                _screenLoaderEffect = StartCoroutine(ScreenLoaderEffect());
        }

        private IEnumerator ScreenLoaderEffect()
        {
            if (_screenLoaderEffectDirection && ScreenColor.a >= 1
                || _screenLoaderEffectDirection == false && ScreenColor.a <= 0)
            {
                if (_screenLoaderEffectDirection == false && ScreenColor.a <= 0)
                    _screenLoader.gameObject.SetActive(false);
                _screenLoaderEffect = null;
                yield break;
            }

            while (_screenLoaderEffectDirection && ScreenColor.a < 1
                   || _screenLoaderEffectDirection == false && ScreenColor.a > 0)
            {
                yield return new WaitForEndOfFrame();
                _screenLoader.color = _screenLoaderEffectDirection
                    ? new Color(ScreenColor.r, ScreenColor.g, ScreenColor.b, ScreenColor.a + 0.05f)
                    : new Color(ScreenColor.r, ScreenColor.g, ScreenColor.b, ScreenColor.a - 0.05f);
            }

            _screenLoaderEffect = null;
        }

        #endregion
    }
}