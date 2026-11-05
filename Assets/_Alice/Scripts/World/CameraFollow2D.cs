using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// Follows a target with look-ahead in the movement direction and stays inside room bounds. Impacts
    /// shake it by whole screen pixels for a moment, so the picture stays on the pixel grid.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        private const float LookAheadSpeedThreshold = 0.5f;

        [SerializeField] private Transform _target;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 2.5f);
        [SerializeField, Min(0f)] private float _lookAhead = 2.5f;
        [SerializeField, Min(0f)] private float _lookAheadSmoothTime = 0.4f;
        [SerializeField, Min(0f)] private float _smoothTime = 0.12f;
        [SerializeField] private Rect _bounds = new Rect(-20f, -10f, 40f, 20f);
        [Tooltip("How long a shake lasts; it fades over that time.")]
        [SerializeField, Min(0.01f)] private float _shakeTime = 0.14f;
        [SerializeField, Min(0f)] private float _maxShakePixels = 3f;

        private Camera _camera;
        private Rigidbody2D _targetBody;
        private Vector3 _velocity;
        private Vector3 _base;
        private bool _hasBase;
        private float _shakePixels;
        private float _shakeStartedAt = float.NegativeInfinity;
        private float _currentLookAhead;
        private float _lookAheadVelocity;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public Rect Bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            Impacts.Happened += Shake;
        }

        private void OnDisable()
        {
            Impacts.Happened -= Shake;
        }

        /// <summary>Jumps straight to the target, as after a change of room, instead of gliding across.</summary>
        public void Snap()
        {
            if (_target == null || _camera == null)
            {
                return;
            }

            _currentLookAhead = 0f;
            _lookAheadVelocity = 0f;
            _velocity = Vector3.zero;
            Vector3 desired = Clamp(_target.position + (Vector3)_offset);
            desired.z = transform.position.z;
            _base = desired;
            _hasBase = true;
            transform.position = desired;
        }

        public void Shake(float pixels)
        {
            bool running = Time.time - _shakeStartedAt < _shakeTime;
            _shakePixels = Mathf.Min(_maxShakePixels, running ? Mathf.Max(_shakePixels, pixels) : pixels);
            _shakeStartedAt = Time.time;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            // Something else (a teleport, the automation menu) may have moved the camera since last frame.
            if (!_hasBase || (transform.position - _base).sqrMagnitude > 1f)
            {
                _base = transform.position;
                _hasBase = true;
            }

            if (_targetBody == null || _targetBody.transform != _target)
            {
                _targetBody = _target.GetComponent<Rigidbody2D>();
            }

            float speedX = _targetBody != null ? _targetBody.linearVelocity.x : 0f;
            if (Mathf.Abs(speedX) > LookAheadSpeedThreshold)
            {
                _currentLookAhead = Mathf.SmoothDamp(
                    _currentLookAhead, Mathf.Sign(speedX) * _lookAhead, ref _lookAheadVelocity, _lookAheadSmoothTime);
            }

            Vector3 desired = _target.position + (Vector3)_offset + new Vector3(_currentLookAhead, 0f, 0f);
            desired = Clamp(desired);
            desired.z = transform.position.z;
            _base = Vector3.SmoothDamp(_base, desired, ref _velocity, _smoothTime);
            transform.position = _base + ShakeOffset();
        }

        private Vector3 ShakeOffset()
        {
            float elapsed = Time.time - _shakeStartedAt;
            if (elapsed >= _shakeTime)
            {
                return Vector3.zero;
            }

            int pixels = Mathf.RoundToInt(_shakePixels * (1f - elapsed / _shakeTime));
            if (pixels <= 0)
            {
                return Vector3.zero;
            }

            float unit = 1f / WorldMetrics.PixelsPerUnit;
            return new Vector3(Random.Range(-pixels, pixels + 1) * unit, Random.Range(-pixels, pixels + 1) * unit, 0f);
        }

        private Vector3 Clamp(Vector3 position)
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            position.x = ClampAxis(position.x, _bounds.xMin + halfWidth, _bounds.xMax - halfWidth);
            position.y = ClampAxis(position.y, _bounds.yMin + halfHeight, _bounds.yMax - halfHeight);
            return position;
        }

        private static float ClampAxis(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }
    }
}
