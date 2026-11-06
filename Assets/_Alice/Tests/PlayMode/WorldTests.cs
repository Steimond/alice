using System.Collections;
using System.Collections.Generic;
using Alice.Abilities;
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
            _doorFrames = null;
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

        [UnityTest]
        public IEnumerator CheshireBridge_TurnsSolidNearAliceOnlyWithTheStep()
        {
            GameObject bridgeObject = Track(new GameObject("TestBridge"));
            BoxCollider2D surface = bridgeObject.AddComponent<BoxCollider2D>();
            surface.size = new Vector2(12f, 0.5f);
            CheshireBridge bridge = bridgeObject.AddComponent<CheshireBridge>();
            bridge.Bind(surface, null);

            GameObject alice = Track(new GameObject("TestAlice"));
            AbilityInventory abilities = alice.AddComponent<AbilityInventory>();
            PlayerPresence.Register(alice.transform);
            try
            {
                alice.transform.position = new Vector3(-7f, 0f, 0f);
                yield return null;
                Assert.That(surface.enabled, Is.False, "Without the Cheshire Step it is only an outline.");
                Assert.That(bridge, Is.InstanceOf<ITemporaryGround>(), "It is never safe footing.");

                abilities.Unlock(AbilityIds.CheshireStep);
                yield return null;
                Assert.That(surface.enabled, Is.True, "Within reach of its end, it turns solid by itself.");

                alice.transform.position = new Vector3(2f, 0.25f, 0f);
                yield return new WaitForSeconds(1f);
                Assert.That(surface.enabled, Is.True, "It holds while she is on it.");

                alice.transform.position = new Vector3(14f, 0f, 0f);
                yield return new WaitForSeconds(0.6f);
                Assert.That(surface.enabled, Is.False, "Once she has moved away, it fades.");
            }
            finally
            {
                PlayerPresence.Unregister(alice.transform);
            }
        }

        [UnityTest]
        public IEnumerator LittleDoor_BiteShrinksAlice_AndTheDoorStaysOpen()
        {
            GameObject alice = Track(new GameObject("TestAlice"));
            AbilityInventory abilities = alice.AddComponent<AbilityInventory>();
            TestShrinkable size = alice.AddComponent<TestShrinkable>();
            PlayerPresence.Register(alice.transform);
            try
            {
                LittleDoor door = CreateLittleDoor();
                yield return null;
                Assert.That(door.IsOpen, Is.False);
                Assert.That(door.CanInteract(Vector2.zero), Is.False, "Without the mushroom nothing happens.");

                abilities.Unlock(AbilityIds.Mushroom);
                Assert.That(door.CanInteract(Vector2.zero), Is.True);
                door.Interact(alice);
                Assert.That(size.IsSmall, Is.True, "The bite makes her small.");
                Assert.That(door.IsOpen, Is.True);
                Assert.That(WorldFlags.Has(door.Flag), Is.True, "Opening is a permanent change of the world.");
                Assert.That(door.CanInteract(Vector2.zero), Is.False, "Already small: no second bite.");

                // The other side of the passage, or the same room loaded again: the door stands open.
                LittleDoor again = CreateLittleDoor();
                yield return null;
                yield return null;
                Assert.That(again.IsOpen, Is.True);
                SpriteRenderer drawn = again.GetComponent<SpriteRenderer>();
                Assert.That(drawn.sprite, Is.SameAs(_doorFrames[_doorFrames.Length - 1]), "It is drawn standing open.");
            }
            finally
            {
                PlayerPresence.Unregister(alice.transform);
            }
        }

        [UnityTest]
        public IEnumerator AbilityBundle_OpensOnInteract_AndAnnouncesTheItem()
        {
            GameObject alice = Track(new GameObject("TestAlice"));
            AbilityInventory abilities = alice.AddComponent<AbilityInventory>();
            PlayerPresence.Register(alice.transform);
            string announced = null;
            void Remember(string id) => announced = id;
            ItemNotice.Found += Remember;
            try
            {
                GameObject bundle = Track(new GameObject("TestBundle"));
                bundle.AddComponent<BoxCollider2D>().isTrigger = true;
                AbilityPickup pickup = bundle.AddComponent<AbilityPickup>();
                pickup.AbilityId = AbilityIds.Clock;
                yield return null;

                Assert.That(pickup.CanInteract(Vector2.zero), Is.True);
                pickup.Interact(alice);
                Assert.That(abilities.Has(AbilityIds.Clock), Is.True);
                Assert.That(bundle.activeSelf, Is.False, "An opened bundle is gone.");
                Assert.That(announced, Is.EqualTo(AbilityIds.Clock), "The find is announced by its ID.");
            }
            finally
            {
                ItemNotice.Found -= Remember;
                PlayerPresence.Unregister(alice.transform);
            }
        }

        private Sprite[] _doorFrames;

        private LittleDoor CreateLittleDoor()
        {
            if (_doorFrames == null)
            {
                Texture2D texture = new Texture2D(2, 2);
                _created.Add(texture);
                _doorFrames = new[]
                {
                    Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero),
                    Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.zero)
                };
                _created.AddRange(_doorFrames);
            }

            GameObject doorObject = Track(new GameObject("TestLittleDoor"));
            LittleDoor door = doorObject.AddComponent<LittleDoor>();
            door.DoorId = "LD_TEST";
            door.Bind(doorObject.AddComponent<SpriteRenderer>(), _doorFrames);
            return door;
        }

        private sealed class TestShrinkable : MonoBehaviour, IShrinkable
        {
            public bool IsSmall { get; private set; }

            public void Shrink()
            {
                IsSmall = true;
            }

            public void Grow()
            {
                IsSmall = false;
            }
        }

        [UnityTest]
        public IEnumerator ClockGuillotine_HeldByTheClock_StaysUp()
        {
            GameObject frame = Track(new GameObject("TestGuillotine"));
            GameObject blade = new GameObject("Blade");
            blade.transform.SetParent(frame.transform, false);
            blade.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            BoxCollider2D body = blade.AddComponent<BoxCollider2D>();
            frame.SetActive(false);
            ClockGuillotine mechanism = frame.AddComponent<ClockGuillotine>();
            mechanism.Bind(blade.transform, body);
            frame.SetActive(true);

            mechanism.Delay(6f);
            // It runs on until the blade is up, at most one cycle, then holds it there.
            yield return new WaitForSeconds(2.4f);
            float lowest = float.MaxValue;
            float until = Time.time + 1.5f;
            while (Time.time < until)
            {
                lowest = Mathf.Min(lowest, blade.transform.localPosition.y);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(lowest, Is.GreaterThan(7f), "Held by the clock, the blade stays raised.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "The clock never touches the global time scale.");
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
