using UnityEngine;

namespace Alice.Combat
{
    /// <summary>Draws any <see cref="IEnemyClipSource"/> from its <see cref="EnemyClipSet"/>.</summary>
    public sealed class EnemyClipView : MonoBehaviour
    {
        private const string DeathClip = "death";

        [SerializeField] private MonoBehaviour _source;
        [SerializeField] private EnemyClipSet _clips;
        [SerializeField] private SpriteRenderer _renderer;

        private IEnemyClipSource _enemy;

        public void Bind(MonoBehaviour source, EnemyClipSet clips, SpriteRenderer renderer)
        {
            _source = source;
            _clips = clips;
            _renderer = renderer;
        }

        private void Awake()
        {
            _enemy = _source as IEnemyClipSource;
        }

        private void LateUpdate()
        {
            if (_enemy == null || _clips == null || _renderer == null)
            {
                return;
            }

            string id = _enemy.ClipId;
            bool found = id == DeathClip
                ? _clips.TryGetFor(id, _enemy.KillingBlow, out EnemyClipSet.Clip clip)
                : _clips.TryGet(id, out clip);
            _renderer.enabled = !string.IsNullOrEmpty(id);
            if (!found)
            {
                return;
            }

            _renderer.flipX = _enemy.Facing < 0;
            _renderer.sprite = EnemyClipSet.FrameAt(clip, _enemy.ClipTime, _enemy.ClipSpan);
        }
    }
}
