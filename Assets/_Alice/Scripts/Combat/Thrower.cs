using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Archetype for an enemy that keeps its distance and lobs things on an arc (the duck washerwoman EN12, GDD
    /// section 9). It draws the arm back and holds it before every throw, and stays open the whole time: Alice
    /// closes in under the fire and punishes the wind-up. Up close it steps back while there is floor behind it.
    /// </summary>
    public sealed class Thrower : EnemyBase
    {
        public enum State
        {
            Guard,
            Walk,
            Throw,
            Stagger,
            Dead
        }


        [SerializeField] private ThrowerSettings _settings;

        private float _cooldownUntil;
        private float _walkDistance;
        private bool _released;

        public ThrowerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public override EnemySettings BaseSettings => _settings;
        public State CurrentState { get; private set; }

        protected override bool ContactHurts => CurrentState != State.Dead && CurrentState != State.Stagger;

        protected override string ArchetypeClipId
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Walk:
                        return "walk";
                    case State.Throw:
                        return "throw";
                    case State.Stagger:
                        return "hurt";
                    case State.Dead:
                        return "death";
                    default:
                        return "idle";
                }
            }
        }

        protected override float ArchetypeClipTime => CurrentState == State.Walk ? _walkDistance / _settings.WalkFrameDistance * 0.1f : StateTime;

        protected override float ArchetypeClipSpan
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Throw:
                        return _settings.Windup + _settings.Recovery;
                    case State.Stagger:
                        return _settings.StaggerTime;
                    default:
                        return 0f;
                }
            }
        }

        protected override void OnSpawn()
        {
            _cooldownUntil = 0f;
            Enter(State.Guard);
        }

        protected override void Tick()
        {
            switch (CurrentState)
            {
                case State.Guard:
                case State.Walk:
                    UpdateGuard();
                    break;
                case State.Throw:
                    UpdateThrow();
                    break;
                case State.Stagger:
                    if (StateTime >= _settings.StaggerTime)
                    {
                        Enter(State.Guard);
                    }

                    break;
            }
        }

        private void UpdateGuard()
        {
            if (!TryGetAlice(out Vector2 alice))
            {
                Wander();
                return;
            }

            float dx = alice.x - Body.position.x;
            float distance = Mathf.Abs(dx);
            int toAlice = dx >= 0f ? 1 : -1;

            if (distance < _settings.KeepAway)
            {
                Facing = -toAlice;
                if (Walk() > 0f)
                {
                    return;
                }
            }
            else if (distance > _settings.ThrowRange)
            {
                Facing = toAlice;
                float nextX = Body.position.x + Facing * _settings.WalkSpeed * Time.fixedDeltaTime;
                if (Mathf.Abs(nextX - Home.x) <= _settings.LeashDistance && Walk() > 0f)
                {
                    return;
                }
            }

            Facing = toAlice;
            if (Time.time >= _cooldownUntil && distance <= _settings.ThrowRange)
            {
                _released = false;
                Enter(State.Throw);
                return;
            }

            SetState(State.Guard);
        }

        /// <summary>Alice unseen: it strolls about its post and now and then stands (<see cref="EnemyBase.Patrol"/>).</summary>
        private void Wander()
        {
            float moved = Patrol();
            _walkDistance += moved;
            SetState(moved > 0f ? State.Walk : State.Guard);
        }

        private float Walk()
        {
            float moved = MoveOnFloor(_settings.WalkSpeed * Time.fixedDeltaTime, out _);
            _walkDistance += moved;
            SetState(moved > 0f ? State.Walk : State.Guard);
            return moved;
        }

        private void UpdateThrow()
        {
            if (!_released && StateTime >= _settings.Windup)
            {
                _released = true;
                Release();
            }

            if (StateTime >= _settings.Windup + _settings.Recovery)
            {
                _cooldownUntil = Time.time + _settings.Cooldown;
                Enter(State.Guard);
            }
        }

        /// <summary>Lets go: an arc aimed at Alice's feet where she stands now.</summary>
        private void Release()
        {
            if (_settings.Projectile == null || PlayerPresence.Current == null)
            {
                return;
            }

            Vector2 from = Body.position + new Vector2(Facing * _settings.ReleasePoint.x, _settings.ReleasePoint.y);
            Vector2 target = (Vector2)PlayerPresence.Current.position + Vector2.up * 0.5f;
            float flight = Mathf.Max(_settings.MinFlightTime, Mathf.Abs(target.x - from.x) * _settings.FlightTimePerTile);
            ArcProjectile projectile = Instantiate(_settings.Projectile, from, Quaternion.identity);
            projectile.gameObject.SetActive(true);
            projectile.Launch(gameObject, ArcProjectile.VelocityToHit(from, target, flight, _settings.ProjectileGravity),
                _settings.ProjectileGravity);
        }

        protected override void OnHurt(HitInfo hit)
        {
            // A blow before the release spoils the throw; that is the reward for closing in.
            if (CurrentState == State.Throw && _released)
            {
                return;
            }

            Facing = -hit.DirectionX;
            _cooldownUntil = Time.time + _settings.Cooldown * 0.5f;
            Enter(State.Stagger);
        }

        protected override void OnStunEnded()
        {
            _cooldownUntil = Time.time + _settings.Cooldown;
            Enter(State.Guard);
        }

        protected override void OnKilled(HitInfo hit)
        {
            Enter(State.Dead);
        }

        private void SetState(State state)
        {
            if (CurrentState != state)
            {
                Enter(state);
            }
        }

        private void Enter(State state)
        {
            CurrentState = state;
            RestartStateClock();
        }
    }
}
