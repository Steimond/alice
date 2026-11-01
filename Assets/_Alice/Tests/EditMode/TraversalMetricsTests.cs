using Alice.Core;
using Alice.Player;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    /// <summary>
    /// Guards the level limits: if a movement constant changes, these fail before any room breaks.
    /// The flight is integrated the same way as PlayerMotor: gravity first, then the step moves the body.
    /// </summary>
    public sealed class TraversalMetricsTests
    {
        private const float Step = 0.02f;
        private const float Margin = 0.1f;

        [Test]
        public void GroundJump_ClearsLedgeLimitButNotOneTileMore()
        {
            float apex = Flight(false, float.NegativeInfinity, out _);

            Assert.That(apex, Is.GreaterThanOrEqualTo(TraversalMetrics.MaxJumpLedge + Margin));
            Assert.That(apex, Is.LessThan(TraversalMetrics.MaxJumpLedge + 1f));
        }

        [Test]
        public void FanJump_ClearsFanLedgeLimit()
        {
            float apex = Flight(true, float.NegativeInfinity, out _);

            Assert.That(apex, Is.GreaterThanOrEqualTo(TraversalMetrics.MaxFanLedge + Margin));
        }

        [Test]
        public void RunningJump_ClearsGapLimitButNotFanGap()
        {
            float reach = Reach(false);

            Assert.That(reach, Is.GreaterThanOrEqualTo(TraversalMetrics.MaxJumpGap + Margin));
            Assert.That(reach, Is.LessThan(TraversalMetrics.MaxFanGap), "The fan gap must need the fan.");
        }

        [Test]
        public void RunningFanJump_ClearsFanGapLimit()
        {
            Assert.That(Reach(true), Is.GreaterThanOrEqualTo(TraversalMetrics.MaxFanGap + Margin));
        }

        [Test]
        public void Passages_SeparateSlideFromStanding()
        {
            Assert.That(TraversalMetrics.SlidingSize.y, Is.LessThan(TraversalMetrics.SlidePassageHeight - Margin));
            Assert.That(TraversalMetrics.StandingSize.y, Is.GreaterThan(TraversalMetrics.SlidePassageHeight + Margin));
            Assert.That(TraversalMetrics.StandingSize.y, Is.LessThan(TraversalMetrics.MinWalkPassageHeight - Margin));
        }

        [Test]
        public void ReferenceResolution_ScalesByWholePixels()
        {
            Assert.That(1920 % WorldMetrics.ReferenceWidth, Is.Zero);
            Assert.That(1080 % WorldMetrics.ReferenceHeight, Is.Zero);
            Assert.That(3840 % WorldMetrics.ReferenceWidth, Is.Zero);
            Assert.That(2160 % WorldMetrics.ReferenceHeight, Is.Zero);
            Assert.That(WorldMetrics.ReferenceWidth % WorldMetrics.TilePixels, Is.Zero);
        }

        /// <summary>
        /// Horizontal distance at full run speed from take-off until the feet drop back to take-off height.
        /// Half the body width counts: Alice lands while her front half is over the far edge.
        /// </summary>
        private static float Reach(bool useFan)
        {
            Flight(useFan, 0f, out float airTime);
            return airTime * TraversalMetrics.MaxRunSpeed + TraversalMetrics.StandingSize.x * 0.5f;
        }

        /// <summary>Returns the apex. With a finite <paramref name="landingHeight"/> the flight runs until the feet drop below it.</summary>
        private static float Flight(bool useFan, float landingHeight, out float airTime)
        {
            float gravity = TraversalMetrics.Gravity;
            float velocity = JumpMath.LaunchSpeed(TraversalMetrics.JumpHeight, gravity, Step);
            bool fanUsed = !useFan;
            float height = 0f;
            float apex = 0f;
            airTime = 0f;

            while (airTime < 5f)
            {
                // The fan is pressed at the apex of the ground jump: the latest moment that still gains height.
                if (!fanUsed && velocity <= 0f)
                {
                    velocity = JumpMath.LaunchSpeed(TraversalMetrics.FanJumpHeight, gravity, Step);
                    fanUsed = true;
                }

                float pull = velocity < 0f ? gravity * TraversalMetrics.FallGravityMultiplier : gravity;
                velocity = Mathf.Max(velocity - pull * Step, -TraversalMetrics.MaxFallSpeed);
                height += velocity * Step;
                airTime += Step;
                apex = Mathf.Max(apex, height);

                bool apexOnly = float.IsNegativeInfinity(landingHeight);
                if (velocity < 0f && fanUsed && (apexOnly || height < landingHeight))
                {
                    break;
                }
            }

            return apex;
        }
    }
}
