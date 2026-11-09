using System.Collections.Generic;
using System.IO;
using Alice.Core;
using UnityEngine;

namespace Alice.Save
{
    /// <summary>
    /// The save of the running game. On start it loads the file, restores the world's permanent changes, hands
    /// each part to the systems on Alice and sends her to the room of her last chair; at every autosave point it
    /// gathers them again and writes. It plays in the slot the main menu chose (<see cref="SaveSlots"/>); the file
    /// lives in persistentDataPath, outside the repository. It also keeps the time played and where Alice was, for
    /// the slot screen.
    /// </summary>
    public sealed class SaveGame : MonoBehaviour
    {
        [Tooltip("Prototype rooms can start clean every time while this is off.")]
        [SerializeField] private bool _loadOnStart = true;

        private SaveStore _store;
        private float _playTimeBefore;
        private float _sessionStart;

        public static string PathFor(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public SaveStore Store => _store ??= SaveSlots.Store(SaveSlots.Current);

        /// <summary>The save read at start, if any; the room loader asks where to begin.</summary>
        public static SaveData Loaded { get; private set; }

        private void OnEnable()
        {
            Autosave.Requested += Write;
        }

        private void OnDisable()
        {
            Autosave.Requested -= Write;
        }

        private void Start()
        {
            Loaded = null;
            _playTimeBefore = 0f;
            _sessionStart = Time.unscaledTime;
            if (!_loadOnStart || !Store.TryLoad(out SaveData data))
            {
                WorldFlags.Replace(null);
                return;
            }

            Loaded = data;
            _playTimeBefore = Mathf.Max(0f, data.PlayTime);
            WorldFlags.Replace(data.Flags);
            foreach (ISaveParticipant participant in Participants())
            {
                participant.Restore(data);
            }

            // A room played on its own in the Editor keeps Alice there; otherwise she wakes in her chair's room.
            if (!RoomTravel.StartedInRoom && !string.IsNullOrEmpty(data.RestRoomId)
                && data.RestRoomId != RoomPresence.CurrentId)
            {
                RoomTravel.Request(data.RestRoomId, data.RestId);
            }
        }

        /// <summary>Whatever changed since the last autosave point is kept when the game closes.</summary>
        private void OnApplicationQuit()
        {
            if (PlayerPresence.Current != null)
            {
                Write();
            }
        }

        private void Write()
        {
            SaveData data = new SaveData();
            foreach (ISaveParticipant participant in Participants())
            {
                participant.Capture(data);
            }

            data.Flags = new List<string>(WorldFlags.All);
            data.RoomId = RoomPresence.CurrentId ?? string.Empty;
            data.PlayTime = _playTimeBefore + (Time.unscaledTime - _sessionStart);
            data.SavedAtUtc = System.DateTime.UtcNow.ToString("o");
            Store.Write(data);
        }

        private ISaveParticipant[] Participants()
        {
            Transform alice = PlayerPresence.Current;
            return alice != null ? alice.GetComponents<ISaveParticipant>() : new ISaveParticipant[0];
        }
    }
}
