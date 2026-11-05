using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// Plays a few frames round and round on a world prop, such as the decree daisy swaying where Alice fell.
    /// Starts at a different frame per object so neighbours do not sway in step.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [SerializeField, Min(0.02f)] private float _frameTime = 0.18f;

        private SpriteRenderer _renderer;
        private float _offset;

        public void Bind(Sprite[] frames, float frameTime)
        {
            _frames = frames;
            _frameTime = frameTime;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _offset = Mathf.Abs(transform.position.x * 0.37f) % 1f;
        }

        private void Update()
        {
            if (_frames == null || _frames.Length == 0)
            {
                return;
            }

            int frame = Mathf.FloorToInt(Time.time / _frameTime + _offset * _frames.Length) % _frames.Length;
            _renderer.sprite = _frames[frame];
        }
    }
}
