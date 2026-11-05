using System.Collections;
using Alice.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.World
{
    /// <summary>
    /// Swaps room scenes (GDD section 13: one room per scene, Alice and the systems live in Bootstrap). On a
    /// travel request it darkens the screen, unloads the room she leaves, loads the one she enters, puts her at
    /// the named spot, moves the camera there at once and lifts the dark. A new game starts in the catalogue's
    /// first room; a room played on its own in the Editor is kept as the start.
    /// </summary>
    public sealed class RoomLoader : MonoBehaviour
    {
        [SerializeField] private RoomCatalog _catalog;
        [SerializeField, Min(0.01f)] private float _fadeTime = 0.18f;

        private string _currentScene;
        private bool _started;
        private float _darkness;

        public RoomCatalog Catalog
        {
            get => _catalog;
            set => _catalog = value;
        }

        private void OnEnable()
        {
            RoomTravel.Requested += OnRequested;
        }

        private void OnDisable()
        {
            RoomTravel.Requested -= OnRequested;
            ScreenDarkness.Set(this, 0f);
            RoomTravel.IsTravelling = false;
        }

        private void Start()
        {
            StartCoroutine(Begin());
        }

        /// <summary>One frame late, so that a loaded save has had the chance to send her to her chair.</summary>
        private IEnumerator Begin()
        {
            yield return null;
            if (_started)
            {
                yield break;
            }

            Room playedRoom = FindAnyObjectByType<Room>();
            if (playedRoom != null)
            {
                _started = true;
                _currentScene = playedRoom.gameObject.scene.name;
                SceneManager.SetActiveScene(playedRoom.gameObject.scene);
                PutAlice(playedRoom.StartPosition, playedRoom.StartFacing);
                playedRoom.ApplyCamera();
                SnapCamera();
                yield break;
            }

            if (_catalog != null && _catalog.Rooms.Length > 0)
            {
                RoomCatalog.Entry first = _catalog.Rooms[0];
                _darkness = 1f;
                ScreenDarkness.Set(this, 1f);
                OnRequested(first.RoomId, first.StartSpot);
            }
        }

        private void OnRequested(string roomId, string spotId)
        {
            if (RoomTravel.IsTravelling)
            {
                return;
            }

            _started = true;
            StartCoroutine(Travel(roomId, spotId));
        }

        private IEnumerator Travel(string roomId, string spotId)
        {
            RoomTravel.IsTravelling = true;
            yield return Fade(1f);

            if (_catalog == null || !_catalog.TryGetScene(roomId, out string sceneName))
            {
                Debug.LogError($"Room {roomId} is not in the room catalogue.", this);
                yield return Fade(0f);
                RoomTravel.IsTravelling = false;
                yield break;
            }

            // Every room scene goes; only the scene holding Alice and the systems stays.
            Scene home = gameObject.scene;
            for (int index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene != home && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            Scene loaded = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(loaded);
            _currentScene = sceneName;
            // Let the new room's objects register their spots.
            yield return null;

            Room room = FindAnyObjectByType<Room>();
            IArrivalSpot spot = RoomTravel.FindSpot(spotId);
            if (spot != null)
            {
                PutAlice(spot.ArrivalPosition, spot.ArrivalFacing);
            }
            else if (room != null)
            {
                PutAlice(room.StartPosition, room.StartFacing);
            }

            if (room != null)
            {
                room.ApplyCamera();
            }

            SnapCamera();
            yield return Fade(0f);
            RoomTravel.IsTravelling = false;
        }

        private IEnumerator Fade(float target)
        {
            float step = 1f / _fadeTime;
            while (!Mathf.Approximately(_darkness, target))
            {
                _darkness = Mathf.MoveTowards(_darkness, target, step * Time.unscaledDeltaTime);
                ScreenDarkness.Set(this, _darkness);
                yield return null;
            }
        }

        private static void PutAlice(Vector2 position, int facing)
        {
            Transform alice = PlayerPresence.Current;
            if (alice != null && alice.TryGetComponent(out IRoomArrival arrival))
            {
                arrival.ArriveAt(position, facing);
            }
        }

        private static void SnapCamera()
        {
            Camera main = Camera.main;
            if (main != null && main.TryGetComponent(out CameraFollow2D follow))
            {
                follow.Snap();
            }
        }
    }
}
