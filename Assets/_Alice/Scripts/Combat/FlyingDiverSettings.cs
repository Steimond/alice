using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// A flyer that hangs out of reach and dives at Alice (the clothes moth EN11, GDD section 9). Heights are of the
    /// body's bottom above the floor under it, in tiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Enemies/Flying Diver Settings", fileName = "FlyingDiver")]
    public sealed class FlyingDiverSettings : EnemySettings
    {
        [Header("Hover")]
        [Tooltip("Above Alice's jump with a blow (about 6.7 tiles): only the fan's second jump reaches it there.")]
        [SerializeField, Min(0f)] private float _hoverHeight = 7f;
        [Tooltip("How far to the side of Alice it hangs while it circles her.")]
        [SerializeField, Min(0f)] private float _hoverOffsetX = 2.5f;
        [SerializeField, Min(0f)] private float _driftSpeed = 2.4f;
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.25f;
        [SerializeField, Min(0.1f)] private float _bobPeriod = 1.1f;

        [Header("Dive")]
        [Tooltip("Starts the wind-up when Alice is this close sideways.")]
        [SerializeField, Min(0f)] private float _attackRangeX = 4f;
        [SerializeField, Min(0f)] private float _windup = 0.7f;
        [SerializeField, Min(0f)] private float _cooldown = 1.4f;
        [SerializeField, Min(0.1f)] private float _diveSpeed = 13f;
        [Tooltip("Lowest point of the dive: the body sweeps through Alice's head and shoulders.")]
        [SerializeField, Min(0f)] private float _diveBottomHeight = 1.2f;
        [Tooltip("The dive aims this far past where Alice stood, so it sweeps through her.")]
        [SerializeField, Min(0f)] private float _diveOvershoot = 0.6f;
        [SerializeField, Min(1)] private int _diveDamage = 15;
        [Tooltip("How long before the dive lands a parry may start and still catch it; 0 = cannot be parried.")]
        [SerializeField, Min(0f)] private float _diveParryWindow = 0.12f;
        [SerializeField] private Knockback _diveKnockback = new Knockback(new Vector2(5f, 6f), 0.22f);

        [Header("Exposed after the dive")]
        [Tooltip("Above the ground blows (they reach about 2.8 tiles) and inside a jump with a blow: caught only in the air.")]
        [SerializeField, Min(0f)] private float _exposedHeight = 3.1f;
        [Tooltip("Sideways drift out of the dive while it climbs to the exposed height.")]
        [SerializeField, Min(0f)] private float _exposedDrift = 1.2f;
        [SerializeField, Min(0f)] private float _exposedRiseTime = 0.25f;
        [SerializeField, Min(0f)] private float _exposedTime = 1.1f;
        [SerializeField, Min(0.1f)] private float _climbSpeed = 3.5f;

        [Header("Knocked down")]
        [Tooltip("A blow in the air or a parry throws it to the floor.")]
        [SerializeField, Min(0.1f)] private float _fallGravity = 40f;
        [SerializeField, Min(0f)] private float _groundedTime = 1.8f;
        [Tooltip("Getting up and lifting off; hits during it knock it down again.")]
        [SerializeField, Min(0.1f)] private float _riseTime = 0.7f;
        [SerializeField, Min(0f)] private float _fallFrameTime = 0.07f;

        public float HoverHeight => _hoverHeight;
        public float HoverOffsetX => _hoverOffsetX;
        public float DriftSpeed => _driftSpeed;
        public float BobAmplitude => _bobAmplitude;
        public float BobPeriod => _bobPeriod;
        public float AttackRangeX => _attackRangeX;
        public float Windup => _windup;
        public float Cooldown => _cooldown;
        public float DiveSpeed => _diveSpeed;
        public float DiveBottomHeight => _diveBottomHeight;
        public float DiveOvershoot => _diveOvershoot;
        public int DiveDamage => _diveDamage;
        public float DiveParryWindow => _diveParryWindow;
        public Knockback DiveKnockback => _diveKnockback;
        public float ExposedHeight => _exposedHeight;
        public float ExposedDrift => _exposedDrift;
        public float ExposedRiseTime => _exposedRiseTime;
        public float ExposedTime => _exposedTime;
        public float ClimbSpeed => _climbSpeed;
        public float FallGravity => _fallGravity;
        public float GroundedTime => _groundedTime;
        public float RiseTime => _riseTime;
        public float FallFrameTime => _fallFrameTime;
    }
}
