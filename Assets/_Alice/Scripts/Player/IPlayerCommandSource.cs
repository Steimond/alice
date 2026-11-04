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

        /// <summary>Time.time of the last Attack press, or negative infinity.</summary>
        float AttackPressedAt { get; }

        /// <summary>Time.time of the last Parry press, or negative infinity.</summary>
        float ParryPressedAt { get; }

        /// <summary>Time.time of the last UseAbility press (the chosen special technique), or negative infinity.</summary>
        float UseAbilityPressedAt { get; }

        /// <summary>Time.time of the last Heal press (smelling salts), or negative infinity.</summary>
        float HealPressedAt { get; }

        /// <summary>Time.time of the last Interact press (it also executes a stunned enemy), or negative infinity.</summary>
        float InteractPressedAt { get; }
    }
}
