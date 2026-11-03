using Alice.Combat;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice's fighting numbers for the M2 experiment. The needle thrust follows GDD 0.16: about 2.75 tiles
    /// from her centre in a narrow band; M2 may move reach by ±0.25 tile without new frames.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Player Combat Settings", fileName = "AliceCombat")]
    public sealed class PlayerCombatSettings : ScriptableObject
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int _maxHealth = 5;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.9f;
        [SerializeField] private Vector2 _knockbackVelocity = new Vector2(6f, 5f);
        [SerializeField, Min(0f)] private float _knockbackTime = 0.22f;
        [SerializeField, Min(0f)] private float _defeatFreezeTime = 0.6f;

        [Header("Needle thrust")]
        [SerializeField, Min(0f)] private float _thrustWindup = 0.07f;
        [SerializeField, Min(0f)] private float _thrustActive = 0.1f;
        [SerializeField, Min(0f)] private float _thrustRecovery = 0.2f;
        [SerializeField, Min(0f)] private float _thrustReachStart = 0.2f;
        [SerializeField, Min(0f)] private float _thrustReach = 2.75f;
        [SerializeField, Min(0f)] private float _thrustHeight = 0.5f;
        [Tooltip("Height of the needle above the feet.")]
        [SerializeField, Min(0f)] private float _thrustCenterHeight = 1.6f;
        [SerializeField, Min(1)] private int _thrustDamage = 1;
        [SerializeField, Range(0f, 0.2f)] private float _attackBufferTime = 0.12f;

        public int MaxHealth => _maxHealth;
        public float HitInvulnerability => _hitInvulnerability;
        public Vector2 KnockbackVelocity => _knockbackVelocity;
        public float KnockbackTime => _knockbackTime;
        public float DefeatFreezeTime => _defeatFreezeTime;
        public MeleeAttackTimeline Thrust => new MeleeAttackTimeline(_thrustWindup, _thrustActive, _thrustRecovery);
        public float ThrustReachStart => _thrustReachStart;
        public float ThrustReach => _thrustReach;
        public float ThrustHeight => _thrustHeight;
        public float ThrustCenterHeight => _thrustCenterHeight;
        public int ThrustDamage => _thrustDamage;
        public float AttackBufferTime => _attackBufferTime;
    }
}
