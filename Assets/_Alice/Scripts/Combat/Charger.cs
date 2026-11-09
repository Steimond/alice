using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Archetype for a runner that rams (the race rat EN02, GDD section 9). It spots Alice on its level, scrapes
    /// and lowers its head in a long wind-up, then charges in a straight line faster than she runs. The ram cannot
    /// be parried: Alice slides through it or jumps over. Past her it skids and turns; into a wall it reels, open.
    /// Teaches keeping space and reading the wind-up.
    /// </summary>
    public sealed class Charger : EnemyBase
    {
        public enum State
        {
            Guard,
            Walk,
            Windup,
            Charge,
            Skid,
            Dazed,
            Stagger,
            Dead
        }


        [SerializeField] private ChargerSettings _settings;

        private float _cooldownUntil;
        private float _charged;
        private float _skidded;
        private float _walkDistance;

        public ChargerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public override EnemySettings BaseSettings => _settings;
        public State CurrentState { get; private set; }

        protected override bool ContactHurts =>
            CurrentState == State.Guard || CurrentState == State.Walk || CurrentState == State.Windup;

        protected override string ArchetypeClipId
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Walk:
                        // Strolling about its post it walks; closing in on Alice it runs.
                        return Awareness.Engaged ? "run" : "walk";
                    case State.Charge:
                        return "run";
                    case State.Windup:
                        return "windup";
                    case State.Skid:
                        return "skid";
                    case State.Dazed:
                        return "daze";
                    case State.Stagger:
                        return "hurt";
                    case State.Dead:
                        return "death";
                    default:
                        return "idle";
                }
            }
        }

        protected override float ArchetypeClipTime =>
            CurrentState != State.Walk ? StateTime
            : Awareness.Engaged ? _walkDistance / _settings.RunFrameDistance * _settings.ChargeFrameTime
            : _walkDistance / _settings.WalkFrameDistance * WalkClipFrameTime;

        // The walk clip's own frame time (clip set "walk"): its frames are paced by the distance walked.
        private const float WalkClipFrameTime = 0.1f;

        protected override float ArchetypeClipSpan
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Windup:
                        return _settings.Windup;
                    case State.Skid:
                        return _settings.SkidTime;
                    case State.Dazed:
                        return _settings.DazedTime;
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
                case State.Windup:
                    if (StateTime >= _settings.Windup)
                    {
                        _charged = 0f;
                        Struck.Clear();
                        Enter(State.Charge);
                    }

                    break;
                case State.Charge:
                    UpdateCharge();
                    break;
                case State.Skid:
                    UpdateSkid();
                    break;
                case State.Dazed:
                    if (StateTime >= _settings.DazedTime)
                    {
                        Recover();
                    }

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
            if (distance > _settings.ChargeStartRange)
            {
                Facing = dx >= 0f ? 1 : -1;
                float nextX = Body.position.x + Facing * _settings.WalkSpeed * Time.fixedDeltaTime;
                if (Mathf.Abs(nextX - Home.x) <= _settings.LeashDistance)
                {
                    Walk();
                    return;
                }

                SetState(State.Guard);
                return;
            }

            if (Time.time < _cooldownUntil)
            {
                Facing = dx >= 0f ? 1 : -1;
                SetState(State.Guard);
                return;
            }

            // Too close to build speed: it backs off first, turned away, and then faces her again.
            if (distance < _settings.MinChargeRange)
            {
                Facing = dx >= 0f ? -1 : 1;
                if (Walk() > 0f)
                {
                    return;
                }
            }

            Facing = dx >= 0f ? 1 : -1;
            Enter(State.Windup);
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

        private void UpdateCharge()
        {
            float step = _settings.ChargeSpeed * Time.fixedDeltaTime;
            float moved = MoveOnFloor(step, out bool hitWall);
            _charged += moved;
            StrikeWithBody(new HitInfo(_settings.ChargeDamage, Facing, gameObject, HitKind.Other,
                _settings.ChargeParryWindow, _settings.ChargeKnockback));

            if (hitWall && moved < step * 0.5f)
            {
                Impacts.Raise(2);
                Enter(State.Dazed);
                return;
            }

            bool pastAlice = PlayerPresence.Current != null
                && Facing * (Body.position.x - PlayerPresence.Current.position.x) > _settings.Overshoot;
            if (moved <= 0f || _charged >= _settings.ChargeDistance || pastAlice)
            {
                _skidded = 0f;
                Enter(State.Skid);
            }
        }

        private void UpdateSkid()
        {
            // Brakes from charge speed to a stop: the distance left eases out over the skid.
            float progress = _settings.SkidTime > 0f ? Mathf.Clamp01(StateTime / _settings.SkidTime) : 1f;
            float target = _settings.SkidDistance * (1f - (1f - progress) * (1f - progress));
            if (target > _skidded)
            {
                float moved = MoveOnFloor(target - _skidded, out _);
                _skidded = moved > 0f ? _skidded + moved : target;
            }

            if (progress >= 1f)
            {
                Recover();
            }
        }

        private void Recover()
        {
            _cooldownUntil = Time.time + _settings.Cooldown;
            if (PlayerPresence.Current != null)
            {
                Facing = PlayerPresence.Current.position.x >= Body.position.x ? 1 : -1;
            }

            Enter(State.Guard);
        }

        protected override void OnHurt(HitInfo hit)
        {
            // Committed to the wind-up and the charge; reeling, it just takes the blows.
            if (CurrentState == State.Guard || CurrentState == State.Walk || CurrentState == State.Stagger)
            {
                Facing = -hit.DirectionX;
                Enter(State.Stagger);
            }
        }

        protected override void OnStunEnded()
        {
            Recover();
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
