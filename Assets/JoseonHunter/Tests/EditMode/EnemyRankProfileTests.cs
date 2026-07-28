using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class EnemyRankProfileTests
    {
        [Test]
        public void EliteHasApprovedCombatAndDisplayMultipliers()
        {
            var elite = EnemyRankProfile.Elite;
            Assert.That(elite.IsElite, Is.True);
            Assert.That(elite.DisplayScale, Is.InRange(1.20f, 1.28f));
            Assert.That(elite.HealthMultiplier, Is.EqualTo(4f));
            Assert.That(elite.ContactDamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(elite.SpeedMultiplier, Is.EqualTo(0.92f));
            Assert.That(elite.ExperienceValue, Is.EqualTo(5));
        }

        [Test]
        public void NormalMatchesHeroScaleAndDropsOneExperience()
        {
            var normal = EnemyRankProfile.Normal;
            Assert.That(normal.DisplayScale, Is.EqualTo(1f));
            Assert.That(normal.ExperienceValue, Is.EqualTo(1));
            Assert.That(normal.IsElite, Is.False);
            Assert.That(normal.IsBoss, Is.False);
        }
    }
}
