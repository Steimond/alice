using System.Collections.Generic;
using Alice.Core;
using Alice.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// The rest menu (GDD section 4), a working version like the HUD: while Alice dozes in the armchair a row
    /// of the techniques she knows appears low in the middle of the screen, the equipped one framed. Left and
    /// right on Move choose; the choice is equipped at once and kept when she gets up. No words, only icons.
    /// </summary>
    public sealed class RestMenu : MonoBehaviour
    {
        private const int SortingOrder = 910;
        private const int IconSize = 32;
        private const int Gap = 6;
        private const int BottomOffset = 70;
        private const float StepThreshold = 0.5f;

        [SerializeField] private HudSkin _skin;

        private PlayerRest _rest;
        private PlayerLoadout _loadout;
        private IPlayerCommandSource _commands;
        private CanvasScaler _scaler;
        private RectTransform _root;
        private RectTransform _row;
        private readonly List<Image> _icons = new List<Image>();
        private readonly List<Image> _frames = new List<Image>();
        private int _builtFor = -1;
        private bool _stickHeld;

        public HudSkin Skin
        {
            get => _skin;
            set => _skin = value;
        }

        public bool IsOpen => _row != null && _row.gameObject.activeSelf;

        private void Awake()
        {
            _rest = GetComponent<PlayerRest>();
            _loadout = GetComponent<PlayerLoadout>();
            _commands = GetComponent<IPlayerCommandSource>();
            if (_skin == null || _rest == null || _loadout == null)
            {
                enabled = false;
                return;
            }

            GameObject canvasObject = new GameObject("RestMenu", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.pixelPerfect = true;
            _scaler = canvasObject.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _root = (RectTransform)canvasObject.transform;

            GameObject row = new GameObject("Techniques", typeof(RectTransform));
            row.transform.SetParent(_root, false);
            _row = (RectTransform)row.transform;
            _row.anchorMin = new Vector2(0.5f, 0f);
            _row.anchorMax = new Vector2(0.5f, 0f);
            _row.pivot = new Vector2(0.5f, 0f);
            _row.anchoredPosition = new Vector2(0f, BottomOffset);
            row.SetActive(false);
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
            bool open = _rest.Stage == PlayerRest.RestStage.Resting;
            if (_row.gameObject.activeSelf != open)
            {
                _row.gameObject.SetActive(open);
                _stickHeld = true;
            }

            if (!open)
            {
                return;
            }

            IReadOnlyList<string> known = _loadout.KnownTechniques;
            if (_builtFor != known.Count)
            {
                Build(known);
            }

            Navigate(known);
            Highlight(known);
        }

        /// <summary>One step per push of the stick or key, so holding it does not race through the row.</summary>
        private void Navigate(IReadOnlyList<string> known)
        {
            float x = _commands != null ? _commands.Move.x : 0f;
            if (Mathf.Abs(x) < StepThreshold)
            {
                _stickHeld = false;
                return;
            }

            if (_stickHeld || known.Count == 0)
            {
                return;
            }

            _stickHeld = true;
            int current = IndexOf(known, _loadout.EquippedTechnique);
            int next = (Mathf.Max(0, current) + (x > 0f ? 1 : -1) + known.Count) % known.Count;
            _loadout.Equip(known[next]);
        }

        private void Build(IReadOnlyList<string> known)
        {
            foreach (Image image in _icons)
            {
                Destroy(image.gameObject);
            }

            foreach (Image image in _frames)
            {
                Destroy(image.gameObject);
            }

            _icons.Clear();
            _frames.Clear();
            int width = known.Count * IconSize + Mathf.Max(0, known.Count - 1) * Gap;
            _row.sizeDelta = new Vector2(width, IconSize);
            for (int index = 0; index < known.Count; index++)
            {
                Vector2 position = new Vector2(index * (IconSize + Gap), 0f);
                Image frame = Create("Frame", null, _skin.Selected);
                Place(frame.rectTransform, position - Vector2.one * 2f, new Vector2(IconSize + 4, IconSize + 4));
                Image back = Create("Back", null, _skin.Frame);
                Place(back.rectTransform, position - Vector2.one, new Vector2(IconSize + 2, IconSize + 2));
                Image icon = Create("Icon", _skin.TechniqueIconFor(known[index]), Color.white);
                Place(icon.rectTransform, position, new Vector2(IconSize, IconSize));
                _frames.Add(frame);
                _icons.Add(back);
                _icons.Add(icon);
            }

            _builtFor = known.Count;
        }

        private void Highlight(IReadOnlyList<string> known)
        {
            int equipped = IndexOf(known, _loadout.EquippedTechnique);
            for (int index = 0; index < _frames.Count; index++)
            {
                _frames[index].enabled = index == equipped;
                // Icons come in pairs: the dark back, then the picture; the unchosen ones are dimmed.
                _icons[index * 2 + 1].color = index == equipped ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }
        }

        private Image Create(string name, Sprite sprite, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(_row, false);
            Image image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static int IndexOf(IReadOnlyList<string> list, string id)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] == id)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
