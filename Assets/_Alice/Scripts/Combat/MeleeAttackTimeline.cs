using UnityEngine;

namespace Alice.Combat
{
    public enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    /// <summary>
    /// The three readable parts of every melee attack (GDD section 4): preparation, the blow itself and
    /// recovery. Only the active part hurts; recovery is the opening the other side punishes.
    /// </summary>
    public readonly struct MeleeAttackTimeline
    {
        public MeleeAttackTimeline(float windup, float active, float recovery)
        {
            Windup = Mathf.Max(0f, windup);
            Active = Mathf.Max(0f, active);
            Recovery = Mathf.Max(0f, recovery);
        }

        public float Windup { get; }
        public float Active { get; }
        public float Recovery { get; }
        public float Duration => Windup + Active + Recovery;

        public AttackPhase PhaseAt(float elapsed)
        {
            if (elapsed < 0f || elapsed >= Duration)
            {
                return AttackPhase.None;
            }

            if (elapsed < Windup)
            {
                return AttackPhase.Windup;
            }

            return elapsed < Windup + Active ? AttackPhase.Active : AttackPhase.Recovery;
        }

        /// <summary>0 at the start of the current phase, approaching 1 at its end.</summary>
        public float PhaseProgress(float elapsed)
        {
            switch (PhaseAt(elapsed))
            {
                case AttackPhase.Windup:
                    return Fraction(elapsed, Windup);
                case AttackPhase.Active:
                    return Fraction(elapsed - Windup, Active);
                case AttackPhase.Recovery:
                    return Fraction(elapsed - Windup - Active, Recovery);
                default:
                    return 0f;
            }
        }

        private static float Fraction(float elapsed, float length)
        {
            return length > 0f ? Mathf.Clamp01(elapsed / length) : 0f;
        }
    }
}
