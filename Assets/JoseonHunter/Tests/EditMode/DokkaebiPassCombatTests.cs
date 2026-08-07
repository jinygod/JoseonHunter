using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class DokkaebiPassCombatTests
    {
        [TestCase(0f, 72, "club_dokkaebi")]
        [TestCase(120f, 92, "shield_guard_dokkaebi")]
        [TestCase(300f, 108, "iron_horn_dokkaebi")]
        [TestCase(420f, 116, "stone_thrower_dokkaebi")]
        [TestCase(600f, 124, "red_horn_elite")]
        [TestCase(720f, 130, "red_horn_elite")]
        [TestCase(840f, 36, "dokkaebi_king")]
        [TestCase(900f, 36, "dokkaebi_king")]
        public void ApprovedWaveWindowsExposeTheirRoleAndCap(float seconds, int cap, string expectedId)
        {
            var waves = StageCombatCatalog.For(StageId.DokkaebiPass).Waves;

            Assert.That(waves.WaveAt(seconds).ActiveCap, Is.EqualTo(cap));
            Assert.That(waves.NormalEntriesAt(seconds).Select(entry => entry.ContentId),
                Does.Contain(expectedId));
        }

        [Test]
        public void ShieldGuardBreaksOnSixBlockedHitsAndCreatesVulnerability()
        {
            var guard = new DirectionalGuardState(6, .15f);

            for (var hit = 0; hit < 5; hit++) Assert.That(guard.ConfirmBlockedHit(), Is.False);
            Assert.That(guard.ConfirmBlockedHit(), Is.True);

            Assert.That(guard.IsBroken, Is.True);
            Assert.That(guard.IncomingDamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void StoneThrowLocksOneTargetAndWarnsBeforeFiring()
        {
            var controller = new EnemyAttackController(
                EnemyAttackKind.WarnedSingleProjectile, initialCooldownSeconds: 0f);

            var warning = controller.Tick(.01f, new Float2(0f, 0f), new Float2(4f, 3f), true);
            var execute = controller.Tick(.75f, default, new Float2(9f, 9f), true);

            Assert.That(warning.Phase, Is.EqualTo(EnemyAttackPhase.Telegraph));
            Assert.That(warning.LockedTarget, Is.EqualTo(new Float2(4f, 3f)));
            Assert.That(warning.WarningSeconds, Is.GreaterThanOrEqualTo(.7f));
            Assert.That(execute.ExecuteStarted, Is.True);
            Assert.That(execute.LockedTarget, Is.EqualTo(new Float2(4f, 3f)));
        }

        [TestCase("club_dokkaebi", EnemyArchetype.Normal)]
        [TestCase("shield_guard_dokkaebi", EnemyArchetype.ShieldDokkaebi)]
        [TestCase("iron_horn_dokkaebi", EnemyArchetype.ChargingHornGhost)]
        [TestCase("stone_thrower_dokkaebi", EnemyArchetype.StoneThrower)]
        [TestCase("red_horn_elite", EnemyArchetype.RedHornElite)]
        public void EveryDokkaebiPassEnemyHasAnExplicitReadableRole(string contentId, EnemyArchetype expected)
        {
            var profile = EnemyArchetypeProfile.ForContentId(contentId);

            Assert.That(profile.ContentId, Is.EqualTo(contentId));
            Assert.That(profile.Archetype, Is.EqualTo(expected));
            Assert.That(profile.HealthMultiplier, Is.GreaterThan(1f));
        }
    }
}
