using System.Collections.Generic;
using Alice.Player;
using Alice.Save;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    public sealed class TrinketTests
    {
        private readonly List<Object> _created = new List<Object>();
        private PlayerTrinkets _trinkets;

        [SetUp]
        public void SetUp()
        {
            GameObject alice = new GameObject("TestAlice");
            _created.Add(alice);
            _trinkets = alice.AddComponent<PlayerTrinkets>();
            _trinkets.Catalog = new[]
            {
                Trinket("T01", 0.8f, 0f, 0),
                Trinket("T02", 1f, 0.05f, 0),
                Trinket("T03", 1f, 0f, 15)
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Toggle_FillsOnlyFreePockets_AndOnlyWithFoundTrinkets()
        {
            Assert.That(_trinkets.Pockets, Is.EqualTo(PlayerTrinkets.StartPockets));
            Assert.That(_trinkets.Toggle("T01"), Is.False, "A trinket not found yet cannot be worn.");

            _trinkets.ReceiveTrinket("T01");
            _trinkets.ReceiveTrinket("T02");
            _trinkets.ReceiveTrinket("T03");
            Assert.That(_trinkets.Toggle("T01"), Is.True);
            Assert.That(_trinkets.Toggle("T02"), Is.True);
            Assert.That(_trinkets.Toggle("T03"), Is.False, "Every trinket takes one pocket; both are full.");

            Assert.That(_trinkets.Toggle("T01"), Is.True, "Toggling a worn trinket takes it off.");
            Assert.That(_trinkets.Toggle("T03"), Is.True);
            Assert.That(_trinkets.Worn, Is.EquivalentTo(new[] { "T02", "T03" }));
        }

        [Test]
        public void WornTrinkets_ChangeDamage_ParryWindow_AndExecutionHeal()
        {
            foreach (string id in new[] { "T01", "T02", "T03" })
            {
                _trinkets.ReceiveTrinket(id);
            }

            Assert.That(_trinkets.ModifyDamage(20), Is.EqualTo(20), "Nothing worn changes nothing.");
            _trinkets.Toggle("T01");
            _trinkets.Toggle("T02");
            Assert.That(_trinkets.ModifyDamage(20), Is.EqualTo(16));
            Assert.That(_trinkets.ModifyDamage(1), Is.EqualTo(1), "A landed blow always costs something.");
            Assert.That(_trinkets.ParryWindowBonus, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(_trinkets.ExecutionHeal, Is.EqualTo(0));
        }

        [Test]
        public void Save_KeepsFoundWornAndPockets_AndDropsWhatIsNotFound()
        {
            _trinkets.ReceiveTrinket("T01");
            _trinkets.Toggle("T01");
            SaveData data = new SaveData();
            _trinkets.Capture(data);

            GameObject other = new GameObject("TestAliceAgain");
            _created.Add(other);
            PlayerTrinkets again = other.AddComponent<PlayerTrinkets>();
            data.WornTrinkets.Add("T03");
            again.Restore(data);
            Assert.That(again.Found, Is.EquivalentTo(new[] { "T01" }));
            Assert.That(again.Worn, Is.EquivalentTo(new[] { "T01" }), "A trinket she never found is not worn.");

            SaveData old = new SaveData { Pockets = 0 };
            again.Restore(old);
            Assert.That(again.Pockets, Is.EqualTo(PlayerTrinkets.StartPockets), "Saves from before pockets start at two.");
        }

        private TrinketDefinition Trinket(string id, float damage, float parry, int heal)
        {
            TrinketDefinition trinket = ScriptableObject.CreateInstance<TrinketDefinition>();
            trinket.Configure(id, null, damage, parry, heal);
            _created.Add(trinket);
            return trinket;
        }
    }
}
