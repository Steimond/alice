using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// An opening at the edge of a room. Walking into its trigger takes Alice to the doorway it names in the
    /// next room; arriving through it puts her down a little inside, facing into the room, so she does not
    /// walk straight back out.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RoomDoorway : MonoBehaviour, IArrivalSpot
    {
        [SerializeField] private string _doorwayId = "D_ROOM_E";
        [SerializeField] private string _targetRoomId = string.Empty;
        [SerializeField] private string _targetDoorwayId = string.Empty;
        [Tooltip("Arrival point relative to the doorway, in tiles: inside the room, feet on the floor.")]
        [SerializeField] private Vector2 _arrivalOffset = new Vector2(-2f, 0f);
        [Tooltip("+1: arriving Alice faces right (into a room whose doorway is on its left wall).")]
        [SerializeField] private int _arrivalFacing = -1;

        private float _armedAt;

        public string DoorwayId
        {
            get => _doorwayId;
            set => _doorwayId = value;
        }

        public void Configure(string targetRoomId, string targetDoorwayId, Vector2 arrivalOffset, int arrivalFacing)
        {
            _targetRoomId = targetRoomId;
            _targetDoorwayId = targetDoorwayId;
            _arrivalOffset = arrivalOffset;
            _arrivalFacing = arrivalFacing >= 0 ? 1 : -1;
        }

        public string SpotId => _doorwayId;
        public Vector2 ArrivalPosition => (Vector2)transform.position + _arrivalOffset;
        public int ArrivalFacing => _arrivalFacing >= 0 ? 1 : -1;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnEnable()
        {
            RoomTravel.Register(this);
            // A room that has just loaded ignores Alice for a moment, in case she lands inside a trigger.
            _armedAt = Time.time + 0.3f;
        }

        private void OnDisable()
        {
            RoomTravel.Unregister(this);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < _armedAt || RoomTravel.IsTravelling || string.IsNullOrEmpty(_targetRoomId))
            {
                return;
            }

            Transform alice = PlayerPresence.Current;
            if (alice == null || other.attachedRigidbody == null || other.attachedRigidbody.transform != alice)
            {
                return;
            }

            RoomTravel.Request(_targetRoomId, _targetDoorwayId);
        }
    }
}
