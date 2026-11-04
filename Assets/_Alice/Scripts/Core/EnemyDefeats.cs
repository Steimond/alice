using System;
using UnityEngine;

namespace Alice.Core
{
    /// <summary>One beaten enemy and what beating it is worth (GDD sections 4 and 10).</summary>
    public readonly struct EnemyDefeat
    {
        public EnemyDefeat(GameObject enemy, HitInfo killingBlow, int decrees, float confidence)
        {
            Enemy = enemy;
            KillingBlow = killingBlow;
            Decrees = decrees;
            Confidence = confidence;
        }

        public GameObject Enemy { get; }
        public HitInfo KillingBlow { get; }

        /// <summary>Queen's decrees, the currency, that fall from the body.</summary>
        public int Decrees { get; }

        /// <summary>Confidence Alice gains for an ordinary kill; an execution multiplies it.</summary>
        public float Confidence { get; }
    }

    /// <summary>Enemies announce their defeat here; Alice's purse and confidence listen without knowing enemy types.</summary>
    public static class EnemyDefeats
    {
        public static event Action<EnemyDefeat> Happened;

        public static void Raise(EnemyDefeat defeat)
        {
            Happened?.Invoke(defeat);
        }
    }
}
