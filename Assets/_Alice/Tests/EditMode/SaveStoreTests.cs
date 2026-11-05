using System.IO;
using Alice.Save;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    public sealed class SaveStoreTests
    {
        private string _directory;
        private SaveStore _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "alice-save-tests-" + System.Guid.NewGuid().ToString("N"));
            _store = new SaveStore(Path.Combine(_directory, "save.json"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void Write_ThenLoad_KeepsEveryField()
        {
            SaveData data = new SaveData
            {
                RestId = "R_P01",
                Decrees = 42,
                Confidence = 17.5f,
                EquippedTechnique = "spin_strike"
            };
            data.Loss = new DecreeLossData { Exists = true, RoomId = "P_COMBAT", X = 3.5f, Y = 0f, Amount = 30 };
            data.KnownTechniques.Add("spin_strike");
            data.Abilities.Add("A01");
            data.RestRoomId = "M3_A";
            data.Flags.Add("door:D_M3A_W");

            _store.Write(data);

            Assert.That(_store.TryLoad(out SaveData loaded), Is.True);
            Assert.That(loaded.RestId, Is.EqualTo("R_P01"));
            Assert.That(loaded.Decrees, Is.EqualTo(42));
            Assert.That(loaded.Confidence, Is.EqualTo(17.5f));
            Assert.That(loaded.Loss.Exists, Is.True);
            Assert.That(loaded.Loss.RoomId, Is.EqualTo("P_COMBAT"));
            Assert.That(loaded.Loss.Amount, Is.EqualTo(30));
            Assert.That(loaded.KnownTechniques, Is.EqualTo(new[] { "spin_strike" }));
            Assert.That(loaded.Abilities, Is.EqualTo(new[] { "A01" }));
            Assert.That(loaded.RestRoomId, Is.EqualTo("M3_A"));
            Assert.That(loaded.Flags, Is.EqualTo(new[] { "door:D_M3A_W" }));
        }

        [Test]
        public void SecondWrite_KeepsThePreviousAsBackup()
        {
            _store.Write(new SaveData { Decrees = 1 });
            _store.Write(new SaveData { Decrees = 2 });

            Assert.That(File.Exists(_store.BackupPath), Is.True);
            SaveData backup = JsonUtility.FromJson<SaveData>(File.ReadAllText(_store.BackupPath));
            Assert.That(backup.Decrees, Is.EqualTo(1));
        }

        [Test]
        public void BrokenFile_FallsBackToBackupAndIsNotOverwrittenByLoading()
        {
            _store.Write(new SaveData { Decrees = 1 });
            _store.Write(new SaveData { Decrees = 2 });
            File.WriteAllText(_store.FilePath, "{ not json");

            Assert.That(_store.TryLoad(out SaveData loaded), Is.True);
            Assert.That(loaded.Decrees, Is.EqualTo(1), "The backup stands in for a broken file.");
            Assert.That(File.ReadAllText(_store.FilePath), Is.EqualTo("{ not json"), "Loading never writes.");
        }

        [Test]
        public void SaveFromANewerGame_IsNotLoaded()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, JsonUtility.ToJson(new SaveData { Schema = SaveData.CurrentSchema + 1 }));

            Assert.That(_store.TryLoad(out _), Is.False);
        }
    }
}
