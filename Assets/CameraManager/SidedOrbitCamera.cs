using JetBrains.Annotations;
using UnityEngine;

namespace OpenUniverse.CameraManager
{
    public class SidedOrbitCamera : MonoBehaviour
    {
        private bool _isDirty = true;

        [SerializeField]
        private GameObject target;

        [SerializeField, SerializeProperty("HorizontalOffset")]
        private HorizontalOrbitCameraOffset horizontalOffset = HorizontalOrbitCameraOffset.None;

        [UsedImplicitly]
        private HorizontalOrbitCameraOffset HorizontalOffset
        {
            get => horizontalOffset;
            set
            {
                horizontalOffset = value;
                _isDirty = true;
            }
        }

        private Camera _camera;
        private Vector3 _cameraOffset = new Vector3(0, 0, 0);
        private Transform _cameraTransform;
        private Transform _originTransform;

        public void Awake()
        {
            if (_camera == null) _camera = transform.GetComponent<Camera>();
            if (_cameraTransform == null) _cameraTransform = transform;
            if (_originTransform == null) _originTransform = new GameObject().transform;
        }

        public void Update()
        {
            if (_camera == null || target == null || !_isDirty) return;

            var originCameraPosition = _cameraTransform.position - _cameraTransform.TransformDirection(_cameraOffset);

            _originTransform.position = originCameraPosition;

            var distance = Vector3.Distance(
                _originTransform.InverseTransformPoint(originCameraPosition),
                _originTransform.InverseTransformPoint(target.transform.position)
            );

            var frustumHeight = 2.0f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var frustumWidth = frustumHeight * _camera.aspect;

            _cameraOffset.x = horizontalOffset == HorizontalOrbitCameraOffset.FullLeftSide ? frustumWidth / 2
                : horizontalOffset == HorizontalOrbitCameraOffset.HalfLeftSide ? frustumWidth / 4
                : horizontalOffset == HorizontalOrbitCameraOffset.FullRightSide ? -frustumWidth / 2
                : horizontalOffset == HorizontalOrbitCameraOffset.HalfRightSide ? -frustumWidth / 4 : 0;

            _cameraTransform.position = originCameraPosition + _cameraTransform.TransformDirection(_cameraOffset);

            _isDirty = false;
        }
    }

    internal enum HorizontalOrbitCameraOffset
    {
        FullLeftSide,
        HalfLeftSide,
        None,
        HalfRightSide,
        FullRightSide
    }
}
