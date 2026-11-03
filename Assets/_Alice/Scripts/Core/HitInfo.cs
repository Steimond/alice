using UnityEngine;

namespace Alice.Core
{
    /// <summary>One landed blow: how much it hurts and which way it pushes.</summary>
    public readonly struct HitInfo
    {
        public HitInfo(int damage, int directionX, GameObject source)
        {
            Damage = damage;
            DirectionX = directionX;
            Source = source;
        }

        public int Damage { get; }

        /// <summary>+1 pushes right, -1 pushes left.</summary>
        public int DirectionX { get; }

        public GameObject Source { get; }
    }
}
