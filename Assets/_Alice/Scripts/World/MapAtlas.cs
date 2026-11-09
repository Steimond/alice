using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Alice.World
{
    /// <summary>
    /// Everything the in-game map draws, baked from the room scenes (Alice → Level → Bake World Map): the zones,
    /// each room's place and cells on the world map, and the fixed icons. A new zone is new rooms with their zone ID
    /// and one more zone entry; nothing in the map code changes. Rooms are addressed by their stable IDs.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Map Atlas", fileName = "MapAtlas")]
    public sealed class MapAtlas : ScriptableObject
    {
        /// <summary>Cell codes in <see cref="RoomEntry.Cells"/>.</summary>
        public const char Rock = '#';
        public const char Air = '.';
        public const char Catwalk = '-';
        public const char Hazard = '~';

        /// <summary>
        /// Icons of things standing on the floor are baked this far above it, in tiles, so the cell they name is the
        /// open one above the floor; the map screen stands the drawn icon on the floor line from there.
        /// </summary>
        public const float FloorIconLift = 0.25f;

        [Serializable]
        public sealed class ZoneEntry
        {
            public string Id;
            public LocalizedString Name = new LocalizedString();
            [Tooltip("Tint of the zone's rooms on the map.")]
            public Color Tint = new Color(0.22f, 0.3f, 0.36f);
        }

        [Serializable]
        public sealed class RoomEntry
        {
            public string Id;
            public string ZoneId;
            public bool Secret;
            [Tooltip("World position of the room's local origin, in tiles.")]
            public Vector2Int Origin;
            [Tooltip("The room's rectangle on the world map, in tiles.")]
            public RectInt Rect;
            [Tooltip("One character per cell, row by row from the bottom: # rock, . air, - catwalk, ~ hazard.")]
            [TextArea(1, 3)]
            public string Cells = string.Empty;

            public char CellAt(int worldX, int worldY)
            {
                int x = worldX - Rect.xMin;
                int y = worldY - Rect.yMin;
                if (x < 0 || y < 0 || x >= Rect.width || y >= Rect.height || Cells.Length < Rect.width * Rect.height)
                {
                    return Rock;
                }

                return Cells[y * Rect.width + x];
            }
        }

        [Serializable]
        public sealed class IconEntry
        {
            public MapIconKind Kind;
            public string RoomId;
            [Tooltip("World position in tiles.")]
            public Vector2 Position;
            [Tooltip("For a bundle: the ability or trinket inside; the icon goes once Alice has it.")]
            public string ItemId;
        }

        [SerializeField] private ZoneEntry[] _zones = new ZoneEntry[0];
        [SerializeField] private RoomEntry[] _rooms = new RoomEntry[0];
        [SerializeField] private IconEntry[] _icons = new IconEntry[0];

        public ZoneEntry[] Zones
        {
            get => _zones;
            set => _zones = value;
        }

        public RoomEntry[] Rooms
        {
            get => _rooms;
            set => _rooms = value;
        }

        public IconEntry[] Icons
        {
            get => _icons;
            set => _icons = value;
        }

        public RoomEntry FindRoom(string roomId)
        {
            return string.IsNullOrEmpty(roomId) ? null : Array.Find(_rooms, room => room != null && room.Id == roomId);
        }

        public ZoneEntry FindZone(string zoneId)
        {
            return string.IsNullOrEmpty(zoneId) ? null : Array.Find(_zones, zone => zone != null && zone.Id == zoneId);
        }

        /// <summary>The rectangle around every room, in world tiles.</summary>
        public RectInt Bounds
        {
            get
            {
                if (_rooms.Length == 0)
                {
                    return new RectInt(0, 0, 1, 1);
                }

                int xMin = int.MaxValue, yMin = int.MaxValue, xMax = int.MinValue, yMax = int.MinValue;
                foreach (RoomEntry room in _rooms)
                {
                    xMin = Mathf.Min(xMin, room.Rect.xMin);
                    yMin = Mathf.Min(yMin, room.Rect.yMin);
                    xMax = Mathf.Max(xMax, room.Rect.xMax);
                    yMax = Mathf.Max(yMax, room.Rect.yMax);
                }

                return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        /// <summary>The cell of a room that holds a point on the world map, in the room's map rectangle.</summary>
        public static Vector2Int CellOf(RoomEntry room, Vector2 world)
        {
            return new Vector2Int(Mathf.FloorToInt(world.x) - room.Rect.xMin, Mathf.FloorToInt(world.y) - room.Rect.yMin);
        }

        /// <summary>A point in a room's own coordinates as a point on the world map.</summary>
        public bool TryToWorld(string roomId, Vector2 local, out Vector2 world)
        {
            RoomEntry room = FindRoom(roomId);
            world = room == null ? local : local + room.Origin;
            return room != null;
        }
    }
}
