using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// A runner that charges in a straight line along the floor (the race rat EN02, GDD section 9). Distances in
    /// tiles, speeds in tiles per second.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Enemies/Charger Settings", fileName = "Charger")]
    public sealed class ChargerSettings : EnemySettings
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _walkSpeed = 2.2f;
        [Tooltip("Tiles per frame of the run clip while it trots, so the feet match the floor.")]
        [SerializeField, Min(0.01f)] private float _runFrameDistance = 0.45f;

        [Header("Charge")]
        [Tooltip("Starts the wind-up when Alice is this close and on its level.")]
        [SerializeField, Min(0f)] private float _chargeStartRange = 8f;
        [Tooltip("Never charges from closer than this: it backs off a step first.")]
        [SerializeField, Min(0f)] private float _minChargeRange = 2.5f;
        [SerializeField, Min(0f)] private float _windup = 0.8f;
        [Tooltip("Faster than Alice runs (7.5): she cannot outrun it, only slide through or jump over.")]
        [SerializeField, Min(0.1f)] private float _chargeSpeed = 11f;
        [SerializeField, Min(0f)] private float _chargeDistance = 12f;
        [Tooltip("Runs on this far past Alice before it brakes.")]
        [SerializeField, Min(0f)] private float _overshoot = 2.5f;
        [SerializeField, Min(1)] private int _chargeDamage = 25;
        [Tooltip("A ram: 0 = cannot be parried (GDD section 4); slide through it or jump over it.")]
        [SerializeField, Min(0f)] private float _chargeParryWindow;
        [SerializeField] private Knockback _chargeKnockback = new Knockback(new Vector2(10f, 7f), 0.3f);
        [SerializeField, Min(0f)] private float _skidTime = 0.55f;
        [SerializeField, Min(0f)] private float _skidDistance = 1.2f;
        [Tooltip("Head first into a wall: it reels and stays open this long.")]
        [SerializeField, Min(0f)] private float _dazedTime = 1.8f;
        [SerializeField, Min(0f)] private float _cooldown = 0.9f;
        [SerializeField, Min(0f)] private float _staggerTime = 0.3f;
        [SerializeField, Min(0.01f)] private float _chargeFrameTime = 0.055f;

        public float WalkSpeed => _walkSpeed;
        public float RunFrameDistance => _runFrameDistance;
        public float ChargeStartRange => _chargeStartRange;
        public float MinChargeRange => _minChargeRange;
        public float Windup => _windup;
        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDistance => _chargeDistance;
        public float Overshoot => _overshoot;
        public int ChargeDamage => _chargeDamage;
        public float ChargeParryWindow => _chargeParryWindow;
        public Knockback ChargeKnockback => _chargeKnockback;
        public float SkidTime => _skidTime;
        public float SkidDistance => _skidDistance;
        public float DazedTime => _dazedTime;
        public float Cooldown => _cooldown;
        public float StaggerTime => _staggerTime;
        public float ChargeFrameTime => _chargeFrameTime;
    }
}
