using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class MoonlitTombCombatTests
    {
        [TestCase(0f, 64, "tomb_attendant")]
        [TestCase(120f, 82, "tomb_archer_ghost")]
        [TestCase(300f, 96, "red_lantern_wraith")]
        [TestCase(420f, 104, "curse_shaman")]
        [TestCase(600f, 112, "grave_ambusher_elite")]
        [TestCase(720f, 118, "grave_ambusher_elite")]
        [TestCase(840f, 32, "eclipse_queen")]
        [TestCase(900f, 32, "eclipse_queen")]
        public void ApprovedWaveWindowsExposeTheirRoleAndCap(float seconds, int cap, string expectedId)
        {
            var waves = StageCombatCatalog.For(StageId.MoonlitTomb).Waves;

            Assert.That(waves.WaveAt(seconds).ActiveCap, Is.EqualTo(cap));
            Assert.That(waves.NormalEntriesAt(seconds).Select(entry => entry.ContentId),
                Does.Contain(expectedId));
        }

        [Test]
        public void ArcherCannotBeginAimingOutsideCameraMargin()
        {
            Assert.That(RangedAttackRules.CanAcquireTarget(false, 8f, 16f), Is.False);
            Assert.That(RangedAttackRules.CanAcquireTarget(true, 18f, 16f), Is.False);
            Assert.That(RangedAttackRules.CanAcquireTarget(true, 12f, 16f), Is.True);
        }

        [Test]
        public void WarnedRangedAttackLocksTargetUntilItExecutes()
        {
            var controller = new EnemyAttackController(
                EnemyAttackKind.WarnedLineProjectile, initialCooldownSeconds: 0f);

            var warning = controller.Tick(.01f, default, new Float2(5f, 2f), true);
            var execute = controller.Tick(warning.WarningSeconds, default, new Float2(-5f, -2f), true);

            Assert.That(warning.Phase, Is.EqualTo(EnemyAttackPhase.Telegraph));
            Assert.That(execute.ExecuteStarted, Is.True);
            Assert.That(execute.LockedTarget, Is.EqualTo(new Float2(5f, 2f)));
        }

        [TestCase("tomb_attendant", EnemyArchetype.Normal)]
        [TestCase("tomb_archer_ghost", EnemyArchetype.TombArcherGhost)]
        [TestCase("red_lantern_wraith", EnemyArchetype.RedLanternWraith)]
        [TestCase("curse_shaman", EnemyArchetype.CurseShaman)]
        [TestCase("grave_ambusher_elite", EnemyArchetype.GraveAmbusherElite)]
        public void EveryMoonlitTombEnemyHasAnExplicitReadableRole(string contentId, EnemyArchetype expected)
        {
            var profile = EnemyArchetypeProfile.ForContentId(contentId);

            Assert.That(profile.ContentId, Is.EqualTo(contentId));
            Assert.That(profile.Archetype, Is.EqualTo(expected));
            Assert.That(profile.HealthMultiplier, Is.GreaterThan(1f));
        }
    }
}
