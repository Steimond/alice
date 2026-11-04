using System.Collections.Generic;
using Alice.Combat;
using Alice.Core;
using Alice.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// In-game HUD (GDD section 4), a working version until it is styled: top left the clarity bar, the
    /// confidence bar and the smelling-salt charges; top right the count of Queen's decrees. Built at
    /// runtime on an overlay canvas scaled by whole multiples of the 640x360 frame, so every element stays
    /// on the pixel grid. No words, only digits.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        private const int SortingOrder = 900;
        private const int RowGap = 3;
        private const int MaxDigits = 7;

        [SerializeField] private HudSkin _skin;

        private Health _health;
        private PlayerSalts _salts;
        private PlayerSpoils _spoils;

        private CanvasScaler _scaler;
        private RectTransform _root;
        private Bar _clarity;
        private Bar _confidence;
        private readonly List<Image> _saltIcons = new List<Image>();
        private Image _decreeIcon;
        private readonly Image[] _digits = new Image[MaxDigits];

        private float _lostShown;
        private int _lastClarity = -1;
        private float _lostSince;

        public HudSkin Skin
        {
            get => _skin;
            set => _skin = value;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _salts = GetComponent<PlayerSalts>();
            _spoils = GetComponent<PlayerSpoils>();
            if (_skin == null || _health == null)
            {
                enabled = false;
                return;
            }

            GameObject canvasObject = new GameObject("Hud", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.pixelPerfect = true;
            _scaler = canvasObject.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _root = (RectTransform)canvasObject.transform;

            int y = _skin.Margin;
            _clarity = new Bar(_root, "Clarity", _skin.Margin, y, _skin.ClarityHeight, _skin);
            y += _skin.ClarityHeight + 2 + RowGap;
            _confidence = new Bar(_root, "Confidence", _skin.Margin, y, _skin.ConfidenceHeight, _skin);
            y += _skin.ConfidenceHeight + 2 + RowGap + 1;

            int x = _skin.Margin;
            int charges = _salts != null ? _salts.MaxCharges : 0;
            for (int index = 0; index < charges; index++)
            {
                Image icon = CreateImage(_root, "Salt", _skin.SaltFull, Color.white);
                Vector2 size = SizeOf(_skin.SaltFull, 6, 9);
                Place(icon.rectTransform, x, y, size);
                x += (int)size.x + 1;
                _saltIcons.Add(icon);
            }

            // The decrees sit in the opposite corner, the count right-aligned left of the icon.
            _decreeIcon = CreateImage(_root, "Decree", _skin.Decree, _skin.Decree != null ? Color.white : _skin.Digits);
            Vector2 decreeSize = SizeOf(_skin.Decree, 7, 8);
            PlaceFromRight(_decreeIcon.rectTransform, _skin.Margin, _skin.Margin, decreeSize);
            int digitsRight = _skin.Margin + (int)decreeSize.x + 2;
            int digitY = _skin.Margin + Mathf.Max(0, ((int)decreeSize.y - PixelDigits.Height) / 2);
            for (int slot = 0; slot < MaxDigits; slot++)
            {
                // Slot 0 is the rightmost digit; outlines of neighbours overlap by one pixel.
                _digits[slot] = CreateImage(_root, "Digit", PixelDigits.For(0), _skin.Digits);
                PlaceFromRight(_digits[slot].rectTransform, digitsRight + slot * (PixelDigits.Width - 1), digitY,
                    new Vector2(PixelDigits.Width, PixelDigits.Height));
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }
        }

        private void LateUpdate()
        {
            _scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(Screen.height / (float)WorldMetrics.ReferenceHeight));

            UpdateClarity();
            if (_spoils != null && _spoils.Confidence != null)
            {
                ConfidencePool confidence = _spoils.Confidence;
                int width = Mathf.RoundToInt(confidence.Max * _skin.ConfidencePixelsPerPoint);
                _confidence.Show(width, Mathf.RoundToInt(confidence.Current * _skin.ConfidencePixelsPerPoint), 0,
                    _skin.ConfidenceFill);
            }

            for (int index = 0; index < _saltIcons.Count; index++)
            {
                bool full = index < _salts.Charges;
                Image icon = _saltIcons[index];
                if (_skin.SaltFull != null)
                {
                    icon.sprite = full || _skin.SaltEmpty == null ? _skin.SaltFull : _skin.SaltEmpty;
                    icon.color = full || _skin.SaltEmpty != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                }
                else
                {
                    icon.color = full ? _skin.ClarityFill : _skin.Empty;
                }
            }

            ShowNumber(_spoils != null ? _spoils.Decrees : 0);
        }

        /// <summary>The part just lost stays red for a moment, then shrinks toward what is left.</summary>
        private void UpdateClarity()
        {
            int current = _health.Current;
            if (_lastClarity < 0 || current > _lastClarity)
            {
                _lostShown = current;
            }
            else if (current < _lastClarity)
            {
                _lostShown = Mathf.Max(_lostShown, _lastClarity);
                _lostSince = Time.unscaledTime;
            }

            _lastClarity = current;
            if (Time.unscaledTime - _lostSince > _skin.LostDelay)
            {
                _lostShown = Mathf.MoveTowards(_lostShown, current, _skin.LostShrinkSpeed * Time.unscaledDeltaTime);
            }

            float scale = _skin.ClarityPixelsPerPoint;
            _clarity.Show(Mathf.RoundToInt(_health.Max * scale), Mathf.RoundToInt(current * scale),
                Mathf.RoundToInt(_lostShown * scale), _skin.ClarityFill);
        }

        private void ShowNumber(int value)
        {
            string text = Mathf.Clamp(value, 0, 9999999).ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int slot = 0; slot < MaxDigits; slot++)
            {
                bool used = slot < text.Length;
                _digits[slot].enabled = used;
                if (used)
                {
                    _digits[slot].sprite = PixelDigits.For(text[text.Length - 1 - slot] - '0');
                }
            }
        }

        private static Vector2 SizeOf(Sprite sprite, int fallbackWidth, int fallbackHeight)
        {
            return sprite != null ? sprite.rect.size : new Vector2(fallbackWidth, fallbackHeight);
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Top-left anchored, in pixels of the reference frame.</summary>
        private static void Place(RectTransform rect, int x, int y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = size;
        }

        /// <summary>Top-right anchored: <paramref name="right"/> is the gap to the right edge.</summary>
        private static void PlaceFromRight(RectTransform rect, int right, int y, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -y);
            rect.sizeDelta = size;
        }

        /// <summary>A one-pixel frame around an empty track, the lost part and the fill.</summary>
        private sealed class Bar
        {
            private readonly Image _frame;
            private readonly Image _track;
            private readonly Image _lost;
            private readonly Image _fill;
            private readonly int _x;
            private readonly int _y;
            private readonly int _height;

            public Bar(Transform parent, string name, int x, int y, int height, HudSkin skin)
            {
                _x = x;
                _y = y;
                _height = height;
                _frame = CreateImage(parent, name + "Frame", null, skin.Frame);
                _track = CreateImage(parent, name + "Track", null, skin.Empty);
                _lost = CreateImage(parent, name + "Lost", null, skin.ClarityLost);
                _fill = CreateImage(parent, name + "Fill", null, Color.white);
            }

            public void Show(int width, int filled, int lost, Color fill)
            {
                Place(_frame.rectTransform, _x, _y, new Vector2(width + 2, _height + 2));
                Place(_track.rectTransform, _x + 1, _y + 1, new Vector2(width, _height));
                _lost.enabled = lost > filled;
                Place(_lost.rectTransform, _x + 1, _y + 1, new Vector2(Mathf.Min(lost, width), _height));
                _fill.enabled = filled > 0;
                _fill.color = fill;
                Place(_fill.rectTransform, _x + 1, _y + 1, new Vector2(Mathf.Min(filled, width), _height));
            }
        }
    }
}
