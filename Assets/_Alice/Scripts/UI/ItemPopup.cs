using System.Collections.Generic;
using Alice.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// What was in the bundle (GDD 0.28): when Alice opens one, its item's icon and name appear in the middle of the
    /// screen for a few seconds and fade. Several finds in a row are shown one after another. The name comes from
    /// the Items localization table and is drawn in the game's pixel font.
    /// </summary>
    public sealed class ItemPopup : MonoBehaviour
    {
        private const int SortingOrder = 920;
        private const int Width = 168;
        private const int Height = 64;
        private const int IconSize = 32;
        private const int FontSize = 12;

        [SerializeField] private HudSkin _skin;
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private Font _font;
        [SerializeField, Min(0.5f)] private float _shownTime = 2.5f;
        [SerializeField, Min(0.05f)] private float _fadeTime = 0.3f;

        private readonly Queue<string> _waiting = new Queue<string>();
        private CanvasScaler _scaler;
        private RectTransform _root;
        private CanvasGroup _group;
        private Image _icon;
        private Text _name;
        private float _shownAt = float.NegativeInfinity;

        public void Bind(HudSkin skin, ItemCatalog catalog, Font font)
        {
            _skin = skin;
            _catalog = catalog;
            _font = font;
        }

        private void Awake()
        {
            if (_skin == null || _catalog == null || _font == null)
            {
                enabled = false;
                return;
            }

            GameObject canvasObject = new GameObject("ItemPopup", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.pixelPerfect = true;
            _scaler = canvasObject.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _group = canvasObject.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _root = (RectTransform)canvasObject.transform;

            RectTransform panel = Create<Image>("Border", _root, Vector2.zero, new Vector2(Width + 2, Height + 2)).rectTransform;
            panel.GetComponent<Image>().color = _skin.Selected;
            Create<Image>("Back", panel, Vector2.zero, new Vector2(Width, Height)).color = _skin.Frame;
            _icon = Create<Image>("Icon", panel, new Vector2(0f, 12f), new Vector2(IconSize, IconSize));
            _name = Create<Text>("Name", panel, new Vector2(0f, -19f), new Vector2(Width - 8, FontSize + 4));
            _name.font = _font;
            _name.fontSize = FontSize;
            _name.alignment = TextAnchor.MiddleCenter;
            _name.horizontalOverflow = HorizontalWrapMode.Overflow;
            _name.color = _skin.Selected;
            // Pixel font: no smoothing when the glyph atlas is rebuilt for new characters.
            Font.textureRebuilt += OnFontTextureRebuilt;
        }

        private void OnEnable()
        {
            ItemNotice.Found += OnFound;
        }

        private void OnDisable()
        {
            ItemNotice.Found -= OnFound;
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }
        }

        private void LateUpdate()
        {
            _scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(Screen.height / (float)WorldMetrics.ReferenceHeight));
            float elapsed = Time.unscaledTime - _shownAt;
            if (elapsed >= _shownTime && _waiting.Count > 0)
            {
                Show(_waiting.Dequeue());
                elapsed = 0f;
            }

            float fadeIn = Mathf.Clamp01(elapsed / _fadeTime);
            float fadeOut = Mathf.Clamp01((_shownTime - elapsed) / _fadeTime);
            _group.alpha = Mathf.Min(fadeIn, fadeOut);
        }

        private void OnFound(string itemId)
        {
            if (_catalog.Find(itemId) != null)
            {
                _waiting.Enqueue(itemId);
            }
        }

        private void Show(string itemId)
        {
            ItemCatalog.Entry entry = _catalog.Find(itemId);
            _icon.sprite = entry.Icon;
            _icon.enabled = entry.Icon != null;
            _name.text = entry.Name.IsEmpty ? string.Empty : entry.Name.GetLocalizedString();
            _shownAt = Time.unscaledTime;
        }

        private void OnFontTextureRebuilt(Font font)
        {
            if (font == _font && font.material != null && font.material.mainTexture != null)
            {
                font.material.mainTexture.filterMode = FilterMode.Point;
            }
        }

        private static T Create<T>(string name, Transform parent, Vector2 centre, Vector2 size) where T : Graphic
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
            child.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)child.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centre;
            rect.sizeDelta = size;
            T graphic = child.GetComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }
    }
}
