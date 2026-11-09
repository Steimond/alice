using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// What every enemy type has whatever its archetype: body, health, how its touch hurts and what beating it is
    /// worth. Each archetype adds its own behaviour numbers in a subclass. Distances in tiles, times in seconds.
    /// </summary>
    public abstract class EnemySettings : ScriptableObject
    {
        [Tooltip("Stable archetype ID from GDD section 9, e.g. EN11.")]
        [SerializeField] private string _archetypeId = "EN00";

        [Header("Body")]
        [Tooltip("Hit and touch box; its bottom is the object's pivot.")]
        [SerializeField] private Vector2 _bodySize = new Vector2(1.2f, 1.6f);
        [SerializeField, Min(1)] private int _maxHealth = 3;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.12f;
        [Tooltip("A blow that lands freezes the enemy for this long, together with the attacker's strike.")]
        [SerializeField, Min(0f)] private float _hitStop = 0.08f;

        [Header("Stun and execution")]
        [Tooltip("Stable ID of this type's execution; Alice keeps frames and timing under the same ID. Empty = no execution and no stun.")]
        [SerializeField] private string _executionId = string.Empty;
        [Tooltip("A blow that leaves this much health or less stuns it once per life; only then can it be executed. 0 = never.")]
        [SerializeField, Min(0)] private int _stunAtHealth = 1;
        [Tooltip("Chance that crossing the threshold stuns it and offers the execution; rolled once per life.")]
        [SerializeField, Range(0f, 1f)] private float _stunChance = 0.5f;
        [Tooltip("How long the stun lasts: Alice may execute, finish with any blow or let it recover.")]
        [SerializeField, Min(0f)] private float _stunnedTime = 2.5f;
        [Tooltip("Length of Alice's execution of this type; the enemy's execution death is spread over it so both end together.")]
        [SerializeField, Min(0.1f)] private float _executionTime = 1.3f;

        [Header("Sight")]
        [SerializeField, Min(0f)] private float _sightRange = 9f;
        [SerializeField, Min(0f)] private float _sightHeight = 3f;
        [Tooltip("Once it has seen Alice it keeps her while she is this close sideways to where it stands now, wider than the sight range.")]
        [SerializeField, Min(0f)] private float _loseSightRange = 15f;
        [Tooltip("The same for height, wider than the sight height.")]
        [SerializeField, Min(0f)] private float _loseSightHeight = 6f;
        [Tooltip("How far from its post it walks while following Alice; past it, it waits facing her.")]
        [SerializeField, Min(0f)] private float _leashDistance = 12f;

        [Header("Patrol")]
        [Tooltip("While it has not seen Alice it strolls to random points this far from its post and back; 0 = it stands at its post.")]
        [SerializeField, Min(0f)] private float _patrolRange = 3f;
        [Tooltip("Strolling speed, slower than its pursuit.")]
        [SerializeField, Min(0f)] private float _patrolSpeed = 1.2f;
        [Tooltip("How long it stands at each point, seconds: a random time between the two.")]
        [SerializeField] private Vector2 _patrolPause = new Vector2(1.5f, 4f);

        [Header("Body contact")]
        [Tooltip("Pressing against the living body hurts (GDD section 4); 0 = the body is harmless.")]
        [SerializeField, Min(0)] private int _contactDamage = 10;
        [SerializeField, Min(0f)] private float _contactMargin = 0.1f;
        [SerializeField, Min(0.05f)] private float _contactInterval = 0.5f;
        [SerializeField] private Knockback _contactKnockback = new Knockback(new Vector2(6f, 5f), 0.22f);

        [Header("Reward")]
        [Tooltip("Queen's decrees, the currency, dropped on defeat.")]
        [SerializeField, Min(0)] private int _decreeReward = 15;
        [Tooltip("Confidence Alice gains for beating it.")]
        [SerializeField, Min(0f)] private float _confidenceReward = 5f;

        public string ArchetypeId => _archetypeId;
        public string ExecutionId => _executionId;
        public int StunAtHealth => _stunAtHealth;
        public float StunChance => _stunChance;
        public float StunnedTime => _stunnedTime;
        public float ExecutionTime => _executionTime;
        public Vector2 BodySize => _bodySize;
        public int MaxHealth => _maxHealth;
        public float HitInvulnerability => _hitInvulnerability;
        public float HitStop => _hitStop;
        public float SightRange => _sightRange;
        public float SightHeight => _sightHeight;
        public float LoseSightRange => _loseSightRange;
        public float LoseSightHeight => _loseSightHeight;
        public float LeashDistance => _leashDistance;
        public float PatrolRange => _patrolRange;
        public float PatrolSpeed => _patrolSpeed;
        public Vector2 PatrolPause => _patrolPause;
        public int ContactDamage => _contactDamage;
        public float ContactMargin => _contactMargin;
        public float ContactInterval => _contactInterval;
        public Knockback ContactKnockback => _contactKnockback;
        public int DecreeReward => _decreeReward;
        public float ConfidenceReward => _confidenceReward;
    }
}
