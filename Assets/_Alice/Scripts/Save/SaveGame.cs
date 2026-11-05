using System.Collections.Generic;
using System.IO;
using Alice.Core;
using UnityEngine;

namespace Alice.Save
{
    /// <summary>
    /// The save of the running game. On start it loads the file, restores the world's permanent changes, hands
    /// each part to the systems on Alice and sends her to the room of her last chair; at every autosave point it
    /// gathers them again and writes. The file lives in persistentDataPath, outside the repository.
    /// </summary>
    public sealed class SaveGame : MonoBehaviour
    {
        [Tooltip("File name inside Application.persistentDataPath.")]
        [SerializeField] private string _fileName = "alice-save.json";
        [Tooltip("Prototype rooms can start clean every time while this is off.")]
        [SerializeField] private bool _loadOnStart = true;

        private SaveStore _store;

        public static string PathFor(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public SaveStore Store => _store ??= new SaveStore(PathFor(_fileName));

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
            if (!_loadOnStart || !Store.TryLoad(out SaveData data))
            {
                WorldFlags.Replace(null);
                return;
            }

            Loaded = data;
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
            Store.Write(data);
        }

        private ISaveParticipant[] Participants()
        {
            Transform alice = PlayerPresence.Current;
            return alice != null ? alice.GetComponents<ISaveParticipant>() : new ISaveParticipant[0];
        }
    }
}
