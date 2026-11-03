using UnityEngine;

namespace Alice.Combat
{
    /// <summary>Frames of one melee enemy type. Art faces right; the view mirrors it.</summary>
    [CreateAssetMenu(menuName = "Alice/Enemy Sprite Set", fileName = "EnemySprites")]
    public sealed class EnemySpriteSet : ScriptableObject
    {
        [SerializeField] private Sprite _idle;
        [SerializeField] private Sprite[] _walk;
        [Tooltip("Tiles walked per walk frame, so steps match the ground speed.")]
        [SerializeField, Min(0.01f)] private float _walkFrameDistance = 0.2f;

        [Header("Attack, spread over each phase")]
        [SerializeField] private Sprite[] _attackWindup;
        [SerializeField] private Sprite[] _attackActive;
        [SerializeField] private Sprite[] _attackRecovery;

        [Header("Death, played once and held")]
        [SerializeField] private Sprite[] _death;
        [SerializeField, Min(0.01f)] private float _deathFrameTime = 0.09f;

        [Tooltip("Transparent rows under the feet that keep the blood pool; the drawing is lowered by them.")]
        [SerializeField, Min(0)] private int _groundOffsetPixels;

        public Sprite Idle => _idle;
        public Sprite[] Walk => _walk;
        public float WalkFrameDistance => _walkFrameDistance;
        public Sprite[] AttackWindup => _attackWindup;
        public Sprite[] AttackActive => _attackActive;
        public Sprite[] AttackRecovery => _attackRecovery;
        public Sprite[] Death => _death;
        public float DeathFrameTime => _deathFrameTime;
        public int GroundOffsetPixels => _groundOffsetPixels;

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
    }
}
