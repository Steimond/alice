using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// One enemy type built on the <see cref="MeleeEnemy"/> archetype: guard, approach, a telegraphed
    /// lunge, recovery. Distances are in tiles from the body centre; heights from the feet.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Melee Enemy Settings", fileName = "MeleeEnemy")]
    public sealed class MeleeEnemySettings : ScriptableObject
    {
        [Tooltip("Stable archetype ID from GDD section 9, e.g. EN09.")]
        [SerializeField] private string _archetypeId = "EN09";

        [Header("Body")]
        [SerializeField] private Vector2 _bodySize = new Vector2(1.2f, 3.2f);
        [SerializeField, Min(1)] private int _maxHealth = 3;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.12f;
        [SerializeField, Min(0f)] private float _staggerTime = 0.3f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _sightRange = 9f;
        [SerializeField, Min(0f)] private float _sightHeight = 3f;
        [Tooltip("How far from its post the enemy follows Alice before giving up.")]
        [SerializeField, Min(0f)] private float _leashDistance = 7f;

        [Header("Attack")]
        [Tooltip("Starts the attack once Alice's centre is this close.")]
        [SerializeField, Min(0f)] private float _attackStartRange = 2.6f;
        [SerializeField, Min(0f)] private float _windup = 0.5f;
        [SerializeField, Min(0f)] private float _active = 0.18f;
        [SerializeField, Min(0f)] private float _recovery = 0.7f;
        [SerializeField, Min(0f)] private float _cooldown = 0.5f;
        [Tooltip("Step forward during the blow; with the reach it gives the 3–4 tiles of pole weapons in GDD section 4.")]
        [SerializeField, Min(0f)] private float _lungeDistance = 0.75f;
        [SerializeField, Min(0f)] private float _reachStart = 0.3f;
        [Tooltip("Spear tip in the thrust frames, measured from the body centre.")]
        [SerializeField, Min(0f)] private float _reach = 2.4f;
        [SerializeField, Min(0f)] private float _hitHeight = 0.5f;
        [Tooltip("Height of the spear in the thrust frames; a slide passes under it.")]
        [SerializeField, Min(0f)] private float _hitCenterHeight = 2.25f;
        [SerializeField, Min(1)] private int _damage = 1;

        public string ArchetypeId => _archetypeId;
        public Vector2 BodySize => _bodySize;
        public int MaxHealth => _maxHealth;
        public float HitInvulnerability => _hitInvulnerability;
        public float StaggerTime => _staggerTime;
        public float WalkSpeed => _walkSpeed;
        public float SightRange => _sightRange;
        public float SightHeight => _sightHeight;
        public float LeashDistance => _leashDistance;
        public float AttackStartRange => _attackStartRange;
        public MeleeAttackTimeline Timeline => new MeleeAttackTimeline(_windup, _active, _recovery);
        public float Cooldown => _cooldown;
        public float LungeDistance => _lungeDistance;
        public float ReachStart => _reachStart;
        public float Reach => _reach;
        public float HitHeight => _hitHeight;
        public float HitCenterHeight => _hitCenterHeight;
        public int Damage => _damage;
    }
}
