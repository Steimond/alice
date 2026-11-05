using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Smelling salts (GDD section 4): Alice stands still, holds the vial under her nose and her clarity
    /// comes back with the sniff. A hit before that wastes the charge. All charges return at a rest, and a
    /// defeat drags her back to one.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerSalts : MonoBehaviour
    {
        [SerializeField] private PlayerCombatSettings _settings;

        private PlayerMotor _motor;
        private Health _health;
        private IPlayerCommandSource _commands;
        private PlayerAttack _attack;
        private PlayerParry _parry;
        private PlayerDamageReaction _damage;
        private float _consumedPressAt = float.NegativeInfinity;
        private float _startedAt;
        private bool _applied;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public int Charges { get; private set; }
        public int MaxCharges => _settings.SaltCharges;
        public bool IsUsing { get; private set; }

        /// <summary>0 at the start of the use, 1 at its end.</summary>
        public float Progress => IsUsing ? Mathf.Clamp01((Time.time - _startedAt) / _settings.SaltTime) : 0f;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _health = GetComponent<Health>();
            _commands = GetComponent<IPlayerCommandSource>();
            _attack = GetComponent<PlayerAttack>();
            _parry = GetComponent<PlayerParry>();
            _damage = GetComponent<PlayerDamageReaction>();
            if (_settings == null)
            {
                Debug.LogError("PlayerSalts needs combat settings.", this);
                enabled = false;
                return;
            }

            Charges = _settings.SaltCharges;
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            WorldReset.Happened += Refill;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            WorldReset.Happened -= Refill;
        }

        public void Refill()
        {
            Charges = _settings.SaltCharges;
        }

        private void FixedUpdate()
        {
            if (IsUsing)
            {
                if (!_applied && Progress >= _settings.SaltTakesEffectAt)
                {
                    _applied = true;
                    _health.Restore(Mathf.CeilToInt(_health.Max * _settings.SaltRestoreShare));
                }

                if (Progress >= 1f)
                {
                    IsUsing = false;
                }

                return;
            }

            TryStart();
        }

        private void TryStart()
        {
            if (_commands == null
                || !InputBuffer.IsFresh(_commands.HealPressedAt, _consumedPressAt, Time.time, _settings.SaltBufferTime))
            {
                return;
            }

            bool busy = !_motor.IsGrounded || _motor.IsLocked || _motor.IsSliding || _motor.RunOnly
                || (_attack != null && _attack.IsAttacking) || (_parry != null && _parry.IsParrying)
                || (_damage != null && _damage.IsDefeated);
            // Busy or an empty pocket: the press is kept for a moment in case that changes. A clear head is no
            // excuse: a mistimed sniff at full clarity wastes the charge, so every misclick costs (GDD 0.20).
            if (busy || Charges <= 0)
            {
                return;
            }

            _consumedPressAt = _commands.HealPressedAt;
            Charges--;
            IsUsing = true;
            _applied = false;
            _startedAt = Time.time;
            _motor.Lock(_settings.SaltTime);
        }

        private void OnDamaged(HitInfo hit)
        {
            // Knocked out of it before the sniff, the charge is spent for nothing.
            IsUsing = false;
        }
    }
}
