using UnityEngine;

namespace Alice.UI
{
    /// <summary>The icon a technique shows in the rest menu, addressed by its stable ID.</summary>
    [System.Serializable]
    public struct TechniqueIcon
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _sprite;

        public TechniqueIcon(string id, Sprite sprite)
        {
            _id = id;
            _sprite = sprite;
        }

        public string Id => _id;
        public Sprite Sprite => _sprite;
    }

    /// <summary>Icons, colours and sizes of the in-game HUD (GDD section 4), in pixels of the 640x360 frame.</summary>
    [CreateAssetMenu(menuName = "Alice/HUD Skin", fileName = "HudSkin")]
    public sealed class HudSkin : ScriptableObject
    {
        [Header("Icons")]
        [SerializeField] private Sprite _saltFull;
        [SerializeField] private Sprite _saltEmpty;
        [SerializeField] private Sprite _decree;

        [Header("Rest menu: technique icons by technique ID")]
        [SerializeField] private TechniqueIcon[] _techniqueIcons = new TechniqueIcon[0];
        [SerializeField] private Color _selected = new Color(0.92f, 0.88f, 0.8f);

        [Header("Clarity bar")]
        [Tooltip("Bar pixels per clarity point, so a raised maximum makes the bar longer.")]
        [SerializeField, Min(0.1f)] private float _clarityPixelsPerPoint = 1f;
        [SerializeField, Min(2)] private int _clarityHeight = 6;
        [SerializeField] private Color _clarityFill = new Color(0.82f, 0.88f, 0.93f);
        [Tooltip("The part just lost shows in this colour, then shrinks.")]
        [SerializeField] private Color _clarityLost = new Color(0.75f, 0.16f, 0.16f);

        [Header("Confidence bar")]
        [SerializeField, Min(0.1f)] private float _confidencePixelsPerPoint = 0.8f;
        [SerializeField, Min(2)] private int _confidenceHeight = 4;
        [SerializeField] private Color _confidenceFill = new Color(0.88f, 0.69f, 0.29f);

        [Header("Common")]
        [SerializeField] private Color _frame = new Color(0.06f, 0.05f, 0.07f);
        [SerializeField] private Color _empty = new Color(0.18f, 0.16f, 0.2f);
        [SerializeField] private Color _digits = new Color(0.92f, 0.88f, 0.8f);
        [SerializeField, Min(0)] private int _margin = 8;
        [SerializeField, Min(0f)] private float _lostDelay = 0.35f;
        [Tooltip("Clarity points per second the lost part shrinks by.")]
        [SerializeField, Min(1f)] private float _lostShrinkSpeed = 60f;

        public Sprite SaltFull => _saltFull;
        public Sprite SaltEmpty => _saltEmpty;
        public Sprite Decree => _decree;
        public float ClarityPixelsPerPoint => _clarityPixelsPerPoint;
        public int ClarityHeight => _clarityHeight;
        public Color ClarityFill => _clarityFill;
        public Color ClarityLost => _clarityLost;
        public float ConfidencePixelsPerPoint => _confidencePixelsPerPoint;
        public int ConfidenceHeight => _confidenceHeight;
        public Color ConfidenceFill => _confidenceFill;
        public Color Frame => _frame;
        public Color Empty => _empty;
        public Color Digits => _digits;
        public int Margin => _margin;
        public float LostDelay => _lostDelay;
        public float LostShrinkSpeed => _lostShrinkSpeed;

        public Color Selected => _selected;

        public Sprite TechniqueIconFor(string techniqueId)
        {
            foreach (TechniqueIcon icon in _techniqueIcons)
            {
                if (icon.Id == techniqueId)
                {
                    return icon.Sprite;
                }
            }

            return null;
        }

        public void AssignTechniqueIcon(string techniqueId, Sprite sprite)
        {
            for (int index = 0; index < _techniqueIcons.Length; index++)
            {
                if (_techniqueIcons[index].Id == techniqueId)
                {
                    _techniqueIcons[index] = new TechniqueIcon(techniqueId, sprite);
                    return;
                }
            }

            System.Array.Resize(ref _techniqueIcons, _techniqueIcons.Length + 1);
            _techniqueIcons[_techniqueIcons.Length - 1] = new TechniqueIcon(techniqueId, sprite);
        }

        public void AssignIcons(Sprite saltFull, Sprite saltEmpty, Sprite decree)
        {
            _saltFull = saltFull;
            _saltEmpty = saltEmpty;
            _decree = decree;
        }
    }
}
