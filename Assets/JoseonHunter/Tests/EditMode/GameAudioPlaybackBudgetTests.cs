using System;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Audio;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameAudioPlaybackBudgetTests
    {
        [Test]
        public void ExperienceCuesInsideNineHundredthsAreRejected()
        {
            var budget = new GameAudioPlaybackBudget(12);

            Assert.That(budget.TryReserve(GameAudioCueId.ExperiencePickup, 10f, 0), Is.True);
            Assert.That(budget.TryReserve(GameAudioCueId.ExperiencePickup, 10.08f, 0), Is.False);
            Assert.That(budget.TryReserve(GameAudioCueId.ExperiencePickup, 10.09f, 0), Is.True);
        }

        [Test]
        public void SameWeaponAttackOnlyReservesOnce()
        {
            var budget = new GameAudioPlaybackBudget(12);

            Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, 71, 4f, 0), Is.True);
            Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, 71, 4.2f, 0), Is.False);
            Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, 72, 4.2f, 0), Is.True);
        }

        [Test]
        public void FullPoolRejectsLowPriorityButAllowsBossWarning()
        {
            var budget = new GameAudioPlaybackBudget(12);

            Assert.That(budget.TryReserve(GameAudioCueId.NormalHit, 1f, 12), Is.False);
            Assert.That(budget.TryReserve(GameAudioCueId.BossWarning, 1f, 12), Is.True);
        }

        [Test]
        public void PlayerDefeatAndBossAttacksCanPreemptAFullPool()
        {
            Assert.That(GameAudioPlaybackBudget.PriorityFor(GameAudioCueId.PlayerDefeat),
                Is.EqualTo(GameAudioPriority.High));
            Assert.That(GameAudioPlaybackBudget.PriorityFor(GameAudioCueId.BossSlam),
                Is.EqualTo(GameAudioPriority.High));
            Assert.That(GameAudioPlaybackBudget.PriorityFor(GameAudioCueId.BossCharge),
                Is.EqualTo(GameAudioPriority.High));
            Assert.That(GameAudioPlaybackBudget.PriorityFor(GameAudioCueId.BossVolley),
                Is.EqualTo(GameAudioPriority.High));
        }

        [Test]
        public void RapidPlayerHurtAndAppraisalTicksAreThrottled()
        {
            var budget = new GameAudioPlaybackBudget(12);

            Assert.That(budget.TryReserve(GameAudioCueId.PlayerHurt, 2f, 0), Is.True);
            Assert.That(budget.TryReserve(GameAudioCueId.PlayerHurt, 2.11f, 0), Is.False);
            Assert.That(budget.TryReserve(GameAudioCueId.PlayerHurt, 2.12f, 0), Is.True);
            Assert.That(budget.TryReserve(GameAudioCueId.AppraisalTick, 4f, 0), Is.True);
            Assert.That(budget.TryReserve(GameAudioCueId.AppraisalTick, 4.035f, 0), Is.False);
            Assert.That(budget.TryReserve(GameAudioCueId.AppraisalTick, 4.04f, 0), Is.True);
        }

        [Test]
        public void CueContractHasNoNormalMonsterDeathSound()
        {
            Assert.That(Enum.GetNames(typeof(GameAudioCueId)),
                Does.Not.Contain("NormalEnemyDefeat"));
            Assert.That(Enum.GetNames(typeof(GameAudioCueId)),
                Does.Not.Contain("NormalMonsterDefeat"));
        }

        [Test]
        public void ResetAllowsAnEarlierTimestampAfterRunRestart()
        {
            var budget = new GameAudioPlaybackBudget(12);
            Assert.That(budget.TryReserve(GameAudioCueId.UiClick, 100f, 0), Is.True);

            budget.Reset();

            Assert.That(budget.TryReserve(GameAudioCueId.UiClick, 1f, 0), Is.True);
        }

        [Test]
        public void AttackHistoryRemainsBounded()
        {
            var budget = new GameAudioPlaybackBudget(12);
            for (var index = 1; index <= 80; index++)
                Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, index, index, 0), Is.True);

            Assert.That(budget.TrackedAttackCount, Is.LessThanOrEqualTo(64));
        }
    }
}
