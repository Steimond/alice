using System;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Numbers of one melee strike: its three phases and a hitbox in tiles from the attacker's feet,
    /// measured along the facing direction. Kept in data so reach is tuned without redrawing frames.
    /// </summary>
    [Serializable]
    public struct MeleeMove
    {
        [SerializeField] private HitKind _kind;
        [SerializeField, Min(0f)] private float _windup;
        [SerializeField, Min(0f)] private float _active;
        [SerializeField, Min(0f)] private float _recovery;
        [SerializeField] private float _reachStart;
        [SerializeField, Min(0f)] private float _reach;
        [SerializeField, Min(0f)] private float _hitHeight;
        [Tooltip("Centre of the hitbox above the feet.")]
        [SerializeField] private float _hitCenterHeight;
        [SerializeField, Min(1)] private int _damage;

        public MeleeMove(HitKind kind, float windup, float active, float recovery, float reachStart, float reach,
            float hitHeight, float hitCenterHeight, int damage)
        {
            _kind = kind;
            _windup = windup;
            _active = active;
            _recovery = recovery;
            _reachStart = reachStart;
            _reach = reach;
            _hitHeight = hitHeight;
            _hitCenterHeight = hitCenterHeight;
            _damage = damage;
        }

        public HitKind Kind => _kind;
        public MeleeAttackTimeline Timeline => new MeleeAttackTimeline(_windup, _active, _recovery);
        public float Reach => _reach;

        /// <summary>Negative when the move also reaches behind the attacker, as a spin does.</summary>
        public float ReachStart => _reachStart;
        public int Damage => _damage;

        public Vector2 HitboxSize => new Vector2(Mathf.Max(0f, _reach - _reachStart), _hitHeight);

        /// <summary>Hitbox centre relative to the feet for the given facing (+1 right, -1 left).</summary>
        public Vector2 HitboxCenter(int facing)
        {
            return new Vector2(facing * (_reachStart + HitboxSize.x * 0.5f), _hitCenterHeight);
        }
    }
}
