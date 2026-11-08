using Alice.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.World
{
    /// <summary>
    /// The stable ID of the room this scene holds (GDD section 13); one per room. It also gives the camera the
    /// room's bounds. A room scene played on its own in the Editor brings in the Bootstrap scene with Alice,
    /// the camera and the systems, and starts her at the room's start point.
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";

        [SerializeField] private string _roomId = "P_ROOM";
        [Tooltip("Rooms loaded by the room loader set the camera's bounds; the older prototype rooms keep their own.")]
        [SerializeField] private bool _controlsCamera;
        [Tooltip("What the camera may show, in tiles.")]
        [SerializeField] private Rect _cameraBounds = new Rect(-20f, -6f, 40f, 20f);
        [Tooltip("Where Alice starts when this room is played on its own.")]
        [SerializeField] private Vector2 _startPosition = Vector2.zero;
        [SerializeField] private int _startFacing = 1;
        [Tooltip("Where the room's local origin lies on its biome's world map, in tiles. Neighbouring rooms meet " +
            "there wall to wall, so a passage is the same opening in both (LevelDesignGuide, section 4); the in-game " +
            "map will draw rooms from it too.")]
        [SerializeField] private Vector2Int _worldPosition = Vector2Int.zero;
        [Tooltip("Stable ID of the zone (biome) this room belongs to on the map, such as B01.")]
        [SerializeField] private string _zoneId = string.Empty;
        [Tooltip("A secret room stays off the map until Alice has been inside, whatever else reveals the map.")]
        [SerializeField] private bool _secret;

        public string ZoneId
        {
            get => _zoneId;
            set => _zoneId = value;
        }

        public bool Secret
        {
            get => _secret;
            set => _secret = value;
        }

        public Vector2Int WorldPosition
        {
            get => _worldPosition;
            set => _worldPosition = value;
        }

        public string RoomId
        {
            get => _roomId;
            set => _roomId = value;
        }

        public Rect CameraBounds
        {
            get => _cameraBounds;
            set => _cameraBounds = value;
        }

        public Vector2 StartPosition
        {
            get => _startPosition;
            set => _startPosition = value;
        }

        public int StartFacing => _startFacing >= 0 ? 1 : -1;

        private void OnEnable()
        {
            RoomPresence.Enter(_roomId);
            ApplyCamera();
        }

        private void OnDisable()
        {
            RoomPresence.Leave(_roomId);
        }

        private void Start()
        {
            // Played on its own: Alice is in none of the loaded scenes, so this room is the start and Bootstrap
            // joins it. Start, not Awake, so that an Alice placed in the same scene has registered by now.
            if (PlayerPresence.Current == null && !SceneManager.GetSceneByName(BootstrapScene).isLoaded
                && Application.CanStreamedLevelBeLoaded(BootstrapScene))
            {
                RoomTravel.StartedInRoom = true;
                SceneManager.LoadScene(BootstrapScene, LoadSceneMode.Additive);
            }

            ApplyCamera();
        }

        public bool ControlsCamera
        {
            get => _controlsCamera;
            set => _controlsCamera = value;
        }

        public void ApplyCamera()
        {
            if (!_controlsCamera)
            {
                return;
            }

            Camera main = Camera.main;
            if (main != null && main.TryGetComponent(out CameraFollow2D follow))
            {
                follow.Bounds = _cameraBounds;
            }
        }
    }
}
