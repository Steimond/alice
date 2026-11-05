using System.IO;
using Alice.Core;
using UnityEngine;

namespace Alice.Save
{
    /// <summary>
    /// The save of the running game. On start it loads the file and hands each part to the systems on Alice;
    /// at every autosave point it gathers them again and writes. The file lives in persistentDataPath,
    /// outside the repository.
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
            if (_loadOnStart && Store.TryLoad(out SaveData data))
            {
                foreach (ISaveParticipant participant in Participants())
                {
                    participant.Restore(data);
                }
            }
        }

        private void Write()
        {
            SaveData data = new SaveData();
            foreach (ISaveParticipant participant in Participants())
            {
                participant.Capture(data);
            }

            Store.Write(data);
        }

        private ISaveParticipant[] Participants()
        {
            Transform alice = PlayerPresence.Current;
            return alice != null ? alice.GetComponents<ISaveParticipant>() : new ISaveParticipant[0];
        }
    }
}
