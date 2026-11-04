using System;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>Frames of one melee enemy type. Art faces right; the view mirrors it.</summary>
    [CreateAssetMenu(menuName = "Alice/Enemy Sprite Set", fileName = "EnemySprites")]
    public sealed class EnemySpriteSet : ScriptableObject
    {
        /// <summary>A death drawn for one kind of killing blow (GDD section 4).</summary>
        [Serializable]
        public struct DeathVariant
        {
            [SerializeField] private HitKind _kind;
            [SerializeField] private Sprite[] _frames;
            [SerializeField, Min(0.01f)] private float _frameTime;

            public DeathVariant(HitKind kind, Sprite[] frames, float frameTime)
            {
                _kind = kind;
                _frames = frames;
                _frameTime = frameTime;
            }

            public HitKind Kind => _kind;
            public Sprite[] Frames => _frames;
            public float FrameTime => _frameTime;
        }

        [SerializeField] private Sprite _idle;
        [SerializeField] private Sprite[] _walk;
        [Tooltip("Tiles walked per walk frame, so steps match the ground speed.")]
        [SerializeField, Min(0.01f)] private float _walkFrameDistance = 0.2f;

        [Header("Attack, spread over each phase")]
        [SerializeField] private Sprite[] _attackWindup;
        [SerializeField] private Sprite[] _attackActive;
        [SerializeField] private Sprite[] _attackRecovery;

        [Header("Hit reaction, spread over the stagger")]
        [SerializeField] private Sprite[] _hurt = new Sprite[0];

        [Header("Thrown open by a parry, spread over the opening")]
        [SerializeField] private Sprite[] _opened = new Sprite[0];

        [Header("Death, played once and held")]
        [Tooltip("Used when no variant matches the killing blow.")]
        [SerializeField] private Sprite[] _death;
        [SerializeField, Min(0.01f)] private float _deathFrameTime = 0.09f;
        [SerializeField] private DeathVariant[] _deathVariants = new DeathVariant[0];

        [Tooltip("Transparent rows under the feet that keep the blood pool; the drawing is lowered by them.")]
        [SerializeField, Min(0)] private int _groundOffsetPixels;

        public Sprite Idle => _idle;
        public Sprite[] Walk => _walk;
        public float WalkFrameDistance => _walkFrameDistance;
        public Sprite[] AttackWindup => _attackWindup;
        public Sprite[] AttackActive => _attackActive;
        public Sprite[] AttackRecovery => _attackRecovery;
        public Sprite[] Hurt => _hurt;
        public Sprite[] Opened => _opened;
        public Sprite[] Death => _death;
        public float DeathFrameTime => _deathFrameTime;
        public DeathVariant[] DeathVariants => _deathVariants;
        public int GroundOffsetPixels => _groundOffsetPixels;

        /// <summary>Frames and timing of the death that matches the killing blow, or the default death.</summary>
        public Sprite[] DeathFor(HitKind kind, out float frameTime)
        {
            if (_deathVariants != null)
            {
                foreach (DeathVariant variant in _deathVariants)
                {
                    if (variant.Kind == kind && variant.Frames != null && variant.Frames.Length > 0)
                    {
                        frameTime = variant.FrameTime;
                        return variant.Frames;
                    }
                }
            }

            frameTime = _deathFrameTime;
            return _death;
        }

        public void Assign(Sprite idle, Sprite[] walk, Sprite[] windup, Sprite[] active, Sprite[] recovery,
            Sprite[] death, int groundOffsetPixels)
        {
            _idle = idle;
            _walk = walk;
            _attackWindup = windup;
            _attackActive = active;
            _attackRecovery = recovery;
            _death = death;
            _groundOffsetPixels = groundOffsetPixels;
        }

        public void AssignReactions(Sprite[] hurt, Sprite[] opened, DeathVariant[] deathVariants)
        {
            _hurt = hurt;
            _opened = opened;
            _deathVariants = deathVariants;
        }
    }
}
