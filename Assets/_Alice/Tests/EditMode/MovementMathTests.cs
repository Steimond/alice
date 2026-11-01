using Alice.Core;
using Alice.Player;
using NUnit.Framework;

namespace Alice.Tests
{
    public sealed class MovementMathTests
    {
        [TestCase(4.2f, 0.38f, 0.02f)]
        [TestCase(3f, 0.38f, 0.02f)]
        [TestCase(4.2f, 0.38f, 1f / 60f)]
        public void LaunchSpeed_ReachesHeightUnderFixedStepIntegration(float height, float timeToApex, float step)
        {
            float gravity = JumpMath.Gravity(height, timeToApex);
            float velocity = JumpMath.LaunchSpeed(height, gravity, step);

            // Same order as PlayerMotor: gravity first, then the step moves the body.
            float position = 0f;
            float apex = 0f;
            while (velocity > 0f)
            {
                velocity -= gravity * step;
                position += velocity * step;
                apex = position > apex ? position : apex;
            }

            Assert.That(apex, Is.EqualTo(height).Within(0.02f));
        }

        [Test]
        public void InputBuffer_AcceptsOnlyUnconsumedPressesInsideWindow()
        {
            Assert.That(InputBuffer.IsFresh(1f, float.NegativeInfinity, 1.05f, 0.1f), Is.True);
            Assert.That(InputBuffer.IsFresh(1f, float.NegativeInfinity, 1.2f, 0.1f), Is.False);
            Assert.That(InputBuffer.IsFresh(1f, 1f, 1.05f, 0.1f), Is.False);
            Assert.That(InputBuffer.IsFresh(float.NegativeInfinity, float.NegativeInfinity, 0f, 0.1f), Is.False);
        }
    }
}
