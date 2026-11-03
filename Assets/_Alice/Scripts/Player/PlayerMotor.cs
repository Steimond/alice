using Alice.Abilities;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Run, variable jump, coyote time, jump buffer, one-way drop-through, slide and the fan air jump.
    /// The body is dynamic for collision resolution, but all velocity is set here in FixedUpdate,
    /// so distances do not depend on the rendering frame rate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour, IHitImmunity
    {
        private const float GroundProbeDistance = 0.05f;
        private const float GroundProbeInset = 0.05f;
        private const float GroundProbeLift = 0.1f;
        private const float GroundProbeHeight = 0.05f;
        private const float RunInputThreshold = 0.2f;
        private const float DropInputThreshold = -0.5f;
        private const float StandClearance = 0.05f;

        [SerializeField] private PlayerMovementSettings _settings;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        private readonly Collider2D[] _overlaps = new Collider2D[8];

        private Rigidbody2D _body;
        private BoxCollider2D _collider;
        private IPlayerCommandSource _commands;
        private AbilityInventory _abilities;
        private ContactFilter2D _groundFilter;
        private ContactFilter2D _solidFilter;

        private float _lastGroundedTime = float.NegativeInfinity;
        private float _consumedJumpAt = float.NegativeInfinity;
        private float _consumedEvadeAt = float.NegativeInfinity;
        private bool _jumpedSinceGrounded;
        private bool _fanUsed;
        private bool _jumpRising;
        private float _slideStartTime;
        private float _slideReadyAt;
        private float _frozenUntil;
        private float _lockedUntil;
        private float _knockbackUntil;
        private Collider2D _ignoredPlatform;
        private float _ignoreUntil;

        public PlayerMovementSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public bool IsGrounded { get; private set; }
        public Collider2D GroundCollider { get; private set; }
        public bool IsSliding { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 Position => _body.position;
        public Vector2 Velocity => _body.linearVelocity;

        public bool IsInvulnerable
        {
            get
            {
                if (!IsSliding)
                {
                    return false;
                }

                float elapsed = Time.time - _slideStartTime;
                return elapsed >= _settings.SlideInvulnerabilityStart && elapsed <= _settings.SlideInvulnerabilityEnd;
            }
        }

        bool IHitImmunity.IsImmune => IsInvulnerable;

        /// <summary>True while an action, a knockback or a freeze owns Alice and input does not move her.</summary>
        public bool IsLocked
        {
            get
            {
                float now = Time.time;
                return now < _lockedUntil || now < _knockbackUntil || now < _frozenUntil;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _commands = GetComponent<IPlayerCommandSource>();
            _abilities = GetComponent<AbilityInventory>();

            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Zero friction keeps Alice from sticking to walls while she pushes into them mid-air.
            _collider.sharedMaterial = new PhysicsMaterial2D("AliceNoFriction") { friction = 0f, bounciness = 0f };

            _groundFilter = new ContactFilter2D { useTriggers = false };
            _groundFilter.SetNormalAngle(45f, 135f);
            _solidFilter = new ContactFilter2D { useTriggers = false };

            if (_settings == null)
            {
                Debug.LogError("PlayerMotor needs movement settings.", this);
                enabled = false;
                return;
            }

            ApplyBodySize(TraversalMetrics.StandingSize);
        }

        private void OnEnable()
        {
            PlayerPresence.Register(transform);
        }

        private void OnDisable()
        {
            PlayerPresence.Unregister(transform);
        }

        /// <summary>
        /// Ignores movement input for an action such as an attack: on the ground Alice stops, in the air
        /// she keeps falling. Jump and slide presses stay buffered.
        /// </summary>
        public void Lock(float duration)
        {
            _lockedUntil = Mathf.Max(_lockedUntil, Time.time + duration);
        }

        /// <summary>Throws Alice with a fixed velocity; gravity still applies, input does not.</summary>
        public void Knockback(Vector2 velocity, float duration)
        {
            if (IsSliding && CanStand())
            {
                EndSlide();
            }

            _knockbackUntil = Time.time + duration;
            _lockedUntil = 0f;
            _jumpRising = false;
            _body.linearVelocity = velocity;
        }

        /// <summary>Holds Alice in place, e.g. after a hazard return.</summary>
        public void Freeze(float duration)
        {
            _frozenUntil = Time.time + duration;
            _body.linearVelocity = Vector2.zero;
        }

        public void Teleport(Vector2 position)
        {
            if (IsSliding)
            {
                EndSlide();
            }

            _body.position = position;
            transform.position = position;
            _body.linearVelocity = Vector2.zero;
            _jumpRising = false;
            _knockbackUntil = 0f;
            _lockedUntil = 0f;
        }

        private void FixedUpdate()
        {
            float now = Time.time;
            float deltaTime = Time.fixedDeltaTime;
            Vector2 velocity = _body.linearVelocity;

            RestoreIgnoredPlatform(now);
            UpdateGround(velocity.y);
            if (IsGrounded)
            {
                _lastGroundedTime = now;
                _jumpedSinceGrounded = false;
                _fanUsed = false;
            }

            if (now < _frozenUntil)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }

            if (now < _knockbackUntil)
            {
                velocity.y = ApplyGravity(velocity.y, deltaTime);
                _body.linearVelocity = velocity;
                return;
            }

            bool locked = now < _lockedUntil;
            Vector2 move = _commands != null && !locked ? _commands.Move : Vector2.zero;
            velocity.x = IsSliding ? UpdateSlide(move, now) : Run(velocity.x, move, deltaTime, now, !locked);
            if (!locked)
            {
                velocity.y = HandleJump(velocity.y, move, now);
            }

            velocity.y = ApplyGravity(velocity.y, deltaTime);
            _body.linearVelocity = velocity;
        }

        private float Run(float velocityX, Vector2 move, float deltaTime, float now, bool canEvade)
        {
            float target = 0f;
            if (Mathf.Abs(move.x) > RunInputThreshold)
            {
                Facing = move.x > 0f ? 1 : -1;
                target = Facing * TraversalMetrics.MaxRunSpeed;
            }

            float rate = !IsGrounded ? _settings.AirAcceleration
                : target != 0f ? _settings.GroundAcceleration
                : _settings.GroundDeceleration;
            velocityX = Mathf.MoveTowards(velocityX, target, rate * deltaTime);

            bool evadePressed = canEvade && _commands != null && InputBuffer.IsFresh(
                _commands.EvadePressedAt, _consumedEvadeAt, now, _settings.EvadeBufferTime);
            if (evadePressed && IsGrounded && now >= _slideReadyAt)
            {
                _consumedEvadeAt = _commands.EvadePressedAt;
                StartSlide(now);
                return Facing * TraversalMetrics.SlideSpeed;
            }

            return velocityX;
        }

        private void StartSlide(float now)
        {
            IsSliding = true;
            _slideStartTime = now;
            ApplyBodySize(TraversalMetrics.SlidingSize);
        }

        private float UpdateSlide(Vector2 move, float now)
        {
            bool finished = now - _slideStartTime >= TraversalMetrics.SlideDuration;
            if (finished && CanStand())
            {
                EndSlide();
                _slideReadyAt = now + _settings.SlideCooldown;
                return Facing * TraversalMetrics.MaxRunSpeed * Mathf.Abs(Mathf.Clamp(move.x, -1f, 1f));
            }

            // Under a low ceiling the slide continues; the player may still turn around.
            if (finished && Mathf.Abs(move.x) > RunInputThreshold)
            {
                Facing = move.x > 0f ? 1 : -1;
            }

            return Facing * TraversalMetrics.SlideSpeed;
        }

        private void EndSlide()
        {
            IsSliding = false;
            ApplyBodySize(TraversalMetrics.StandingSize);
        }

        private float HandleJump(float velocityY, Vector2 move, float now)
        {
            if (_commands == null)
            {
                return velocityY;
            }

            if (InputBuffer.IsFresh(_commands.JumpPressedAt, _consumedJumpAt, now, _settings.JumpBufferTime))
            {
                bool canGroundJump = !_jumpedSinceGrounded && now - _lastGroundedTime <= _settings.CoyoteTime;
                if (IsGrounded && move.y < DropInputThreshold && TryDropThrough(now))
                {
                    _consumedJumpAt = _commands.JumpPressedAt;
                }
                else if (canGroundJump && (!IsSliding || CanStand()))
                {
                    if (IsSliding)
                    {
                        EndSlide();
                        _slideReadyAt = now + _settings.SlideCooldown;
                    }

                    _consumedJumpAt = _commands.JumpPressedAt;
                    _jumpedSinceGrounded = true;
                    _jumpRising = true;
                    velocityY = TraversalMetrics.JumpSpeed;
                }
                else if (!IsGrounded && !IsSliding && !_fanUsed && HasAbility(AbilityIds.Fan))
                {
                    _consumedJumpAt = _commands.JumpPressedAt;
                    _jumpedSinceGrounded = true;
                    _fanUsed = true;
                    _jumpRising = true;
                    velocityY = TraversalMetrics.FanJumpSpeed;
                }
            }

            // Releasing the button while rising cuts the jump: short tap, low hop.
            if (_jumpRising && !_commands.JumpHeld)
            {
                _jumpRising = false;
                if (velocityY > 0f)
                {
                    velocityY *= _settings.JumpCutMultiplier;
                }
            }

            if (velocityY <= 0f)
            {
                _jumpRising = false;
            }

            return velocityY;
        }

        private float ApplyGravity(float velocityY, float deltaTime)
        {
            float gravity = TraversalMetrics.Gravity;
            if (velocityY < 0f)
            {
                gravity *= TraversalMetrics.FallGravityMultiplier;
            }

            return Mathf.Max(velocityY - gravity * deltaTime, -TraversalMetrics.MaxFallSpeed);
        }

        private bool HasAbility(string id)
        {
            return _abilities != null && _abilities.Has(id);
        }

        private void UpdateGround(float velocityY)
        {
            IsGrounded = false;
            GroundCollider = null;
            if (velocityY > 0.01f)
            {
                return;
            }

            // A thin probe narrower than the body, starting just above the feet. Casting the whole body would
            // touch walls and one-way platform sides; such starting overlaps report an upward normal and
            // let Alice jump in mid-air. Starting overlaps of the probe itself are skipped for the same reason.
            Vector2 size = new Vector2(_collider.size.x - 2f * GroundProbeInset, GroundProbeHeight);
            Vector2 origin = _body.position + new Vector2(0f, GroundProbeLift + GroundProbeHeight * 0.5f);
            int count = Physics2D.BoxCast(origin, size, 0f, Vector2.down, _groundFilter, _hits,
                GroundProbeLift + GroundProbeDistance);
            for (int index = 0; index < count; index++)
            {
                RaycastHit2D probe = _hits[index];
                Collider2D hit = probe.collider;
                if (hit != _collider && hit != _ignoredPlatform && probe.fraction > 0f)
                {
                    IsGrounded = true;
                    GroundCollider = hit;
                    return;
                }
            }
        }

        private bool TryDropThrough(float now)
        {
            if (GroundCollider == null || !GroundCollider.usedByEffector
                || !GroundCollider.TryGetComponent(out PlatformEffector2D _))
            {
                return false;
            }

            RestoreIgnoredPlatform(float.PositiveInfinity);
            _ignoredPlatform = GroundCollider;
            _ignoreUntil = now + _settings.DropThroughTime;
            Physics2D.IgnoreCollision(_collider, _ignoredPlatform, true);
            IsGrounded = false;
            GroundCollider = null;
            _jumpedSinceGrounded = true;
            return true;
        }

        private void RestoreIgnoredPlatform(float now)
        {
            if (_ignoredPlatform != null && now >= _ignoreUntil)
            {
                Physics2D.IgnoreCollision(_collider, _ignoredPlatform, false);
                _ignoredPlatform = null;
            }
        }

        private bool CanStand()
        {
            Vector2 standing = TraversalMetrics.StandingSize;
            Vector2 center = _body.position + new Vector2(0f, (standing.y + StandClearance) * 0.5f);
            Vector2 size = new Vector2(standing.x * 0.95f, standing.y - StandClearance);
            int count = Physics2D.OverlapBox(center, size, 0f, _solidFilter, _overlaps);
            for (int index = 0; index < count; index++)
            {
                Collider2D other = _overlaps[index];
                if (other != _collider && !other.usedByEffector && other != _ignoredPlatform)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyBodySize(Vector2 size)
        {
            // Pivot stays at the feet, so shrinking for a slide keeps Alice on the ground.
            _collider.size = size;
            _collider.offset = new Vector2(0f, size.y * 0.5f);
        }
    }
}
