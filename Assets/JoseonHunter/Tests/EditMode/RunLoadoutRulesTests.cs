using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class RunLoadoutRulesTests
    {
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        public void Replacement_level_is_one_lower_and_clamped_to_run_bounds(
            int discardedLevel,
            int expectedLevel)
        {
            Assert.That(RunLoadoutRules.ReplacementLevel(discardedLevel), Is.EqualTo(expectedLevel));
        }
    }
}
