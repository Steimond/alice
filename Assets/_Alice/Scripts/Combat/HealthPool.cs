using UnityEngine;

namespace Alice.Combat
{
    /// <summary>Hit points without timing or Unity objects, so the rules are testable in EditMode.</summary>
    public sealed class HealthPool
    {
        public HealthPool(int max)
        {
            Max = Mathf.Max(1, max);
            Current = Max;
        }

        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>Returns false when nothing changed: already dead or a non-positive amount.</summary>
        public bool Damage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return false;
            }

            Current = Mathf.Max(0, Current - amount);
            return true;
        }

        public void Refill()
        {
            Current = Max;
        }
    }
}
