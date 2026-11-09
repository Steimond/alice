using System.IO;
using UnityEngine;

namespace Alice.Save
{
    /// <summary>
    /// The three save slots of the main menu (GDD 0.35). The menu picks one before the game starts; the running
    /// game's <see cref="SaveGame"/> reads and writes only that slot's file. A room played on its own in the
    /// Editor uses slot 1. The single file of earlier builds becomes slot 1 the first time slots are read.
    /// </summary>
    public static class SaveSlots
    {
        public const int Count = 3;
        private const string LegacyFile = "alice-save.json";

        private static int _current = 1;

        /// <summary>The slot the game plays in, 1 to <see cref="Count"/>.</summary>
        public static int Current
        {
            get => _current;
            set => _current = Mathf.Clamp(value, 1, Count);
        }

        public static string FileName(int slot)
        {
            return $"alice-save-{Mathf.Clamp(slot, 1, Count)}.json";
        }

        public static SaveStore Store(int slot)
        {
            MoveLegacySave();
            return new SaveStore(SaveGame.PathFor(FileName(slot)));
        }

        public static bool TryRead(int slot, out SaveData data)
        {
            return Store(slot).TryLoad(out data);
        }

        public static void Delete(int slot)
        {
            Store(slot).Delete();
        }

        private static void MoveLegacySave()
        {
            string legacy = SaveGame.PathFor(LegacyFile);
            string first = SaveGame.PathFor(FileName(1));
            if (!File.Exists(legacy) || File.Exists(first) || File.Exists(first + ".bak"))
            {
                return;
            }

            try
            {
                File.Move(legacy, first);
                if (File.Exists(legacy + ".bak"))
                {
                    File.Move(legacy + ".bak", first + ".bak");
                }
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"The old save could not be moved to slot 1: {exception.Message}");
            }
        }
    }
}
