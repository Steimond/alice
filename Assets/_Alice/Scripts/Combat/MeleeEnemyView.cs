using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>Draws a <see cref="MeleeEnemy"/> from its <see cref="EnemySpriteSet"/> by state and attack phase.</summary>
    public sealed class MeleeEnemyView : MonoBehaviour
    {
        [SerializeField] private MeleeEnemy _enemy;
        [SerializeField] private EnemySpriteSet _sprites;
        [SerializeField] private SpriteRenderer _renderer;

        public void Bind(MeleeEnemy enemy, EnemySpriteSet sprites, SpriteRenderer renderer)
        {
            _enemy = enemy;
            _sprites = sprites;
            _renderer = renderer;
        }

        private void Start()
        {
            if (_renderer != null && _sprites != null)
            {
                // Whole pixels keep the sprite on the pixel grid.
                _renderer.transform.localPosition =
                    new Vector3(0f, -_sprites.GroundOffsetPixels / (float)WorldMetrics.PixelsPerUnit, 0f);
            }
        }

        private void LateUpdate()
        {
            if (_enemy == null || _sprites == null || _renderer == null)
            {
                return;
            }

            _renderer.flipX = _enemy.Facing < 0;
            _renderer.sprite = SelectSprite();
        }

        private Sprite SelectSprite()
        {
            switch (_enemy.CurrentState)
            {
                case MeleeEnemy.State.Attack:
                    return SelectAttackSprite();
                case MeleeEnemy.State.Dead:
                    Sprite[] death = _sprites.Death;
                    if (death == null || death.Length == 0)
                    {
                        return null;
                    }

                    int frame = Mathf.FloorToInt(_enemy.StateTime / _sprites.DeathFrameTime);
                    return death[Mathf.Min(frame, death.Length - 1)];
                default:
                    Sprite[] walk = _sprites.Walk;
                    if (_enemy.IsWalking && walk != null && walk.Length > 0)
                    {
                        return walk[Mathf.FloorToInt(_enemy.WalkDistance / _sprites.WalkFrameDistance) % walk.Length];
                    }

                    return _sprites.Idle;
            }
        }

        private Sprite SelectAttackSprite()
        {
            MeleeAttackTimeline timeline = _enemy.Settings.Timeline;
            float elapsed = _enemy.StateTime;
            Sprite[] frames = timeline.PhaseAt(elapsed) switch
            {
                AttackPhase.Windup => _sprites.AttackWindup,
                AttackPhase.Active => _sprites.AttackActive,
                AttackPhase.Recovery => _sprites.AttackRecovery,
                _ => null
            };

            if (frames == null || frames.Length == 0)
            {
                return _sprites.Idle;
            }

            int frame = Mathf.FloorToInt(timeline.PhaseProgress(elapsed) * frames.Length);
            return frames[Mathf.Min(frame, frames.Length - 1)];
        }
    }
}
