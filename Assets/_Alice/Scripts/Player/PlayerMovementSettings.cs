using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Feel tuning for Alice that does not change which ledges and gaps she clears.
    /// Distances, speeds and body size are fixed in <see cref="TraversalMetrics"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Player Movement Settings", fileName = "AliceMovement")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Run")]
        [SerializeField, Min(0.001f)] private float _accelerationTime = 0.08f;
        [SerializeField, Min(0.001f)] private float _decelerationTime = 0.06f;
        [SerializeField, Min(0.001f)] private float _airAccelerationTime = 0.12f;

        [Header("Jump")]
        [SerializeField, Range(0f, 1f)] private float _jumpCutMultiplier = 0.45f;
        [SerializeField, Range(0f, 0.15f)] private float _coyoteTime = 0.1f;
        [SerializeField, Range(0f, 0.2f)] private float _jumpBufferTime = 0.12f;
        [SerializeField, Min(0f)] private float _dropThroughTime = 0.25f;

        [Header("Slide")]
        [SerializeField, Min(0f)] private float _slideCooldown = 0.3f;
        [SerializeField, Min(0f)] private float _slideInvulnerabilityStart = 0.04f;
        [SerializeField, Min(0f)] private float _slideInvulnerabilityEnd = 0.26f;
        [SerializeField, Min(0f)] private float _evadeBufferTime = 0.1f;

        [Header("Enemy bodies")]
        [Tooltip("How fast Alice is pushed out sideways when she ends up inside a living enemy (landing on it, a slide ending under it). Tiles per second.")]
        [SerializeField, Min(0f)] private float _bodyPushOutSpeed = 6f;

        [Header("Safe return")]
        [SerializeField, Min(0f)] private float _hazardFreezeTime = 0.35f;

        public float GroundAcceleration => TraversalMetrics.MaxRunSpeed / _accelerationTime;
        public float GroundDeceleration => TraversalMetrics.MaxRunSpeed / _decelerationTime;
        public float AirAcceleration => TraversalMetrics.MaxRunSpeed / _airAccelerationTime;
        public float JumpCutMultiplier => _jumpCutMultiplier;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public float DropThroughTime => _dropThroughTime;
        public float SlideCooldown => _slideCooldown;
        public float SlideInvulnerabilityStart => _slideInvulnerabilityStart;
        public float SlideInvulnerabilityEnd => _slideInvulnerabilityEnd;
        public float EvadeBufferTime => _evadeBufferTime;
        public float HazardFreezeTime => _hazardFreezeTime;
        public float BodyPushOutSpeed => _bodyPushOutSpeed;
    }
}
