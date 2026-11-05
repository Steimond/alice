using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice's confidence (GDD section 4): earned slowly by beating enemies, spent by special techniques,
    /// each at its own price. Plain numbers, so the rules are testable in EditMode.
    /// </summary>
    public sealed class ConfidencePool
    {
        public ConfidencePool(float max, float current = 0f)
        {
            Max = Mathf.Max(1f, max);
            Current = Mathf.Clamp(current, 0f, Max);
        }

        public float Max { get; private set; }
        public float Current { get; private set; }
        public float Share => Current / Max;

        public void Gain(float amount)
        {
            if (amount > 0f)
            {
                Current = Mathf.Min(Max, Current + amount);
            }
        }

        /// <summary>Loading a save puts back what she had.</summary>
        public void Set(float amount)
        {
            Current = Mathf.Clamp(amount, 0f, Max);
        }

        public bool CanAfford(float cost)
        {
            return cost <= Current;
        }

        /// <summary>All or nothing: a technique Alice cannot pay for does not start.</summary>
        public bool TrySpend(float cost)
        {
            if (cost < 0f || !CanAfford(cost))
            {
                return false;
            }

            Current -= cost;
            return true;
        }

        /// <summary>A "Eat me" cake raises the maximum; what she has stays.</summary>
        public void RaiseMax(float amount)
        {
            if (amount > 0f)
            {
                Max += amount;
            }
        }
    }
}
