using System.Collections.Generic;
using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    public enum PlayerStrike
    {
        None,
        Thrust,
        Slash,
        AirSlash,
        Counter,
        Execution
    }

    /// <summary>
    /// Needle attacks (GDD section 4): on the ground a short combo — thrust, then a slash if Attack is
    /// pressed again before the thrust has recovered; in the air a slash. Right after a parry the attack is a deep
    /// counter thrust. Interact next to a stunned enemy executes it, in sync with the enemy and safe from
    /// other blows; the execution is a choice, so Attack never starts one. Movement input is locked for the whole
    /// strike, so recovery is a real commitment; only the combo cuts it short.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAttack : MonoBehaviour, IHitImmunity
    {
        [SerializeField] private PlayerCombatSettings _settings;

        private readonly HashSet<IDamageable> _struck = new HashSet<IDamageable>();
        private readonly Collider2D[] _overlaps = new Collider2D[8];

        private PlayerMotor _motor;
        private IPlayerCommandSource _commands;
        private PlayerParry _parry;
        private float _consumedAttackAt = float.NegativeInfinity;
        private float _consumedInteractAt = float.NegativeInfinity;
        private float _startedAt;
        private int _facing;
        private bool _comboQueued;
        private MeleeAttackTimeline _timeline;

        public bool IsAttacking => Strike != PlayerStrike.None;
        public PlayerStrike Strike { get; private set; }

        /// <summary>ID of the execution being played, for the matching frames.</summary>
        public string ExecutionId { get; private set; }

        public AttackPhase Phase => IsAttacking ? _timeline.PhaseAt(Time.time - _startedAt) : AttackPhase.None;

        /// <summary>0 at the start of the current phase, approaching 1 at its end.</summary>
        public float PhaseProgress => IsAttacking ? _timeline.PhaseProgress(Time.time - _startedAt) : 0f;

        /// <summary>The side the strike goes to; Alice cannot turn while it lasts.</summary>
        public int Facing => _facing;

        bool IHitImmunity.IsImmune => Strike == PlayerStrike.Execution;

        private MeleeMove CurrentMove => Strike switch
        {
            PlayerStrike.Slash => _settings.Slash,
            PlayerStrike.AirSlash => _settings.AirSlash,
            PlayerStrike.Counter => _settings.Counter,
            _ => _settings.Thrust
        };

        public void Bind(PlayerCombatSettings settings)
        {
            _settings = settings;
        }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _commands = GetComponent<IPlayerCommandSource>();
            _parry = GetComponent<PlayerParry>();
            if (_settings == null)
            {
                Debug.LogError("PlayerAttack needs combat settings.", this);
                enabled = false;
            }
        }

        /// <summary>Drops the attack, e.g. when Alice is hit out of it. An execution cannot be interrupted.</summary>
        public void Cancel()
        {
            if (Strike == PlayerStrike.Execution && Phase != AttackPhase.None)
            {
                return;
            }

            Strike = PlayerStrike.None;
            _comboQueued = false;
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
            if (Strike == PlayerStrike.Thrust && phase != AttackPhase.Windup && HasFreshPress(now))
            {
                _consumedAttackAt = _commands.AttackPressedAt;
                _comboQueued = true;
            }

            if (_comboQueued && phase == AttackPhase.Recovery && _motor.IsGrounded)
            {
                Begin(PlayerStrike.Slash, now, _settings.Slash.Timeline);
                return;
            }

            if (phase == AttackPhase.None)
            {
                Strike = PlayerStrike.None;
                _comboQueued = false;
                TryStart(now);
                return;
            }

            if (phase == AttackPhase.Active && Strike != PlayerStrike.Execution)
            {
                MeleeMove move = CurrentMove;
                HitInfo hit = new HitInfo(move.Damage, _facing, gameObject, move.Kind);
                MeleeHitbox.Strike(_motor.Position + move.HitboxCenter(_facing), move.HitboxSize, hit, gameObject,
                    _struck, _overlaps);
            }
        }

        private void TryStart(float now)
        {
            if (_motor.IsSliding || _motor.IsLocked || _commands == null)
            {
                return;
            }

            if (_motor.IsGrounded
                && InputBuffer.IsFresh(_commands.InteractPressedAt, _consumedInteractAt, now, _settings.AttackBufferTime)
                && TryExecute(now))
            {
                _consumedInteractAt = _commands.InteractPressedAt;
                return;
            }

            if (!HasFreshPress(now))
            {
                return;
            }

            _consumedAttackAt = _commands.AttackPressedAt;

            if (_parry != null && _parry.CounterReady && _motor.IsGrounded)
            {
                // The counter goes where the parry faced, even if the stick has moved since.
                _parry.ConsumeCounter();
                _facing = _parry.Facing;
                Begin(PlayerStrike.Counter, now, _settings.Counter.Timeline);
                return;
            }

            _facing = _motor.Facing;
            PlayerStrike strike = _motor.IsGrounded ? PlayerStrike.Thrust : PlayerStrike.AirSlash;
            Begin(strike, now, strike == PlayerStrike.Thrust ? _settings.Thrust.Timeline : _settings.AirSlash.Timeline);
        }

        /// <summary>Executes the nearest stunned enemy in reach on either side, if Alice has frames for it.</summary>
        private bool TryExecute(float now)
        {
            float reach = _settings.ExecutionReach;
            Vector2 center = _motor.Position + new Vector2(0f, 1.5f);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            int count = Physics2D.OverlapBox(center, new Vector2(reach * 2f, 3f), 0f, filter, _overlaps);

            IExecutable target = null;
            ExecutionSpec spec = default;
            float best = float.MaxValue;
            for (int index = 0; index < count; index++)
            {
                Collider2D other = _overlaps[index];
                GameObject owner = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
                if (owner == gameObject || !owner.TryGetComponent(out IExecutable candidate) || !candidate.CanBeExecuted
                    || !_settings.TryGetExecution(candidate.ExecutionId, out ExecutionSpec candidateSpec))
                {
                    continue;
                }

                float distance = Mathf.Abs(candidate.Position.x - _motor.Position.x);
                if (distance < best)
                {
                    best = distance;
                    target = candidate;
                    spec = candidateSpec;
                }
            }

            if (target == null)
            {
                return false;
            }

            // Alice steps to the spot the paired frames were drawn for and faces the enemy.
            int direction = target.Position.x >= _motor.Position.x ? 1 : -1;
            _motor.Teleport(new Vector2(target.Position.x - direction * spec.Distance, _motor.Position.y));
            _motor.Face(direction);
            _facing = direction;
            _parry?.ConsumeCounter();
            ExecutionId = spec.Id;
            Begin(PlayerStrike.Execution, now, spec.Timeline);
            target.BeginExecution(gameObject, -direction);
            return true;
        }

        private void Begin(PlayerStrike strike, float now, MeleeAttackTimeline timeline)
        {
            Strike = strike;
            _timeline = timeline;
            _startedAt = now;
            _comboQueued = false;
            _struck.Clear();
            _motor.Lock(timeline.Duration);
        }

        private bool HasFreshPress(float now)
        {
            return _commands != null
                && InputBuffer.IsFresh(_commands.AttackPressedAt, _consumedAttackAt, now, _settings.AttackBufferTime);
        }
    }
}
