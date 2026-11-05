using System.Collections;
using System.Collections.Generic;
using Alice.Core;
using Alice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alice.Tests
{
    public sealed class WorldTests
    {
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            WorldFlags.Replace(null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.Destroy(created);
            }

            _created.Clear();
            WorldFlags.Replace(null);
        }

        [UnityTest]
        public IEnumerator PermanentDoor_OpensOnlyFromTheLatchSide_AndStaysOpen()
        {
            PermanentDoor door = CreateDoor("D_TEST", -1, out Collider2D body);
            yield return null;

            Assert.That(door.IsOpen, Is.False);
            Assert.That(body.enabled, Is.True, "Shut, the door blocks the passage.");
            Assert.That(door.CanInteract(new Vector2(3f, 0f)), Is.False, "No latch on this side.");
            Assert.That(door.CanInteract(new Vector2(-3f, 0f)), Is.True);

            door.Interact(null);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(body.enabled, Is.False, "Open, it lets Alice through.");
            Assert.That(WorldFlags.Has(door.Flag), Is.True, "Opening is a permanent change of the world.");

            // The room is loaded again (a rest, a defeat, a reload): the same door starts open.
            PermanentDoor again = CreateDoor("D_TEST", -1, out Collider2D againBody);
            yield return null;
            Assert.That(again.IsOpen, Is.True);
            Assert.That(againBody.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator ArrivalSpots_AreFoundById_WhileTheirRoomIsLoaded()
        {
            GameObject doorwayObject = Track(new GameObject("TestDoorway"));
            doorwayObject.SetActive(false);
            doorwayObject.transform.position = new Vector3(10f, 2.5f, 0f);
            doorwayObject.AddComponent<BoxCollider2D>().isTrigger = true;
            RoomDoorway doorway = doorwayObject.AddComponent<RoomDoorway>();
            doorway.DoorwayId = "D_TEST_E";
            doorway.Configure("M3_B", "D_M3B_W", new Vector2(-3.5f, -2.5f), -1);
            doorwayObject.SetActive(true);
            yield return null;

            IArrivalSpot spot = RoomTravel.FindSpot("D_TEST_E");
            Assert.That(spot, Is.Not.Null);
            Assert.That(spot.ArrivalPosition, Is.EqualTo(new Vector2(6.5f, 0f)));
            Assert.That(spot.ArrivalFacing, Is.EqualTo(-1));

            doorwayObject.SetActive(false);
            Assert.That(RoomTravel.FindSpot("D_TEST_E"), Is.Null, "An unloaded room's spots are gone.");
        }

        private PermanentDoor CreateDoor(string id, int latchSide, out Collider2D body)
        {
            GameObject door = Track(new GameObject("TestDoor"));
            door.SetActive(false);
            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(door.transform, false);
            body = bodyObject.AddComponent<BoxCollider2D>();
            PermanentDoor component = door.AddComponent<PermanentDoor>();
            component.DoorId = id;
            component.Bind(latchSide, body, null, new Sprite[0]);
            door.SetActive(true);
            return component;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
