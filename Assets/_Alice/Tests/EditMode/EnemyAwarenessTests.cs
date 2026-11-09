using Alice.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    public sealed class EnemyAwarenessTests
    {
        [Test]
        public void NoticesOnlyInsideSight_ButKeepsHerUntilSheLeavesTheWiderZone()
        {
            var awareness = new EnemyAwareness();

            Assert.That(awareness.Track(new Vector2(12f, 0f), 9f, 3f, 15f, 6f), Is.False, "Past the sight range she is unseen.");
            Assert.That(awareness.Track(new Vector2(8f, 0f), 9f, 3f, 15f, 6f), Is.True, "Inside it she is noticed.");
            Assert.That(awareness.Track(new Vector2(12f, 4f), 9f, 3f, 15f, 6f), Is.True, "Noticed, she is kept further out.");
            Assert.That(awareness.Track(new Vector2(16f, 0f), 9f, 3f, 15f, 6f), Is.False, "Past the wider zone she is lost.");
            Assert.That(awareness.Track(new Vector2(12f, 0f), 9f, 3f, 15f, 6f), Is.False, "Lost, she must be noticed anew.");
        }

        [Test]
        public void Forget_DropsHerAtOnce()
        {
            var awareness = new EnemyAwareness();
            awareness.Track(Vector2.zero, 9f, 3f, 15f, 6f);
            awareness.Forget();

            Assert.That(awareness.Engaged, Is.False);
            Assert.That(awareness.Track(new Vector2(12f, 0f), 9f, 3f, 15f, 6f), Is.False);
        }
    }
}
