using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class RunRuleTests
    {
        [TestCase(0f, RunPhase.WaveOne)]
        [TestCase(45f, RunPhase.WaveTwo)]
        [TestCase(90f, RunPhase.WaveThree)]
        [TestCase(135f, RunPhase.Peak)]
        [TestCase(165f, RunPhase.BossWarning)]
        [TestCase(180f, RunPhase.Boss)]
        [TestCase(240f, RunPhase.Expired)]
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
            Assert.That(clock.Advance(180f), Is.EqualTo(RunPhase.Boss));
        }

        [TestCase(RunPhase.WaveOne, 28)]
        [TestCase(RunPhase.WaveTwo, 36)]
        [TestCase(RunPhase.WaveThree, 48)]
        [TestCase(RunPhase.Peak, 64)]
        [TestCase(RunPhase.Boss, 36)]
        public void WaveScheduleUsesApprovedActiveCaps(RunPhase phase, int expected)
        {
            Assert.That(WaveSchedule.For(phase).ActiveCap, Is.EqualTo(expected));
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
    }
}
