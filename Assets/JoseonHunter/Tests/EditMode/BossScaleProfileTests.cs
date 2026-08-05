using JoseonHunter.Domain.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class BossScaleProfileTests
    {
        [TestCase(BossCombatRole.FirstMidBoss, 1.7f)]
        [TestCase(BossCombatRole.SecondMidBoss, 1.9f)]
        [TestCase(BossCombatRole.FinalBoss, 2.3f)]
        public void BossRolesHaveClearlyOrderedSilhouetteMultipliers(BossCombatRole role, float expected)
        {
            Assert.That(BossScaleProfile.MultiplierFor(role), Is.EqualTo(expected));
        }

        [Test]
        public void ContactRadiusFollowsVisibleBossScale()
        {
            Assert.That(BossScaleProfile.ContactRadius(0.42f, BossCombatRole.FinalBoss),
                Is.EqualTo(0.966f).Within(.001f));
        }
    }
}

