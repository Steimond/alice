using System.Collections.Generic;
using System.Linq;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Alice.Editor
{
    /// <summary>
    /// The biome as one world (LevelDesignGuide, section 4: a room is a place, not a portal). Every palette room has
    /// a world position; this checks that the rooms do not overlap and that every passage through a room's edge
    /// continues as a passage on the same world cells of the room next to it — or is reported as leading nowhere.
    /// It also draws the whole map for the Level Palette.
    /// </summary>
    public static class AliceLevelWorld
    {
        /// <summary>A room as the world sees it: its rectangle in world tiles and what fills each cell.</summary>
        public sealed class RoomShape
        {
            public string Id;
            public RectInt Rect;
            public bool[,] Solid;
            public bool[,] OneWay;
            public bool[,] Hazard;
            public readonly List<(string Id, Vector2 Position, string Target)> Doorways = new List<(string, Vector2, string)>();

            public RoomShape(string id, RectInt rect)
            {
                Id = id;
                Rect = rect;
                Solid = new bool[rect.width, rect.height];
                OneWay = new bool[rect.width, rect.height];
                Hazard = new bool[rect.width, rect.height];
            }

            public bool Contains(Vector2Int cell) => Rect.Contains(cell);

            public bool IsSolidAt(Vector2Int cell) => Solid[cell.x - Rect.xMin, cell.y - Rect.yMin];
        }

        private static readonly Vector2Int[] Sides = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

        /// <summary>
        /// Reads every room scene in the folder. Scenes that were not open are opened alongside and closed again
        /// without saving; rooms without painted geometry are skipped.
        /// </summary>
        public static List<RoomShape> LoadRooms(string folder = AliceLevelRoomTools.NewRoomsFolder)
        {
            var rooms = new List<RoomShape>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return rooms;
            }

            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path))
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                try
                {
                    RoomShape shape = Shape(scene);
                    if (shape != null)
                    {
                        rooms.Add(shape);
                    }
                }
                finally
                {
                    if (!wasOpen)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            return rooms;
        }

        public static RoomShape Shape(Scene scene)
        {
            Room room = AliceLevelRoomTools.FindRoom(scene);
            Tilemap solid = AliceLevelGeometry.FindLayer(scene, LevelBrush.Solid);
            if (room == null || solid == null)
            {
                return null;
            }

            Rect local = room.CameraBounds;
            var localRect = new RectInt(Mathf.RoundToInt(local.xMin), Mathf.RoundToInt(local.yMin),
                Mathf.RoundToInt(local.width), Mathf.RoundToInt(local.height));
            var shape = new RoomShape(room.RoomId, new RectInt(localRect.position + room.WorldPosition, localRect.size));
            Fill(shape.Solid, solid, localRect);
            Fill(shape.OneWay, AliceLevelGeometry.FindLayer(scene, LevelBrush.OneWay), localRect);
            Fill(shape.Hazard, AliceLevelGeometry.FindLayer(scene, LevelBrush.Hazard), localRect);
            foreach (RoomDoorway doorway in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RoomDoorway>(true)))
            {
                shape.Doorways.Add((doorway.DoorwayId, (Vector2)doorway.transform.position + room.WorldPosition, doorway.TargetRoomId));
            }

            return shape;
        }

        private static void Fill(bool[,] grid, Tilemap layer, RectInt localRect)
        {
            if (layer == null)
            {
                return;
            }

            for (int x = 0; x < localRect.width; x++)
            {
                for (int y = 0; y < localRect.height; y++)
                {
                    grid[x, y] = layer.HasTile(new Vector3Int(localRect.xMin + x, localRect.yMin + y, 0));
                }
            }
        }

        /// <summary>Overlaps, passages that end in a neighbour's wall, passages to nowhere and misplaced doorway targets.</summary>
        public static List<string> Check(IList<RoomShape> rooms)
        {
            var problems = new List<string>();
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (rooms[i].Rect.Overlaps(rooms[j].Rect))
                    {
                        problems.Add($"{rooms[i].Id} и {rooms[j].Id} накладываются друг на друга.");
                    }
                }
            }

            foreach (RoomShape room in rooms)
            {
                // Cells grouped by what lies beyond them, so a passage 4 tiles high is one message, not four.
                var blocked = new Dictionary<string, List<Vector2Int>>();
                var nowhere = new Dictionary<string, List<Vector2Int>>();
                foreach (Vector2Int cell in EdgeCells(room.Rect))
                {
                    if (room.IsSolidAt(cell))
                    {
                        continue;
                    }

                    foreach (Vector2Int side in Sides)
                    {
                        Vector2Int beyond = cell + side;
                        if (room.Contains(beyond))
                        {
                            continue;
                        }

                        RoomShape neighbour = rooms.FirstOrDefault(other => other != room && other.Contains(beyond));
                        string key = $"{SideName(side)}";
                        if (neighbour == null)
                        {
                            Add(nowhere, key, cell);
                        }
                        else if (neighbour.IsSolidAt(beyond))
                        {
                            Add(blocked, key + " → " + neighbour.Id, cell);
                        }
                    }
                }

                foreach (KeyValuePair<string, List<Vector2Int>> pair in blocked)
                {
                    problems.Add($"{room.Id}: проход ({pair.Key.Split(' ')[0]}, {Cells(pair.Value)}) упирается в стену {pair.Key.Split(' ').Last()}.");
                }

                foreach (KeyValuePair<string, List<Vector2Int>> pair in nowhere)
                {
                    problems.Add($"{room.Id}: проход ({pair.Key}, {Cells(pair.Value)}) ведёт туда, где нет комнаты.");
                }

                foreach ((string id, Vector2 position, string target) in room.Doorways)
                {
                    RoomShape targetRoom = rooms.FirstOrDefault(other => other.Id == target);
                    if (targetRoom != null && Distance(targetRoom.Rect, position) > 2f)
                    {
                        problems.Add($"{room.Id}: проём {id} ведёт в {target}, но эта комната не примыкает к нему на карте.");
                    }
                }
            }

            return problems;
        }

        private static void Add(Dictionary<string, List<Vector2Int>> groups, string key, Vector2Int cell)
        {
            if (!groups.TryGetValue(key, out List<Vector2Int> cells))
            {
                cells = new List<Vector2Int>();
                groups[key] = cells;
            }

            cells.Add(cell);
        }

        private static IEnumerable<Vector2Int> EdgeCells(RectInt rect)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    if (x == rect.xMin || y == rect.yMin || x == rect.xMax - 1 || y == rect.yMax - 1)
                    {
                        yield return new Vector2Int(x, y);
                    }
                }
            }
        }

        private static string SideName(Vector2Int side)
        {
            return side == Vector2Int.left ? "запад" : side == Vector2Int.right ? "восток" : side == Vector2Int.down ? "низ" : "верх";
        }

        // World cells as ranges along the edge: "x 4–7, y 23" or "x 80, y 2–5".
        private static string Cells(List<Vector2Int> cells)
        {
            int xMin = cells.Min(c => c.x), xMax = cells.Max(c => c.x);
            int yMin = cells.Min(c => c.y), yMax = cells.Max(c => c.y);
            string Range(int a, int b) => a == b ? a.ToString() : $"{a}–{b}";
            return $"мир x {Range(xMin, xMax)}, y {Range(yMin, yMax)}";
        }

        private static float Distance(RectInt rect, Vector2 point)
        {
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>The whole map, <paramref name="pixelsPerTile"/> per tile, and where it starts in world tiles.</summary>
        public static Texture2D Render(IList<RoomShape> rooms, int pixelsPerTile, out RectInt world)
        {
            world = rooms.Count == 0 ? new RectInt(0, 0, 1, 1) : rooms[0].Rect;
            foreach (RoomShape room in rooms)
            {
                world = new RectInt(Mathf.Min(world.xMin, room.Rect.xMin), Mathf.Min(world.yMin, room.Rect.yMin),
                    0, 0) { xMax = Mathf.Max(world.xMax, room.Rect.xMax), yMax = Mathf.Max(world.yMax, room.Rect.yMax) };
            }

            world = new RectInt(world.xMin - 2, world.yMin - 2, world.width + 4, world.height + 4);
            var texture = new Texture2D(world.width * pixelsPerTile, world.height * pixelsPerTile, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            var pixels = Enumerable.Repeat(new Color32(12, 12, 16, 255), texture.width * texture.height).ToArray();
            var rock = new Color32(70, 72, 80, 255);
            var air = new Color32(28, 30, 40, 255);
            var catwalk = new Color32(180, 150, 100, 255);
            var hazard = new Color32(170, 40, 40, 255);
            foreach (RoomShape room in rooms)
            {
                for (int x = 0; x < room.Rect.width; x++)
                {
                    for (int y = 0; y < room.Rect.height; y++)
                    {
                        Color32 color = room.Solid[x, y] ? rock : room.Hazard[x, y] ? hazard : room.OneWay[x, y] ? catwalk : air;
                        Paint(pixels, texture.width, pixelsPerTile, room.Rect.xMin + x - world.xMin, room.Rect.yMin + y - world.yMin, color);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static void Paint(Color32[] pixels, int width, int scale, int tileX, int tileY, Color32 color)
        {
            for (int px = 0; px < scale; px++)
            {
                for (int py = 0; py < scale; py++)
                {
                    pixels[(tileY * scale + py) * width + tileX * scale + px] = color;
                }
            }
        }
    }
}
