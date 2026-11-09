using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alice.World
{
    /// <summary>A mark the player left on the map: which one and where, in world tiles.</summary>
    public readonly struct MapMark
    {
        public readonly MapIconKind Kind;
        public readonly Vector2 Position;

        public MapMark(MapIconKind kind, Vector2 position)
        {
            Kind = kind;
            Position = position;
        }
    }

    /// <summary>
    /// What the player knows of the map (GDD 0.34): the cells Alice has explored and the marks left. The map opens
    /// as she goes: the open cells around her that she can reach without passing through rock, never a whole room
    /// at once. A zone can still be revealed whole (a map bought or found), but not its secret rooms. Plain data,
    /// so the rules are tested without a scene.
    /// </summary>
    public sealed class MapState
    {
        public const int MaxMarks = 64;

        // Explored cells per room as a bit set, index y * width + x of the room's map rectangle.
        private readonly Dictionary<string, byte[]> _explored = new Dictionary<string, byte[]>();
        private readonly HashSet<string> _whole = new HashSet<string>();
        private readonly List<MapMark> _marks = new List<MapMark>();

        /// <summary>Changes whenever explored cells or marks change, so the map redraws only then.</summary>
        public int Version { get; private set; }

        /// <summary>Rooms revealed whole, by a map rather than by walking.</summary>
        public IEnumerable<string> WholeRooms => _whole;

        public IReadOnlyList<MapMark> Marks => _marks;

        /// <summary>Anything of the room is on the map.</summary>
        public bool IsRevealed(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) && (_whole.Contains(roomId) || _explored.ContainsKey(roomId));
        }

        public bool IsExplored(MapAtlas.RoomEntry room, int x, int y)
        {
            if (room == null || x < 0 || y < 0 || x >= room.Rect.width || y >= room.Rect.height)
            {
                return false;
            }

            if (_whole.Contains(room.Id))
            {
                return true;
            }

            return _explored.TryGetValue(room.Id, out byte[] bits) && Get(bits, y * room.Rect.width + x);
        }

        /// <summary>
        /// Alice stands in <paramref name="cell"/> of the room: every open cell within <paramref name="radius"/>
        /// tiles that she could reach from there without crossing rock goes on the map. Returns how many were new.
        /// </summary>
        public int Explore(MapAtlas.RoomEntry room, Vector2Int cell, int radius)
        {
            if (room == null || room.Rect.width <= 0 || room.Rect.height <= 0 || _whole.Contains(room.Id))
            {
                return 0;
            }

            int width = room.Rect.width;
            int height = room.Rect.height;
            // Her feet may touch the floor's top row; start from the first open cell above.
            Vector2Int start = cell;
            while (start.y < cell.y + 3 && IsRock(room, start))
            {
                start.y++;
            }

            if (start.x < 0 || start.x >= width || start.y < 0 || start.y >= height || IsRock(room, start))
            {
                return 0;
            }

            if (!_explored.TryGetValue(room.Id, out byte[] bits) || bits.Length < (width * height + 7) / 8)
            {
                byte[] grown = new byte[(width * height + 7) / 8];
                if (bits != null)
                {
                    Array.Copy(bits, grown, Mathf.Min(bits.Length, grown.Length));
                }

                bits = grown;
                _explored[room.Id] = bits;
            }

            int added = 0;
            int radiusSquared = radius * radius;
            var seen = new HashSet<int> { start.y * width + start.x };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int at = queue.Dequeue();
                int index = at.y * width + at.x;
                if (!Get(bits, index))
                {
                    Set(bits, index);
                    added++;
                }

                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = at + step;
                    Vector2Int offset = next - cell;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height
                        || offset.sqrMagnitude > radiusSquared || IsRock(room, next) || !seen.Add(next.y * width + next.x))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            if (added > 0)
            {
                Version++;
            }

            return added;
        }

        /// <summary>Reveals every room of a zone that is not secret, whole.</summary>
        public int RevealZone(MapAtlas atlas, string zoneId)
        {
            int added = 0;
            foreach (MapAtlas.RoomEntry room in atlas.Rooms)
            {
                if (room.ZoneId == zoneId && !room.Secret && _whole.Add(room.Id))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                Version++;
            }

            return added;
        }

        public bool AddMark(MapIconKind kind, Vector2 position)
        {
            if (_marks.Count >= MaxMarks)
            {
                return false;
            }

            _marks.Add(new MapMark(kind, position));
            Version++;
            return true;
        }

        /// <summary>Removes the mark nearest to the point if it lies within <paramref name="radius"/> tiles.</summary>
        public bool RemoveMarkNear(Vector2 position, float radius)
        {
            int nearest = -1;
            float best = radius;
            for (int index = 0; index < _marks.Count; index++)
            {
                float distance = Vector2.Distance(_marks[index].Position, position);
                if (distance <= best)
                {
                    best = distance;
                    nearest = index;
                }
            }

            if (nearest < 0)
            {
                return false;
            }

            _marks.RemoveAt(nearest);
            Version++;
            return true;
        }

        /// <summary>The explored cells of every room, for the save: room ID and the bit set in Base64.</summary>
        public IEnumerable<KeyValuePair<string, string>> ExploredForSave()
        {
            foreach (KeyValuePair<string, byte[]> pair in _explored)
            {
                yield return new KeyValuePair<string, string>(pair.Key, Convert.ToBase64String(pair.Value));
            }
        }

        /// <summary>A save is loaded or a new game starts: the map becomes exactly this.</summary>
        public void Replace(IEnumerable<KeyValuePair<string, string>> explored, IEnumerable<string> wholeRooms,
            IEnumerable<MapMark> marks)
        {
            _explored.Clear();
            _whole.Clear();
            _marks.Clear();
            if (explored != null)
            {
                foreach (KeyValuePair<string, string> pair in explored)
                {
                    if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
                    {
                        continue;
                    }

                    try
                    {
                        _explored[pair.Key] = Convert.FromBase64String(pair.Value);
                    }
                    catch (FormatException)
                    {
                        // A damaged entry loses that room's map, not the save.
                    }
                }
            }

            if (wholeRooms != null)
            {
                foreach (string roomId in wholeRooms)
                {
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        _whole.Add(roomId);
                    }
                }
            }

            if (marks != null)
            {
                foreach (MapMark mark in marks)
                {
                    if (_marks.Count < MaxMarks)
                    {
                        _marks.Add(mark);
                    }
                }
            }

            Version++;
        }

        private static readonly Vector2Int[] Steps = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

        private static bool IsRock(MapAtlas.RoomEntry room, Vector2Int cell)
        {
            return room.CellAt(cell.x + room.Rect.xMin, cell.y + room.Rect.yMin) == MapAtlas.Rock;
        }

        private static bool Get(byte[] bits, int index)
        {
            return index >= 0 && index / 8 < bits.Length && (bits[index / 8] & (1 << (index % 8))) != 0;
        }

        private static void Set(byte[] bits, int index)
        {
            bits[index / 8] |= (byte)(1 << (index % 8));
        }
    }
}
