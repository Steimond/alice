using Alice.Combat;
using NUnit.Framework;

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
        public void HealthPool_RejectsNonPositiveDamage()
        {
            HealthPool pool = new HealthPool(2);

            Assert.That(pool.Damage(0), Is.False);
            Assert.That(pool.Current, Is.EqualTo(2));
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
