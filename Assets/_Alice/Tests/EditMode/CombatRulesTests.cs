using Alice.Combat;
using Alice.Core;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    public sealed class CombatRulesTests
    {
        [Test]
        public void HealthPool_DiesAtZeroAndIgnoresFurtherDamage()
        {
            HealthPool pool = new HealthPool(3);

            Assert.That(pool.Damage(2), Is.True);
            Assert.That(pool.Damage(5), Is.True);
            Assert.That(pool.IsDead, Is.True);
            Assert.That(pool.Current, Is.EqualTo(0));
            Assert.That(pool.Damage(1), Is.False, "The dead take no more hits.");

            pool.Refill();
            Assert.That(pool.Current, Is.EqualTo(3));
        }

        [Test]
        public void HealthPool_RestoreStopsAtMaxAndNeverRevives()
        {
            HealthPool pool = new HealthPool(100);
            pool.Damage(30);
            Assert.That(pool.Restore(40), Is.EqualTo(30));
            Assert.That(pool.Current, Is.EqualTo(100));

            pool.Damage(100);
            Assert.That(pool.Restore(50), Is.EqualTo(0));
            Assert.That(pool.IsDead, Is.True);
        }

        [Test]
        public void ConfidencePool_GainsUpToMaxAndSpendsAllOrNothing()
        {
            Alice.Player.ConfidencePool pool = new Alice.Player.ConfidencePool(100f);
            pool.Gain(30f);
            Assert.That(pool.TrySpend(40f), Is.False, "A technique she cannot pay for does not start.");
            Assert.That(pool.Current, Is.EqualTo(30f));
            Assert.That(pool.TrySpend(25f), Is.True);
            Assert.That(pool.Current, Is.EqualTo(5f).Within(1e-4f));

            pool.Gain(500f);
            Assert.That(pool.Current, Is.EqualTo(100f));
            pool.RaiseMax(20f);
            Assert.That(pool.Max, Is.EqualTo(120f));
            Assert.That(pool.Current, Is.EqualTo(100f), "A cake raises the maximum, not what she has.");
        }

        [Test]
        public void HealthPool_RejectsNonPositiveDamage()
        {
            HealthPool pool = new HealthPool(2);

            Assert.That(pool.Damage(0), Is.False);
            Assert.That(pool.Current, Is.EqualTo(2));
        }

        [Test]
        public void DeathFor_PicksVariantByKillingBlowElseDefault()
        {
            Texture2D texture = new Texture2D(2, 2);
            Sprite cleave = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero);
            Sprite stab = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero);
            EnemySpriteSet sprites = ScriptableObject.CreateInstance<EnemySpriteSet>();
            try
            {
                sprites.Assign(cleave, new Sprite[0], new Sprite[0], new Sprite[0], new Sprite[0], new[] { cleave }, 0);
                sprites.AssignReactions(new Sprite[0], new Sprite[0],
                    new[] { new EnemySpriteSet.DeathVariant(HitKind.Thrust, new[] { stab }, 0.2f) });

                Assert.That(sprites.DeathFor(HitKind.Thrust, out float stabTime), Is.EqualTo(new[] { stab }));
                Assert.That(stabTime, Is.EqualTo(0.2f));
                Assert.That(sprites.DeathFor(HitKind.Slash, out float slashTime), Is.EqualTo(new[] { cleave }));
                Assert.That(slashTime, Is.EqualTo(sprites.DeathFrameTime));
            }
            finally
            {
                Object.DestroyImmediate(sprites);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Timeline_SplitsIntoReadablePhases()
        {
            MeleeAttackTimeline timeline = new MeleeAttackTimeline(0.5f, 0.2f, 0.3f);

            Assert.That(timeline.Duration, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(timeline.PhaseAt(-0.01f), Is.EqualTo(AttackPhase.None));
            Assert.That(timeline.PhaseAt(0f), Is.EqualTo(AttackPhase.Windup));
            Assert.That(timeline.PhaseAt(0.6f), Is.EqualTo(AttackPhase.Active));
            Assert.That(timeline.PhaseAt(0.8f), Is.EqualTo(AttackPhase.Recovery));
            Assert.That(timeline.PhaseAt(1f), Is.EqualTo(AttackPhase.None));
            Assert.That(timeline.PhaseProgress(0.6f), Is.EqualTo(0.5f).Within(1e-4f));
        }
    }
}
