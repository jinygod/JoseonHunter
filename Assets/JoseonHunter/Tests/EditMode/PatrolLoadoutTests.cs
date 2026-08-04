using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PatrolLoadoutTests
    {
        [Test]
        public void LoadoutCopiesStyleSelectionAndCannotBeMutatedByCaller()
        {
            var styles = new Dictionary<WeaponId, WeaponLegacyPathId>
            {
                [WeaponId.GakgungShot] = WeaponLegacyPathId.GakgungSunPiercer
            };
            var loadout = new PatrolLoadout("각궁 저격", WeaponId.GakgungShot, styles, "normal");

            styles[WeaponId.GakgungShot] = WeaponLegacyPathId.GakgungSplitFletching;

            Assert.That(loadout.Name, Is.EqualTo("각궁 저격"));
            Assert.That(loadout.StyleFor(WeaponId.GakgungShot), Is.EqualTo(WeaponLegacyPathId.GakgungSunPiercer));
            Assert.That(loadout.StyleFor(WeaponId.HwandoFlyingBlade).Value, Is.Null);
        }

        [Test]
        public void LoadoutRejectsUnknownStartingWeapon()
        {
            Assert.That(() => new PatrolLoadout("시험", new WeaponId("unknown"),
                new Dictionary<WeaponId, WeaponLegacyPathId>(), "normal"),
                Throws.ArgumentException);
        }

        [Test]
        public void RunSettlementCopiesMasteryAndRejectsNegativeRewards()
        {
            var mastery = new Dictionary<WeaponId, int> { [WeaponId.FrostFlask] = 9 };
            var settlement = new RunSettlement(mastery, 7, 11, 42f, false, true);
            mastery[WeaponId.FrostFlask] = 99;

            Assert.That(settlement.Mastery[WeaponId.FrostFlask], Is.EqualTo(9));
            Assert.That(() => new RunSettlement(mastery, -1, 0, 0f, false, false),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
