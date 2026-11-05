using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>The stable ID of the room this scene holds (GDD section 13); one per room.</summary>
    public sealed class Room : MonoBehaviour
    {
        [SerializeField] private string _roomId = "P_ROOM";

        public string RoomId
        {
            get => _roomId;
            set => _roomId = value;
        }

        private void OnEnable()
        {
            RoomPresence.Enter(_roomId);
        }

        private void OnDisable()
        {
            RoomPresence.Leave(_roomId);
        }
    }
}
