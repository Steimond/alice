using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// What a landed hit does to Alice: knockback away from the blow, a dropped attack and a flinch. At zero
    /// health the defeat of GDD section 4 plays out: she collapses, the screen goes dark, she is moved to the
    /// rest and the world resets, then she wakes with a sharp breath and gets up. Rest points arrive in M3,
    /// so until then she wakes where the room started her.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerDamageReaction : MonoBehaviour, IHitImmunity
    {
        public enum DefeatStage
        {
            None,
            Falling,
            Dark,
            Waking
        }

        [SerializeField] private PlayerCombatSettings _settings;

        private PlayerMotor _motor;
        private PlayerAttack _attack;
        private Health _health;
        private Vector2 _wakePosition;
        private int _wakeFacing;
        private float _stageStartedAt;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public DefeatStage Stage { get; private set; }
        public float StageTime => Time.time - _stageStartedAt;
        public bool IsDefeated => Stage != DefeatStage.None;
        public float HurtAt { get; private set; } = float.NegativeInfinity;
        public bool IsHurt => !IsDefeated && Time.time - HurtAt < _settings.HurtTime;

        /// <summary>0 = clear, 1 = black; read by the screen fade.</summary>
        public float Darkness
        {
            get
            {
                switch (Stage)
                {
                    case DefeatStage.Falling:
                        float fadeFrom = _settings.DefeatFallTime - _settings.DefeatFadeTime;
                        return Mathf.Clamp01((StageTime - fadeFrom) / _settings.DefeatFadeTime);
                    case DefeatStage.Dark:
                        return 1f;
                    case DefeatStage.Waking:
                        return 1f - Mathf.Clamp01(StageTime / _settings.WakeFadeTime);
                    default:
                        return 0f;
                }
            }
        }

        // Nothing touches her while she lies there or gets up.
        bool IHitImmunity.IsImmune => IsDefeated;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _attack = GetComponent<PlayerAttack>();
            _health = GetComponent<Health>();
            _wakePosition = transform.position;
            _wakeFacing = _motor.Facing;
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

        private void FixedUpdate()
        {
            switch (Stage)
            {
                case DefeatStage.Falling:
                    if (StageTime >= _settings.DefeatFallTime)
                    {
                        WakeAtRest();
                    }

                    break;
                case DefeatStage.Dark:
                    if (StageTime >= _settings.DefeatDarkTime)
                    {
                        Enter(DefeatStage.Waking);
                    }

                    break;
                case DefeatStage.Waking:
                    if (StageTime >= _settings.WakeTime)
                    {
                        Enter(DefeatStage.None);
                    }

                    break;
            }
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

            HurtAt = Time.time;
            Vector2 push = _settings.KnockbackVelocity;
            _motor.Knockback(new Vector2(hit.DirectionX * push.x, push.y), _settings.KnockbackTime);
        }

        private void OnDied(HitInfo hit)
        {
            Enter(DefeatStage.Falling);
            _motor.Lock(_settings.DefeatFallTime + _settings.DefeatDarkTime + _settings.WakeTime);
        }

        /// <summary>In full darkness: back at the rest, whole again, and the world reset around her.</summary>
        private void WakeAtRest()
        {
            Enter(DefeatStage.Dark);
            _motor.Teleport(_wakePosition);
            _motor.Face(_wakeFacing);
            _health.Refill();
            WorldReset.Raise();
        }

        private void Enter(DefeatStage stage)
        {
            Stage = stage;
            _stageStartedAt = Time.time;
        }
    }
}
