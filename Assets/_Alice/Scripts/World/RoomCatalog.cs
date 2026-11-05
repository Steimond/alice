using UnityEngine;

namespace Alice.World
{
    /// <summary>Which scene holds which room, by the room's stable ID; the first entry is the new-game room.</summary>
    [CreateAssetMenu(menuName = "Alice/Room Catalog", fileName = "Rooms")]
    public sealed class RoomCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            [SerializeField] private string _roomId;
            [Tooltip("Scene name in the build list.")]
            [SerializeField] private string _scene;
            [Tooltip("Where a new game puts Alice in this room: a spot ID (doorway or chair), or empty for the room's start point.")]
            [SerializeField] private string _startSpot;

            public Entry(string roomId, string scene, string startSpot)
            {
                _roomId = roomId;
                _scene = scene;
                _startSpot = startSpot;
            }

            public string RoomId => _roomId;
            public string Scene => _scene;
            public string StartSpot => _startSpot;
        }

        [SerializeField] private Entry[] _rooms = new Entry[0];

        public Entry[] Rooms
        {
            get => _rooms;
            set => _rooms = value;
        }

        public bool TryGetScene(string roomId, out string scene)
        {
            foreach (Entry entry in _rooms)
            {
                if (entry.RoomId == roomId)
                {
                    scene = entry.Scene;
                    return true;
                }
            }

            scene = null;
            return false;
        }
    }
}
