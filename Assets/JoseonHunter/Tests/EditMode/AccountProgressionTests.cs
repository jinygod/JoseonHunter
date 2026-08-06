using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;
using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class AccountProgressionTests
    {
        [TestCase(0f, 0, false, false, 0)]
        [TestCase(900f, 800, true, false, 600)]
        [TestCase(42f, 21, false, true, 3)]
        [TestCase(9000f, 8000, false, false, 350)]
        public void RewardUsesTimeKillsVictoryAndAbandonment(
            float seconds, int kills, bool victory, bool abandoned, int expected)
        {
            var settlement = new RunSettlement(
                new Dictionary<WeaponId, int>(), 0, kills, seconds, victory, abandoned);

            Assert.That(AccountProgression.RewardFor(settlement), Is.EqualTo(expected));
        }

        [Test]
        public void OmenAndGreatOmenMultiplyAccountExperienceAfterBaseReward()
        {
            var omen = new RunSettlement(
                new Dictionary<WeaponId, int>(), 0, 800, 900f, true, false,
                new StageSelection(StageId.GwigokField, StageDifficulty.Omen), 35);
            var greatOmen = new RunSettlement(
                new Dictionary<WeaponId, int>(), 0, 800, 900f, true, false,
                new StageSelection(StageId.GwigokField, StageDifficulty.GreatOmen), 35);

            Assert.That(AccountProgression.RewardFor(omen), Is.EqualTo(750));
            Assert.That(AccountProgression.RewardFor(greatOmen), Is.EqualTo(900));
        }

        [Test]
        public void RequiredExperienceUsesApprovedCurveAtBoundaries()
        {
            Assert.That(AccountProgression.RequiredForNextLevel(1), Is.EqualTo(100));
            Assert.That(AccountProgression.RequiredForNextLevel(2), Is.EqualTo(142));
            Assert.That(AccountProgression.RequiredForNextLevel(20), Is.EqualTo(1582));
        }

        [Test]
        public void LevelTwentyStartsAtTwelveThousandNineHundredFiftyEight()
        {
            Assert.That(AccountProgression.TotalExperienceForLevel(20), Is.EqualTo(12958));
        }

        [Test]
        public void StateCarriesExperienceAcrossLevelBoundaries()
        {
            var state = AccountProgression.StateFor(250);

            Assert.That(state.Level, Is.EqualTo(3));
            Assert.That(state.CurrentLevelExperience, Is.EqualTo(8));
            Assert.That(state.NextLevelRequirement, Is.EqualTo(188));
            Assert.That(state.TotalExperience, Is.EqualTo(250));
            Assert.That(state.IsMaximumLevel, Is.False);
        }

        [Test]
        public void StateNormalizesNegativeExperienceToLevelOne()
        {
            var state = AccountProgression.StateFor(-50);

            Assert.That(state.Level, Is.EqualTo(1));
            Assert.That(state.CurrentLevelExperience, Is.Zero);
            Assert.That(state.NextLevelRequirement, Is.EqualTo(100));
        }

        [Test]
        public void StateClampsExperienceAtMaximumLevel()
        {
            var maximumStart = AccountProgression.TotalExperienceForLevel(100);
            var state = AccountProgression.StateFor(int.MaxValue);

            Assert.That(state.Level, Is.EqualTo(100));
            Assert.That(state.TotalExperience, Is.EqualTo(maximumStart));
            Assert.That(state.CurrentLevelExperience, Is.Zero);
            Assert.That(state.NextLevelRequirement, Is.Zero);
            Assert.That(state.IsMaximumLevel, Is.True);
        }

        [Test]
        public void TryAddCarriesRewardWithoutExceedingMaximumLevel()
        {
            var maximumStart = AccountProgression.TotalExperienceForLevel(100);

            Assert.That(AccountProgression.TryAdd(maximumStart - 10, 100, out var next), Is.True);
            Assert.That(next, Is.EqualTo(maximumStart));
        }

        [TestCase(-1, 10)]
        [TestCase(10, -1)]
        public void TryAddRejectsInvalidInputsWithoutProducingProgress(int current, int reward)
        {
            Assert.That(AccountProgression.TryAdd(current, reward, out var next), Is.False);
            Assert.That(next, Is.Zero);
        }
    }
}
