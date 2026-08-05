using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class EnemyHealthCurveTests
    {
        [TestCase(-1f, 18f)]
        [TestCase(0f, 18f)]
        [TestCase(180f, 42f)]
        [TestCase(360f, 68f)]
        [TestCase(600f, 105f)]
        [TestCase(900f, 155f)]
        [TestCase(901f, 155f)]
        public void BaseHealthUsesAuthoredFifteenMinuteMilestones(float elapsedSeconds, float expected)
        {
            Assert.That(EnemyHealthCurve.BaseHealthAt(elapsedSeconds), Is.EqualTo(expected).Within(.001f));
        }

        [Test]
        public void BaseHealthNeverFallsAsTheStageAdvances()
        {
            var previous = EnemyHealthCurve.BaseHealthAt(0f);
            for (var second = 1; second <= 900; second++)
            {
                var current = EnemyHealthCurve.BaseHealthAt(second);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
                previous = current;
            }
        }
    }
}
