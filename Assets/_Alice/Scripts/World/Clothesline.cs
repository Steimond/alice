using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A laundry rope between two wall hooks, for dressing rooms (biome 1). The rope sprite tiles along the length;
    /// the hooks sit at both ends, the right one mirrored. The pivot is the middle of the rope's span, at its foot:
    /// a sheet hanging from it (its pegs on the rope) stands with its pivot 4.25 tiles (68 px) below the rope's pivot.
    /// </summary>
    [ExecuteAlways]
    public sealed class Clothesline : MonoBehaviour
    {
        [Tooltip("Span between the hooks, in whole tiles so the rope lies on the grid.")]
        [SerializeField, Min(1)] private int _length = 8;
        [SerializeField] private SpriteRenderer _rope;
        [SerializeField] private SpriteRenderer _leftHook;
        [SerializeField] private SpriteRenderer _rightHook;

        public int Length
        {
            get => _length;
            set
            {
                _length = Mathf.Max(1, value);
                Layout();
            }
        }

        public void Bind(SpriteRenderer rope, SpriteRenderer leftHook, SpriteRenderer rightHook)
        {
            _rope = rope;
            _leftHook = leftHook;
            _rightHook = rightHook;
            Layout();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Resizing a tiled sprite sends messages Unity forbids inside OnValidate; lay out on the next editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    Layout();
                }
            };
#endif
        }

        private void Awake()
        {
            Layout();
        }

        private void Layout()
        {
            if (_rope != null && _rope.sprite != null)
            {
                _rope.drawMode = SpriteDrawMode.Tiled;
                _rope.size = new Vector2(_length, _rope.sprite.bounds.size.y);
                _rope.transform.localPosition = Vector3.zero;
            }

            // The hook art carries a stub of rope on its inner side, drawn at the same height above its foot as the
            // tiled rope, so a hook placed right against the rope's end continues it.
            float half = _length * 0.5f;
            if (_leftHook != null && _leftHook.sprite != null)
            {
                float width = _leftHook.sprite.bounds.size.x;
                _leftHook.transform.localPosition = new Vector3(-half - width * 0.5f, 0f, 0f);
                _leftHook.flipX = false;
            }

            if (_rightHook != null && _rightHook.sprite != null)
            {
                float width = _rightHook.sprite.bounds.size.x;
                _rightHook.transform.localPosition = new Vector3(half + width * 0.5f, 0f, 0f);
                _rightHook.flipX = true;
            }
        }
    }
}
