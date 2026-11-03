using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// What a landed hit does to Alice: knockback away from the blow, a dropped attack and, at zero health,
    /// waking up again. Rest points arrive in M3, so until then she wakes where the room started her.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerDamageReaction : MonoBehaviour
    {
        [SerializeField] private PlayerCombatSettings _settings;

        private PlayerMotor _motor;
        private PlayerAttack _attack;
        private Health _health;
        private Vector2 _wakePosition;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _attack = GetComponent<PlayerAttack>();
            _health = GetComponent<Health>();
            _wakePosition = transform.position;
            if (_settings == null)
            {
                Debug.LogError("PlayerDamageReaction needs combat settings.", this);
                enabled = false;
                return;
            }

            _health.Initialize(_settings.MaxHealth, _settings.HitInvulnerability);
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDamaged(HitInfo hit)
        {
            if (_attack != null)
            {
                _attack.Cancel();
            }

            if (_health.IsDead)
            {
                return;
            }

            Vector2 push = _settings.KnockbackVelocity;
            _motor.Knockback(new Vector2(hit.DirectionX * push.x, push.y), _settings.KnockbackTime);
        }

        private void OnDied(HitInfo hit)
        {
            _motor.Teleport(_wakePosition);
            _motor.Freeze(_settings.DefeatFreezeTime);
            _health.Refill();
        }
    }
}
