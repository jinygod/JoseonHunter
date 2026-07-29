using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAffixPresentationTests
    {
        [Test]
        public void PercentagePointValueIsNotMultipliedByOneHundredAgain()
        {
            var roll = new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.High, 23.88d);

            Assert.That(WeaponAffixValueFormatter.Describe(roll), Is.EqualTo("Damage +24%"));
        }

        [Test]
        public void CooldownReductionKeepsItsNegativeSign()
        {
            var roll = new WeaponAffixRoll(WeaponAffixStat.Cooldown, WeaponAffixTier.Standard, -8.4d);

            Assert.That(WeaponAffixValueFormatter.Describe(roll), Is.EqualTo("Cooldown -8%"));
        }
    }
}
