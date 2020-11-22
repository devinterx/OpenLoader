using System;
using UnityEngine;

namespace OpenUniverse.CameraManager
{
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 5.0f;
        public float xSpeed = 120.0f;
        public float ySpeed = 120.0f;

        public float yMinLimit = -20f;
        public float yMaxLimit = 80f;

        public float zoomStep = 1f;

        public float distanceMin = .5f;
        public float distanceMax = 15f;
        private float _x;
        private float _y;

        private void LateUpdate()
        {
            if (!target || !Input.GetButton("Fire1") && Math.Abs(Input.mouseScrollDelta.y) < 0.001f) return;
        
            var targetPosition = target.position;
        
            _x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            _y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        
            _y = ClampAngle(_y, yMinLimit, yMaxLimit);
        
            var rotation = Quaternion.Euler(_y, _x, 0);
        
            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * zoomStep, distanceMin, distanceMax);
        
            if (Physics.Linecast(targetPosition, transform.position, out var hit))
            {
                distance -= hit.distance;
            }
        
            var negotiateDistance = new Vector3(0.0f, 0.0f, -distance);
            var position = rotation * negotiateDistance + targetPosition;
        
            transform.rotation = rotation;
            // var offsetZ = distance * Mathf.Tan(Mathf.Deg2Rad * 90f - 70f);
            // transform.position = new Vector3(position.x, position.y, position.z - offsetZ);
            transform.position = position;
        }
        
        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
        
            return Mathf.Clamp(angle, min, max);
        }
    }
}
