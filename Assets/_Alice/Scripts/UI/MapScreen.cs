using System.Collections.Generic;
using System.Linq;
using Alice.Core;
using Alice.Player;
using Alice.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// The map screen (GDD 0.33, 0.34). The Map button opens and closes it; Evade closes it too. It opens at room
    /// scale, centred on Alice, so part of the room around her fills the window; Jump switches to the overview and
    /// back. A cursor in the middle moves over the map with the stick; Interact leaves the chosen mark there or takes
    /// away the mark under it, the shoulder buttons (Heal, UseAbility) choose the kind of mark. It shows only what
    /// Alice has explored: walls, passages, resting chairs and the bundles still lying where she saw them. On the
    /// right the legend lists what the visible map shows. The world keeps running — the global time is never touched — but
    /// Alice stands still while the map is open, so it is no refuge in a fight.
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        private const int SortingOrder = 920;
        private const int FontSize = 12;
        private const int ViewportWidth = 420;
        private const int ViewportHeight = 260;
        private const int LegendWidth = 170;
        private const int LegendRow = 14;
        // Screen pixels per second, so the cursor crosses the window in the same time at either zoom.
        private const float CursorPixelsPerSecond = 150f;
        private const float MarkRemoveRadius = 2f;
        private const float BlinkPeriod = 0.8f;
        private const float AfterCloseLock = 0.2f;
        // Room scale first (8 px per tile: about 52 × 32 tiles in the window), then the overview.
        private static readonly int[] Zooms = { 8, 2 };

        [SerializeField] private MapSkin _skin;

        private MapProgress _progress;
        private PlayerMotor _motor;
        private PlayerRest _rest;
        private IPlayerCommandSource _commands;
        private InputAction _mapAction;

        private CanvasScaler _scaler;
        private RectTransform _root;
        private RectTransform _screen;
        private RawImage _map;
        private RectTransform _icons;
        private RawImage _alice;
        private Text _title;
        private Text _hints;
        private RectTransform _legend;
        private Texture2D _mapTexture;
        private Texture2D _cursorTexture;

        private int _zoomIndex;
        private int _markIndex;
        private Vector2 _cursor;
        private int _drawnVersion = -1;
        private int _drawnZoom = -1;
        private string _drawnRoom;
        private float _openedAt;
        private float _seenInteract;
        private float _seenJump;
        private float _seenEvade;
        private float _seenHeal;
        private float _seenAbility;

        public MapSkin Skin
        {
            get => _skin;
            set => _skin = value;
        }

        public bool IsOpen => _screen != null && _screen.gameObject.activeSelf;

        private int Zoom => Zooms[_zoomIndex];

        // Enough paper round the rooms that the window never shows the edge of the sheet: the cursor stays inside
        // the rooms, so half the window beyond them is always covered.
        private int PaperMargin => Mathf.CeilToInt(Mathf.Max(ViewportWidth, ViewportHeight) * 0.5f / Zoom) + 2;

        private MapAtlas Atlas => _progress != null ? _progress.Atlas : null;

        private void Awake()
        {
            _progress = GetComponent<MapProgress>();
            _motor = GetComponent<PlayerMotor>();
            _rest = GetComponent<PlayerRest>();
            _commands = GetComponent<IPlayerCommandSource>();
            if (_skin == null || _progress == null || _motor == null || _commands == null)
            {
                enabled = false;
                return;
            }

            _mapAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Gameplay/Map") : null;
            Build();
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            if (_mapTexture != null)
            {
                Destroy(_mapTexture);
            }

            if (_cursorTexture != null)
            {
                Destroy(_cursorTexture);
            }
        }

        private void Update()
        {
            _scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(Screen.height / (float)WorldMetrics.ReferenceHeight));
            bool toggle = _mapAction != null && _mapAction.WasPressedThisFrame();
            if (!IsOpen)
            {
                if (toggle && CanOpen())
                {
                    Open();
                }

                return;
            }

            if (toggle || Pressed(_commands.EvadePressedAt, ref _seenEvade))
            {
                Close();
                return;
            }

            // Alice stands still; the lock also keeps her actions from taking the map's button presses.
            _motor.Lock(0.1f);
            MoveCursor();
            HandleMarks();
            if (Pressed(_commands.JumpPressedAt, ref _seenJump))
            {
                _zoomIndex = (_zoomIndex + 1) % Zooms.Length;
            }

            Redraw();
            Place();
        }

        private bool CanOpen()
        {
            return Atlas != null && !_motor.IsLocked && (_rest == null || _rest.Stage == PlayerRest.RestStage.None);
        }

        private void Open()
        {
            _screen.gameObject.SetActive(true);
            _openedAt = Time.time;
            _seenInteract = _seenJump = _seenEvade = _seenHeal = _seenAbility = Time.time;
            _zoomIndex = 0;
            // The cursor starts on Alice's icon, which stands above her feet.
            _cursor = AliceOnMap(out _) + new Vector2(0f, (MapPainter.LineThickness(Zoom) + (MapGlyphs.Size + 2) * 0.5f) / Zoom);
            _drawnVersion = -1;
            Redraw();
            Place();
        }

        private void Close()
        {
            _screen.gameObject.SetActive(false);
            _motor.Lock(AfterCloseLock);
        }

        // A press counts once, and only if it came after the map opened.
        private bool Pressed(float pressedAt, ref float seen)
        {
            if (pressedAt <= seen || pressedAt < _openedAt)
            {
                return false;
            }

            seen = pressedAt;
            return true;
        }

        private void MoveCursor()
        {
            Vector2 move = _commands.Move;
            if (move.sqrMagnitude < 0.04f)
            {
                return;
            }

            _cursor += move * (CursorPixelsPerSecond / Zoom * Time.unscaledDeltaTime);
            RectInt bounds = Atlas.Bounds;
            _cursor.x = Mathf.Clamp(_cursor.x, bounds.xMin, bounds.xMax);
            _cursor.y = Mathf.Clamp(_cursor.y, bounds.yMin, bounds.yMax);
        }

        private void HandleMarks()
        {
            if (Pressed(_commands.HealPressedAt, ref _seenHeal))
            {
                _markIndex = (_markIndex + MapIconKinds.PlayerMarks.Length - 1) % MapIconKinds.PlayerMarks.Length;
                _drawnVersion = -1;
            }

            if (Pressed(_commands.UseAbilityPressedAt, ref _seenAbility))
            {
                _markIndex = (_markIndex + 1) % MapIconKinds.PlayerMarks.Length;
                _drawnVersion = -1;
            }

            if (Pressed(_commands.InteractPressedAt, ref _seenInteract)
                && !_progress.State.RemoveMarkNear(_cursor, MarkRemoveRadius))
            {
                _progress.State.AddMark(MapIconKinds.PlayerMarks[_markIndex], _cursor);
            }
        }

        private Vector2 AliceOnMap(out bool known)
        {
            known = Atlas.TryToWorld(RoomPresence.CurrentId, transform.position, out Vector2 world);
            return world;
        }

        private void Redraw()
        {
            string room = RoomPresence.CurrentId;
            if (_drawnVersion == _progress.State.Version && _drawnZoom == Zoom && _drawnRoom == room)
            {
                return;
            }

            _drawnVersion = _progress.State.Version;
            _drawnZoom = Zoom;
            _drawnRoom = room;
            _mapTexture = MapPainter.Paint(Atlas, _progress.State, room, Zoom, _mapTexture, PaperMargin);
            _map.texture = _mapTexture;
            _map.rectTransform.sizeDelta = new Vector2(_mapTexture.width, _mapTexture.height);

            MapAtlas.RoomEntry current = Atlas.FindRoom(room);
            MapAtlas.ZoneEntry zone = current != null ? Atlas.FindZone(current.ZoneId) : null;
            _title.text = zone != null && !zone.Name.IsEmpty ? zone.Name.GetLocalizedString() : Localized(_skin.UnknownZone);
            _hints.text = Localized(_skin.Hints);
            BuildIcons();
        }

        private void BuildIcons()
        {
            foreach (Transform child in _icons)
            {
                Destroy(child.gameObject);
            }

            var shown = new HashSet<MapIconKind> { MapIconKind.Alice };
            foreach (MapAtlas.IconEntry icon in Atlas.Icons)
            {
                if (!IsSeen(icon) || (icon.Kind == MapIconKind.Item && BundleItems.AliceHas(icon.ItemId)))
                {
                    continue;
                }

                StandIcon(Icon(icon.Kind, _icons), icon.Position - new Vector2(0f, MapAtlas.FloorIconLift));
                shown.Add(icon.Kind);
            }

            foreach (MapMark mark in _progress.State.Marks)
            {
                AddIcon(mark.Kind, mark.Position, 1f);
                shown.Add(mark.Kind);
            }

            _alice.transform.SetAsLastSibling();
            BuildLegend(shown);
        }

        // A chair or a bundle is on the map once the cell it stands in has been explored.
        private bool IsSeen(MapAtlas.IconEntry icon)
        {
            MapAtlas.RoomEntry room = Atlas.FindRoom(icon.RoomId);
            if (room == null)
            {
                return false;
            }

            Vector2Int cell = MapAtlas.CellOf(room, icon.Position);
            return _progress.State.IsExplored(room, cell.x, cell.y);
        }

        // Something standing on the floor: the icon's bottom sits on the chalk floor line, not across it, so Alice
        // and the chairs read as standing on the floor rather than inside it.
        private void StandIcon(RawImage image, Vector2 floorPoint)
        {
            image.rectTransform.pivot = new Vector2(0.5f, 0f);
            image.rectTransform.anchoredPosition = ToMapPixels(floorPoint) + new Vector2(0f, MapPainter.LineThickness(Zoom));
        }

        private void AddIcon(MapIconKind kind, Vector2 world, float alpha)
        {
            RawImage image = Icon(kind, _icons);
            image.color = new Color(1f, 1f, 1f, alpha);
            image.rectTransform.anchoredPosition = ToMapPixels(world);
        }

        private void BuildLegend(HashSet<MapIconKind> shown)
        {
            foreach (Transform child in _legend)
            {
                Destroy(child.gameObject);
            }

            float y = 0f;
            foreach (MapIconKind kind in System.Enum.GetValues(typeof(MapIconKind)).Cast<MapIconKind>().Where(shown.Contains))
            {
                LegendRowAt(kind, Localized(_skin.Find(kind)?.Label), y);
                y -= LegendRow;
            }

            y -= LegendRow / 2;
            MapIconKind chosen = MapIconKinds.PlayerMarks[_markIndex];
            LegendRowAt(chosen, $"{Localized(_skin.MarkCaption)} {Localized(_skin.Find(chosen)?.Label)}", y);
        }

        private void LegendRowAt(MapIconKind kind, string label, float y)
        {
            RawImage icon = Icon(kind, _legend);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 1f);
            icon.rectTransform.anchoredPosition = new Vector2(6f, y - 6f);
            Text text = Label(_legend, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(LegendWidth - 16, LegendRow);
            text.rectTransform.anchoredPosition = new Vector2(14f, y - 6f);
            text.text = label;
        }

        // Every frame: the map slides so the cursor stays in the middle; Alice's mark blinks where she is.
        private void Place()
        {
            _map.rectTransform.anchoredPosition = -ToMapPixels(_cursor);
            Vector2 alice = AliceOnMap(out bool known);
            _alice.gameObject.SetActive(known);
            StandIcon(_alice, alice);
            bool lit = Mathf.Repeat(Time.unscaledTime, BlinkPeriod) < BlinkPeriod * 0.65f;
            _alice.color = new Color(1f, 1f, 1f, lit ? 1f : 0.35f);
        }

        private Vector2 ToMapPixels(Vector2 world)
        {
            Vector2Int origin = MapPainter.Origin(Atlas, PaperMargin);
            return new Vector2(Mathf.Round((world.x - origin.x) * Zoom), Mathf.Round((world.y - origin.y) * Zoom));
        }

        private static string Localized(UnityEngine.Localization.LocalizedString text)
        {
            return text == null || text.IsEmpty ? string.Empty : text.GetLocalizedString();
        }

        private void Build()
        {
            GameObject canvasObject = new GameObject("MapScreen", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.pixelPerfect = true;
            _scaler = canvasObject.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _root = (RectTransform)canvasObject.transform;

            _screen = Rect("Screen", _root, Vector2.zero, Vector2.zero);
            _screen.anchorMin = Vector2.zero;
            _screen.anchorMax = Vector2.one;
            Image dim = _screen.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.03f, 0.05f, 0.88f);

            // A thin chalk frame, then the viewport filled with the map's paper so space beyond the map reads as paper.
            Vector2 viewportCentre = new Vector2(-(LegendWidth / 2 + 8), -4f);
            Image frame = Rect("Frame", _screen, viewportCentre, new Vector2(ViewportWidth + 2, ViewportHeight + 2)).gameObject.AddComponent<Image>();
            frame.color = new Color(0.85f, 0.82f, 0.75f, 0.6f);
            RectTransform viewport = Rect("Viewport", _screen, viewportCentre, new Vector2(ViewportWidth, ViewportHeight));
            viewport.gameObject.AddComponent<Image>().color = MapPainter.PaperColor;
            viewport.gameObject.AddComponent<RectMask2D>();

            _map = Rect("Map", viewport, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            _map.rectTransform.pivot = Vector2.zero;
            _icons = Rect("Icons", _map.rectTransform, Vector2.zero, Vector2.zero);
            _icons.anchorMin = _icons.anchorMax = Vector2.zero;
            _alice = Icon(MapIconKind.Alice, _map.rectTransform);

            _cursorTexture = CursorTexture();
            RawImage cursor = Rect("Cursor", viewport, Vector2.zero, new Vector2(_cursorTexture.width, _cursorTexture.height)).gameObject.AddComponent<RawImage>();
            cursor.texture = _cursorTexture;

            _title = Label(_screen, TextAnchor.MiddleCenter);
            _title.rectTransform.anchoredPosition = new Vector2(0f, ViewportHeight / 2 + 14);
            _title.rectTransform.sizeDelta = new Vector2(600f, 16f);

            _hints = Label(_screen, TextAnchor.MiddleCenter);
            _hints.rectTransform.anchoredPosition = new Vector2(0f, -(ViewportHeight / 2 + 16));
            _hints.rectTransform.sizeDelta = new Vector2(620f, 16f);

            _legend = Rect("Legend", _screen, new Vector2(ViewportWidth / 2 - LegendWidth / 2 + 16 + LegendWidth / 2, ViewportHeight / 2 - 4f), new Vector2(LegendWidth, ViewportHeight));
            _legend.pivot = new Vector2(0.5f, 1f);

            _screen.gameObject.SetActive(false);
        }

        private RawImage Icon(MapIconKind kind, RectTransform parent)
        {
            MapSkin.Legend entry = _skin.Find(kind);
            RawImage image = Rect(kind.ToString(), parent, Vector2.zero, new Vector2(MapGlyphs.Size + 2, MapGlyphs.Size + 2)).gameObject.AddComponent<RawImage>();
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.zero;
            if (entry != null && entry.Icon != null)
            {
                image.texture = entry.Icon.texture;
                Rect r = entry.Icon.textureRect;
                Texture texture = entry.Icon.texture;
                image.uvRect = new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height);
                image.rectTransform.sizeDelta = r.size;
            }
            else
            {
                image.texture = MapGlyphs.Texture(kind);
            }

            return image;
        }

        private Text Label(RectTransform parent, TextAnchor alignment)
        {
            Text text = Rect("Text", parent, Vector2.zero, new Vector2(100f, 16f)).gameObject.AddComponent<Text>();
            text.font = _skin.Font;
            text.fontSize = FontSize;
            text.alignment = alignment;
            text.color = new Color(0.9f, 0.87f, 0.8f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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

        // A hollow square of chalk with a gap in each side: the place a mark would go.
        private static Texture2D CursorTexture()
        {
            const int size = 11;
            var pixels = new Color32[size * size];
            var chalk = new Color32(232, 224, 204, 255);
            for (int i = 0; i < size; i++)
            {
                bool gap = i >= 4 && i <= 6;
                if (gap)
                {
                    continue;
                }

                pixels[i] = chalk;
                pixels[(size - 1) * size + i] = chalk;
                pixels[i * size] = chalk;
                pixels[i * size + size - 1] = chalk;
            }

            pixels[(size / 2) * size + size / 2] = chalk;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "MapCursor" };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
