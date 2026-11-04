using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Parry on its own button (GDD section 4). A blow from the front in the short window at the start is
    /// turned aside: it does no damage, the attacker is thrown open and Alice's next attack becomes the
    /// counter. A parry that catches nothing leaves her standing open until it ends.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerParry : MonoBehaviour, IHitInterceptor
    {
        [SerializeField] private PlayerCombatSettings _settings;

        private PlayerMotor _motor;
        private PlayerAttack _attack;
        private IPlayerCommandSource _commands;
        private float _consumedParryAt = float.NegativeInfinity;
        private float _startedAt;
        private float _endsAt;
        private float _counterUntil;

        public bool IsParrying { get; private set; }
        public bool Succeeded { get; private set; }
        public float SucceededAt { get; private set; }
        public int Facing { get; private set; } = 1;
        public float Elapsed => Time.time - _startedAt;

        /// <summary>True while the next attack is the counter after a successful parry.</summary>
        public bool CounterReady => Time.time < _counterUntil;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _attack = GetComponent<PlayerAttack>();
            _commands = GetComponent<IPlayerCommandSource>();
            if (_settings == null)
            {
                Debug.LogError("PlayerParry needs combat settings.", this);
                enabled = false;
            }
        }

        /// <summary>Spends the counter, so one parry gives one counter.</summary>
        public void ConsumeCounter()
        {
            _counterUntil = 0f;
        }

        public bool TryIntercept(HitInfo hit)
        {
            // The blow sets how early a parry may start; a blow pushing Alice backwards came from the side she faces.
            if (!IsParrying || Succeeded || hit.ParryWindow <= 0f || Elapsed > hit.ParryWindow || hit.DirectionX == Facing)
            {
                return false;
            }

            Succeeded = true;
            SucceededAt = Time.time;
            _endsAt = Time.time + _settings.ParrySuccessHold;
            _counterUntil = Time.time + _settings.CounterWindow;
            _motor.ShortenLock(_settings.ParrySuccessHold);
            if (hit.Source != null && hit.Source.TryGetComponent(out IParryable attacker))
            {
                attacker.OnParried(gameObject);
            }

            return true;
        }

        private void FixedUpdate()
        {
            float now = Time.time;
            if (IsParrying && now >= _endsAt)
            {
                IsParrying = false;
            }

            if (IsParrying || _commands == null
                || !InputBuffer.IsFresh(_commands.ParryPressedAt, _consumedParryAt, now, _settings.ParryBufferTime)
                || _motor.IsSliding || _motor.IsLocked || !_motor.IsGrounded
                || (_attack != null && _attack.IsAttacking))
            {
                return;
            }

            _consumedParryAt = _commands.ParryPressedAt;
            IsParrying = true;
            Succeeded = false;
            Facing = _motor.Facing;
            _startedAt = now;
            _endsAt = now + _settings.ParryDuration;
            _motor.Lock(_settings.ParryDuration);
        }
    }
}
