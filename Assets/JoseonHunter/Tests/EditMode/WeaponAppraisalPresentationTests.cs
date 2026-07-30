using System;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
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
        public void ApprovedAutomaticDurationsStayShort()
        {
            Assert.That(WeaponAffixRevealTimeline.For(Result()).Duration, Is.InRange(1.2f, 1.55f));
            Assert.That(WeaponAffixRevealTimeline.For(Result(WeaponPotentialId.HwandoVenomFang)).Duration,
                Is.LessThanOrEqualTo(2.1f));
            Assert.That(WeaponAffixRevealTimeline.For(Result(
                WeaponPotentialId.HwandoVenomFang,
                WeaponPotentialId.HwandoReturningAfterimage,
                WeaponPotentialId.HwandoFlyingBladeDance)).Duration, Is.LessThanOrEqualTo(2.4f));
        }

        private static WeaponAffixRollResult Result(params WeaponPotentialId[] potentials) =>
            new(new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 23.88d),
                Array.AsReadOnly(potentials));
    }
}
