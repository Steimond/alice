using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// How a blow was delivered. Enemies pick their death by the killing blow (GDD section 4):
    /// a thrust leaves a small wound, a slash cleaves, a spin beheads.
    /// </summary>
    public enum HitKind
    {
        Other,
        Thrust,
        Slash,
        Spin,
        Plunge,
        Counter,
        Execution
    }

    /// <summary>One landed blow: how much it hurts, which way it pushes and what kind of strike it was.</summary>
    public readonly struct HitInfo
    {
        public HitInfo(int damage, int directionX, GameObject source, HitKind kind = HitKind.Other, float parryWindow = 0f,
            Knockback knockback = default)
        {
            Damage = damage;
            DirectionX = directionX;
            Source = source;
            Kind = kind;
            ParryWindow = parryWindow;
            Knockback = knockback;
        }

        /// <summary>
        /// How hard this blow throws the one it hits; set by the attacker, so a heavy enemy throws Alice
        /// farther than a light one. Empty means the target's own default.
        /// </summary>
        public Knockback Knockback { get; }

        public int Damage { get; }

        /// <summary>+1 pushes right, -1 pushes left.</summary>
        public int DirectionX { get; }

        public GameObject Source { get; }

        public HitKind Kind { get; }

        /// <summary>
        /// How long before this blow lands a parry may start and still catch it, in seconds. Each attack
        /// sets its own; zero means it cannot be parried (grabs, area blows, GDD section 4).
        /// </summary>
        public float ParryWindow { get; }
    }
}
