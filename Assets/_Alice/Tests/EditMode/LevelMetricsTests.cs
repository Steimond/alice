using System.Linq;
using Alice.Editor;
using Alice.Player;
using NUnit.Framework;

namespace Alice.Tests
{
    /// <summary>The level-design sizes window: its worked-out reaches agree with the level limits rooms use.</summary>
    public sealed class LevelMetricsTests
    {
        [Test]
        public void ARunningJumpClearsTheLargestGap()
        {
            Assert.That(AliceLevelMetrics.RunningJumpReach(0f), Is.GreaterThanOrEqualTo(TraversalMetrics.MaxJumpGap));
            Assert.That(AliceLevelMetrics.RunningJumpReach(0f, true), Is.GreaterThanOrEqualTo(TraversalMetrics.MaxFanGap));
        }

        [Test]
        public void LedgesAboveTheLimitAreOutOfReach()
        {
            Assert.That(AliceLevelMetrics.RunningJumpReach(TraversalMetrics.MaxJumpLedge), Is.GreaterThan(0f));
            Assert.That(AliceLevelMetrics.RunningJumpReach(TraversalMetrics.MaxJumpLedge + 1), Is.LessThan(0f));
            Assert.That(AliceLevelMetrics.RunningJumpReach(TraversalMetrics.MaxFanLedge, true), Is.GreaterThan(0f));
        }

        [Test]
        public void DroppingFurtherCarriesFurther()
        {
            Assert.That(AliceLevelMetrics.RunningJumpReach(-8f), Is.GreaterThan(AliceLevelMetrics.RunningJumpReach(-2f)));
        }

        [Test]
        public void TheListIsBuiltFromTheGameData()
        {
            var groups = AliceLevelMetrics.Build();
            Assert.That(groups.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(groups.SelectMany(group => group.Rows).All(row => !string.IsNullOrEmpty(row.Value)), Is.True);
            Assert.That(AliceLevelMetrics.ToMarkdown(groups), Does.Contain("| Что | Размер | Пояснение |"));
        }
    }
}
