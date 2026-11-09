using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// A thrown thing on a ballistic arc: a clothespin, later a kitchen knife. It hurts Alice once and breaks on her
    /// or on anything solid. It is not parried back (GDD section 4, projectiles without reflection); the slide and
    /// jumping aside avoid it. It spins by whole quarter turns, so the pixels stay on the grid.
    /// </summary>
    public sealed class ArcProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("The spin, one frame per step; drawn by quarter turns of one sprite.")]
        [SerializeField] private Sprite[] _spinFrames = new Sprite[0];
        [SerializeField, Min(0.01f)] private float _spinFrameTime = 0.06f;
        [SerializeField, Min(1)] private int _damage = 15;
        [SerializeField] private Knockback _knockback = new Knockback(new Vector2(4f, 4f), 0.18f);
        [Tooltip("Radius of the hit check, tiles.")]
        [SerializeField, Min(0.05f)] private float _radius = 0.35f;
        [SerializeField, Min(0.1f)] private float _lifetime = 4f;

        private readonly Collider2D[] _overlaps = new Collider2D[8];
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[2];

        private GameObject _thrower;
        private Vector2 _velocity;
        private float _gravity;
        private float _bornAt;
        private ContactFilter2D _solidFilter;
        private ContactFilter2D _bodyFilter;

        public void Bind(SpriteRenderer renderer, Sprite[] spinFrames)
        {
            _renderer = renderer;
            _spinFrames = spinFrames;
        }

        /// <summary>Starts the flight; the projectile owns itself from here and ends on a hit or after its lifetime.</summary>
        public void Launch(GameObject thrower, Vector2 velocity, float gravity)
        {
            _thrower = thrower;
            _velocity = velocity;
            _gravity = gravity;
            _bornAt = Time.time;
            _solidFilter = new ContactFilter2D { useTriggers = false };
            _bodyFilter = new ContactFilter2D { useTriggers = true };
            // The thrower may be gone by the time it lands (a rest resets the room); the arc still finishes.
            WorldReset.Happened += Remove;
        }

        private void OnDestroy()
        {
            WorldReset.Happened -= Remove;
        }

        /// <summary>
        /// Launch velocity that lands at <paramref name="target"/> after <paramref name="flightTime"/> under
        /// <paramref name="gravity"/>.
        /// </summary>
        public static Vector2 VelocityToHit(Vector2 from, Vector2 target, float flightTime, float gravity)
        {
            Vector2 delta = target - from;
            return new Vector2(delta.x / flightTime, delta.y / flightTime + 0.5f * gravity * flightTime);
        }

        private void FixedUpdate()
        {
            if (Time.time - _bornAt > _lifetime)
            {
                Remove();
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            _velocity.y -= _gravity * deltaTime;
            Vector2 position = transform.position;
            Vector2 step = _velocity * deltaTime;
            if (HitsAlice(position + step))
            {
                return;
            }

            int blocked = Physics2D.CircleCast(position, _radius * 0.5f, step.normalized, _solidFilter, _hits, step.magnitude);
            for (int index = 0; index < blocked; index++)
            {
                // Alice is solid for physics; a slide that dodged the hit must not stop it either.
                if (_hits[index].transform != PlayerPresence.Current)
                {
                    Remove();
                    return;
                }
            }

            transform.position = position + step;
        }

        private bool HitsAlice(Vector2 at)
        {
            int count = Physics2D.OverlapCircle(at, _radius, _bodyFilter, _overlaps);
            for (int index = 0; index < count; index++)
            {
                Collider2D other = _overlaps[index];
                GameObject owner = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
                if (owner == _thrower || owner.transform != PlayerPresence.Current || !owner.TryGetComponent(out IDamageable target))
                {
                    continue;
                }

                // A slide or a dodge frame lets it pass; it flies on.
                int direction = _velocity.x >= 0f ? 1 : -1;
                if (target.ReceiveHit(new HitInfo(_damage, direction, gameObject, HitKind.Other, 0f, _knockback)))
                {
                    Remove();
                    return true;
                }
            }

            return false;
        }

        private void LateUpdate()
        {
            if (_renderer == null || _spinFrames == null || _spinFrames.Length == 0)
            {
                return;
            }

            int frame = Mathf.FloorToInt((Time.time - _bornAt) / _spinFrameTime) % _spinFrames.Length;
            _renderer.sprite = _spinFrames[frame];
            _renderer.flipX = _velocity.x < 0f;
        }

        private void Remove()
        {
            Destroy(gameObject);
        }
    }
}
