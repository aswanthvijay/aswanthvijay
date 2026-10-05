using Runeheir.Controls;
using UnityEngine;

namespace Runeheir.Cameras
{
    /// <summary>
    /// 2.5D isometric follow camera (GDD Phase 2 Step 2): Pitch -45°, Yaw 45°, smooth follow,
    /// mouse-wheel zoom and Ragnarok-style right-drag rotation.
    /// Put it on the Main Camera and assign a target (or call <see cref="SetTarget"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Tooltip("Point on the target the camera looks at (chest height).")]
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1f, 0f);

        [Header("Angles (GDD: Pitch -45°, Yaw 45°)")]
        [Tooltip("Negative = looking down. -45 is the GDD isometric angle.")]
        [SerializeField, Range(-89f, -10f)] private float pitch = -45f;

        [SerializeField] private float yaw = 45f;

        [Header("Distance / Zoom")]
        [SerializeField, Min(1f)] private float distance = 18f;
        [SerializeField, Min(1f)] private float minDistance = 9f;
        [SerializeField, Min(1f)] private float maxDistance = 32f;
        [SerializeField, Min(0.1f)] private float zoomStep = 2f;
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.12f;

        [Header("Follow")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.1f;

        [Header("Lens")]
        [Tooltip("Perspective with a narrow FOV gives the HD-XileRO look; orthographic is a true isometric.")]
        [SerializeField] private bool orthographic;

        [SerializeField, Range(10f, 60f)] private float fieldOfView = 30f;

        [Tooltip("Orthographic size per meter of distance.")]
        [SerializeField] private float orthoSizePerDistance = 0.3f;

        [Header("Rotation (hold right mouse and drag)")]
        [SerializeField] private bool allowYawRotation = true;
        [SerializeField] private float rotateDegreesPerPixel = 0.3f;

        private Camera _camera;
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _currentDistance;
        private float _targetDistance;
        private float _distanceVelocity;
        private float _currentYaw;
        private bool _rotating;
        private Vector2 _lastPointer;
        private float _shakeUntil;
        private float _shakeDuration;
        private float _shakeStrength;

        public Transform Target => target;

        public float Yaw => _currentYaw;

        public Camera Camera => _camera;

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap && target != null)
            {
                _focus = target.position + focusOffset;
                _focusVelocity = Vector3.zero;
                ApplyTransform();
            }
        }

        /// <summary>Back to the GDD angles and default zoom.</summary>
        public void ResetView()
        {
            _currentYaw = yaw;
            _targetDistance = distance;
        }

        /// <summary>Screen shake (Fist of Odin, MVP stomps).</summary>
        public void Shake(float strength, float duration)
        {
            _shakeStrength = strength;
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeUntil = Time.time + _shakeDuration;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _currentYaw = yaw;
            _targetDistance = _currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            _focus = target != null ? target.position + focusOffset : transform.position + transform.forward * distance;
            ApplyLens();
            ApplyTransform();
        }

        private void LateUpdate()
        {
            HandleInput();

            if (target != null)
            {
                Vector3 desired = target.position + focusOffset;
                _focus = followSmoothTime > 0f
                    ? Vector3.SmoothDamp(_focus, desired, ref _focusVelocity, followSmoothTime)
                    : desired;
            }

            _currentDistance = zoomSmoothTime > 0f
                ? Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distanceVelocity, zoomSmoothTime)
                : _targetDistance;

            ApplyLens();
            ApplyTransform();
        }

        private void HandleInput()
        {
            bool overUI = UIFocus.PointerOverUI;

            float scroll = GameInput.ScrollSteps;
            if (scroll != 0f && !overUI)
            {
                _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomStep, minDistance, maxDistance);
            }

            if (!allowYawRotation)
            {
                return;
            }

            Vector2 pointer = GameInput.PointerPosition;
            if (GameInput.SecondaryDown && !overUI)
            {
                _rotating = true;
                _lastPointer = pointer;
            }

            if (_rotating && GameInput.SecondaryHeld)
            {
                _currentYaw += (pointer.x - _lastPointer.x) * rotateDegreesPerPixel;
                _lastPointer = pointer;
            }

            if (!GameInput.SecondaryHeld)
            {
                _rotating = false;
            }
        }

        private void ApplyLens()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.orthographic = orthographic;
            if (orthographic)
            {
                _camera.orthographicSize = _currentDistance * orthoSizePerDistance;
            }
            else
            {
                _camera.fieldOfView = fieldOfView;
            }
        }

        private void ApplyTransform()
        {
            // Unity's X rotation is positive when looking down, hence -pitch.
            Quaternion rotation = Quaternion.Euler(-pitch, _currentYaw, 0f);
            Vector3 position = _focus - rotation * Vector3.forward * _currentDistance;

            if (Time.time < _shakeUntil)
            {
                float falloff = (_shakeUntil - Time.time) / _shakeDuration;
                position += Random.insideUnitSphere * (_shakeStrength * falloff);
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        private void OnValidate()
        {
            maxDistance = Mathf.Max(maxDistance, minDistance);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        [ContextMenu("Snap To Target")]
        private void SnapToTarget()
        {
            _camera = GetComponent<Camera>();
            _currentYaw = yaw;
            _currentDistance = _targetDistance = distance;
            _focus = target != null ? target.position + focusOffset : _focus;
            ApplyLens();
            ApplyTransform();
        }
    }
}
