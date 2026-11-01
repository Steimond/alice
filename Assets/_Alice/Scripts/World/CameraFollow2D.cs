using UnityEngine;

namespace Alice.World
{
    /// <summary>Follows a target with look-ahead in the movement direction and stays inside room bounds.</summary>
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

        private Camera _camera;
        private Rigidbody2D _targetBody;
        private Vector3 _velocity;
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

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
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
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);
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
