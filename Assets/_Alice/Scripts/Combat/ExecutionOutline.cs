using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Pulsing one-pixel outline while an enemy can be executed, so the player sees at once that the
    /// execution is on offer. Four flat-colour copies of the current sprite, shifted by a whole pixel each way
    /// and drawn behind it, form the outline and stay on the pixel grid.
    /// </summary>
    public sealed class ExecutionOutline : MonoBehaviour
    {
        private static readonly Vector2[] Offsets = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

        [SerializeField] private MeleeEnemy _enemy;
        [SerializeField] private SpriteRenderer _source;
        [Tooltip("Alice/SpriteSilhouette: draws the sprite in one flat colour.")]
        [SerializeField] private Shader _silhouette;
        [SerializeField] private Color _color = new Color(1f, 0.86f, 0.45f);
        [SerializeField, Min(0.01f)] private float _pulsePeriod = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.35f;

        private SpriteRenderer[] _copies;

        private Material _material;

        public void Bind(MeleeEnemy enemy, SpriteRenderer source, Shader silhouette)
        {
            _enemy = enemy;
            _source = source;
            _silhouette = silhouette;
        }

        private void Awake()
        {
            if (_source == null || _silhouette == null)
            {
                enabled = false;
                return;
            }

            _material = new Material(_silhouette);
            float pixel = 1f / Alice.Core.WorldMetrics.PixelsPerUnit;
            _copies = new SpriteRenderer[Offsets.Length];
            for (int index = 0; index < Offsets.Length; index++)
            {
                GameObject copy = new GameObject("Outline");
                copy.transform.SetParent(_source.transform, false);
                copy.transform.localPosition = Offsets[index] * pixel;
                SpriteRenderer renderer = copy.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = _material;
                renderer.sortingLayerID = _source.sortingLayerID;
                renderer.sortingOrder = _source.sortingOrder - 1;
                renderer.enabled = false;
                _copies[index] = renderer;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void LateUpdate()
        {
            bool visible = _enemy != null && _enemy.CanBeExecuted;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f * Mathf.PI / _pulsePeriod);
            Color color = _color;
            color.a = Mathf.Lerp(_minAlpha, 1f, wave);

            foreach (SpriteRenderer copy in _copies)
            {
                copy.enabled = visible;
                if (!visible)
                {
                    continue;
                }

                copy.sprite = _source.sprite;
                copy.flipX = _source.flipX;
                copy.color = color;
            }
        }
    }
}
