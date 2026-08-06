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
