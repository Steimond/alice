using System.Collections.Generic;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Archetype for a foot soldier with a telegraphed melee lunge (first used by the card soldier, EN09
    /// without the shield). Stands guard, approaches Alice inside its leash, winds up, steps into the blow
    /// and stays open while recovering. Hits stagger it except during the blow itself, so trading is a
    /// timing decision. The body is a kinematic trigger: Alice and her slide pass through it, weapons
    /// still find it. A parry throws it open for Alice's counter; a blow that leaves it nearly beaten stuns
    /// it once, and only a stunned enemy can be executed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
    public sealed class MeleeEnemy : MonoBehaviour, IParryable, IExecutable
    {
        public enum State
        {
            Guard,
            Chase,
            Attack,
            Stagger,
            Opened,
            Stunned,
            Dead
        }

        private const float ProbeLift = 0.1f;
        private const float GroundProbeDepth = 0.4f;
        private const float StepMargin = 0.1f;
        private const float HomeTolerance = 0.1f;

        [SerializeField] private MeleeEnemySettings _settings;
        [Tooltip("+1 faces right, -1 faces left at the start and after a rest.")]
        [SerializeField] private int _initialFacing = -1;

        private readonly HashSet<IDamageable> _struck = new HashSet<IDamageable>();
        private readonly Collider2D[] _overlaps = new Collider2D[8];
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        private Rigidbody2D _body;
        private BoxCollider2D _collider;
        private Health _health;
        private ContactFilter2D _solidFilter;
        private Vector2 _home;
        private float _stateStartedAt;
        private float _cooldownUntil;
        private float _lungeMoved;
        private bool _wasStunned;

        public MeleeEnemySettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public int InitialFacing
        {
            get => _initialFacing;
            set => _initialFacing = value >= 0 ? 1 : -1;
        }

        public State CurrentState { get; private set; }
        public int Facing { get; private set; }
        public float StateTime => Time.time - _stateStartedAt;
        public bool IsWalking { get; private set; }

        /// <summary>The blow that killed it; the view picks the matching death.</summary>
        public HitKind KillingBlow { get; private set; }
        public float WalkDistance { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _health = GetComponent<Health>();

            if (_settings == null)
            {
                Debug.LogError("MeleeEnemy needs settings.", this);
                enabled = false;
                return;
            }

            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _collider.isTrigger = true;
            _collider.size = _settings.BodySize;
            _collider.offset = new Vector2(0f, _settings.BodySize.y * 0.5f);
            _solidFilter = new ContactFilter2D { useTriggers = false };

            _home = _body.position;
            _health.Initialize(_settings.MaxHealth, _settings.HitInvulnerability);
            Facing = _initialFacing >= 0 ? 1 : -1;
            Enter(State.Guard);
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            WorldReset.Happened += ResetToHome;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            WorldReset.Happened -= ResetToHome;
        }

        /// <summary>Back to the post with full health, as after Alice rests (GDD section 4).</summary>
        public void ResetToHome()
        {
            _body.position = _home;
            transform.position = _home;
            _health.Refill();
            _collider.enabled = true;
            KillingBlow = HitKind.Other;
            _wasStunned = false;
            Facing = _initialFacing >= 0 ? 1 : -1;
            _cooldownUntil = 0f;
            Enter(State.Guard);
        }

        private void FixedUpdate()
        {
            IsWalking = false;
            switch (CurrentState)
            {
                case State.Guard:
                    UpdateGuard();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Stagger:
                    if (StateTime >= _settings.StaggerTime)
                    {
                        Enter(State.Chase);
                    }

                    break;
                case State.Opened:
                    if (StateTime >= _settings.OpenedTime)
                    {
                        _cooldownUntil = Time.time + _settings.Cooldown;
                        Enter(State.Chase);
                    }

                    break;
                case State.Stunned:
                    if (StateTime >= _settings.StunnedTime)
                    {
                        _cooldownUntil = Time.time + _settings.Cooldown;
                        Enter(State.Chase);
                    }

                    break;
            }
        }

        private void UpdateGuard()
        {
            if (TryGetTarget(out _))
            {
                Enter(State.Chase);
                return;
            }

            float toHome = _home.x - _body.position.x;
            if (Mathf.Abs(toHome) > HomeTolerance)
            {
                Facing = toHome > 0f ? 1 : -1;
                Walk(Mathf.Min(Mathf.Abs(toHome), _settings.WalkSpeed * Time.fixedDeltaTime));
            }
            else
            {
                Facing = _initialFacing >= 0 ? 1 : -1;
            }
        }

        private void UpdateChase()
        {
            if (!TryGetTarget(out Vector2 target))
            {
                Enter(State.Guard);
                return;
            }

            float dx = target.x - _body.position.x;
            Facing = dx >= 0f ? 1 : -1;
            if (Mathf.Abs(dx) <= _settings.AttackStartRange)
            {
                if (Time.time >= _cooldownUntil)
                {
                    _struck.Clear();
                    _lungeMoved = 0f;
                    Enter(State.Attack);
                }

                return;
            }

            float nextX = _body.position.x + Facing * _settings.WalkSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(nextX - _home.x) <= _settings.LeashDistance)
            {
                Walk(_settings.WalkSpeed * Time.fixedDeltaTime);
            }
        }

        private void UpdateAttack()
        {
            MeleeAttackTimeline timeline = _settings.Timeline;
            float elapsed = StateTime;
            AttackPhase phase = timeline.PhaseAt(elapsed);
            if (phase == AttackPhase.None)
            {
                _cooldownUntil = Time.time + _settings.Cooldown;
                Enter(State.Chase);
                return;
            }

            if (phase != AttackPhase.Active)
            {
                return;
            }

            float lunge = _settings.LungeDistance * timeline.PhaseProgress(elapsed) - _lungeMoved;
            if (lunge > 0f)
            {
                _lungeMoved += Move(lunge);
            }

            float length = _settings.Reach - _settings.ReachStart;
            Vector2 center = _body.position + new Vector2(Facing * (_settings.ReachStart + length * 0.5f),
                _settings.HitCenterHeight);
            HitInfo hit = new HitInfo(_settings.Damage, Facing, gameObject, HitKind.Thrust, _settings.ParryWindow);
            MeleeHitbox.Strike(center, new Vector2(length, _settings.HitHeight), hit, gameObject, _struck, _overlaps);
        }

        private void OnDamaged(HitInfo hit)
        {
            if (_health.IsDead)
            {
                return;
            }

            // Nearly beaten, it reels once per life, even out of its own blow. The execution is offered, never
            // forced: any further blow still finishes it.
            if (!_wasStunned && CurrentState != State.Stunned && _health.Current <= _settings.StunAtHealth
                && !string.IsNullOrEmpty(_settings.ExecutionId))
            {
                _wasStunned = true;
                Facing = -hit.DirectionX;
                Enter(State.Stunned);
                return;
            }

            // Committed to the blow: hits land but do not interrupt it. Thrown open or stunned, it stays so.
            bool inBlow = CurrentState == State.Attack && _settings.Timeline.PhaseAt(StateTime) == AttackPhase.Active;
            if (!inBlow && CurrentState != State.Opened && CurrentState != State.Stunned)
            {
                Facing = -hit.DirectionX;
                Enter(State.Stagger);
            }
        }

        public bool CanBeExecuted => CurrentState == State.Stunned &&!string.IsNullOrEmpty(_settings.ExecutionId);
        public string ExecutionId => _settings.ExecutionId;
        public Vector2 Position => _body.position;

        public void BeginExecution(GameObject by, int fromSide)
        {
            if (!CanBeExecuted)
            {
                return;
            }

            // Facing the executioner; the death frames are this type's execution and start now, in sync with Alice.
            _health.Kill(new HitInfo(_health.Current, -fromSide, by, HitKind.Execution));
        }

        public void OnParried(GameObject by)
        {
            if (CurrentState == State.Dead)
            {
                return;
            }

            Enter(State.Opened);
        }

        private void OnDied(HitInfo hit)
        {
            Facing = -hit.DirectionX;
            KillingBlow = hit.Kind;
            _collider.enabled = false;
            Enter(State.Dead);
            EnemyDefeats.Raise(new EnemyDefeat(gameObject, hit, _settings.DecreeReward, _settings.ConfidenceReward));
        }

        private bool TryGetTarget(out Vector2 target)
        {
            target = default;
            Transform player = PlayerPresence.Current;
            if (player == null)
            {
                return false;
            }

            target = player.position;
            Vector2 offset = target - _body.position;
            bool seen = Mathf.Abs(offset.x) <= _settings.SightRange && Mathf.Abs(offset.y) <= _settings.SightHeight;
            bool withinLeash = Mathf.Abs(target.x - _home.x) <= _settings.LeashDistance + _settings.AttackStartRange;
            return seen && withinLeash;
        }

        private void Walk(float distance)
        {
            float moved = Move(distance);
            if (moved > 0f)
            {
                IsWalking = true;
                WalkDistance += moved;
            }
        }

        /// <summary>Moves forward unless the floor ends or something solid is in the way; returns the distance.</summary>
        private float Move(float distance)
        {
            float halfWidth = _settings.BodySize.x * 0.5f;
            Vector2 position = _body.position;
            Vector2 front = position + new Vector2(Facing * (halfWidth + distance + StepMargin), ProbeLift);
            if (Physics2D.Raycast(front, Vector2.down, _solidFilter, _hits, ProbeLift + GroundProbeDepth) == 0)
            {
                return 0f;
            }

            Vector2 chest = position + new Vector2(0f, _settings.BodySize.y * 0.5f);
            int blocked = Physics2D.Raycast(chest, new Vector2(Facing, 0f), _solidFilter, _hits,
                halfWidth + distance + StepMargin);
            for (int index = 0; index < blocked; index++)
            {
                if (_hits[index].collider != _collider)
                {
                    return 0f;
                }
            }

            _body.MovePosition(position + new Vector2(Facing * distance, 0f));
            return distance;
        }

        private void Enter(State state)
        {
            CurrentState = state;
            _stateStartedAt = Time.time;
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null)
            {
                return;
            }

            int facing = Application.isPlaying ? Facing : (_initialFacing >= 0 ? 1 : -1);
            float length = _settings.Reach - _settings.ReachStart;
            Vector3 center = transform.position + new Vector3(facing * (_settings.ReachStart + length * 0.5f),
                _settings.HitCenterHeight, 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, new Vector3(length, _settings.HitHeight, 0f));
        }
    }
}
