using System.Linq;
using Alice.Core;
using Alice.Save;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// The map Alice carries: reveals every room she enters and keeps the player's marks, in the save. Lives on
    /// Alice next to the other save participants; the map screen reads it.
    /// </summary>
    public sealed class MapProgress : MonoBehaviour, ISaveParticipant
    {
        [SerializeField] private MapAtlas _atlas;

        public MapState State { get; } = new MapState();

        public MapAtlas Atlas
        {
            get => _atlas;
            set => _atlas = value;
        }

        private void OnEnable()
        {
            RoomPresence.Entered += OnEntered;
            OnEntered(RoomPresence.CurrentId);
        }

        private void OnDisable()
        {
            RoomPresence.Entered -= OnEntered;
        }

        private void OnEntered(string roomId)
        {
            State.Visit(roomId);
        }

        public void Capture(SaveData data)
        {
            data.MapRooms = State.Revealed.ToList();
            data.MapMarks = State.Marks
                .Select(mark => new MapMarkData { Kind = (int)mark.Kind, X = mark.Position.x, Y = mark.Position.y })
                .ToList();
        }

        public void Restore(SaveData data)
        {
            State.Replace(data.MapRooms,
                (data.MapMarks ?? new System.Collections.Generic.List<MapMarkData>())
                .Select(mark => new MapMark((MapIconKind)mark.Kind, new Vector2(mark.X, mark.Y))));
            // The room she loads into is known even if the save predates the map.
            State.Visit(RoomPresence.CurrentId);
        }
    }
}
