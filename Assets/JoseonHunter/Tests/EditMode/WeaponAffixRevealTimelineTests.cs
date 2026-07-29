using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAffixRevealTimelineTests
    {
        [TestCase(WeaponAffixTier.Standard, 0, .86f)]
        [TestCase(WeaponAffixTier.High, 0, 1.08f)]
        [TestCase(WeaponAffixTier.Perfect, 0, 1.28f)]
        [TestCase(WeaponAffixTier.Standard, 1, 1.38f)]
        [TestCase(WeaponAffixTier.Standard, 2, 1.66f)]
        [TestCase(WeaponAffixTier.Standard, 3, 1.96f)]
        public void Duration_matches_the_pacing_contract(WeaponAffixTier tier, int potentialCount, float expected)
        {
            var timeline = WeaponAffixRevealTimeline.For(Result(tier, potentialCount));
            Assert.That(timeline.Duration, Is.EqualTo(expected));
        }

        [Test]
        public void Awarded_lines_stop_in_order_after_the_affix()
        {
            var timeline = WeaponAffixRevealTimeline.For(Result(WeaponAffixTier.Perfect, 3));
            Assert.That(timeline.AffixStopsAt, Is.GreaterThan(timeline.SpinEndsAt));
            Assert.That(timeline.PotentialStopsAt(0), Is.GreaterThan(timeline.AffixStopsAt));
            Assert.That(timeline.PotentialStopsAt(1), Is.GreaterThan(timeline.PotentialStopsAt(0)));
            Assert.That(timeline.PotentialStopsAt(2), Is.GreaterThan(timeline.PotentialStopsAt(1)));
            Assert.That(timeline.ReadStartsAt, Is.GreaterThan(timeline.PotentialStopsAt(2)));
        }

        [Test]
        public void Unawarded_lines_never_receive_a_stop_time()
        {
            var timeline = WeaponAffixRevealTimeline.For(Result(WeaponAffixTier.Standard, 1));
            Assert.That(timeline.PotentialStopsAt(0), Is.LessThan(float.PositiveInfinity));
            Assert.That(timeline.PotentialStopsAt(1), Is.EqualTo(float.PositiveInfinity));
            Assert.That(timeline.PotentialStopsAt(2), Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void Skip_preserves_final_stop_and_readability()
        {
            var timeline = WeaponAffixRevealTimeline.For(Result(WeaponAffixTier.Perfect, 3));
            Assert.That(timeline.SkipFinishAt(0f), Is.GreaterThan(timeline.PotentialStopsAt(2)));
            Assert.That(timeline.SkipFinishAt(0f), Is.LessThan(timeline.Duration));
            Assert.That(timeline.SkipFinishAt(1.9f), Is.EqualTo(timeline.Duration));
        }

        private static WeaponAffixRollResult Result(WeaponAffixTier tier, int potentialCount)
        {
            var potentials = new WeaponPotentialId[potentialCount];
            for (var index = 0; index < potentialCount; index++)
                potentials[index] = new WeaponPotentialId("timeline_test_" + index);
            return new WeaponAffixRollResult(new WeaponAffixRoll(WeaponAffixStat.Damage, tier, .2d), potentials);
        }
    }
}
