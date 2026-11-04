using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice's fighting numbers for the M2 experiment (GDD section 4). The thrust reaches about 2.75 tiles
    /// in a narrow band, the slash and the air attack cover a wide arc of about 2 tiles; M2 may move reach
    /// by ±0.25 tile without new frames.
    /// </summary>
    /// <summary>Alice's side of one enemy type's execution: where she stands and how long each part lasts.</summary>
    [System.Serializable]
    public struct ExecutionSpec
    {
        [SerializeField] private string _id;
        [Tooltip("Step in and grab.")]
        [SerializeField, Min(0f)] private float _windup;
        [Tooltip("The killing blow.")]
        [SerializeField, Min(0f)] private float _active;
        [Tooltip("Finishing the body off and stepping back.")]
        [SerializeField, Min(0f)] private float _recovery;
        [Tooltip("Distance between Alice's and the enemy's feet for the paired frames, in tiles.")]
        [SerializeField, Min(0f)] private float _distance;

        public ExecutionSpec(string id, float windup, float active, float recovery, float distance)
        {
            _id = id;
            _windup = windup;
            _active = active;
            _recovery = recovery;
            _distance = distance;
        }

        public string Id => _id;
        public MeleeAttackTimeline Timeline => new MeleeAttackTimeline(_windup, _active, _recovery);
        public float Distance => _distance;
    }

    [CreateAssetMenu(menuName = "Alice/Player Combat Settings", fileName = "AliceCombat")]
    public sealed class PlayerCombatSettings : ScriptableObject
    {
        [Header("Clarity (Alice's health, GDD section 4)")]
        [SerializeField, Min(1)] private int _maxHealth = 100;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.9f;
        [SerializeField] private Vector2 _knockbackVelocity = new Vector2(6f, 5f);
        [SerializeField, Min(0f)] private float _knockbackTime = 0.22f;
        [Tooltip("How long the hurt frames show after a hit that does not kill.")]
        [SerializeField, Min(0.01f)] private float _hurtTime = 0.3f;

        [Header("Defeat and waking (GDD section 4: a fall, darkness, a sharp breath at the rest)")]
        [Tooltip("The collapse frames; the screen darkens over the last part of it.")]
        [SerializeField, Min(0.01f)] private float _defeatFallTime = 1.3f;
        [SerializeField, Min(0.01f)] private float _defeatFadeTime = 0.5f;
        [Tooltip("Full darkness; Alice is moved to the rest and the world is reset at its start.")]
        [SerializeField, Min(0f)] private float _defeatDarkTime = 0.5f;
        [Tooltip("The gasp and getting up; the darkness lifts over its first part.")]
        [SerializeField, Min(0.01f)] private float _wakeTime = 1f;
        [SerializeField, Min(0.01f)] private float _wakeFadeTime = 0.35f;

        [Header("Smelling salts: restore clarity")]
        [SerializeField, Min(0)] private int _saltCharges = 2;
        [Tooltip("Share of the clarity scale one charge restores.")]
        [SerializeField, Range(0f, 1f)] private float _saltRestoreShare = 0.4f;
        [Tooltip("The whole use, standing still; a hit during it wastes the charge.")]
        [SerializeField, Min(0.05f)] private float _saltTime = 0.8f;
        [Tooltip("Share of the use after which the clarity comes back (the sniff).")]
        [SerializeField, Range(0f, 1f)] private float _saltTakesEffectAt = 0.55f;
        [SerializeField, Range(0f, 0.2f)] private float _saltBufferTime = 0.1f;

        [Header("Confidence: spent by special techniques")]
        [SerializeField, Min(1f)] private float _confidenceMax = 100f;
        [Tooltip("An execution gives this many times the enemy's ordinary confidence reward.")]
        [SerializeField, Min(1f)] private float _executionConfidenceFactor = 3f;

        [Header("Needle")]
        [Tooltip("First hit on the ground: far and narrow.")]
        [SerializeField] private MeleeMove _thrust = new MeleeMove(HitKind.Thrust, 0.07f, 0.1f, 0.2f, 0.2f, 2.75f, 0.5f, 1.6f, 1);
        [Tooltip("Second hit of the combo: a wide arc in front.")]
        [SerializeField] private MeleeMove _slash = new MeleeMove(HitKind.Slash, 0.08f, 0.12f, 0.28f, 0f, 2f, 2.6f, 1.5f, 1);
        [Tooltip("Attack in the air: the same arc, reaching below the feet.")]
        [SerializeField] private MeleeMove _airSlash = new MeleeMove(HitKind.Slash, 0.06f, 0.12f, 0.2f, 0f, 2f, 2.6f, 1.2f, 1);
        [SerializeField, Range(0f, 0.2f)] private float _attackBufferTime = 0.12f;

        [Header("Parry and counter")]
        [Tooltip("Whole parry when nothing is caught. How early it may start before a blow is set by each attack (its parry window); after that Alice stands open.")]
        [SerializeField, Min(0f)] private float _parryDuration = 0.5f;
        [Tooltip("How long Alice holds after a successful parry before she can act.")]
        [SerializeField, Min(0f)] private float _parrySuccessHold = 0.12f;
        [Tooltip("After a successful parry the next attack within this time is the counter.")]
        [SerializeField, Min(0f)] private float _counterWindow = 0.9f;
        [SerializeField, Range(0f, 0.2f)] private float _parryBufferTime = 0.08f;
        [Tooltip("Deep thrust into the opened enemy; strong enough to kill an ordinary soldier.")]
        [SerializeField] private MeleeMove _counter = new MeleeMove(HitKind.Counter, 0.05f, 0.1f, 0.45f, 0.2f, 2.75f, 1f, 1.6f, 2);

        [Header("Executions")]
        [Tooltip("An attack within this distance of an opened enemy executes it instead of the counter.")]
        [SerializeField, Min(0f)] private float _executionReach = 3f;
        [SerializeField] private ExecutionSpec[] _executions = { new ExecutionSpec("card_tear", 0.3f, 0.35f, 0.65f, 1.8f) };

        public int MaxHealth => _maxHealth;
        public float HitInvulnerability => _hitInvulnerability;
        public Vector2 KnockbackVelocity => _knockbackVelocity;
        public float KnockbackTime => _knockbackTime;
        public int SaltCharges => _saltCharges;
        public float SaltRestoreShare => _saltRestoreShare;
        public float SaltTime => _saltTime;
        public float SaltTakesEffectAt => _saltTakesEffectAt;
        public float SaltBufferTime => _saltBufferTime;
        public float ConfidenceMax => _confidenceMax;
        public float ExecutionConfidenceFactor => _executionConfidenceFactor;
        public float HurtTime => _hurtTime;
        public float DefeatFallTime => _defeatFallTime;
        public float DefeatFadeTime => Mathf.Min(_defeatFadeTime, _defeatFallTime);
        public float DefeatDarkTime => _defeatDarkTime;
        public float WakeTime => _wakeTime;
        public float WakeFadeTime => Mathf.Min(_wakeFadeTime, _wakeTime);
        public MeleeMove Thrust => _thrust;
        public MeleeMove Slash => _slash;
        public MeleeMove AirSlash => _airSlash;
        public float AttackBufferTime => _attackBufferTime;
        public float ParryDuration => _parryDuration;
        public float ParrySuccessHold => _parrySuccessHold;
        public float CounterWindow => _counterWindow;
        public float ParryBufferTime => _parryBufferTime;
        public MeleeMove Counter => _counter;
        public float ExecutionReach => _executionReach;

        public bool TryGetExecution(string id, out ExecutionSpec spec)
        {
            foreach (ExecutionSpec candidate in _executions)
            {
                if (candidate.Id == id)
                {
                    spec = candidate;
                    return true;
                }
            }

            spec = default;
            return false;
        }
    }
}
