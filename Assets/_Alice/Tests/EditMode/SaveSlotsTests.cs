using System.Linq;
using Alice.Core;
using Alice.Save;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    /// <summary>Save slots and settings of the main menu (GDD 0.35), without touching the player's real files.</summary>
    public sealed class SaveSlotsTests
    {
        [Test]
        public void EverySlotHasItsOwnFile()
        {
            string[] names = Enumerable.Range(1, SaveSlots.Count).Select(SaveSlots.FileName).ToArray();
            Assert.That(names.Distinct().Count(), Is.EqualTo(SaveSlots.Count));
            Assert.That(SaveSlots.FileName(0), Is.EqualTo(SaveSlots.FileName(1)), "Out-of-range slots fall back to the nearest.");
        }

        [Test]
        public void TheCurrentSlotStaysInRange()
        {
            int before = SaveSlots.Current;
            try
            {
                SaveSlots.Current = 9;
                Assert.That(SaveSlots.Current, Is.EqualTo(SaveSlots.Count));
                SaveSlots.Current = -1;
                Assert.That(SaveSlots.Current, Is.EqualTo(1));
            }
            finally
            {
                SaveSlots.Current = before;
            }
        }

        [Test]
        public void TheSlotSummaryFieldsSurviveTheFile()
        {
            var data = new SaveData { RoomId = "L03", PlayTime = 3725f, SavedAtUtc = "2026-11-08T21:03:37.0000000Z", Decrees = 120 };
            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(loaded.RoomId, Is.EqualTo("L03"));
            Assert.That(loaded.PlayTime, Is.EqualTo(3725f));
            Assert.That(loaded.SavedAtUtc, Is.EqualTo(data.SavedAtUtc));
        }

        [Test]
        public void SettingsKeepTheirValuesInJson()
        {
            var settings = new GameSettings { Language = "en", FullScreen = false, WindowScale = 4, ScreenShake = false, MusicVolume = 3 };
            GameSettings loaded = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(settings));
            Assert.That(loaded.Language, Is.EqualTo("en"));
            Assert.That(loaded.FullScreen, Is.False);
            Assert.That(loaded.WindowScale, Is.EqualTo(4));
            Assert.That(loaded.ScreenShake, Is.False);
            Assert.That(loaded.MusicVolume, Is.EqualTo(3));
        }
    }
}
