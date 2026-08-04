using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponMasteryProgressionTests
    {
        [Test]
        public void EveryWeaponHasBaseAndTwoExistingLegacyStyles()
        {
            Assert.That(WeaponMasteryCatalog.All, Has.Count.EqualTo(WeaponRoster.All.Count));
            foreach (var weapon in WeaponRoster.All)
            {
                var styles = WeaponMasteryCatalog.StylesFor(weapon);
                Assert.That(styles, Has.Count.EqualTo(3), weapon.Value);
                Assert.That(styles[0].IsBase, Is.True, weapon.Value);
                Assert.That(styles.Skip(1).All(style =>
                    WeaponLegacyCatalog.TryGet(style.LegacyPathId, out var definition) &&
                    definition.WeaponId.Equals(weapon)), Is.True, weapon.Value);
            }
        }

        [Test]
        public void StylePurchaseConsumesCoinsButNeverMastery()
        {
            var data = SaveDataV1.CreateDefaults();
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 2000;
            data.Coins = 800;

            var result = new WeaponMasteryProgression(data).Purchase(
                WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSunPiercer);

            Assert.That(result.Success, Is.True);
            Assert.That(data.Coins, Is.Zero);
            Assert.That(data.WeaponMasteryPoints[WeaponId.GakgungShot.Value], Is.EqualTo(2000));
            Assert.That(data.UnlockedWeaponStyles, Contains.Item(WeaponLegacyPathId.GakgungSunPiercer.Value));
        }

        [Test]
        public void LockedStylePurchaseLeavesAllProgressUnchanged()
        {
            var data = SaveDataV1.CreateDefaults();
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 1999;
            data.Coins = 800;

            var result = new WeaponMasteryProgression(data).Purchase(
                WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSunPiercer);

            Assert.That(result.Error, Is.EqualTo(ProgressionError.InsufficientMastery));
            Assert.That(data.Coins, Is.EqualTo(800));
            Assert.That(data.UnlockedWeaponStyles, Is.Empty);
        }

        [Test]
        public void SecondStyleUsesApprovedLongTermThresholdAndPrice()
        {
            var style = WeaponMasteryCatalog.StylesFor(WeaponId.GakgungShot)[2];

            Assert.That(style.RequiredMastery, Is.EqualTo(8000));
            Assert.That(style.CoinCost, Is.EqualTo(2400));
        }
    }
}
