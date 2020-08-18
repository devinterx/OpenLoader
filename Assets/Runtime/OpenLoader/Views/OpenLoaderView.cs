using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUniverse.Runtime.OpenLoader.Views
{
    public class OpenLoaderView : MonoBehaviour, IOpenLoaderView
    {
        private const string LoaderUiName = "LoaderUi";
        private const string ScreenName = "LoaderScreen";
        private const string VersionName = "Version";
        private const string ProgressBarName = "ProgressBar";
        private const string ProgressBarLabelName = "Label";

        public string Version
        {
            get => _version != null ? _version.text : "";
            set
            {
                if (_version != null) _version.text = value;
            }
        }

        public float Progress
        {
            get => _slider != null ? _slider.value : 0.0f;
            set
            {
                if (_slider != null) _slider.value = value;
            }
        }

        public string ProgressStatus
        {
            get => _sliderLabel != null ? _sliderLabel.text : "";
            set
            {
                if (_sliderLabel != null) _sliderLabel.text = value;
            }
        }

        private Transform _openLoader;
        private Transform _openLoaderUi;
        private Image _screenLoader;
        private Color ScreenColor => _screenLoader != null ? _screenLoader.color : new Color(0, 0, 0, 1.0f);
        private Slider _slider;
        private TMP_Text _sliderLabel;
        private TMP_Text _version;

        private void Awake()
        {
            if (_openLoader == null) _openLoader = transform;
            if (_openLoader == null) return;

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

            if (_openLoaderUi == null) return;

            if (_version == null)
                _version = _openLoaderUi.Find(VersionName)?.GetComponent<TMP_Text>();
            if (_slider == null)
                _slider = _openLoaderUi.Find(ProgressBarName)?.GetComponent<Slider>();
            if (_slider != null && _sliderLabel == null)
                _sliderLabel = _slider.transform.Find(ProgressBarLabelName)?.GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (OpenLoaderSystem.Instance == null) return;
            if (!OpenLoaderSystem.Instance.debug || !Application.isPlaying) return; // TODO:: not implemented

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

        public bool IsShowScreenUi => _openLoaderUi && _openLoaderUi.gameObject.activeSelf;

        public void ShowLoaderUi()
        {
            if (_openLoaderUi.gameObject.activeSelf) return;

            _openLoaderUi.gameObject.SetActive(true);
        }

        public void HideLoaderUi()
        {
            if (!_openLoaderUi.gameObject.activeSelf) return;

            _openLoaderUi.gameObject.SetActive(false);
        }

        public bool IsShowScreenLoader => _screenLoader && _screenLoader.gameObject.activeSelf && ScreenColor.a > 0f;

        public void ShowScreenLoader()
        {
            if (!_screenLoader.gameObject.activeSelf) _screenLoader.gameObject.SetActive(true);
            if (_screenLoader.gameObject.activeSelf && ScreenColor.a < 1) RunScreenLoaderEffect(true);
        }

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

        public static GameObject InstantiateView()
        {
            var openLoaderViewPrefab = Resources.Load<GameObject>("prefabs/OpenLoaderView");
            if (openLoaderViewPrefab == null)
                throw new FileNotFoundException("OpenLoader: no open loader prefab found.");

            var loaderView = Instantiate(openLoaderViewPrefab, Vector3.zero, Quaternion.identity);
            loaderView.name = "OpenLoaderView (Canvas)";

            return loaderView;
        }
    }
}
