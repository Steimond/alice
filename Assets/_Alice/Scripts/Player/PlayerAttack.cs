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
        Execution,
        Technique
    }

    /// <summary>
    /// Needle attacks (GDD section 4): on the ground a short combo — thrust, then a slash if Attack is
    /// pressed again before the thrust has recovered; in the air a slash. Right after a parry the attack is a deep
    /// counter thrust. Interact next to a stunned enemy executes it, in sync with the enemy and safe from
    /// other blows; the execution is a choice, so Attack never starts one. UseAbility performs the special
    /// technique chosen at a rest if Alice has the confidence to pay for it. Movement input is locked for the
    /// whole strike, so recovery is a real commitment; only the combo cuts it short.
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
        private PlayerLoadout _loadout;
        private PlayerSpoils _spoils;
        private float _consumedAttackAt = float.NegativeInfinity;
        private float _consumedInteractAt = float.NegativeInfinity;
        private float _consumedAbilityAt = float.NegativeInfinity;
        private MeleeMove _techniqueMove;
        private float _hitStopUntil;
        private float _startedAt;
        private int _facing;
        private bool _comboQueued;
        private MeleeAttackTimeline _timeline;

        public bool IsAttacking => Strike != PlayerStrike.None;
        public PlayerStrike Strike { get; private set; }

        /// <summary>ID of the execution being played, for the matching frames.</summary>
        public string ExecutionId { get; private set; }

        /// <summary>ID of the technique being performed, for the matching frames.</summary>
        public string TechniqueId { get; private set; }

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
            PlayerStrike.Technique => _techniqueMove,
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
            _loadout = GetComponent<PlayerLoadout>();
            _spoils = GetComponent<PlayerSpoils>();
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

            // Hit-stop: the strike's clock stands still for a moment after a blow lands, so it feels heavy.
            // Only this strike pauses; the game's time scale is never touched.
            if (now < _hitStopUntil)
            {
                _startedAt += Time.fixedDeltaTime;
                _motor.Lock(_timeline.Duration - (now - _startedAt));
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
                if (StrikeWith(move) > 0)
                {
                    _settings.ImpactOf(move.Kind, out float hitStop, out float shake);
                    _hitStopUntil = now + hitStop;
                    Impacts.Raise(shake);
                }
            }
        }

        /// <summary>
        /// A move that also reaches behind her (a spin) strikes each side with its own direction, so a body
        /// behind Alice is thrown and falls away from her too.
        /// </summary>
        /// <returns>How many bodies the blow landed on.</returns>
        private int StrikeWith(MeleeMove move)
        {
            if (move.ReachStart >= 0f)
            {
                HitInfo hit = new HitInfo(move.Damage, _facing, gameObject, move.Kind);
                return MeleeHitbox.Strike(_motor.Position + move.HitboxCenter(_facing), move.HitboxSize, hit,
                    gameObject, _struck, _overlaps);
            }

            float height = move.HitboxSize.y;
            float centerHeight = move.HitboxCenter(_facing).y;
            return StrikeSide(_facing, move.Reach, height, centerHeight, move)
                + StrikeSide(-_facing, -move.ReachStart, height, centerHeight, move);
        }

        private int StrikeSide(int side, float reach, float height, float centerHeight, MeleeMove move)
        {
            HitInfo hit = new HitInfo(move.Damage, side, gameObject, move.Kind);
            Vector2 center = _motor.Position + new Vector2(side * reach * 0.5f, centerHeight);
            return MeleeHitbox.Strike(center, new Vector2(reach, height), hit, gameObject, _struck, _overlaps);
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

            if (_motor.IsGrounded
                && InputBuffer.IsFresh(_commands.UseAbilityPressedAt, _consumedAbilityAt, now, _settings.AttackBufferTime))
            {
                _consumedAbilityAt = _commands.UseAbilityPressedAt;
                if (TryTechnique(now))
                {
                    return;
                }
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

        /// <summary>The chosen technique, paid in full from confidence; without enough, nothing happens.</summary>
        private bool TryTechnique(float now)
        {
            if (_loadout == null || _spoils == null || _spoils.Confidence == null
                || !_settings.TryGetTechnique(_loadout.EquippedTechnique, out TechniqueSpec technique)
                || !_spoils.Confidence.TrySpend(technique.ConfidenceCost))
            {
                return false;
            }

            // Spent confidence is kept at once, so quitting cannot buy it back (GDD section 10).
            Autosave.Request();
            _facing = _motor.Facing;
            _techniqueMove = technique.Move;
            TechniqueId = technique.Id;
            Begin(PlayerStrike.Technique, now, technique.Move.Timeline);
            return true;
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
