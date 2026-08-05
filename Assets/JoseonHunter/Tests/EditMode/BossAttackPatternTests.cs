using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class BossAttackPatternTests
    {
        [Test]
        public void TelegraphLocksPlayerPositionAndNeverExecutesEarly()
        {
            var controller = new BossAttackController(BossCombatRole.FirstMidBoss, 0f);

            var warning = controller.Tick(.01f, new Float2(0f, 0f), new Float2(3f, 4f), 1f);
            var beforeImpact = controller.Tick(1.08f, new Float2(0f, 0f), new Float2(9f, 9f), 1f);

            Assert.That(warning.Phase, Is.EqualTo(BossAttackPhase.Telegraph));
            Assert.That(warning.Kind, Is.EqualTo(BossAttackKind.SuppressionSlam));
            Assert.That(warning.LockedTarget, Is.EqualTo(new Float2(3f, 4f)));
            Assert.That(beforeImpact.ExecuteStarted, Is.False);
            Assert.That(beforeImpact.LockedTarget, Is.EqualTo(new Float2(3f, 4f)));
        }

        [Test]
        public void TelegraphEmitsExactlyOneExecutionStart()
        {
            var controller = new BossAttackController(BossCombatRole.SecondMidBoss, 0f);
            controller.Tick(.01f, default, new Float2(5f, 0f), 1f);

            var impact = controller.Tick(.95f, default, new Float2(8f, 0f), 1f);
            var continuation = controller.Tick(.01f, default, new Float2(8f, 0f), 1f);

            Assert.That(impact.Kind, Is.EqualTo(BossAttackKind.BloodCharge));
            Assert.That(impact.Phase, Is.EqualTo(BossAttackPhase.Execute));
            Assert.That(impact.ExecuteStarted, Is.True);
            Assert.That(continuation.ExecuteStarted, Is.False);
        }

        [Test]
        public void FinalBossRotatesChargeSlamAndVolley()
        {
            var controller = new BossAttackController(BossCombatRole.FinalBoss, 0f);

            Assert.That(BeginAndFinishAttack(controller), Is.EqualTo(BossAttackKind.BloodCharge));
            Assert.That(BeginAndFinishAttack(controller), Is.EqualTo(BossAttackKind.SuppressionSlam));
            Assert.That(BeginAndFinishAttack(controller), Is.EqualTo(BossAttackKind.SpiritVolley));
            Assert.That(BeginAndFinishAttack(controller), Is.EqualTo(BossAttackKind.BloodCharge));
        }

        [Test]
        public void FinalBossBelowHalfHealthRecoversFasterWithoutShorteningWarningBelowPointSeven()
        {
            var healthy = new BossAttackController(BossCombatRole.FinalBoss, 0f);
            var enraged = new BossAttackController(BossCombatRole.FinalBoss, 0f);

            var healthyWarning = healthy.Tick(.01f, default, new Float2(2f, 0f), 1f);
            var enragedWarning = enraged.Tick(.01f, default, new Float2(2f, 0f), .49f);

            Assert.That(healthyWarning.WarningDurationSeconds, Is.GreaterThanOrEqualTo(.7f));
            Assert.That(enragedWarning.WarningDurationSeconds, Is.GreaterThanOrEqualTo(.7f));
            Assert.That(enraged.RecoveryDurationSeconds(.49f), Is.LessThan(healthy.RecoveryDurationSeconds(1f)));
        }

        private static BossAttackKind BeginAndFinishAttack(BossAttackController controller)
        {
            var warning = controller.Tick(10f, default, new Float2(2f, 0f), 1f);
            var execute = controller.Tick(10f, default, new Float2(3f, 0f), 1f);
            controller.Tick(10f, default, default, 1f);
            controller.Tick(10f, default, default, 1f);
            return execute.ExecuteStarted ? execute.Kind : warning.Kind;
        }
    }
}

