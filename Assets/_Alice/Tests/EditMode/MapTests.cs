using System.Linq;
using Alice.Save;
using Alice.UI;
using Alice.World;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    /// <summary>The in-game map (GDD 0.33): what is revealed, the player's marks, the save, and the drawing.</summary>
    public sealed class MapTests
    {
        private MapAtlas _atlas;

        [SetUp]
        public void SetUp()
        {
            _atlas = ScriptableObject.CreateInstance<MapAtlas>();
            // Two rooms side by side, 6 × 4 tiles, rock round the edge and a passage between them at y 1–2.
            _atlas.Rooms = new[]
            {
                Room("A", "Z1", false, 0, "######" + "#....." + "#....." + "######"),
                Room("B", "Z1", false, 6, "######" + "......" + ".....#" + "######"),
                Room("S", "Z1", true, 12, "######" + "#....#" + "#....#" + "######")
            };
            _atlas.Zones = new[] { new MapAtlas.ZoneEntry { Id = "Z1", Tint = Color.blue } };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_atlas);
        }

        private static MapAtlas.RoomEntry Room(string id, string zone, bool secret, int x, string rowsFromTop)
        {
            // Written top row first for readability; the atlas stores rows from the bottom.
            string[] rows = Enumerable.Range(0, 4).Select(i => rowsFromTop.Substring(i * 6, 6)).Reverse().ToArray();
            return new MapAtlas.RoomEntry
            {
                Id = id, ZoneId = zone, Secret = secret, Origin = new Vector2Int(x, 0),
                Rect = new RectInt(x, 0, 6, 4), Cells = string.Concat(rows)
            };
        }

        [Test]
        public void EnteringARoomRevealsItOnce()
        {
            var state = new MapState();
            Assert.That(state.Visit("A"), Is.True);
            Assert.That(state.Visit("A"), Is.False);
            Assert.That(state.IsRevealed("A"), Is.True);
            Assert.That(state.IsRevealed("B"), Is.False);
        }

        [Test]
        public void RevealingAZoneSkipsItsSecretRooms()
        {
            var state = new MapState();
            Assert.That(state.RevealZone(_atlas, "Z1"), Is.EqualTo(2));
            Assert.That(state.IsRevealed("S"), Is.False);
            state.Visit("S");
            Assert.That(state.IsRevealed("S"), Is.True);
        }

        [Test]
        public void AMarkIsLeftAndTakenAwayNearby()
        {
            var state = new MapState();
            state.AddMark(MapIconKind.MarkCross, new Vector2(3f, 2f));
            Assert.That(state.RemoveMarkNear(new Vector2(9f, 2f), 2f), Is.False);
            Assert.That(state.RemoveMarkNear(new Vector2(3.5f, 2.5f), 2f), Is.True);
            Assert.That(state.Marks, Is.Empty);
        }

        [Test]
        public void TheMapSurvivesASave()
        {
            var alice = new GameObject("Alice");
            try
            {
                MapProgress progress = alice.AddComponent<MapProgress>();
                progress.State.Visit("A");
                progress.State.AddMark(MapIconKind.MarkButton, new Vector2(4f, 1f));
                var data = new SaveData();
                progress.Capture(data);

                MapProgress loaded = new GameObject("Loaded").AddComponent<MapProgress>();
                loaded.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data)));
                Assert.That(loaded.State.IsRevealed("A"), Is.True);
                Assert.That(loaded.State.Marks.Single().Kind, Is.EqualTo(MapIconKind.MarkButton));
                Assert.That(loaded.State.Marks.Single().Position, Is.EqualTo(new Vector2(4f, 1f)));
                Object.DestroyImmediate(loaded.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(alice);
            }
        }

        [Test]
        public void ARoomPointBecomesAWorldPoint()
        {
            Assert.That(_atlas.TryToWorld("B", new Vector2(2f, 1f), out Vector2 world), Is.True);
            Assert.That(world, Is.EqualTo(new Vector2(8f, 1f)));
            Assert.That(_atlas.TryToWorld("Nowhere", Vector2.zero, out _), Is.False);
        }

        [Test]
        public void OnlyRevealedRoomsAreDrawnAndPassagesStayOpen()
        {
            var state = new MapState();
            state.Visit("A");
            const int scale = 2;
            Texture2D map = MapPainter.Paint(_atlas, state, "A", scale);
            try
            {
                Vector2Int origin = MapPainter.Origin(_atlas);
                Color32 Pixel(float worldX, float worldY) =>
                    map.GetPixel(Mathf.FloorToInt((worldX - origin.x) * scale), Mathf.FloorToInt((worldY - origin.y) * scale));
                Color32 paper = Pixel(-3.5f, -3.5f);

                // Inside A: filled. Inside B (not revealed): paper.
                Assert.That(Pixel(2.5f, 1.5f), Is.Not.EqualTo(paper));
                Assert.That(Pixel(8.5f, 1.5f), Is.EqualTo(paper));
                // A's east edge cell at the passage (5, 1): its top row is plain fill on both pixels — no chalk on
                // the east side, because B continues there. Its bottom row is chalk: rock lies below.
                int px = (5 - origin.x) * scale;
                int py = (1 - origin.y) * scale;
                Color32 fill = map.GetPixel(px, py + 1);
                Assert.That((Color32)map.GetPixel(px + 1, py + 1), Is.EqualTo(fill));
                Assert.That((Color32)map.GetPixel(px, py), Is.Not.EqualTo(fill));
            }
            finally
            {
                Object.DestroyImmediate(map);
            }
        }
    }
}
