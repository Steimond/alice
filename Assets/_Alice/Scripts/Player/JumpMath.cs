using UnityEngine;

namespace Alice.Player
{
    /// <summary>Derives physics values from designer-facing jump height and timing.</summary>
    public static class JumpMath
    {
        public static float Gravity(float height, float timeToApex)
        {
            return 2f * height / (timeToApex * timeToApex);
        }

        /// <summary>
        /// Launch speed whose apex equals <paramref name="height"/> under the motor's fixed-step integration:
        /// gravity is applied before the step moves the body, which alone would lose about v * dt / 2 of height.
        /// </summary>
        public static float LaunchSpeed(float height, float gravity, float fixedDeltaTime)
        {
            float halfStep = fixedDeltaTime * 0.5f;
            return gravity * (halfStep + Mathf.Sqrt(halfStep * halfStep + 2f * height / gravity));
        }
    }
}
