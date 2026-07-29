using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponVisualCueTests
    {
        [Test]
        public void LevelThreeCue_IsVisiblyStrongerThanLevelOneWithoutDoublingSize()
        {
            var one = new WeaponVisualCue(WeaponId.GakgungShot, WeaponVisualStage.Impact, 1, false, 1f, .12f);
            var three = new WeaponVisualCue(WeaponId.GakgungShot, WeaponVisualStage.Impact, 3, false, 1f, .12f);

            Assert.That(three.ResolvedScale, Is.GreaterThan(one.ResolvedScale));
            Assert.That(three.ResolvedScale, Is.LessThan(2f));
        }

        [Test]
        public void EvolvedCue_OutlivesNormalCueButRemainsShort()
        {
            var normal = new WeaponVisualCue(WeaponId.ThunderCrashBomb, WeaponVisualStage.Detonation, 5, false, 1f, .2f);
            var evolved = new WeaponVisualCue(WeaponId.ThunderCrashBomb, WeaponVisualStage.Detonation, 5, true, 1f, .2f);

            Assert.That(evolved.ResolvedLifetime, Is.GreaterThan(normal.ResolvedLifetime));
            Assert.That(evolved.ResolvedLifetime, Is.LessThanOrEqualTo(.32f));
        }
    }
}
