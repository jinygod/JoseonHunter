using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class ExperiencePickupBudgetTests
    {
        [TestCase(1, ExperiencePickupTier.Small)]
        [TestCase(4, ExperiencePickupTier.Small)]
        [TestCase(5, ExperiencePickupTier.Medium)]
        [TestCase(19, ExperiencePickupTier.Medium)]
        [TestCase(20, ExperiencePickupTier.Large)]
        [TestCase(999, ExperiencePickupTier.Large)]
        public void ValueTiersUseReadableBoundaries(int value, ExperiencePickupTier expected)
        {
            Assert.That(ExperiencePickupBudget.TierFor(value), Is.EqualTo(expected));
        }

        [Test]
        public void MergeChoosesNearestPickupWithinTheBoundedActiveSet()
        {
            var positions = new List<Float2>
            {
                new Float2(-5f, 0f),
                new Float2(1f, 1f),
                new Float2(8f, 0f)
            };

            Assert.That(ExperiencePickupBudget.FindNearestMergeIndex(positions, new Float2(1.2f, .8f)),
                Is.EqualTo(1));
        }

        [Test]
        public void MergePreservesAllExperienceValue()
        {
            Assert.That(ExperiencePickupBudget.MergeValue(17, 9), Is.EqualTo(26));
        }

        [Test]
        public void MobilePickupBudgetIsHardCappedAtOneHundredEighty()
        {
            Assert.That(ExperiencePickupBudget.MaximumActivePickups, Is.EqualTo(180));
            Assert.That(ExperiencePickupBudget.ShouldMerge(180), Is.True);
            Assert.That(ExperiencePickupBudget.ShouldMerge(179), Is.False);
        }
    }
}

