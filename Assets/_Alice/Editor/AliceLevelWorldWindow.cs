using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// Alice → Level → Biome Map: every palette room drawn on one world map with its ID, and the world check below
    /// it — overlaps, passages that end in a wall, passages to nowhere. Opened from the Level Palette.
    /// </summary>
    public sealed class AliceLevelWorldWindow : EditorWindow
    {
        private const int PixelsPerTile = 3;

        private List<AliceLevelWorld.RoomShape> _rooms = new List<AliceLevelWorld.RoomShape>();
        private List<string> _problems = new List<string>();
        private Texture2D _map;
        private RectInt _world;
        private float _zoom = 1f;
        private Vector2 _scroll;

        [MenuItem("Alice/Level/Biome Map")]
        public static void Open()
        {
            GetWindow<AliceLevelWorldWindow>("Карта биома").Refresh();
        }

        public void Refresh()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            _rooms = AliceLevelWorld.LoadRooms();
            _problems = AliceLevelWorld.Check(_rooms);
            if (_map != null)
            {
                DestroyImmediate(_map);
            }

            _map = AliceLevelWorld.Render(_rooms, PixelsPerTile, out _world);
            Repaint();
        }

        private void OnDisable()
        {
            if (_map != null)
            {
                DestroyImmediate(_map);
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Обновить", GUILayout.Width(100f)))
                {
                    Refresh();
                }

                _zoom = EditorGUILayout.Slider("Масштаб", _zoom, 0.5f, 4f);
            }

            EditorGUILayout.LabelField($"Комнат: {_rooms.Count}. Проблем: {_problems.Count}.");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_map != null)
            {
                Rect area = GUILayoutUtility.GetRect(_map.width * _zoom, _map.height * _zoom,
                    GUILayout.Width(_map.width * _zoom), GUILayout.Height(_map.height * _zoom));
                GUI.DrawTexture(area, _map, ScaleMode.StretchToFill);
                float scale = PixelsPerTile * _zoom;
                foreach (AliceLevelWorld.RoomShape room in _rooms)
                {
                    // The texture's origin is its bottom-left; the GUI's is top-left.
                    var rect = new Rect(area.x + (room.Rect.xMin - _world.xMin) * scale,
                        area.yMax - (room.Rect.yMax - _world.yMin) * scale, room.Rect.width * scale, room.Rect.height * scale);
                    Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(1f, 0.85f, 0.3f, 0.9f));
                    GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 200f, 18f), $"{room.Id}  ({room.Rect.xMin}, {room.Rect.yMin})", EditorStyles.whiteBoldLabel);
                }
            }

            foreach (string problem in _problems)
            {
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
