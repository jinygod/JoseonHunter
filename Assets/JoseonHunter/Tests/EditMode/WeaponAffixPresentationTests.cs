using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
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

        [Test]
        public void AppraisalModelCombinesRewardWithCurrentWeaponState()
        {
            var result = new WeaponAffixRollResult(
                new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.High, 23.88d),
                new[] { WeaponPotentialId.GakgungFullDraw });
            var reward = new ProgressionRewardEvent(
                "gakgung_shot", "gakgung_shot", 3, ProgressionRewardKind.WeaponLevel,
                "각궁", "레벨 3", null, result);
            var slot = new WeaponSlotView(
                "gakgung_shot", "각궁", 3, null, "Damage +24%",
                new[] { WeaponPotentialId.GakgungSplitFletching, WeaponPotentialId.GakgungFullDraw },
                behavior: "적을 관통하는 화살");

            var model = WeaponAppraisalViewModel.From(reward, slot);

            Assert.That(model.WeaponId, Is.EqualTo("gakgung_shot"));
            Assert.That(model.DisplayName, Is.EqualTo("각궁"));
            Assert.That(model.Behavior, Is.EqualTo("적을 관통하는 화살"));
            Assert.That(model.ExistingPotentialCount, Is.EqualTo(1));
            Assert.That(model.CurrentPotentials, Has.Count.EqualTo(2));
            Assert.That(model.IsNewAcquisition, Is.False);
            Assert.That(model.AccumulatedAffixSummary, Is.EqualTo("Damage +24%"));
        }
    }
}
