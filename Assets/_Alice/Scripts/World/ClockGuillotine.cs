using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A marked mechanism for the clock (A04, GDD section 5): a heavy blade that drops across the passage and
    /// lifts too briefly to run under. It runs on its own clock. The clock ability holds it back: the mechanism
    /// keeps running until the blade is up, then the blade stays up until the hold ends, so Alice always gets
    /// the time to pass. The global time scale is never touched. A blade that comes down on Alice hurts and
    /// throws her back.
    /// </summary>
    public sealed class ClockGuillotine : MonoBehaviour, IDelayableMechanism
    {
        [SerializeField] private Transform _blade;
        [SerializeField] private Collider2D _bladeBody;
        [Tooltip("Blade raised: passable.")]
        [SerializeField, Min(0.05f)] private float _openTime = 0.35f;
        [SerializeField, Min(0.05f)] private float _closedTime = 1.6f;
        [SerializeField, Min(0.02f)] private float _moveTime = 0.12f;
        [Tooltip("How high the blade lifts, in tiles.")]
        [SerializeField, Min(0.5f)] private float _lift = 5f;
        [SerializeField, Min(1)] private int _damage = 20;
        [SerializeField] private Knockback _knockback = new Knockback(new Vector2(7f, 4f), 0.22f);

        private float _clock;
        private float _delayedUntil = float.NegativeInfinity;
        private bool _wasDown;
        private float _restY;

        public bool IsDelayed => Time.time < _delayedUntil;

        public void Bind(Transform blade, Collider2D bladeBody)
        {
            _blade = blade;
            _bladeBody = bladeBody;
        }

        public void Delay(float seconds)
        {
            _delayedUntil = Time.time + seconds;
        }

        private float Cycle => _openTime + _closedTime + _moveTime * 2f;

        private void Awake()
        {
            _restY = _blade != null ? _blade.localPosition.y : 0f;
        }

        private void FixedUpdate()
        {
            bool heldOpen = IsDelayed && IsOpenAt(_clock);
            if (!heldOpen)
            {
                _clock = (_clock + Time.fixedDeltaTime) % Cycle;
            }

            float raise = RaiseAt(_clock);
            if (_blade != null)
            {
                Vector3 local = _blade.localPosition;
                local.y = _restY + raise * _lift;
                _blade.localPosition = local;
            }

            bool down = raise <= 0.05f;
            if (down && !_wasDown)
            {
                CrushAlice();
            }

            _wasDown = down;
        }

        private bool IsOpenAt(float t)
        {
            return t >= _moveTime && t < _moveTime + _openTime;
        }

        /// <summary>0 = down across the passage, 1 = raised.</summary>
        private float RaiseAt(float t)
        {
            if (t < _moveTime)
            {
                return t / _moveTime;
            }

            t -= _moveTime;
            if (t < _openTime)
            {
                return 1f;
            }

            t -= _openTime;
            if (t < _moveTime)
            {
                return 1f - t / _moveTime;
            }

            return 0f;
        }

        private void CrushAlice()
        {
            Transform alice = PlayerPresence.Current;
            if (alice == null || _bladeBody == null || !alice.TryGetComponent(out Collider2D body)
                || !alice.TryGetComponent(out IDamageable target) || !_bladeBody.bounds.Intersects(body.bounds))
            {
                return;
            }

            int away = alice.position.x >= transform.position.x ? 1 : -1;
            target.ReceiveHit(new HitInfo(_damage, away, gameObject, knockback: _knockback));
        }
    }
}
