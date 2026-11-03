using System.Collections.Generic;
using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// The needle thrust on the ground and in the air. Movement input is locked for the whole attack, so
    /// recovery is a real commitment. Alice has no thrust frames yet; a grey-box needle shows the blow.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private PlayerCombatSettings _settings;
        [SerializeField] private SpriteRenderer _thrustMarker;

        private readonly HashSet<IDamageable> _struck = new HashSet<IDamageable>();
        private readonly Collider2D[] _overlaps = new Collider2D[8];

        private PlayerMotor _motor;
        private IPlayerCommandSource _commands;
        private float _consumedAttackAt = float.NegativeInfinity;
        private float _startedAt;
        private int _facing;

        public bool IsAttacking { get; private set; }

        public AttackPhase Phase => IsAttacking ? _settings.Thrust.PhaseAt(Time.time - _startedAt) : AttackPhase.None;

        public void Bind(PlayerCombatSettings settings, SpriteRenderer thrustMarker)
        {
            _settings = settings;
            _thrustMarker = thrustMarker;
        }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _commands = GetComponent<IPlayerCommandSource>();
            if (_settings == null)
            {
                Debug.LogError("PlayerAttack needs combat settings.", this);
                enabled = false;
                return;
            }

            ShowMarker(false);
        }

        /// <summary>Drops the attack, e.g. when Alice is hit out of it.</summary>
        public void Cancel()
        {
            IsAttacking = false;
            ShowMarker(false);
        }

        private void FixedUpdate()
        {
            float now = Time.time;
            if (!IsAttacking)
            {
                TryStart(now);
                return;
            }

            AttackPhase phase = Phase;
            if (phase == AttackPhase.None)
            {
                Cancel();
                TryStart(now);
                return;
            }

            ShowMarker(phase == AttackPhase.Active);
            if (phase == AttackPhase.Active)
            {
                float length = _settings.ThrustReach - _settings.ThrustReachStart;
                Vector2 center = _motor.Position + new Vector2(_facing * (_settings.ThrustReachStart + length * 0.5f),
                    _settings.ThrustCenterHeight);
                HitInfo hit = new HitInfo(_settings.ThrustDamage, _facing, gameObject);
                MeleeHitbox.Strike(center, new Vector2(length, _settings.ThrustHeight), hit, gameObject, _struck, _overlaps);
            }
        }

        private void TryStart(float now)
        {
            if (_commands == null || _motor.IsSliding || _motor.IsLocked
                || !InputBuffer.IsFresh(_commands.AttackPressedAt, _consumedAttackAt, now, _settings.AttackBufferTime))
            {
                return;
            }

            _consumedAttackAt = _commands.AttackPressedAt;
            _startedAt = now;
            _facing = _motor.Facing;
            _struck.Clear();
            IsAttacking = true;
            _motor.Lock(_settings.Thrust.Duration);
            PlaceMarker();
        }

        private void PlaceMarker()
        {
            if (_thrustMarker == null)
            {
                return;
            }

            float length = _settings.ThrustReach - _settings.ThrustReachStart;
            _thrustMarker.size = new Vector2(length, 1f / WorldMetrics.PixelsPerUnit);
            _thrustMarker.transform.localPosition = new Vector3(_facing * (_settings.ThrustReachStart + length * 0.5f),
                _settings.ThrustCenterHeight, 0f);
        }

        private void ShowMarker(bool visible)
        {
            if (_thrustMarker != null)
            {
                _thrustMarker.enabled = visible;
            }
        }
    }
}
