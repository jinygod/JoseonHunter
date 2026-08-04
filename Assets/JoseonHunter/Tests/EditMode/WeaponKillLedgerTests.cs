using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponKillLedgerTests
    {
        [Test]
        public void LastConfirmedWeaponOwnsTheKill()
        {
            var ledger = new RunWeaponKillLedger();
            ledger.RecordHit(7, WeaponId.HwandoFlyingBlade);
            ledger.RecordHit(7, WeaponId.GakgungShot);

            ledger.ConfirmDeath(7, EnemyMasteryClass.Normal);

            Assert.That(ledger.PointsFor(WeaponId.HwandoFlyingBlade), Is.Zero);
            Assert.That(ledger.PointsFor(WeaponId.GakgungShot), Is.EqualTo(1));
        }

        [TestCase(EnemyMasteryClass.Normal, 1)]
        [TestCase(EnemyMasteryClass.Special, 3)]
        [TestCase(EnemyMasteryClass.Elite, 10)]
        [TestCase(EnemyMasteryClass.MidBoss, 30)]
        [TestCase(EnemyMasteryClass.FinalBoss, 100)]
        public void DeathClassMapsToApprovedPoints(EnemyMasteryClass enemyClass, int expected)
        {
            var ledger = new RunWeaponKillLedger();
            ledger.RecordHit(11, WeaponId.HwandoFlyingBlade);

            Assert.That(ledger.ConfirmDeath(11, enemyClass), Is.EqualTo(expected));
            Assert.That(ledger.PointsFor(WeaponId.HwandoFlyingBlade), Is.EqualTo(expected));
        }

        [Test]
        public void DuplicateDeathAndUnattributedDamageNeverAwardPoints()
        {
            var ledger = new RunWeaponKillLedger();
            ledger.RecordHit(3, WeaponId.FrostFlask);
            Assert.That(ledger.ConfirmDeath(3, EnemyMasteryClass.Special), Is.EqualTo(3));
            Assert.That(ledger.ConfirmDeath(3, EnemyMasteryClass.Special), Is.Zero);

            ledger.RecordHit(4, WeaponId.FrostFlask);
            ledger.ForgetTarget(4);
            Assert.That(ledger.ConfirmDeath(4, EnemyMasteryClass.FinalBoss), Is.Zero);
            Assert.That(ledger.PointsFor(WeaponId.FrostFlask), Is.EqualTo(3));
        }

        [Test]
        public void SnapshotIsDetachedAndResetClearsRunState()
        {
            var ledger = new RunWeaponKillLedger();
            ledger.RecordHit(5, WeaponId.ThunderCrashBomb);
            ledger.ConfirmDeath(5, EnemyMasteryClass.Elite);
            var snapshot = ledger.Snapshot();

            ledger.Reset();

            Assert.That(snapshot[WeaponId.ThunderCrashBomb], Is.EqualTo(10));
            Assert.That(ledger.PointsFor(WeaponId.ThunderCrashBomb), Is.Zero);
        }
    }
}
