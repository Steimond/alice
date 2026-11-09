using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// An enemy that keeps its distance and lobs things at Alice (the duck washerwoman EN12, GDD section 9; the
    /// kitchen thrower EN07 is the same archetype with other data). Distances in tiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Enemies/Thrower Settings", fileName = "Thrower")]
    public sealed class ThrowerSettings : EnemySettings
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.4f;
        [SerializeField, Min(0.01f)] private float _walkFrameDistance = 0.18f;
        [Tooltip("It walks up until Alice is this close, then stands and throws.")]
        [SerializeField, Min(0f)] private float _throwRange = 9f;
        [Tooltip("Closer than this it steps back to get room, when there is floor behind it.")]
        [SerializeField, Min(0f)] private float _keepAway = 3f;

        [Header("Throw")]
        [Tooltip("From reaching into the bag to letting go: the arm drawn back and held is the tell.")]
        [SerializeField, Min(0f)] private float _windup = 1f;
        [SerializeField, Min(0f)] private float _recovery = 0.35f;
        [SerializeField, Min(0f)] private float _cooldown = 1.1f;
        [Tooltip("Where the projectile leaves the hand, from the feet, with the facing on x.")]
        [SerializeField] private Vector2 _releasePoint = new Vector2(0.6f, 2.4f);
        [Tooltip("Flight time per tile of distance; the arc is aimed at Alice's feet.")]
        [SerializeField, Min(0.01f)] private float _flightTimePerTile = 0.085f;
        [SerializeField, Min(0.1f)] private float _minFlightTime = 0.45f;
        [SerializeField, Min(0.1f)] private float _projectileGravity = 30f;
        [SerializeField] private ArcProjectile _projectile;
        [SerializeField, Min(0f)] private float _staggerTime = 0.3f;

        public float WalkSpeed => _walkSpeed;
        public float WalkFrameDistance => _walkFrameDistance;
        public float ThrowRange => _throwRange;
        public float KeepAway => _keepAway;
        public float Windup => _windup;
        public float Recovery => _recovery;
        public float Cooldown => _cooldown;
        public Vector2 ReleasePoint => _releasePoint;
        public float FlightTimePerTile => _flightTimePerTile;
        public float MinFlightTime => _minFlightTime;
        public float ProjectileGravity => _projectileGravity;
        public float StaggerTime => _staggerTime;

        public ArcProjectile Projectile
        {
            get => _projectile;
            set => _projectile = value;
        }
    }
}
