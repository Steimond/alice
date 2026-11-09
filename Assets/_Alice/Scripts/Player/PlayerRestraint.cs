using Alice.Combat;
using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice caught and held, e.g. wrapped by the ambushing sheet (GDD section 9). She stands frozen inside the
    /// holder, the holder draws her, and every press of attack, jump, slide or parry is a jerk towards freedom.
    /// The holder hurts her on its own rhythm; it cannot be parried.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerRestraint : MonoBehaviour, IRestrainable
    {
        // Longer than any hold lasts; the holder or her struggle always ends it first.
        private const float HoldLock = 30f;

        [Tooltip("Torn free by her own struggle, she hops clear of the holder: back and up, tiles per second.")]
        [SerializeField] private Vector2 _escapeHop = new Vector2(4f, 7f);
        [SerializeField, Min(0f)] private float _escapeHopTime = 0.2f;

        private PlayerMotor _motor;
        private Health _health;
        private IPlayerCommandSource _commands;
        private PlayerDamageReaction _damage;
        private GameObject _holder;
        private int _pressesNeeded;
        private int _presses;
        private float _lastSeenPress;

        public bool IsRestrained => _holder != null;
        public float StruggleProgress => _pressesNeeded > 0 ? Mathf.Clamp01(_presses / (float)_pressesNeeded) : 0f;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _health = GetComponent<Health>();
            _commands = GetComponent<IPlayerCommandSource>();
            _damage = GetComponent<PlayerDamageReaction>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
            if (IsRestrained)
            {
                Free();
            }
        }

        public bool TryRestrain(GameObject by, int pressesToEscape, Vector2 holdAt)
        {
            bool defeated = _damage != null && _damage.IsDefeated;
            if (IsRestrained || defeated || _health.IsDead || _motor.PassesBodies || _motor.IsInvulnerable)
            {
                return false;
            }

            _holder = by;
            _pressesNeeded = Mathf.Max(1, pressesToEscape);
            _presses = 0;
            _lastSeenPress = LatestPress();
            _motor.Teleport(holdAt);
            _motor.Freeze(HoldLock);
            return true;
        }

        public void Release(GameObject by)
        {
            if (by == _holder)
            {
                Free();
            }
        }

        private void Update()
        {
            if (!IsRestrained)
            {
                return;
            }

            // Keeps her frozen through the knockback of the holder's own squeezes.
            _motor.Freeze(HoldLock);
            float latest = LatestPress();
            if (latest > _lastSeenPress)
            {
                _lastSeenPress = latest;
                _presses++;
                if (_presses >= _pressesNeeded)
                {
                    Free();
                    _motor.Knockback(new Vector2(-_motor.Facing * _escapeHop.x, _escapeHop.y), _escapeHopTime);
                }
            }
        }

        private float LatestPress()
        {
            if (_commands == null)
            {
                return float.NegativeInfinity;
            }

            return Mathf.Max(Mathf.Max(_commands.AttackPressedAt, _commands.JumpPressedAt),
                Mathf.Max(_commands.EvadePressedAt, _commands.ParryPressedAt));
        }

        private void OnDied(HitInfo hit)
        {
            // The defeat takes over her body; a frozen motor would hold her through the wake-up.
            if (IsRestrained)
            {
                Free();
            }
        }

        private void Free()
        {
            _holder = null;
            _motor.Freeze(0f);
        }
    }
}
