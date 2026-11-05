using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// The mushroom (A02, GDD section 5): at a little door a bite makes Alice small enough to pass. Small is only
    /// for that passage (GDD 0.26): she can do nothing but run, and she grows back as soon as she walks away from
    /// the spot where she shrank, enters another room, is hit or the world resets, so death and reload always
    /// restore her size. How she looks while small is <see cref="PlayerSpriteView"/>'s business.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerSize : MonoBehaviour, IShrinkable
    {
        [Tooltip("She grows back once she is this far from where she shrank, in tiles.")]
        [SerializeField, Min(1f)] private float _growDistance = 4f;
        [Tooltip("She stands still while she bites the mushroom and shrinks, in seconds.")]
        [SerializeField, Min(0f)] private float _shrinkHold = 1.1f;
        [Tooltip("She stands still while she grows back, in seconds.")]
        [SerializeField, Min(0f)] private float _growHold = 0.5f;

        private Health _health;
        private PlayerMotor _motor;
        private string _roomWhenShrunk;
        private Vector2 _shrunkAt;

        public bool IsSmall { get; private set; }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<PlayerMotor>();
        }

        private void OnEnable()
        {
            _health.Damaged += OnHit;
            WorldReset.Happened += Grow;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnHit;
            WorldReset.Happened -= Grow;
        }

        private void Update()
        {
            if (!IsSmall || RoomTravel.IsTravelling)
            {
                return;
            }

            bool movedOn = RoomPresence.CurrentId != _roomWhenShrunk;
            bool walkedAway = Vector2.Distance(transform.position, _shrunkAt) > _growDistance;
            if (movedOn || walkedAway)
            {
                Grow();
            }
        }

        public void Shrink()
        {
            if (IsSmall)
            {
                return;
            }

            IsSmall = true;
            _roomWhenShrunk = RoomPresence.CurrentId;
            _shrunkAt = transform.position;
            _motor.RunOnly = true;
            _motor.Lock(_shrinkHold);
        }

        public void Grow()
        {
            GrowBack(true);
        }

        private void OnHit(HitInfo hit)
        {
            // The hit's own reaction takes her over; holding her still on top would fight the knockback.
            GrowBack(false);
        }

        private void GrowBack(bool hold)
        {
            if (!IsSmall)
            {
                return;
            }

            IsSmall = false;
            _motor.RunOnly = false;
            if (hold)
            {
                _motor.Lock(_growHold);
            }
        }
    }
}
