using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Archetype for a flyer that hangs out of reach and dives (the clothes moth EN11, GDD section 9). It drifts
    /// above Alice, folds its wings in a long wind-up, dives through her head height, then hangs exposed a little
    /// above the reach of ground blows: Alice catches it with a blow in a jump. A blow in the air or a parried dive
    /// throws it to the floor, where any blow lands until it lifts off again.
    /// </summary>
    public sealed class FlyingDiver : EnemyBase, IParryable
    {
        public enum State
        {
            Hover,
            Windup,
            Dive,
            Exposed,
            Climb,
            Falling,
            Grounded,
            Rising,
            Dead
        }

        [SerializeField] private FlyingDiverSettings _settings;

        private float _floorY;
        private float _cooldownUntil;
        private Vector2 _diveTarget;
        private Vector2 _exposedFrom;
        private Vector2 _exposedTo;
        private float _fallSpeed;
        private bool _landedDead;

        public FlyingDiverSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public override EnemySettings BaseSettings => _settings;
        public State CurrentState { get; private set; }

        public override bool BlocksBodies => false;
        protected override bool ContactHurts => false;

        public override string ClipId
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Windup:
                        return "windup";
                    case State.Dive:
                        return "dive";
                    case State.Falling:
                        return "fall";
                    case State.Grounded:
                        return "grounded";
                    case State.Rising:
                        return "rise";
                    case State.Dead:
                        return _landedDead ? "death" : "fall";
                    default:
                        return "hover";
                }
            }
        }

        public override float ClipSpan
        {
            get
            {
                switch (CurrentState)
                {
                    case State.Windup:
                        return _settings.Windup;
                    case State.Rising:
                        return _settings.RiseTime;
                    default:
                        return 0f;
                }
            }
        }

        /// <summary>Floor height under the post; hover and dive heights are measured from it.</summary>
        public float FloorY => _floorY;

        protected override void OnSpawn()
        {
            _floorY = TryFindFloor(Home, out float floor) ? floor : Home.y - _settings.HoverHeight;
            _cooldownUntil = 0f;
            _fallSpeed = 0f;
            _landedDead = false;
            Enter(State.Hover);
        }

        protected override void Tick()
        {
            switch (CurrentState)
            {
                case State.Hover:
                    UpdateHover();
                    break;
                case State.Windup:
                    if (StateTime >= _settings.Windup)
                    {
                        BeginDive();
                    }

                    break;
                case State.Dive:
                    UpdateDive();
                    break;
                case State.Exposed:
                    UpdateExposed();
                    break;
                case State.Climb:
                    UpdateClimb();
                    break;
                case State.Falling:
                    if (FallToFloor())
                    {
                        Enter(State.Grounded);
                    }

                    break;
                case State.Grounded:
                    if (StateTime >= _settings.GroundedTime)
                    {
                        Enter(State.Rising);
                    }

                    break;
                case State.Rising:
                    UpdateRising();
                    break;
                case State.Dead:
                    if (!_landedDead && FallToFloor())
                    {
                        _landedDead = true;
                        RestartStateClock();
                    }

                    break;
            }
        }

        private void UpdateHover()
        {
            float bob = Mathf.Sin(Time.time * 2f * Mathf.PI / _settings.BobPeriod) * _settings.BobAmplitude;
            Vector2 goal = new Vector2(Home.x, _floorY + _settings.HoverHeight + bob);
            if (TryGetAliceBelow(out Vector2 alice))
            {
                int side = Body.position.x >= alice.x ? 1 : -1;
                goal.x = Mathf.Clamp(alice.x + side * _settings.HoverOffsetX,
                    Home.x - _settings.LeashDistance, Home.x + _settings.LeashDistance);
                Facing = alice.x >= Body.position.x ? 1 : -1;
                bool close = Mathf.Abs(alice.x - Body.position.x) <= _settings.AttackRangeX;
                if (close && Time.time >= _cooldownUntil && Mathf.Abs(Body.position.x - goal.x) < 1f)
                {
                    Enter(State.Windup);
                    return;
                }
            }

            Body.MovePosition(Vector2.MoveTowards(Body.position, goal, _settings.DriftSpeed * Time.fixedDeltaTime));
        }

        /// <summary>
        /// It looks down on the whole space between the floor and its hover height, not a box around its own body:
        /// it hangs far above the one it hunts.
        /// </summary>
        private bool TryGetAliceBelow(out Vector2 alice)
        {
            alice = default;
            Transform player = PlayerPresence.Current;
            if (player == null)
            {
                return false;
            }

            alice = player.position;
            bool sideways = Mathf.Abs(alice.x - Body.position.x) <= _settings.SightRange
                && Mathf.Abs(alice.x - Home.x) <= _settings.LeashDistance + _settings.HoverOffsetX;
            bool between = alice.y >= _floorY - _settings.SightHeight && alice.y <= _floorY + _settings.HoverHeight;
            return sideways && between;
        }

        /// <summary>Aims at where Alice stands when the wind-up ends, a little past her, at head height.</summary>
        private void BeginDive()
        {
            Vector2 alice = PlayerPresence.Current != null ? (Vector2)PlayerPresence.Current.position : Body.position;
            Facing = alice.x >= Body.position.x ? 1 : -1;
            float floor = TryFindFloor(alice + Vector2.up, out float under) ? under : _floorY;
            _diveTarget = new Vector2(alice.x + Facing * _settings.DiveOvershoot, floor + _settings.DiveBottomHeight);
            Struck.Clear();
            Enter(State.Dive);
        }

        private void UpdateDive()
        {
            Vector2 next = Vector2.MoveTowards(Body.position, _diveTarget, _settings.DiveSpeed * Time.fixedDeltaTime);
            Body.MovePosition(next);
            HitInfo hit = new HitInfo(_settings.DiveDamage, Facing, gameObject, HitKind.Thrust, _settings.DiveParryWindow,
                _settings.DiveKnockback);
            StrikeWithBody(hit);
            if ((next - _diveTarget).sqrMagnitude < 0.0004f)
            {
                _exposedFrom = next;
                _exposedTo = new Vector2(next.x + Facing * _settings.ExposedDrift, _floorY + _settings.ExposedHeight);
                Enter(State.Exposed);
            }
        }

        private void UpdateExposed()
        {
            float rise = _settings.ExposedRiseTime > 0f ? Mathf.Clamp01(StateTime / _settings.ExposedRiseTime) : 1f;
            float eased = 1f - (1f - rise) * (1f - rise);
            float bob = Mathf.Sin(StateTime * 2f * Mathf.PI / _settings.BobPeriod) * _settings.BobAmplitude * 0.5f;
            Body.MovePosition(Vector2.Lerp(_exposedFrom, _exposedTo, eased) + new Vector2(0f, rise >= 1f ? bob : 0f));
            if (StateTime >= _settings.ExposedRiseTime + _settings.ExposedTime)
            {
                Enter(State.Climb);
            }
        }

        private void UpdateClimb()
        {
            Vector2 goal = new Vector2(Body.position.x, _floorY + _settings.HoverHeight);
            Vector2 next = Vector2.MoveTowards(Body.position, goal, _settings.ClimbSpeed * Time.fixedDeltaTime);
            Body.MovePosition(next);
            if ((next - goal).sqrMagnitude < 0.0004f)
            {
                _cooldownUntil = Time.time + _settings.Cooldown;
                Enter(State.Hover);
            }
        }

        private void UpdateRising()
        {
            // It shakes itself on the floor for the first half of the clip, then lifts off.
            float lift = Mathf.Clamp01((StateTime / _settings.RiseTime - 0.5f) * 2f);
            float floor = TryFindFloor(Body.position + Vector2.up * 0.2f, out float under) ? under : _floorY;
            Body.MovePosition(new Vector2(Body.position.x, floor + lift * _settings.ExposedHeight));
            if (StateTime >= _settings.RiseTime)
            {
                Enter(State.Climb);
            }
        }

        /// <summary>Drops under gravity; true once it lies on the floor.</summary>
        private bool FallToFloor()
        {
            float floor = TryFindFloor(Body.position + Vector2.up * 0.2f, out float under) ? under : _floorY;
            _fallSpeed += _settings.FallGravity * Time.fixedDeltaTime;
            float y = Body.position.y - _fallSpeed * Time.fixedDeltaTime;
            if (y <= floor)
            {
                Body.MovePosition(new Vector2(Body.position.x, floor));
                _fallSpeed = 0f;
                return true;
            }

            Body.MovePosition(new Vector2(Body.position.x, y));
            return false;
        }

        protected override void OnHurt(HitInfo hit)
        {
            // On the floor it just takes the blows; anywhere else the blow throws it down.
            if (CurrentState == State.Grounded)
            {
                return;
            }

            Facing = -hit.DirectionX;
            KnockDown();
        }

        public void OnParried(GameObject by)
        {
            if (!IsDead)
            {
                KnockDown();
            }
        }

        private void KnockDown()
        {
            _fallSpeed = 0f;
            _cooldownUntil = Time.time + _settings.Cooldown;
            Enter(State.Falling);
        }

        protected override void OnKilled(HitInfo hit)
        {
            _fallSpeed = 0f;
            float floor = TryFindFloor(Body.position + Vector2.up * 0.2f, out float under) ? under : _floorY;
            _landedDead = Body.position.y <= floor + 0.05f;
            Enter(State.Dead);
        }

        private void Enter(State state)
        {
            CurrentState = state;
            RestartStateClock();
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null)
            {
                return;
            }

            float floor = Application.isPlaying ? _floorY : transform.position.y - _settings.HoverHeight;
            float x = transform.position.x;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(x - 1f, floor + _settings.HoverHeight), new Vector3(x + 1f, floor + _settings.HoverHeight));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(x - 1f, floor + _settings.ExposedHeight), new Vector3(x + 1f, floor + _settings.ExposedHeight));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(x - 1f, floor + _settings.DiveBottomHeight), new Vector3(x + 1f, floor + _settings.DiveBottomHeight));
        }
    }
}
