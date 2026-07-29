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

        [Test]
        public void ProjectilePresentationStaysSmallerThanACombatantAcrossAllWeapons()
        {
            foreach (var weaponId in WeaponRoster.All)
            {
                var scale = WeaponPresentationScale.For(
                    weaponId,
                    WeaponVisualStage.Projectile,
                    1f,
                    level: 5,
                    evolved: true);

                Assert.That(scale, Is.InRange(0.08f, 0.22f), weaponId.Value);
            }
        }

        [Test]
        public void AreaEffectsGrowWithPowerWithoutReturningToScreenFillingScale()
        {
            var normal = WeaponPresentationScale.For(
                WeaponId.ThunderCrashBomb,
                WeaponVisualStage.Detonation,
                1f,
                level: 1,
                evolved: false);
            var evolved = WeaponPresentationScale.For(
                WeaponId.ThunderCrashBomb,
                WeaponVisualStage.Detonation,
                1f,
                level: 5,
                evolved: true);

            Assert.That(evolved, Is.GreaterThan(normal));
            Assert.That(evolved, Is.LessThanOrEqualTo(0.72f));
        }

        [Test]
        public void VisualPartRanges_MatchEveryApprovedPolishFrameWithoutOverlap()
        {
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(WeaponVisualPartIndex.Hwando.Projectile, Is.EqualTo(0));
                Assert.That(WeaponVisualPartIndex.Hwando.Trail, Is.EqualTo(4));
                Assert.That(WeaponVisualPartIndex.Hwando.Impact, Is.EqualTo(8));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.HwandoFlyingBlade), Is.EqualTo(12));

                Assert.That(WeaponVisualPartIndex.Gakgung.Windup, Is.EqualTo(0));
                Assert.That(WeaponVisualPartIndex.Gakgung.Projectile, Is.EqualTo(3));
                Assert.That(WeaponVisualPartIndex.Gakgung.Impact, Is.EqualTo(6));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.GakgungShot), Is.EqualTo(11));

                Assert.That(WeaponVisualPartIndex.Singijeon.Projectile, Is.EqualTo(0));
                Assert.That(WeaponVisualPartIndex.Singijeon.Trail, Is.EqualTo(4));
                Assert.That(WeaponVisualPartIndex.Singijeon.Detonation, Is.EqualTo(9));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.SingijeonVolley), Is.EqualTo(15));

                Assert.That(WeaponVisualPartIndex.Talisman.Field, Is.EqualTo(4));
                Assert.That(WeaponVisualPartIndex.Talisman.Impact, Is.EqualTo(9));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.TalismanThrow), Is.EqualTo(14));

                Assert.That(WeaponVisualPartIndex.ThunderCrash.Windup, Is.EqualTo(6));
                Assert.That(WeaponVisualPartIndex.ThunderCrash.Detonation, Is.EqualTo(10));
                Assert.That(WeaponVisualPartIndex.ThunderCrash.Field, Is.EqualTo(16));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.ThunderCrashBomb), Is.EqualTo(21));

                Assert.That(WeaponVisualPartIndex.Jangseung.Field, Is.EqualTo(5));
                Assert.That(WeaponVisualPartIndex.Jangseung.Impact, Is.EqualTo(9));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.JangseungWard), Is.EqualTo(14));

                Assert.That(WeaponVisualPartIndex.FrostFlask.Field, Is.EqualTo(6));
                Assert.That(WeaponVisualPartIndex.FrostFlask.Impact, Is.EqualTo(11));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.FrostFlask), Is.EqualTo(17));

                Assert.That(WeaponVisualPartIndex.WindThunderFan.Field, Is.EqualTo(5));
                Assert.That(WeaponVisualPartIndex.WindThunderFan.Impact, Is.EqualTo(9));
                Assert.That(WeaponVisualPartIndex.RequiredCount(WeaponId.WindThunderFan), Is.EqualTo(15));
            });
        }
    }
}
