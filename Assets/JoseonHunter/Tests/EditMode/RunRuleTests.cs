using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class RunRuleTests
    {
        [TestCase(0f, RunPhase.WaveOne)]
        [TestCase(120f, RunPhase.WaveTwo)]
        [TestCase(300f, RunPhase.WaveThree)]
        [TestCase(600f, RunPhase.Peak)]
        [TestCase(840f, RunPhase.BossWarning)]
        [TestCase(900f, RunPhase.Boss)]
        [TestCase(960f, RunPhase.Expired)]
        public void ClockUsesApprovedBoundaries(float seconds, RunPhase expected)
        {
            var clock = new RunClock();

            Assert.That(clock.Advance(seconds), Is.EqualTo(expected));
        }

        [Test]
        public void ClockRejectsNonFiniteDeltaWithoutPoisoningElapsedTime()
        {
            var clock = new RunClock();

            Assert.That(() => clock.Advance(float.NaN), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => clock.Advance(float.PositiveInfinity), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(clock.Advance(900f), Is.EqualTo(RunPhase.Boss));
        }

        [Test]
        public void ClockRetainsElapsedSecondsForRunSessionConsumers()
        {
            var clock = new RunClock();

            clock.Advance(120f);

            Assert.That(clock.ElapsedSeconds, Is.EqualTo(120f));
        }

        [TestCase(RunPhase.WaveOne, 72)]
        [TestCase(RunPhase.WaveTwo, 104)]
        [TestCase(RunPhase.WaveThree, 128)]
        [TestCase(RunPhase.Peak, 140)]
        [TestCase(RunPhase.Boss, 36)]
        public void WaveScheduleUsesApprovedActiveCaps(RunPhase phase, int expected)
        {
            Assert.That(WaveSchedule.For(phase).ActiveCap, Is.EqualTo(expected));
        }

        [Test]
        public void OpeningWaveContainsOnlyPlagueRats()
        {
            var wave = WaveSchedule.For(RunPhase.WaveOne);

            Assert.That(wave.WeightedContent, Has.Count.EqualTo(1));
            Assert.That(wave.WeightedContent[0].ContentId, Is.EqualTo("plague_rat"));
            Assert.That(wave.WeightedContent[0].Weight, Is.EqualTo(100));
        }

        [Test]
        public void EveryPlayablePhaseHasContentIds()
        {
            foreach (RunPhase phase in new[]
                     {
                         RunPhase.WaveOne, RunPhase.WaveTwo, RunPhase.WaveThree,
                         RunPhase.Peak, RunPhase.BossWarning, RunPhase.Boss
                     })
            {
                Assert.That(WaveSchedule.For(phase).ContentIds, Is.Not.Empty);
            }
        }

        [Test]
        public void StoppedRunWavesHaveNoActiveEnemyCap()
        {
            Assert.That(WaveSchedule.For(RunPhase.Boss, true).ActiveCap, Is.EqualTo(0));
        }
    }
}
