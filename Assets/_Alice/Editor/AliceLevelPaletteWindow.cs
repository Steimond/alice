using System.Collections.Generic;
using System.Linq;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Alice → Level Palette: everything for building a room in one window. Geometry is painted on the tile grid
    /// in the Scene view (drag a rectangle, Shift erases); objects are picked by category and clicked onto the
    /// floor. The room tools fit the camera, set the start, check the room and bake the art.
    /// Labels are Russian: the window is for the owner, and nothing here reaches the player.
    /// </summary>
    public sealed class AliceLevelPaletteWindow : EditorWindow
    {
        private enum Tool
        {
            None,
            Paint,
            Place,
            Start
        }

        private const float ThumbnailSize = 64f;

        private static readonly string[] BrushLabels = { "Массив", "Мостки", "Опасность", "Задняя стена" };

        private Tool _tool;
        private LevelBrush _brush;
        private GameObject _prefab;
        private List<AliceLevelPaletteCatalog.Category> _categories = new List<AliceLevelPaletteCatalog.Category>();
        private int _category;
        private Vector2 _scroll;
        private string _newRoomId = "L01";
        private List<string> _problems = new List<string>();
        private string _status = string.Empty;

        private bool _dragging;
        private Vector2Int _dragStart;
        private Vector2Int _dragEnd;

        [MenuItem("Alice/Level Palette")]
        public static void Open()
        {
            GetWindow<AliceLevelPaletteWindow>("Палитра уровня").Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Reload();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnFocus()
        {
            Reload();
        }

        private void Reload()
        {
            _categories = AliceLevelPaletteCatalog.Load();
            _category = Mathf.Clamp(_category, 0, Mathf.Max(0, _categories.Count - 1));
        }

        private void OnGUI()
        {
            Scene scene = SceneManager.GetActiveScene();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawRoom(scene);
            EditorGUILayout.Space();
            DrawGeometry();
            EditorGUILayout.Space();
            DrawObjects();
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                SetTool(Tool.None);
                Event.current.Use();
            }
        }

        private void DrawRoom(Scene scene)
        {
            EditorGUILayout.LabelField("Комната", EditorStyles.boldLabel);
            Room room = AliceLevelRoomTools.FindRoom(scene);
            EditorGUILayout.LabelField("Сцена", scene.name);
            EditorGUILayout.LabelField("ID", room != null ? room.RoomId : "— нет компонента Room —");

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newRoomId = EditorGUILayout.TextField(_newRoomId, GUILayout.MinWidth(60f));
                    if (GUILayout.Button("Новая комната"))
                    {
                        AliceLevelRoomTools.CreateRoom(_newRoomId, out _status);
                        SetTool(Tool.None);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Камера по массиву"))
                    {
                        _status = AliceLevelRoomTools.FitCamera(scene)
                            ? "Границы камеры подогнаны под массив."
                            : "Нужен Room и нарисованный массив.";
                    }

                    if (GUILayout.Toggle(_tool == Tool.Start, "Точка старта", "Button") != (_tool == Tool.Start))
                    {
                        SetTool(_tool == Tool.Start ? Tool.None : Tool.Start);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Проверить комнату"))
                    {
                        _problems = AliceLevelRoomTools.Validate(scene);
                        _status = _problems.Count == 0 ? "Проблем не найдено." : $"Проблем: {_problems.Count}.";
                    }

                    if (GUILayout.Button("Запечь: плитка"))
                    {
                        AliceRoomArtBaker.BakeTilesFromMenu();
                        _status = "Арт запечён (плитка прачечной). Сохраните сцену.";
                    }

                    if (GUILayout.Button("Запечь: плиты"))
                    {
                        AliceRoomArtBaker.BakeSlabsFromMenu();
                        _status = "Арт запечён (мокрые плиты). Сохраните сцену.";
                    }
                }
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }

            foreach (string problem in _problems)
            {
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }
        }

        private void DrawGeometry()
        {
            EditorGUILayout.LabelField("Геометрия", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (LevelBrush brush in AliceLevelGeometry.Brushes)
                {
                    Color previous = GUI.backgroundColor;
                    GUI.backgroundColor = AliceLevelGeometry.BrushColor(brush);
                    bool active = _tool == Tool.Paint && _brush == brush;
                    if (GUILayout.Toggle(active, BrushLabels[(int)brush], "Button", GUILayout.Height(28f)) != active)
                    {
                        _brush = brush;
                        SetTool(active ? Tool.None : Tool.Paint);
                    }

                    GUI.backgroundColor = previous;
                }
            }

            EditorGUILayout.HelpBox(
                "В окне сцены: протянуть — залить прямоугольник клеток, с Shift — стереть, Esc — выйти из кисти. " +
                "Каждая клетка — тайл 16×16; блоки с коллизией пересобираются сами.",
                MessageType.None);
        }

        private void DrawObjects()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Объекты", EditorStyles.boldLabel);
                if (GUILayout.Button("Заполнить из готовых", GUILayout.Width(150f)))
                {
                    AliceLevelPaletteCatalog.Fill();
                    Reload();
                }
            }

            if (_categories.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Каталог пуст. «Заполнить из готовых» соберёт префабы из комнат M3; свои кладите в {AliceLevelPaletteCatalog.Root}/<Категория>.",
                    MessageType.Info);
                return;
            }

            _category = GUILayout.Toolbar(_category, _categories.Select(category => category.Label).ToArray());
            AliceLevelPaletteCatalog.Category current = _categories[_category];
            if (current.Prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox($"Пусто. Префабы этой категории — в {AliceLevelPaletteCatalog.Root}/{current.Folder}.", MessageType.None);
                return;
            }

            int columns = Mathf.Max(1, (int)((position.width - 20f) / (ThumbnailSize + 8f)));
            for (int start = 0; start < current.Prefabs.Count; start += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (GameObject prefab in current.Prefabs.Skip(start).Take(columns))
                    {
                        DrawEntry(prefab);
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.HelpBox("Выберите объект и щёлкните в окне сцены: он встанет низом на линию сетки под курсором. Esc — снять выбор.", MessageType.None);
        }

        private void DrawEntry(GameObject prefab)
        {
            bool active = _tool == Tool.Place && _prefab == prefab;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ThumbnailSize + 4f)))
            {
                Rect rect = GUILayoutUtility.GetRect(ThumbnailSize, ThumbnailSize, GUILayout.Width(ThumbnailSize), GUILayout.Height(ThumbnailSize));
                if (active)
                {
                    EditorGUI.DrawRect(rect, new Color(0.25f, 0.5f, 0.9f, 0.6f));
                }
                else
                {
                    EditorGUI.DrawRect(rect, new Color(0.17f, 0.18f, 0.2f));
                }

                DrawThumbnail(rect, prefab);
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    _prefab = prefab;
                    SetTool(active ? Tool.None : Tool.Place);
                    Event.current.Use();
                }

                GUILayout.Label(prefab.name, EditorStyles.miniLabel, GUILayout.Width(ThumbnailSize + 4f));
            }
        }

        // Pixel art is drawn from the sprite itself without smoothing; Unity's own previews blur it.
        private static void DrawThumbnail(Rect rect, GameObject prefab)
        {
            SpriteRenderer renderer = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(candidate => candidate.sprite != null)
                .OrderByDescending(candidate => candidate.sprite.rect.width * candidate.sprite.rect.height)
                .FirstOrDefault();
            if (renderer == null)
            {
                GUI.Label(rect, "без\nрисунка", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Sprite sprite = renderer.sprite;
            Texture2D texture = sprite.texture;
            Rect source = sprite.textureRect;
            float scale = Mathf.Min(rect.width / source.width, rect.height / source.height);
            if (scale >= 1f)
            {
                scale = Mathf.Floor(scale);
            }

            var target = new Rect(0f, 0f, source.width * scale, source.height * scale) { center = rect.center };
            var coords = new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height);
            Color previous = GUI.color;
            GUI.color = renderer.color;
            GUI.DrawTextureWithTexCoords(target, texture, coords);
            GUI.color = previous;
        }

        private void SetTool(Tool tool)
        {
            _tool = tool;
            _dragging = false;
            if (tool != Tool.None)
            {
                // Clicks in the Scene view now paint or place instead of selecting.
                Tools.current = UnityEditor.Tool.None;
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView view)
        {
            if (_tool == Tool.None || EditorApplication.isPlaying)
            {
                return;
            }

            Event current = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(control);
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                SetTool(Tool.None);
                current.Use();
                return;
            }

            Vector2 world = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;
            Scene scene = SceneManager.GetActiveScene();
            switch (_tool)
            {
                case Tool.Paint:
                    HandlePaint(scene, current, world);
                    break;
                case Tool.Place:
                case Tool.Start:
                    HandlePoint(scene, current, world);
                    break;
            }

            if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
            {
                view.Repaint();
            }
        }

        private void HandlePaint(Scene scene, Event current, Vector2 world)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
            bool erase = current.shift;
            if (current.type == EventType.MouseDown && current.button == 0 && !current.alt)
            {
                _dragging = true;
                _dragStart = cell;
                _dragEnd = cell;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _dragging)
            {
                _dragEnd = cell;
                current.Use();
            }
            else if (current.type == EventType.MouseUp && _dragging && current.button == 0)
            {
                _dragging = false;
                AliceLevelGeometry.Paint(scene, _brush, CellRect(_dragStart, _dragEnd), erase);
                current.Use();
            }

            RectInt preview = _dragging ? CellRect(_dragStart, _dragEnd) : new RectInt(cell, Vector2Int.one);
            Color color = erase ? new Color(0.1f, 0.1f, 0.1f) : AliceLevelGeometry.BrushColor(_brush);
            Handles.DrawSolidRectangleWithOutline(new Rect(preview.position, preview.size),
                new Color(color.r, color.g, color.b, 0.35f), erase ? Color.red : Color.white);
            if (_dragging)
            {
                Handles.Label(new Vector3(preview.xMin, preview.yMax + 0.6f, 0f), $"{preview.width}×{preview.height}");
            }
        }

        // Objects stand on a grid line, centred on whole or half tiles, so they line up with painted cells.
        private void HandlePoint(Scene scene, Event current, Vector2 world)
        {
            var floor = new Vector2(Mathf.Round(world.x * 2f) * 0.5f, Mathf.Round(world.y));
            Handles.color = Color.yellow;
            Handles.DrawLine(new Vector3(floor.x - 0.5f, floor.y, 0f), new Vector3(floor.x + 0.5f, floor.y, 0f));
            Handles.DrawLine(new Vector3(floor.x, floor.y, 0f), new Vector3(floor.x, floor.y + 1f, 0f));
            Handles.Label(new Vector3(floor.x + 0.3f, floor.y + 1.2f, 0f),
                _tool == Tool.Start ? "старт" : _prefab != null ? _prefab.name : string.Empty);

            if (current.type != EventType.MouseDown || current.button != 0 || current.alt)
            {
                return;
            }

            if (_tool == Tool.Start)
            {
                AliceLevelRoomTools.SetStart(scene, floor);
                _status = $"Старт комнаты: {floor.x}, {floor.y}.";
                SetTool(Tool.None);
            }
            else if (_prefab != null)
            {
                Selection.activeGameObject = AliceLevelRoomTools.Place(scene, _prefab, floor);
            }

            current.Use();
        }

        private static RectInt CellRect(Vector2Int a, Vector2Int b)
        {
            int xMin = Mathf.Min(a.x, b.x);
            int yMin = Mathf.Min(a.y, b.y);
            return new RectInt(xMin, yMin, Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);
        }
    }
}
