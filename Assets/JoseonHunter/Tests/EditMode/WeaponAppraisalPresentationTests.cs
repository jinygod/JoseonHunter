using System;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAppraisalPresentationTests
    {
        [TestCase(23.88d, 0f, 0)]
        [TestCase(23.88d, 1f, 24)]
        [TestCase(-8.4d, 1f, -8)]
        public void DisplayValueMovesFromZeroToRoundedTarget(double target, float progress, int expected)
        {
            Assert.That(WeaponAppraisalPresentation.DisplayValueAt(target, progress), Is.EqualTo(expected));
        }

        [Test]
        public void DisplayValueMovesMonotonicallyTowardPositiveTarget()
        {
            var previous = 0;
            for (var step = 1; step <= 10; step++)
            {
                var current = WeaponAppraisalPresentation.DisplayValueAt(23.88d, step / 10f);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
                previous = current;
            }
        }

        [Test]
        public void ExistingPotentialStaysRevealed()
        {
            var timeline = WeaponAffixRevealTimeline.For(Result());

            Assert.That(WeaponAppraisalPresentation.ResolveSlot(0, 1, 0, .5f, timeline),
                Is.EqualTo(WeaponPotentialSlotKind.Existing));
        }

        [Test]
        public void FailedPotentialShakesThenSettlesAsEmpty()
        {
            var timeline = WeaponAffixRevealTimeline.For(Result());

            Assert.That(WeaponAppraisalPresentation.ResolveSlot(0, 0, 0, timeline.AffixStopsAt + .05f, timeline),
                Is.EqualTo(WeaponPotentialSlotKind.Shaking));
            Assert.That(WeaponAppraisalPresentation.ResolveSlot(0, 0, 0, timeline.ReadStartsAt, timeline),
                Is.EqualTo(WeaponPotentialSlotKind.Empty));
        }

        [Test]
        public void AwardedPotentialRevealsAtItsOwnStop()
        {
            var result = Result(WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage);
            var timeline = WeaponAffixRevealTimeline.For(result);

            Assert.That(WeaponAppraisalPresentation.ResolveSlot(1, 0, 2,
                    timeline.PotentialStopsAt(1) - .01f, timeline),
                Is.EqualTo(WeaponPotentialSlotKind.Shaking));
            Assert.That(WeaponAppraisalPresentation.ResolveSlot(1, 0, 2,
                    timeline.PotentialStopsAt(1), timeline),
                Is.EqualTo(WeaponPotentialSlotKind.Revealed));
        }

        [Test]
        public void AutomaticDurationsReserveTheCountUpAndRemainUnderThreeSeconds()
        {
            Assert.That(WeaponAffixRevealTimeline.For(Result()).Duration, Is.InRange(1.8f, 2.1f));
            Assert.That(WeaponAffixRevealTimeline.For(Result(WeaponPotentialId.HwandoVenomFang)).Duration,
                Is.LessThanOrEqualTo(2.3f));
            Assert.That(WeaponAffixRevealTimeline.For(Result(
                WeaponPotentialId.HwandoVenomFang,
                WeaponPotentialId.HwandoReturningAfterimage,
                WeaponPotentialId.HwandoFlyingBladeDance)).Duration, Is.LessThanOrEqualTo(2.7f));
        }

        [Test]
        public void FirstAcquisitionUsesFullScrollReveal()
        {
            var model = Model(1, ProgressionRewardKind.NewWeapon, WeaponAffixTier.Standard);

            Assert.That(WeaponAppraisalPresentation.ProfileFor(model),
                Is.EqualTo(WeaponAppraisalRevealProfile.FirstAcquisition));
            Assert.That(WeaponAppraisalPresentation.ScrollOpenAt(
                WeaponAppraisalRevealProfile.FirstAcquisition, 0f), Is.LessThan(.15f));
            Assert.That(WeaponAppraisalPresentation.ScrollOpenAt(
                WeaponAppraisalRevealProfile.FirstAcquisition, .4f), Is.EqualTo(1f));
            Assert.That(WeaponAffixRevealTimeline.For(model).Duration, Is.EqualTo(1.84f).Within(.001f));
        }

        [Test]
        public void RepeatStandardUsesFastPartiallyOpenReveal()
        {
            var model = Model(2, ProgressionRewardKind.WeaponLevel, WeaponAffixTier.Standard);

            Assert.That(WeaponAppraisalPresentation.ProfileFor(model),
                Is.EqualTo(WeaponAppraisalRevealProfile.RepeatStandard));
            Assert.That(WeaponAppraisalPresentation.ScrollOpenAt(
                WeaponAppraisalRevealProfile.RepeatStandard, 0f), Is.GreaterThan(.5f));
            Assert.That(WeaponAffixRevealTimeline.For(model).Duration, Is.EqualTo(1.58f).Within(.001f));
        }

        [Test]
        public void RareRepeatUpgradeUsesCeremonialReveal()
        {
            var model = Model(3, ProgressionRewardKind.WeaponLevel, WeaponAffixTier.High);

            Assert.That(WeaponAppraisalPresentation.ProfileFor(model),
                Is.EqualTo(WeaponAppraisalRevealProfile.Ceremonial));
            Assert.That(WeaponAffixRevealTimeline.For(model).Duration, Is.EqualTo(2.03f).Within(.001f));
        }

        private static WeaponAffixRollResult Result(params WeaponPotentialId[] potentials) =>
            new(new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 23.88d),
                Array.AsReadOnly(potentials));

        private static WeaponAppraisalViewModel Model(
            int level,
            ProgressionRewardKind kind,
            WeaponAffixTier tier)
        {
            var result = new WeaponAffixRollResult(
                new WeaponAffixRoll(WeaponAffixStat.Damage, tier, 23.88d),
                Array.Empty<WeaponPotentialId>());
            var reward = new ProgressionRewardEvent(
                "hwando_flying_blade", "hwando_flying_blade", level, kind,
                "Hwando Flying Blade", "Level " + level, null, result);
            var slot = new WeaponSlotView(
                "hwando_flying_blade", "Hwando Flying Blade", level, null,
                "Damage +24%", behavior: "Returning blade");
            return WeaponAppraisalViewModel.From(reward, slot);
        }
    }
}
