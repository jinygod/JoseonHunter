using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAffixRevealTimelineTests
    {
        [TestCase(WeaponAffixTier.Standard, 0, 1.25f)]
        [TestCase(WeaponAffixTier.High, 0, 1.45f)]
        [TestCase(WeaponAffixTier.Perfect, 0, 1.55f)]
        [TestCase(WeaponAffixTier.Standard, 1, 2.10f)]
        [TestCase(WeaponAffixTier.Standard, 2, 2.28f)]
        [TestCase(WeaponAffixTier.Standard, 3, 2.40f)]
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
        public void Appraisal_has_a_readable_count_up_and_lock_window()
        {
            var timeline = WeaponAffixRevealTimeline.For(Result(WeaponAffixTier.Standard, 0));
            Assert.That(timeline.SpinEndsAt, Is.GreaterThanOrEqualTo(.4f));
            Assert.That(timeline.AffixStopsAt - timeline.SpinEndsAt, Is.GreaterThanOrEqualTo(.3f));
            Assert.That(timeline.Duration, Is.GreaterThanOrEqualTo(1.2f));
        }

        [Test]
        public void Reel_motion_decelerates_before_the_stop()
        {
            const float spinEndsAt = 1.2f;
            const float stopAt = 1.8f;
            var earlyDistance = WeaponAffixReelMotion.TravelAt(1.4f, spinEndsAt, stopAt, 0) -
                WeaponAffixReelMotion.TravelAt(1.2f, spinEndsAt, stopAt, 0);
            var lateDistance = WeaponAffixReelMotion.TravelAt(1.8f, spinEndsAt, stopAt, 0) -
                WeaponAffixReelMotion.TravelAt(1.6f, spinEndsAt, stopAt, 0);
            Assert.That(lateDistance, Is.LessThan(earlyDistance));
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
            Assert.That(timeline.SkipFinishAt(0f), Is.GreaterThanOrEqualTo(1.1f));
            Assert.That(timeline.SkipFinishAt(0f), Is.LessThan(timeline.Duration));
            Assert.That(timeline.SkipFinishAt(3.3f), Is.EqualTo(timeline.Duration));
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
