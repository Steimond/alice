using System;
using System.IO;
using UnityEngine;

namespace Alice.Save
{
    /// <summary>
    /// One save file on disk (GDD section 10): written to a temporary file and swapped in, with the previous
    /// version kept as a backup. A file that fails to load is never overwritten by the attempt; the backup
    /// is tried instead.
    /// </summary>
    public sealed class SaveStore
    {
        private readonly string _path;

        public SaveStore(string path)
        {
            _path = path;
        }

        public string FilePath => _path;
        public string BackupPath => _path + ".bak";
        private string TempPath => _path + ".tmp";

        public bool Exists => File.Exists(_path) || File.Exists(BackupPath);

        public void Write(SaveData data)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(_path))
            {
                File.Replace(TempPath, _path, BackupPath);
            }
            else
            {
                File.Move(TempPath, _path);
            }
        }

        public bool TryLoad(out SaveData data)
        {
            return TryRead(_path, out data) || TryRead(BackupPath, out data);
        }

        public void Delete()
        {
            foreach (string path in new[] { _path, BackupPath, TempPath })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save {path} could not be read: {exception.Message}");
                data = null;
            }

            if (data == null || data.Schema <= 0 || data.Schema > SaveData.CurrentSchema)
            {
                data = null;
                return false;
            }

            data.Loss ??= new DecreeLossData();
            data.KnownTechniques ??= new System.Collections.Generic.List<string>();
            data.Abilities ??= new System.Collections.Generic.List<string>();
            return true;
        }
    }
}
