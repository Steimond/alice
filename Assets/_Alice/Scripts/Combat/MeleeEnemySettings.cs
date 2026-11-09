using Alice.Core;
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
        [Tooltip("Stable ID of this type's execution; Alice keeps frames and timing under the same ID. Empty = no execution.")]
        [SerializeField] private string _executionId = "card_tear";

        [Header("Body")]
        [SerializeField] private Vector2 _bodySize = new Vector2(1.2f, 3.2f);
        [SerializeField, Min(1)] private int _maxHealth = 3;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.12f;
        [SerializeField, Min(0f)] private float _staggerTime = 0.3f;
        [Tooltip("Thrown open by a parry: the time Alice has for the counter.")]
        [SerializeField, Min(0f)] private float _openedTime = 1.2f;
        [Tooltip("A blow that leaves this much health or less stuns the enemy once per life; only then can it be executed. 0 = never.")]
        [SerializeField, Min(0)] private int _stunAtHealth = 1;
        [Tooltip("How long the stun lasts: Alice may execute, finish with any blow or let it recover.")]
        [SerializeField, Min(0f)] private float _stunnedTime = 2.5f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _sightRange = 9f;
        [SerializeField, Min(0f)] private float _sightHeight = 3f;
        [Tooltip("Once it has seen Alice it keeps her while she is this close sideways to where it stands now, wider than the sight range.")]
        [SerializeField, Min(0f)] private float _loseSightRange = 15f;
        [Tooltip("The same for height, wider than the sight height.")]
        [SerializeField, Min(0f)] private float _loseSightHeight = 6f;
        [Tooltip("How far from its post it walks while following Alice; past it, it waits facing her.")]
        [SerializeField, Min(0f)] private float _leashDistance = 12f;

        [Header("Attack")]
        [Tooltip("Starts the attack once Alice's centre is this close.")]
        [SerializeField, Min(0f)] private float _attackStartRange = 3.2f;
        [SerializeField, Min(0f)] private float _windup = 0.7f;
        [SerializeField, Min(0f)] private float _active = 0.18f;
        [SerializeField, Min(0f)] private float _recovery = 0.7f;
        [SerializeField, Min(0f)] private float _cooldown = 0.5f;
        [Tooltip("Step forward during the blow; with the reach it gives the 3–4 tiles of pole weapons in GDD section 4.")]
        [SerializeField, Min(0f)] private float _lungeDistance = 0.5f;
        [SerializeField, Min(0f)] private float _reachStart = 0.3f;
        [Tooltip("Spear tip in the thrust frames, measured from the body centre.")]
        [SerializeField, Min(0f)] private float _reach = 3.3f;
        [SerializeField, Min(0f)] private float _hitHeight = 0.5f;
        [Tooltip("Height of the spear in the thrust frames; a slide passes under it.")]
        [SerializeField, Min(0f)] private float _hitCenterHeight = 2.25f;
        [Tooltip("Alice's clarity is a 100-point scale: an ordinary blow takes about a fifth of it.")]
        [SerializeField, Min(1)] private int _damage = 20;
        [Tooltip("How long before the blow lands a parry may start and still catch it; 0 = cannot be parried.")]
        [SerializeField, Min(0f)] private float _parryWindow = 0.12f;
        [Tooltip("How far the blow throws Alice; a property of the enemy, heavier ones throw farther.")]
        [SerializeField] private Knockback _knockback = new Knockback(new Vector2(6f, 5f), 0.22f);

        [Header("Body contact")]
        [Tooltip("Pressing against the living body hurts (GDD section 4); smaller than a blow and never parried. Off while stunned or opened, so Alice can step in to finish it.")]
        [SerializeField, Min(0)] private int _contactDamage = 10;
        [Tooltip("How far beyond the body the touch reaches, in tiles; a little more than the gap Alice stops at.")]
        [SerializeField, Min(0f)] private float _contactMargin = 0.1f;
        [Tooltip("Alice has no grace period after a hit, so the touch repeats no more often than this, in seconds.")]
        [SerializeField, Min(0.05f)] private float _contactInterval = 0.5f;
        [Tooltip("How far touching the body throws Alice.")]
        [SerializeField] private Knockback _contactKnockback = new Knockback(new Vector2(6f, 5f), 0.22f);

        [Tooltip("A blow that lands freezes the enemy for this long, together with the attacker's strike.")]
        [SerializeField, Min(0f)] private float _hitStop = 0.08f;

        public float HitStop => _hitStop;

        [Header("Reward")]
        [Tooltip("Queen's decrees, the currency, dropped on defeat.")]
        [SerializeField, Min(0)] private int _decreeReward = 15;
        [Tooltip("Confidence Alice gains for beating it; it builds slowly.")]
        [SerializeField, Min(0f)] private float _confidenceReward = 5f;

        public int DecreeReward => _decreeReward;
        public float ConfidenceReward => _confidenceReward;
        public string ArchetypeId => _archetypeId;
        public string ExecutionId => _executionId;
        public Vector2 BodySize => _bodySize;
        public int MaxHealth => _maxHealth;
        public float HitInvulnerability => _hitInvulnerability;
        public float StaggerTime => _staggerTime;
        public float OpenedTime => _openedTime;
        public int StunAtHealth => _stunAtHealth;
        public float StunnedTime => _stunnedTime;
        public float WalkSpeed => _walkSpeed;
        public float SightRange => _sightRange;
        public float SightHeight => _sightHeight;
        public float LoseSightRange => _loseSightRange;
        public float LoseSightHeight => _loseSightHeight;
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
        public float ParryWindow => _parryWindow;
        public int ContactDamage => _contactDamage;
        public float ContactMargin => _contactMargin;
        public Knockback Knockback => _knockback;
        public Knockback ContactKnockback => _contactKnockback;
        public float ContactInterval => _contactInterval;
    }
}
