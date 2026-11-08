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
    /// What the player knows of the map (GDD section 10): the rooms revealed and the marks left. A room is revealed
    /// by entering it; a whole zone can be revealed at once (a map bought or found), but its secret rooms only by
    /// being inside them. Plain data, so the rules are tested without a scene.
    /// </summary>
    public sealed class MapState
    {
        public const int MaxMarks = 64;

        private readonly HashSet<string> _revealed = new HashSet<string>();
        private readonly List<MapMark> _marks = new List<MapMark>();

        /// <summary>Raised when the revealed rooms or the marks change, so the map redraws only then.</summary>
        public int Version { get; private set; }

        public IEnumerable<string> Revealed => _revealed;

        public IReadOnlyList<MapMark> Marks => _marks;

        public bool IsRevealed(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) && _revealed.Contains(roomId);
        }

        /// <summary>Alice is inside the room: it is on the map from now on, secret or not.</summary>
        public bool Visit(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || !_revealed.Add(roomId))
            {
                return false;
            }

            Version++;
            return true;
        }

        /// <summary>Reveals every room of a zone that is not secret.</summary>
        public int RevealZone(MapAtlas atlas, string zoneId)
        {
            int added = 0;
            foreach (MapAtlas.RoomEntry room in atlas.Rooms)
            {
                if (room.ZoneId == zoneId && !room.Secret && _revealed.Add(room.Id))
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

        /// <summary>A save is loaded or a new game starts: the map becomes exactly this.</summary>
        public void Replace(IEnumerable<string> revealed, IEnumerable<MapMark> marks)
        {
            _revealed.Clear();
            _marks.Clear();
            if (revealed != null)
            {
                foreach (string roomId in revealed)
                {
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        _revealed.Add(roomId);
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
    }
}
