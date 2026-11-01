using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice's movement values that decide which ledges, gaps and passages she can clear, in tiles.
    /// Every room is drawn against the level limits below, so these are constants: tuning them later
    /// would silently break rooms that already exist. Pure feel (acceleration, buffers, coyote time)
    /// stays tunable in <see cref="PlayerMovementSettings"/>. EditMode tests check that the movement
    /// values still satisfy the limits.
    /// </summary>
    public static class TraversalMetrics
    {
        // Body. The pivot is at the feet.
        public static readonly Vector2 StandingSize = new Vector2(0.75f, 2.85f);
        public static readonly Vector2 SlidingSize = new Vector2(0.75f, 1.1f);

        // Movement.
        public const float MaxRunSpeed = 7.5f;
        public const float JumpHeight = 4.2f;
        public const float TimeToApex = 0.38f;
        public const float FallGravityMultiplier = 1.8f;
        public const float MaxFallSpeed = 22f;
        public const float FanJumpHeight = 3f;
        public const float SlideSpeed = 12f;
        public const float SlideDuration = 0.35f;

        // Level limits: what a room may demand. Larger values need an ability or are deliberately unreachable.
        public const int MaxJumpLedge = 4;
        public const int MaxJumpGap = 5;
        public const int MaxFanLedge = 6;
        public const int MaxFanGap = 6;
        /// <summary>Passages of this height block standing and admit only a slide.</summary>
        public const int SlidePassageHeight = 2;
        /// <summary>Lowest passage Alice walks through upright.</summary>
        public const int MinWalkPassageHeight = 3;

        public static float Gravity => JumpMath.Gravity(JumpHeight, TimeToApex);
        public static float JumpSpeed => JumpMath.LaunchSpeed(JumpHeight, Gravity, Time.fixedDeltaTime);
        public static float FanJumpSpeed => JumpMath.LaunchSpeed(FanJumpHeight, Gravity, Time.fixedDeltaTime);
    }
}
