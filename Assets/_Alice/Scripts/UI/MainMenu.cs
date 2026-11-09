using System;
using System.Collections.Generic;
using System.Globalization;
using Alice.Core;
using Alice.Save;
using Alice.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// The main menu (GDD 0.35), the first scene of the game. The logo stands over a picture of the fall down the
    /// rabbit hole; under it: Play, Settings, Quit. Play leads to three save slots, each showing where Alice was,
    /// the time played, her decrees and finds; an empty slot starts a new game, a used one continues or is
    /// deleted after a question. Settings change language, screen, sound and camera shake at once. Built in code
    /// from the <see cref="MenuSkin"/>, like the map and the rest menu; whole-pixel scaling of 640 × 360.
    /// Stick, arrows or mouse choose, Submit confirms, Cancel goes back.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        private const int FontSize = 12;
        private const float ListLeft = -270f;
        private const float ListTop = 26f;
        private const float LineHeight = 17f;
        private const float SlotHeight = 54f;
        private const float ListWidth = 276f;

        private enum Page
        {
            Main,
            Slots,
            SlotActions,
            ConfirmDelete,
            Settings
        }

        [SerializeField] private MenuSkin _skin;

        private CanvasScaler _scaler;
        private RectTransform _list;
        private RectTransform _cursor;
        private Text _title;
        private Text _hints;
        private readonly List<MenuLine> _items = new List<MenuLine>();
        private Page _page = Page.Main;
        private int _slot = 1;
        private int _selected;
        private bool _starting;
        private Action _back;

        public MenuSkin Skin
        {
            get => _skin;
            set => _skin = value;
        }

        private void Awake()
        {
            if (_skin == null)
            {
                enabled = false;
                return;
            }

            EnsureEventSystem();
            Build();
        }

        private void OnEnable()
        {
            GameSettings.Changed += Refresh;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Refresh;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void Start()
        {
            Show(Page.Main);
        }

        private void Update()
        {
            _scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(Screen.height / (float)WorldMetrics.ReferenceHeight));
            // A click on empty space must not leave the menu with nothing chosen for the keyboard and the pad.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && _items.Count > 0)
            {
                Select(Mathf.Clamp(_selected, 0, _items.Count - 1));
            }
        }

        private void OnLocaleChanged(Locale locale)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_list != null)
            {
                Show(_page, _selected);
            }
        }

        private void Show(Page page, int select = 0)
        {
            _page = page;
            foreach (MenuLine item in _items)
            {
                Destroy(item.gameObject);
            }

            _items.Clear();
            MenuSkin.Words words = _skin.Text;
            _title.text = string.Empty;
            _hints.text = L(words.Hints);
            float y = ListTop;
            switch (page)
            {
                case Page.Main:
                    _back = null;
                    AddLine(words.Play, ref y, () => Show(Page.Slots));
                    AddLine(words.Settings, ref y, () => Show(Page.Settings));
                    AddLine(words.Quit, ref y, Quit);
                    break;
                case Page.Slots:
                    _back = () => Show(Page.Main);
                    _title.text = L(words.SlotsTitle);
                    for (int slot = 1; slot <= SaveSlots.Count; slot++)
                    {
                        AddSlot(slot, ref y);
                    }

                    y -= 4f;
                    AddLine(words.Back, ref y, _back);
                    break;
                case Page.SlotActions:
                    _back = () => Show(Page.Slots, _slot - 1);
                    _title.text = string.Format(L(words.Slot), _slot);
                    AddLine(words.Continue, ref y, () => StartGame(_slot));
                    AddLine(words.Delete, ref y, () => Show(Page.ConfirmDelete, 1));
                    AddLine(words.Back, ref y, _back);
                    break;
                case Page.ConfirmDelete:
                    _back = () => Show(Page.SlotActions, 1);
                    _title.text = string.Format(L(words.DeleteQuestion), _slot);
                    AddLine(words.Yes, ref y, () =>
                    {
                        SaveSlots.Delete(_slot);
                        Show(Page.Slots, _slot - 1);
                    });
                    AddLine(words.No, ref y, _back);
                    break;
                case Page.Settings:
                    _back = () => Show(Page.Main, 1);
                    _title.text = L(words.SettingsTitle);
                    AddSettings(ref y);
                    y -= 4f;
                    AddLine(words.Back, ref y, _back);
                    break;
            }

            Link();
            Select(Mathf.Clamp(select, 0, _items.Count - 1));
        }

        private void AddSlot(int slot, ref float y)
        {
            MenuSkin.Words words = _skin.Text;
            string text;
            Action action;
            if (SaveSlots.TryRead(slot, out SaveData data))
            {
                string place = PlaceOf(data);
                int minutes = Mathf.FloorToInt(data.PlayTime / 60f);
                string time = string.Format(L(words.PlayTime), minutes / 60, minutes % 60);
                int finds = (data.Abilities?.Count ?? 0) + (data.Trinkets?.Count ?? 0);
                text = $"{string.Format(L(words.Slot), slot)} · {place}\n"
                    + $"{time} · {string.Format(L(words.Decrees), data.Decrees)} · {string.Format(L(words.Finds), finds)}\n"
                    + When(data.SavedAtUtc);
                action = () =>
                {
                    _slot = slot;
                    Show(Page.SlotActions);
                };
            }
            else
            {
                text = $"{string.Format(L(words.Slot), slot)}\n{L(words.EmptySlot)}";
                action = () => StartGame(slot);
            }

            MenuLine item = AddItem(text, ref y, SlotHeight, TextAnchor.UpperLeft);
            item.Submitted = action;
        }

        private void AddSettings(ref float y)
        {
            MenuSkin.Words words = _skin.Text;
            GameSettings settings = GameSettings.Current;
            AddSetting(words.Language, LanguageName(settings.Language), ref y, step =>
            {
                List<string> codes = LanguageCodes();
                int index = codes.IndexOf(settings.Language);
                settings.Language = codes[(Mathf.Max(0, index) + step + codes.Count) % codes.Count];
            });
            AddSetting(words.Screen, L(settings.FullScreen ? words.FullScreen : words.Windowed), ref y,
                _ => settings.FullScreen = !settings.FullScreen);
            AddSetting(words.WindowScale, $"×{settings.WindowScale}", ref y, step =>
                settings.WindowScale = Wrap(settings.WindowScale + step, GameSettings.MinWindowScale, GameSettings.MaxWindowScale));
            AddSetting(words.VSync, OnOff(settings.VSync), ref y, _ => settings.VSync = !settings.VSync);
            AddSetting(words.MasterVolume, Level(settings.MasterVolume), ref y, step =>
                settings.MasterVolume = Mathf.Clamp(settings.MasterVolume + step, 0, GameSettings.MaxVolume));
            AddSetting(words.MusicVolume, Level(settings.MusicVolume), ref y, step =>
                settings.MusicVolume = Mathf.Clamp(settings.MusicVolume + step, 0, GameSettings.MaxVolume));
            AddSetting(words.EffectsVolume, Level(settings.EffectsVolume), ref y, step =>
                settings.EffectsVolume = Mathf.Clamp(settings.EffectsVolume + step, 0, GameSettings.MaxVolume));
            AddSetting(words.ScreenShake, OnOff(settings.ScreenShake), ref y, _ => settings.ScreenShake = !settings.ScreenShake);
        }

        private void AddSetting(LocalizedString label, string value, ref float y, Action<int> change)
        {
            MenuLine item = AddItem(L(label), ref y, LineHeight, TextAnchor.MiddleLeft);
            Text shown = Label(item.transform as RectTransform, TextAnchor.MiddleRight);
            shown.rectTransform.anchorMin = Vector2.zero;
            shown.rectTransform.anchorMax = Vector2.one;
            shown.rectTransform.sizeDelta = Vector2.zero;
            shown.text = $"‹ {value} ›";
            item.Value = shown;
            int index = _items.Count - 1;
            item.Changed = step =>
            {
                change(step);
                _selected = index;
                GameSettings.Current.Commit();
            };
            item.Submitted = () => item.Changed(1);
        }

        private void AddLine(LocalizedString text, ref float y, Action action)
        {
            MenuLine item = AddItem(L(text), ref y, LineHeight, TextAnchor.MiddleLeft);
            item.Submitted = action;
        }

        private MenuLine AddItem(string text, ref float y, float height, TextAnchor anchor)
        {
            RectTransform rect = Rect("Item", _list, new Vector2(ListLeft, y), new Vector2(ListWidth, height));
            rect.pivot = new Vector2(0f, 1f);
            // A transparent image so the mouse can hit the whole line.
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            MenuLine item = rect.gameObject.AddComponent<MenuLine>();
            item.transition = Selectable.Transition.None;
            Text label = Label(rect, anchor);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            label.text = text;
            item.Label = label;
            int index = _items.Count;
            item.Highlighted = selected =>
            {
                _selected = index;
                PlaceCursor(selected);
            };
            item.Cancelled = () => _back?.Invoke();
            _items.Add(item);
            y -= height;
            return item;
        }

        // Up and down walk the list and wrap round; left and right belong to the setting lines.
        private void Link()
        {
            for (int index = 0; index < _items.Count; index++)
            {
                _items[index].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = _items[(index + _items.Count - 1) % _items.Count],
                    selectOnDown = _items[(index + 1) % _items.Count]
                };
            }
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _items.Count || EventSystem.current == null)
            {
                return;
            }

            _selected = index;
            EventSystem.current.SetSelectedGameObject(_items[index].gameObject);
            PlaceCursor(_items[index]);
        }

        private void PlaceCursor(MenuLine item)
        {
            if (_cursor == null)
            {
                return;
            }

            var rect = (RectTransform)item.transform;
            float lineMiddle = rect.anchoredPosition.y - Mathf.Min(rect.sizeDelta.y, LineHeight) * 0.5f;
            _cursor.anchoredPosition = new Vector2(rect.anchoredPosition.x - 4f, Mathf.Round(lineMiddle));
        }

        private void StartGame(int slot)
        {
            if (_starting)
            {
                return;
            }

            _starting = true;
            SaveSlots.Current = slot;
            SceneManager.LoadScene(_skin.GameScene, LoadSceneMode.Single);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private string PlaceOf(SaveData data)
        {
            string roomId = string.IsNullOrEmpty(data.RoomId) ? data.RestRoomId : data.RoomId;
            MapAtlas atlas = _skin.Atlas;
            MapAtlas.RoomEntry room = atlas != null ? atlas.FindRoom(roomId) : null;
            MapAtlas.ZoneEntry zone = room != null ? atlas.FindZone(room.ZoneId) : null;
            return zone != null && !zone.Name.IsEmpty ? zone.Name.GetLocalizedString() : L(_skin.Text.UnknownPlace);
        }

        private static string When(string savedAtUtc)
        {
            if (!DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime saved))
            {
                return string.Empty;
            }

            Locale locale = LocalizationSettings.SelectedLocale;
            CultureInfo culture = locale != null ? locale.Identifier.CultureInfo ?? CultureInfo.InvariantCulture : CultureInfo.InvariantCulture;
            return saved.ToLocalTime().ToString("d MMMM yyyy, HH:mm", culture);
        }

        private string LanguageName(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return L(_skin.Text.LanguageSystem);
            }

            // Each language is named in itself, as players look for it.
            CultureInfo culture = new LocaleIdentifier(code).CultureInfo;
            if (culture == null)
            {
                return code;
            }

            string name = culture.NativeName;
            return name.Length > 0 ? char.ToUpper(name[0], culture) + name.Substring(1) : code;
        }

        private static List<string> LanguageCodes()
        {
            var codes = new List<string> { string.Empty };
            foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
            {
                codes.Add(locale.Identifier.Code);
            }

            return codes;
        }

        private string OnOff(bool on)
        {
            return L(on ? _skin.Text.On : _skin.Text.Off);
        }

        private static string Level(int value)
        {
            return $"{value}/{GameSettings.MaxVolume}";
        }

        private static int Wrap(int value, int min, int max)
        {
            return value > max ? min : value < min ? max : value;
        }

        private static string L(LocalizedString text)
        {
            return text == null || text.IsEmpty ? string.Empty : text.GetLocalizedString();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var system = new GameObject("EventSystem", typeof(EventSystem));
            var module = system.AddComponent<InputSystemUIInputModule>();
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                return;
            }

            // The UI map of AliceInput: the menu reads the same bindings as the rest of the game.
            module.actionsAsset = actions;
            module.move = Reference(actions, "UI/Navigate");
            module.submit = Reference(actions, "UI/Submit");
            module.cancel = Reference(actions, "UI/Cancel");
            module.point = Reference(actions, "UI/Point");
            module.leftClick = Reference(actions, "UI/Click");
        }

        private static InputActionReference Reference(InputActionAsset actions, string path)
        {
            InputAction action = actions.FindAction(path);
            return action != null ? InputActionReference.Create(action) : null;
        }

        private void Build()
        {
            var canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            _scaler = canvasObject.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var root = (RectTransform)canvasObject.transform;

            // Everything below is laid out on the 640 × 360 screen, centred.
            Image black = Rect("Black", root, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            black.rectTransform.anchorMin = Vector2.zero;
            black.rectTransform.anchorMax = Vector2.one;
            black.color = Color.black;
            RectTransform screen = Rect("Screen", root, Vector2.zero,
                new Vector2(WorldMetrics.ReferenceWidth, WorldMetrics.ReferenceHeight));

            if (_skin.Background != null)
            {
                Image background = Rect("Background", screen, Vector2.zero,
                    new Vector2(WorldMetrics.ReferenceWidth, WorldMetrics.ReferenceHeight)).gameObject.AddComponent<Image>();
                background.sprite = _skin.Background;
            }

            if (_skin.Logo != null)
            {
                Vector2 size = _skin.Logo.rect.size;
                RectTransform logo = Rect("Logo", screen, new Vector2(-306f + size.x * 0.5f, 115f), size);
                logo.gameObject.AddComponent<Image>().sprite = _skin.Logo;
            }

            _list = Rect("List", screen, Vector2.zero, Vector2.zero);

            _title = Label(screen, TextAnchor.MiddleLeft);
            _title.rectTransform.anchoredPosition = new Vector2(ListLeft + ListWidth * 0.5f, ListTop + 12f);
            _title.rectTransform.sizeDelta = new Vector2(ListWidth, 16f);
            _title.color = new Color(0.86f, 0.62f, 0.42f);

            if (_skin.Cursor != null)
            {
                _cursor = Rect("Cursor", screen, Vector2.zero, _skin.Cursor.rect.size);
                _cursor.pivot = new Vector2(1f, 0.5f);
                _cursor.gameObject.AddComponent<Image>().sprite = _skin.Cursor;
                _cursor.SetAsLastSibling();
            }

            _hints = Label(screen, TextAnchor.MiddleLeft);
            _hints.rectTransform.anchoredPosition = new Vector2(ListLeft + 200f, -171f);
            _hints.rectTransform.sizeDelta = new Vector2(400f, 16f);
            _hints.color = new Color(0.55f, 0.5f, 0.5f);
            _hints.text = L(_skin.Text.Hints);
        }

        private Text Label(RectTransform parent, TextAnchor alignment)
        {
            Text text = Rect("Text", parent, Vector2.zero, new Vector2(100f, 16f)).gameObject.AddComponent<Text>();
            text.font = _skin.Font;
            text.fontSize = FontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, RectTransform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
