using System.Collections.Generic;
using Alice.Core;
using Alice.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// The rest menu (GDD sections 4 and 10), a working version like the HUD. While Alice dozes in the armchair two
    /// rows of icons appear low in the middle of the screen, with no words. Below: the techniques she knows, the
    /// equipped one framed; left and right equip at once. Above: the pocket trinkets she has found, the worn ones
    /// lit, with a mark per apron pocket over them (filled for a used pocket). Up and down move between the rows;
    /// in the trinket row left and right move the cursor and Interact puts the trinket on or takes it off
    /// (GDD 0.27). Everything is kept when she gets up.
    /// </summary>
    public sealed class RestMenu : MonoBehaviour
    {
        private const int SortingOrder = 910;
        private const int IconSize = 32;
        private const int Gap = 6;
        private const int BottomOffset = 70;
        private const int RowGap = 14;
        private const int PocketSize = 6;
        private const int PocketGap = 3;
        private const float StepThreshold = 0.5f;
        private const float Dimmed = 0.45f;

        private enum Row
        {
            Techniques,
            Trinkets
        }

        [SerializeField] private HudSkin _skin;

        private PlayerRest _rest;
        private PlayerLoadout _loadout;
        private PlayerTrinkets _trinkets;
        private IPlayerCommandSource _commands;
        private CanvasScaler _scaler;
        private RectTransform _root;
        private RectTransform _menu;
        private RectTransform _techniqueRow;
        private RectTransform _trinketRow;
        private RectTransform _pocketRow;
        private readonly List<Image> _techniqueIcons = new List<Image>();
        private readonly List<Image> _techniqueFrames = new List<Image>();
        private readonly List<Image> _trinketIcons = new List<Image>();
        private readonly List<Image> _trinketFrames = new List<Image>();
        private readonly List<Image> _pockets = new List<Image>();
        private int _builtTechniques = -1;
        private int _builtTrinkets = -1;
        private int _builtPockets = -1;
        private bool _stickHeld;
        private Row _row = Row.Techniques;
        private int _cursor;
        private float _openedAt;
        private float _consumedInteractAt = float.NegativeInfinity;

        public HudSkin Skin
        {
            get => _skin;
            set => _skin = value;
        }

        public bool IsOpen => _menu != null && _menu.gameObject.activeSelf;

        private void Awake()
        {
            _rest = GetComponent<PlayerRest>();
            _loadout = GetComponent<PlayerLoadout>();
            _trinkets = GetComponent<PlayerTrinkets>();
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

            _menu = CreateRow("Menu", _root, 0f);
            _techniqueRow = CreateRow("Techniques", _menu, BottomOffset);
            _trinketRow = CreateRow("Trinkets", _menu, BottomOffset + IconSize + RowGap);
            _pocketRow = CreateRow("Pockets", _menu, BottomOffset + 2 * IconSize + RowGap + PocketGap * 2);
            _menu.gameObject.SetActive(false);
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
            if (_menu.gameObject.activeSelf != open)
            {
                _menu.gameObject.SetActive(open);
                _stickHeld = true;
                _row = Row.Techniques;
                _openedAt = Time.time;
            }

            if (!open)
            {
                return;
            }

            IReadOnlyList<string> known = _loadout.KnownTechniques;
            IReadOnlyList<string> found = _trinkets != null ? _trinkets.Found : new List<string>();
            int pockets = _trinkets != null ? _trinkets.Pockets : 0;
            if (_builtTechniques != known.Count)
            {
                BuildTechniques(known);
            }

            if (_builtTrinkets != found.Count || _builtPockets != pockets)
            {
                BuildTrinkets(found, pockets);
            }

            Navigate(known, found);
            ToggleTrinket(found);
            Highlight(known, found);
        }

        /// <summary>One step per push of the stick or key, so holding it does not race through a row.</summary>
        private void Navigate(IReadOnlyList<string> known, IReadOnlyList<string> found)
        {
            Vector2 move = _commands != null ? _commands.Move : Vector2.zero;
            bool vertical = Mathf.Abs(move.y) >= StepThreshold && Mathf.Abs(move.y) > Mathf.Abs(move.x);
            bool horizontal = Mathf.Abs(move.x) >= StepThreshold && !vertical;
            if (!vertical && !horizontal)
            {
                _stickHeld = false;
                return;
            }

            if (_stickHeld)
            {
                return;
            }

            _stickHeld = true;
            if (vertical)
            {
                _row = move.y > 0f && found.Count > 0 ? Row.Trinkets : Row.Techniques;
                _cursor = Mathf.Clamp(_cursor, 0, Mathf.Max(0, found.Count - 1));
                return;
            }

            int step = move.x > 0f ? 1 : -1;
            if (_row == Row.Trinkets)
            {
                _cursor = (_cursor + step + found.Count) % found.Count;
                return;
            }

            if (known.Count == 0)
            {
                return;
            }

            int current = IndexOf(known, _loadout.EquippedTechnique);
            _loadout.Equip(known[(Mathf.Max(0, current) + step + known.Count) % known.Count]);
        }

        private void ToggleTrinket(IReadOnlyList<string> found)
        {
            if (_commands == null || _trinkets == null || _row != Row.Trinkets || found.Count == 0)
            {
                return;
            }

            // Only presses made while the menu is open; the one that sat her down does not count.
            float pressedAt = _commands.InteractPressedAt;
            if (pressedAt <= _openedAt || pressedAt <= _consumedInteractAt)
            {
                return;
            }

            _consumedInteractAt = pressedAt;
            _trinkets.Toggle(found[_cursor]);
        }

        private void BuildTechniques(IReadOnlyList<string> known)
        {
            Clear(_techniqueIcons);
            Clear(_techniqueFrames);
            _techniqueRow.sizeDelta = new Vector2(RowWidth(known.Count), IconSize);
            for (int index = 0; index < known.Count; index++)
            {
                AddSlot(_techniqueRow, index, _skin.TechniqueIconFor(known[index]), _techniqueFrames, _techniqueIcons);
            }

            _builtTechniques = known.Count;
        }

        private void BuildTrinkets(IReadOnlyList<string> found, int pockets)
        {
            Clear(_trinketIcons);
            Clear(_trinketFrames);
            Clear(_pockets);
            _trinketRow.sizeDelta = new Vector2(RowWidth(found.Count), IconSize);
            for (int index = 0; index < found.Count; index++)
            {
                TrinketDefinition trinket = _trinkets.Definition(found[index]);
                AddSlot(_trinketRow, index, trinket != null ? trinket.Icon : null, _trinketFrames, _trinketIcons);
            }

            // The pockets: a small square each, over the trinkets.
            _pocketRow.sizeDelta = new Vector2(pockets * PocketSize + Mathf.Max(0, pockets - 1) * PocketGap, PocketSize);
            for (int index = 0; index < pockets; index++)
            {
                Image border = Create(_pocketRow, "PocketBorder", null, _skin.Selected);
                Place(border.rectTransform, new Vector2(index * (PocketSize + PocketGap), 0f), Vector2.one * PocketSize);
                Image fill = Create(_pocketRow, "Pocket", null, _skin.Frame);
                Place(fill.rectTransform, new Vector2(index * (PocketSize + PocketGap) + 1f, 1f), Vector2.one * (PocketSize - 2));
                _pockets.Add(border);
                _pockets.Add(fill);
            }

            _builtTrinkets = found.Count;
            _builtPockets = pockets;
        }

        private void Highlight(IReadOnlyList<string> known, IReadOnlyList<string> found)
        {
            int equipped = IndexOf(known, _loadout.EquippedTechnique);
            bool techniquesFocused = _row == Row.Techniques;
            for (int index = 0; index < _techniqueFrames.Count; index++)
            {
                _techniqueFrames[index].enabled = index == equipped;
                _techniqueFrames[index].color = Fade(_skin.Selected, techniquesFocused ? 1f : Dimmed);
                // Icons come in pairs: the dark back, then the picture; the unchosen ones are dimmed.
                _techniqueIcons[index * 2 + 1].color = Fade(Color.white, index == equipped ? 1f : Dimmed);
            }

            for (int index = 0; index < _trinketFrames.Count; index++)
            {
                bool worn = _trinkets.IsWorn(found[index]);
                _trinketFrames[index].enabled = _row == Row.Trinkets && index == _cursor;
                _trinketIcons[index * 2 + 1].color = Fade(Color.white, worn ? 1f : Dimmed);
            }

            int used = _trinkets != null ? _trinkets.Worn.Count : 0;
            for (int index = 0; index < _pockets.Count / 2; index++)
            {
                // A used pocket is filled with the frame colour, a free one shows the dark inside.
                _pockets[index * 2 + 1].color = index < used ? _skin.Selected : _skin.Frame;
            }
        }

        private void AddSlot(RectTransform row, int index, Sprite icon, List<Image> frames, List<Image> icons)
        {
            Vector2 position = new Vector2(index * (IconSize + Gap), 0f);
            Image frame = Create(row, "Frame", null, _skin.Selected);
            Place(frame.rectTransform, position - Vector2.one * 2f, new Vector2(IconSize + 4, IconSize + 4));
            Image back = Create(row, "Back", null, _skin.Frame);
            Place(back.rectTransform, position - Vector2.one, new Vector2(IconSize + 2, IconSize + 2));
            Image picture = Create(row, "Icon", icon, Color.white);
            Place(picture.rectTransform, position, new Vector2(IconSize, IconSize));
            frames.Add(frame);
            icons.Add(back);
            icons.Add(picture);
        }

        private static RectTransform CreateRow(string name, Transform parent, float bottom)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            return rect;
        }

        private static Image Create(RectTransform row, string name, Sprite sprite, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(row, false);
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

        private static void Clear(List<Image> images)
        {
            foreach (Image image in images)
            {
                Destroy(image.gameObject);
            }

            images.Clear();
        }

        private static int RowWidth(int count)
        {
            return count * IconSize + Mathf.Max(0, count - 1) * Gap;
        }

        private static Color Fade(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
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
