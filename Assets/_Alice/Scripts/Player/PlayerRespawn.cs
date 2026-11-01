using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>Remembers the last fully supported ground and returns Alice there after a hazard.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerRespawn : MonoBehaviour, IHazardTarget
    {
        private const float FootProbeHeight = 0.05f;
        private const float FootProbeDistance = 0.15f;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        private PlayerMotor _motor;
        private ContactFilter2D _solidFilter;
        private Vector2 _safePosition;

        public Vector2 SafePosition => _safePosition;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _solidFilter = new ContactFilter2D { useTriggers = false };
            _safePosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (_motor.IsGrounded && !_motor.IsSliding && BothFeetSupported())
            {
                _safePosition = _motor.Position;
            }
        }

        public void OnHazardTouched()
        {
            // Hazard damage arrives with health in M2; M1 only checks the safe return.
            _motor.Teleport(_safePosition);
            _motor.Freeze(_motor.Settings.HazardFreezeTime);
        }

        private bool BothFeetSupported()
        {
            float halfWidth = TraversalMetrics.StandingSize.x * 0.5f;
            return IsSupported(-halfWidth) && IsSupported(halfWidth);
        }

        private bool IsSupported(float offsetX)
        {
            Vector2 origin = _motor.Position + new Vector2(offsetX, FootProbeHeight);
            int count = Physics2D.Raycast(origin, Vector2.down, _solidFilter, _hits, FootProbeDistance);
            for (int index = 0; index < count; index++)
            {
                Rigidbody2D body = _hits[index].rigidbody;
                if (body == null || body.gameObject != gameObject)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
