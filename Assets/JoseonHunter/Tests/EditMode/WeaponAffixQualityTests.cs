using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAffixQualityTests
    {
        [TestCase(0f, WeaponAffixQualityBand.Ash)]
        [TestCase(.299f, WeaponAffixQualityBand.Ash)]
        [TestCase(.30f, WeaponAffixQualityBand.Green)]
        [TestCase(.50f, WeaponAffixQualityBand.Blue)]
        [TestCase(.70f, WeaponAffixQualityBand.Crimson)]
        [TestCase(.90f, WeaponAffixQualityBand.Gold)]
        [TestCase(2f, WeaponAffixQualityBand.Gold)]
        public void BandForUsesApprovedBoundaries(float score, WeaponAffixQualityBand expected)
        {
            Assert.That(WeaponAffixQuality.BandFor(score), Is.EqualTo(expected));
        }

        [Test]
        public void ScoreReturnsZeroWithoutRolls()
        {
            Assert.That(WeaponAffixQuality.Score(null), Is.Zero);
            Assert.That(WeaponAffixQuality.Score(System.Array.Empty<WeaponAffixRoll>()), Is.Zero);
        }

        [Test]
        public void ScoreAveragesNormalizedActualValuesAcrossStats()
        {
            var rolls = new[]
            {
                new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 10d),
                new WeaponAffixRoll(WeaponAffixStat.Area, WeaponAffixTier.Perfect, 20d),
                new WeaponAffixRoll(WeaponAffixStat.Cooldown, WeaponAffixTier.High, -8.5d)
            };

            Assert.That(WeaponAffixQuality.Score(rolls), Is.EqualTo(.5f).Within(.0001f));
        }

        [Test]
        public void ScoreClampsValuesOutsideAuthoredRange()
        {
            var rolls = new[]
            {
                new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 0d),
                new WeaponAffixRoll(WeaponAffixStat.Duration, WeaponAffixTier.Perfect, 100d)
            };

            Assert.That(WeaponAffixQuality.Score(rolls), Is.EqualTo(.5f).Within(.0001f));
        }
    }
}
