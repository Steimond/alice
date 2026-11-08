using System.Collections.Generic;
using Alice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    /// <summary>The biome map check: neighbouring rooms meet wall to wall and share their passages (LevelDesignGuide, 4).</summary>
    public sealed class LevelWorldTests
    {
        // A room with solid walls all round and openings cut into its west or east wall: floor y, height 4.
        private static AliceLevelWorld.RoomShape Room(string id, int x, int y, int width, int height, int[] westOpenings, int[] eastOpenings)
        {
            var room = new AliceLevelWorld.RoomShape(id, new RectInt(x, y, width, height));
            for (int cx = 0; cx < width; cx++)
            {
                for (int cy = 0; cy < height; cy++)
                {
                    room.Solid[cx, cy] = cx < 2 || cx >= width - 2 || cy < 2 || cy >= height - 2;
                }
            }

            Cut(room, 0, westOpenings);
            Cut(room, width - 2, eastOpenings);
            return room;
        }

        private static void Cut(AliceLevelWorld.RoomShape room, int x, int[] floors)
        {
            foreach (int floor in floors)
            {
                for (int cy = floor; cy < floor + 4; cy++)
                {
                    room.Solid[x, cy] = false;
                    room.Solid[x + 1, cy] = false;
                }
            }
        }

        [Test]
        public void MatchingPassagesOnBothSidesAreFine()
        {
            var rooms = new List<AliceLevelWorld.RoomShape>
            {
                Room("A", 0, 0, 20, 12, new int[0], new[] { 2, 6 }),
                Room("B", 20, 0, 20, 12, new[] { 2, 6 }, new int[0])
            };

            Assert.That(AliceLevelWorld.Check(rooms), Is.Empty);
        }

        [Test]
        public void APassageMissingOnTheOtherSideIsReported()
        {
            // B has a second passage 4 higher back to A; A has no such exit.
            var rooms = new List<AliceLevelWorld.RoomShape>
            {
                Room("A", 0, 0, 20, 12, new int[0], new[] { 2 }),
                Room("B", 20, 0, 20, 12, new[] { 2, 6 }, new int[0])
            };

            List<string> problems = AliceLevelWorld.Check(rooms);
            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain("B").And.Contain("упирается в стену A"));
        }

        [Test]
        public void APassageWithNoRoomBeyondIsReported()
        {
            var rooms = new List<AliceLevelWorld.RoomShape> { Room("A", 0, 0, 20, 12, new[] { 2 }, new int[0]) };

            List<string> problems = AliceLevelWorld.Check(rooms);
            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain("нет комнаты"));
        }

        [Test]
        public void OverlappingRoomsAreReported()
        {
            var rooms = new List<AliceLevelWorld.RoomShape>
            {
                Room("A", 0, 0, 20, 12, new int[0], new int[0]),
                Room("B", 15, 4, 20, 12, new int[0], new int[0])
            };

            Assert.That(AliceLevelWorld.Check(rooms), Has.Some.Contains("накладываются"));
        }
    }
}
