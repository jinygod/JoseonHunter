using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StagePacingTimelineTests
    {
        [TestCase(120f, StageMilestone.FirstSurge)]
        [TestCase(300f, StageMilestone.FirstMidBoss)]
        [TestCase(420f, StageMilestone.SecondSurge)]
        [TestCase(600f, StageMilestone.SecondMidBoss)]
        [TestCase(720f, StageMilestone.FinalSurge)]
        [TestCase(840f, StageMilestone.FinalBossWarning)]
        [TestCase(900f, StageMilestone.FinalBoss)]
        public void ProductionTimelineCrossesAuthoredMilestones(float seconds, StageMilestone milestone)
        {
            var timeline = StagePacingTimeline.ForDuration(900f);

            Assert.That(timeline.Crossed(seconds - .1f, seconds, milestone), Is.True);
            Assert.That(timeline.Crossed(seconds, seconds + .1f, milestone), Is.False);
        }

        [Test]
        public void PreviewCompressesFinalBossToFiftySeconds()
        {
            var timeline = StagePacingTimeline.ForDuration(60f);

            Assert.That(timeline.Crossed(49.9f, 50f, StageMilestone.FinalBoss), Is.True);
            Assert.That(timeline.EventWindowSeconds, Is.EqualTo(50f));
        }

        [Test]
        public void PreviewPreservesRelativeMidBossTiming()
        {
            var timeline = StagePacingTimeline.ForDuration(60f);

            Assert.That(timeline.Crossed(16.5f, 16.7f, StageMilestone.FirstMidBoss), Is.True);
            Assert.That(timeline.Crossed(33.2f, 33.4f, StageMilestone.SecondMidBoss), Is.True);
        }

        [Test]
        public void ThreeMinutePrototypeKeepsFinalBossAtRunEnd()
        {
            var timeline = StagePacingTimeline.ForDuration(180f);

            Assert.That(timeline.RunDurationSeconds, Is.EqualTo(180f));
            Assert.That(timeline.EventWindowSeconds, Is.EqualTo(180f));
            Assert.That(timeline.Crossed(179.9f, 180f, StageMilestone.FinalBoss), Is.True);
        }

        [Test]
        public void SurgeRaisesPressureWithoutExceedingMobileCap()
        {
            var timeline = StagePacingTimeline.ForDuration(900f);
            var calm = timeline.Sample(90f);
            var surge = timeline.Sample(125f);

            Assert.That(surge.EnemiesPerSecond, Is.GreaterThan(calm.EnemiesPerSecond));
            Assert.That(surge.ActiveCap, Is.GreaterThan(calm.ActiveCap));
            Assert.That(surge.ActiveCap, Is.LessThanOrEqualTo(StagePacingTimeline.MobileActiveCap));
            Assert.That(surge.SurgeIntensity, Is.GreaterThan(0));
        }

        [Test]
        public void LateRunPressureGrowsWithoutUnboundedSpawning()
        {
            var timeline = StagePacingTimeline.ForDuration(900f);
            var opening = timeline.Sample(0f);
            var late = timeline.Sample(800f);

            Assert.That(late.EnemiesPerSecond, Is.GreaterThan(opening.EnemiesPerSecond));
            Assert.That(late.BatchSize, Is.InRange(3, 6));
            Assert.That(late.SpawnIntervalSeconds, Is.GreaterThanOrEqualTo(.07f));
            Assert.That(late.ActiveCap, Is.LessThanOrEqualTo(StagePacingTimeline.MobileActiveCap));
        }
    }
}
