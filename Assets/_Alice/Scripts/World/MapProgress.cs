using System.Collections.Generic;
using System.Linq;
using Alice.Core;
using Alice.Save;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// The map Alice carries (GDD 0.34): as she walks, the cells around her that she can reach go on the map; it also
    /// keeps the player's marks, all in the save. Lives on Alice next to the other save participants; the map screen
    /// reads it.
    /// </summary>
    public sealed class MapProgress : MonoBehaviour, ISaveParticipant
    {
        [SerializeField] private MapAtlas _atlas;

        [Tooltip("How far around Alice the map opens, in tiles, through open space only.")]
        [SerializeField, Min(1)] private int _exploreRadius = 9;

        private string _lastRoom;
        private Vector2Int _lastCell;

        public MapState State { get; } = new MapState();

        public MapAtlas Atlas
        {
            get => _atlas;
            set => _atlas = value;
        }

        private void OnEnable()
        {
            _lastRoom = null;
        }

        private void LateUpdate()
        {
            ExploreHere();
        }

        /// <summary>Opens the map around where Alice stands; cheap when she has not left her cell.</summary>
        public void ExploreHere()
        {
            if (_atlas == null)
            {
                return;
            }

            string roomId = RoomPresence.CurrentId;
            MapAtlas.RoomEntry room = _atlas.FindRoom(roomId);
            if (room == null)
            {
                return;
            }

            Vector2Int cell = MapAtlas.CellOf(room, (Vector2)transform.position + room.Origin);
            if (roomId == _lastRoom && cell == _lastCell)
            {
                return;
            }

            _lastRoom = roomId;
            _lastCell = cell;
            State.Explore(room, cell, _exploreRadius);
        }

        public void Capture(SaveData data)
        {
            data.MapCells = State.ExploredForSave()
                .Select(pair => new MapRoomCellsData { Room = pair.Key, Bits = pair.Value })
                .ToList();
            data.MapWholeRooms = State.WholeRooms.ToList();
            data.MapMarks = State.Marks
                .Select(mark => new MapMarkData { Kind = (int)mark.Kind, X = mark.Position.x, Y = mark.Position.y })
                .ToList();
        }

        public void Restore(SaveData data)
        {
            State.Replace(
                (data.MapCells ?? new List<MapRoomCellsData>())
                .Where(cells => cells != null)
                .Select(cells => new KeyValuePair<string, string>(cells.Room, cells.Bits)),
                data.MapWholeRooms,
                (data.MapMarks ?? new List<MapMarkData>())
                .Select(mark => new MapMark((MapIconKind)mark.Kind, new Vector2(mark.X, mark.Y))));
            // The place she loads into is known at once.
            _lastRoom = null;
            ExploreHere();
        }
    }
}
