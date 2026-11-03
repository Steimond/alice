using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Readable hit feedback without extra frames: a short tint on every landed hit and, for Alice,
    /// blinking while the post-hit protection lasts.
    /// </summary>
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _flashColor = new Color(1f, 0.45f, 0.45f);
        [SerializeField, Min(0f)] private float _flashTime = 0.08f;
        [SerializeField] private bool _blinkWhileRecovering;
        [SerializeField, Min(0.01f)] private float _blinkPeriod = 0.1f;

        private float _flashUntil;

        public void Bind(Health health, SpriteRenderer renderer, bool blinkWhileRecovering)
        {
            _health = health;
            _renderer = renderer;
            _blinkWhileRecovering = blinkWhileRecovering;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
            }
        }

        private void OnDamaged(HitInfo hit)
        {
            _flashUntil = Time.time + _flashTime;
        }

        private void LateUpdate()
        {
            if (_renderer == null || _health == null)
            {
                return;
            }

            Color color = Time.time < _flashUntil ? _flashColor : Color.white;
            if (_blinkWhileRecovering && _health.IsRecovering && !_health.IsDead)
            {
                bool hidden = Mathf.FloorToInt(Time.time / _blinkPeriod) % 2 == 1;
                color.a = hidden ? 0.35f : 1f;
            }

            _renderer.color = color;
        }
    }
}
