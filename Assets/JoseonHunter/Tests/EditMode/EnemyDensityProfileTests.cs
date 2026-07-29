using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class EnemyDensityProfileTests
    {
        [Test]
        public void ExpandedBattlefieldSupportsARealHorde()
        {
            Assert.That(EnemyDensityProfile.MaximumActiveEnemies, Is.GreaterThanOrEqualTo(120));
        }

        [Test]
        public void OpeningSecondsSpawnSeveralEnemiesPerSecond()
        {
            var enemiesPerSecond = EnemyDensityProfile.BatchSize(0f) /
                EnemyDensityProfile.SpawnInterval(0f);

            Assert.That(enemiesPerSecond, Is.GreaterThanOrEqualTo(8f));
        }

        [Test]
        public void PressureRisesWithoutUnboundedPerFrameSpawning()
        {
            Assert.That(EnemyDensityProfile.BatchSize(1f), Is.InRange(3, 4));
            Assert.That(EnemyDensityProfile.SpawnInterval(1f), Is.InRange(.08f, .12f));
        }
    }
}
