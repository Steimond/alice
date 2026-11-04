using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// A throw away from a blow: launch speed along the hit direction and upward, in tiles per second,
    /// and how long the target loses control. Each attack carries its own (GDD section 4).
    /// </summary>
    [System.Serializable]
    public struct Knockback
    {
        [Tooltip("x: speed away from the attacker, y: upward speed, tiles per second.")]
        [SerializeField] private Vector2 _velocity;
        [SerializeField, Min(0f)] private float _duration;

        public Knockback(Vector2 velocity, float duration)
        {
            _velocity = velocity;
            _duration = duration;
        }

        public Vector2 Velocity => _velocity;
        public float Duration => _duration;
        public bool IsSet => _duration > 0f;
    }
}
