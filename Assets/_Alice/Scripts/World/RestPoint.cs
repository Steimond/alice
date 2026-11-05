using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// The armchair Alice rests in (GDD section 4, owner's choice of 4 Nov 2026; each biome later dresses it
    /// with something of its own). Interact within the trigger sits her down: clarity and salts come back,
    /// ordinary enemies return to their posts, the game saves and she may change her technique.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RestPoint : MonoBehaviour, IRestSpot, IArrivalSpot
    {
        [SerializeField] private string _restId = "R_P01";
        [Tooltip("+1: Alice sits facing right.")]
        [SerializeField] private int _facing = 1;
        [Tooltip("Where her feet stand when she sits, relative to the chair's pivot, in tiles.")]
        [SerializeField] private Vector2 _seatOffset = Vector2.zero;

        public string RestId
        {
            get => _restId;
            set => _restId = value;
        }

        public Vector2 SeatPosition => (Vector2)transform.position + _seatOffset;

        public int Facing
        {
            get => _facing >= 0 ? 1 : -1;
            set => _facing = value >= 0 ? 1 : -1;
        }

        private void Reset()
        {
            BoxCollider2D zone = GetComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(3f, 3f);
            zone.offset = new Vector2(0f, 1.5f);
        }

        // Waking after a defeat or loading a game puts her down at the chair.
        string IArrivalSpot.SpotId => _restId;
        Vector2 IArrivalSpot.ArrivalPosition => SeatPosition;
        int IArrivalSpot.ArrivalFacing => Facing;

        private void OnEnable()
        {
            RestSpots.Register(this);
            RoomTravel.Register(this);
        }

        private void OnDisable()
        {
            RestSpots.Unregister(this);
            RoomTravel.Unregister(this);
        }
    }
}
