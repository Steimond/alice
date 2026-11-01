using UnityEngine;

namespace Alice.Player
{
    /// <summary>What Alice is asked to do. Presses are timestamps so the motor can buffer them.</summary>
    public interface IPlayerCommandSource
    {
        Vector2 Move { get; }
        bool JumpHeld { get; }

        /// <summary>Time.time of the last Jump press, or negative infinity.</summary>
        float JumpPressedAt { get; }

        /// <summary>Time.time of the last Evade press, or negative infinity.</summary>
        float EvadePressedAt { get; }
    }
}
