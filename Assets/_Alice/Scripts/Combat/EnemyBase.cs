using System.Collections.Generic;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>What an enemy wants drawn now; <see cref="EnemyClipView"/> turns it into a frame.</summary>
    public interface IEnemyClipSource
    {
        /// <summary>Stable clip name in the type's <see cref="EnemyClipSet"/>, e.g. "hover", "death".</summary>
        string ClipId { get; }

        /// <summary>Seconds since the clip started; below zero while a hit-stop holds the clip back.</summary>
        float ClipTime { get; }

        /// <summary>When above zero the clip is spread over this many seconds instead of its own frame time.</summary>
        float ClipSpan { get; }

        /// <summary>+1 faces right, -1 faces left; the art faces right.</summary>
        int Facing { get; }

        /// <summary>The blow that killed it; the death clip for that kind wins over the plain one.</summary>
        HitKind KillingBlow { get; }
    }

    /// <summary>
    /// Shared body of the archetypes added after the soldier (GDD section 9): a kinematic trigger with health, a
    /// post it returns to after a rest, a hit-stop, body contact that hurts and the reward on defeat. Each archetype
    /// only writes its own states. The pivot is at the bottom of the body box.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour, IBodyBlocker, IEnemyClipSource
    {
        private const float ProbeLift = 0.1f;
        private const float GroundProbeDepth = 0.4f;
        private const float StepMargin = 0.1f;
        protected const float FloorSearchDepth = 30f;

        [Tooltip("+1 faces right, -1 faces left at the start and after a rest.")]
        [SerializeField] private int _initialFacing = -1;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[4];
        protected readonly HashSet<IDamageable> Struck = new HashSet<IDamageable>();
        protected readonly Collider2D[] Overlaps = new Collider2D[8];

        private float _frozenUntil;
        private float _nextContactAt;
        private float _stateStartedAt;

        protected Rigidbody2D Body { get; private set; }
        protected BoxCollider2D BodyCollider { get; private set; }
        protected Health Health { get; private set; }
        protected ContactFilter2D SolidFilter { get; private set; }
        protected Vector2 Home { get; private set; }

        public abstract EnemySettings BaseSettings { get; }

        public int InitialFacing
        {
            get => _initialFacing;
            set => _initialFacing = value >= 0 ? 1 : -1;
        }

        public int Facing { get; protected set; }
        public HitKind KillingBlow { get; private set; }
        public bool IsDead { get; private set; }
        public Vector2 Position => Body != null ? Body.position : (Vector2)transform.position;

        /// <summary>Seconds in the current state, held back by the hit-stop.</summary>
        public float StateTime => Time.time - _stateStartedAt;

        public abstract string ClipId { get; }
        public virtual float ClipTime => StateTime;
        public virtual float ClipSpan => 0f;

        public virtual bool BlocksBodies => !IsDead;

        /// <summary>True while the body's touch hurts.</summary>
        protected virtual bool ContactHurts => !IsDead;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            BodyCollider = GetComponent<BoxCollider2D>();
            Health = GetComponent<Health>();
            if (BaseSettings == null)
            {
                Debug.LogError(GetType().Name + " needs settings.", this);
                enabled = false;
                return;
            }

            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            BodyCollider.isTrigger = true;
            BodyCollider.size = BaseSettings.BodySize;
            BodyCollider.offset = new Vector2(0f, BaseSettings.BodySize.y * 0.5f);
            SolidFilter = new ContactFilter2D { useTriggers = false };

            Home = Body.position;
            Health.Initialize(BaseSettings.MaxHealth, BaseSettings.HitInvulnerability);
            Facing = _initialFacing >= 0 ? 1 : -1;
            OnSpawn();
        }

        protected virtual void OnEnable()
        {
            Health.Damaged += HandleDamaged;
            Health.Died += HandleDied;
            WorldReset.Happened += ResetToHome;
        }

        protected virtual void OnDisable()
        {
            Health.Damaged -= HandleDamaged;
            Health.Died -= HandleDied;
            WorldReset.Happened -= ResetToHome;
        }

        /// <summary>Back to the post with full health, as after Alice rests (GDD section 4).</summary>
        public void ResetToHome()
        {
            Body.position = Home;
            transform.position = Home;
            Health.Refill();
            BodyCollider.enabled = true;
            KillingBlow = HitKind.Other;
            IsDead = false;
            Facing = _initialFacing >= 0 ? 1 : -1;
            _frozenUntil = 0f;
            OnSpawn();
        }

        private void FixedUpdate()
        {
            // Hit-stop: the body holds still for a moment after a blow, its state clock and frames with it.
            if (Time.time < _frozenUntil)
            {
                _stateStartedAt += Time.fixedDeltaTime;
                return;
            }

            Tick();
            if (ContactHurts)
            {
                TouchAlice();
            }
        }

        /// <summary>Puts the enemy into its first state; called on spawn and after every rest.</summary>
        protected abstract void OnSpawn();

        /// <summary>One physics step of the archetype's own behaviour.</summary>
        protected abstract void Tick();

        /// <summary>A blow landed and the enemy lives on.</summary>
        protected abstract void OnHurt(HitInfo hit);

        /// <summary>The enemy has just died; the archetype picks its dying state.</summary>
        protected abstract void OnKilled(HitInfo hit);

        protected void RestartStateClock()
        {
            _stateStartedAt = Time.time;
        }

        private void HandleDamaged(HitInfo hit)
        {
            _frozenUntil = Time.time + BaseSettings.HitStop;
            if (!Health.IsDead)
            {
                OnHurt(hit);
            }
        }

        private void HandleDied(HitInfo hit)
        {
            IsDead = true;
            KillingBlow = hit.Kind;
            Facing = -hit.DirectionX;
            BodyCollider.enabled = false;
            OnKilled(hit);
            EnemyDefeats.Raise(new EnemyDefeat(gameObject, hit, BaseSettings.DecreeReward, BaseSettings.ConfidenceReward));
        }

        /// <summary>
        /// Body contact hurts while it fights. Alice has no grace period after a hit, so the touch is spaced by its
        /// own interval, and her knockback carries her off the body. Only Alice: enemies never wear each other down.
        /// </summary>
        private void TouchAlice()
        {
            Transform alice = PlayerPresence.Current;
            if (alice == null || BaseSettings.ContactDamage <= 0 || Time.time < _nextContactAt)
            {
                return;
            }

            if (!alice.TryGetComponent(out Collider2D aliceBody) || !alice.TryGetComponent(out IDamageable target)
                || (alice.TryGetComponent(out IBodyPasser passer) && passer.PassesBodies))
            {
                return;
            }

            Bounds box = BodyBounds(BaseSettings.ContactMargin);
            Bounds other = aliceBody.bounds;
            if (!box.Intersects(other))
            {
                return;
            }

            int away = other.center.x >= Body.position.x ? 1 : -1;
            if (target.ReceiveHit(new HitInfo(BaseSettings.ContactDamage, away, gameObject,
                knockback: BaseSettings.ContactKnockback)))
            {
                _nextContactAt = Time.time + BaseSettings.ContactInterval;
            }
        }

        /// <summary>The body box in the world, widened sideways by <paramref name="margin"/>.</summary>
        protected Bounds BodyBounds(float margin = 0f)
        {
            Vector2 size = BaseSettings.BodySize;
            Vector2 center = Body.position + new Vector2(0f, size.y * 0.5f);
            return new Bounds(center, new Vector3(size.x + margin * 2f, size.y, 1f));
        }

        /// <summary>Hits Alice with the body box once per <see cref="Struck"/> set: a dive, a charge.</summary>
        protected int StrikeWithBody(HitInfo hit, float margin = 0f)
        {
            Bounds box = BodyBounds(margin);
            return MeleeHitbox.Strike(box.center, box.size, hit, gameObject, Struck, Overlaps);
        }

        /// <summary>Alice's feet, when she is seen inside the sight box and the leash.</summary>
        protected bool TryGetAlice(out Vector2 target, float extraLeash = 0f)
        {
            target = default;
            Transform player = PlayerPresence.Current;
            if (player == null)
            {
                return false;
            }

            target = player.position;
            Vector2 offset = target - Body.position;
            bool seen = Mathf.Abs(offset.x) <= BaseSettings.SightRange && Mathf.Abs(offset.y) <= BaseSettings.SightHeight;
            bool withinLeash = Mathf.Abs(target.x - Home.x) <= BaseSettings.LeashDistance + extraLeash;
            return seen && withinLeash;
        }

        /// <summary>
        /// Moves along the floor in <see cref="Facing"/> unless the floor ends or something solid stands in the way.
        /// Returns the distance moved; <paramref name="hitWall"/> tells a wall from a ledge.
        /// </summary>
        protected float MoveOnFloor(float distance, out bool hitWall)
        {
            hitWall = false;
            float halfWidth = BaseSettings.BodySize.x * 0.5f;
            Vector2 position = Body.position;
            Vector2 chest = position + new Vector2(0f, BaseSettings.BodySize.y * 0.5f);
            int blocked = Physics2D.Raycast(chest, new Vector2(Facing, 0f), SolidFilter, _hits,
                halfWidth + distance + StepMargin);
            for (int index = 0; index < blocked; index++)
            {
                // Alice is solid for physics, but she is a target, not a wall.
                if (_hits[index].collider != BodyCollider && _hits[index].transform != PlayerPresence.Current)
                {
                    hitWall = true;
                    float room = Mathf.Max(0f, _hits[index].distance - halfWidth - StepMargin);
                    distance = Mathf.Min(distance, room);
                    break;
                }
            }

            Vector2 front = position + new Vector2(Facing * (halfWidth + distance + StepMargin), ProbeLift);
            if (Physics2D.Raycast(front, Vector2.down, SolidFilter, _hits, ProbeLift + GroundProbeDepth) == 0)
            {
                return 0f;
            }

            if (distance <= 0f)
            {
                return 0f;
            }

            Body.MovePosition(position + new Vector2(Facing * distance, 0f));
            return distance;
        }

        /// <summary>Height of the solid floor under <paramref name="from"/>, or false when there is none.</summary>
        protected bool TryFindFloor(Vector2 from, out float floorY)
        {
            floorY = from.y;
            int count = Physics2D.Raycast(from + Vector2.up * ProbeLift, Vector2.down, SolidFilter, _hits, FloorSearchDepth);
            for (int index = 0; index < count; index++)
            {
                if (_hits[index].collider != BodyCollider && _hits[index].transform != PlayerPresence.Current)
                {
                    floorY = _hits[index].point.y;
                    return true;
                }
            }

            return false;
        }
    }
}
