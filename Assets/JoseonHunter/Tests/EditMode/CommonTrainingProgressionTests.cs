using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CommonTrainingProgressionTests
    {
        [Test]
        public void LevelOneAllowsFiveTotalPurchasesAndCapsTheirEffectAtTenPercent()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 2000;
            var progression = new CommonTrainingProgression(data);

            for (var rank = 0; rank < 5; rank++)
                Assert.That(progression.Purchase(CommonTrainingId.Vitality).Success, Is.True);

            Assert.That(data.Coins, Is.EqualTo(420));
            Assert.That(progression.Multiplier(CommonTrainingId.Vitality), Is.EqualTo(1.10f).Within(.0001f));
            Assert.That(progression.Purchase(CommonTrainingId.Vitality).Error,
                Is.EqualTo(ProgressionError.AccountLevelRequired));
        }

        [TestCase(1, 100)]
        [TestCase(5, 600)]
        [TestCase(6, 643)]
        [TestCase(10, 975)]
        [TestCase(15, 1750)]
        [TestCase(20, 2925)]
        public void CostForRankUsesApprovedCurve(int rank, int expected)
        {
            Assert.That(CommonTrainingProgression.CostForRank(rank), Is.EqualTo(expected));
        }

        [TestCase(0, 0f)]
        [TestCase(5, .10f)]
        [TestCase(10, .13f)]
        [TestCase(20, .15f)]
        public void BonusForRankUsesDiminishingReturns(int rank, float expected)
        {
            Assert.That(CommonTrainingProgression.BonusForRank(rank), Is.EqualTo(expected).Within(.0001f));
        }

        [Test]
        public void LevelOneStopsTheSixthTotalPurchaseWithoutChangingCoinsOrRanks()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 10000;
            var progression = new CommonTrainingProgression(data);
            for (var index = 0; index < 5; index++)
                Assert.That(progression.Purchase(CommonTrainingId.Vitality).Success, Is.True);
            var coins = data.Coins;

            var result = progression.Purchase(CommonTrainingId.Power);

            Assert.That(result.Error, Is.EqualTo(ProgressionError.AccountLevelRequired));
            Assert.That(data.Coins, Is.EqualTo(coins));
            Assert.That(data.CommonTrainingRanks[CommonTrainingId.Power.ToString()], Is.Zero);
        }

        [Test]
        public void LevelTwentyAllowsTheHundredthTotalRank()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(20);
            data.Coins = 10000;
            data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Footwork.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Learning.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Guard.ToString()] = 19;
            var progression = new CommonTrainingProgression(data);

            var result = progression.Purchase(CommonTrainingId.Guard);

            Assert.That(result.Success, Is.True);
            Assert.That(progression.TotalRanks, Is.EqualTo(100));
            Assert.That(data.CommonTrainingRanks[CommonTrainingId.Guard.ToString()], Is.EqualTo(20));
        }

        [Test]
        public void PerTrackRankTwentyRemainsTheMaximum()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(20);
            data.Coins = 10000;
            data.CommonTrainingRanks[CommonTrainingId.Resonance.ToString()] = 20;

            var result = new CommonTrainingProgression(data).Purchase(CommonTrainingId.Resonance);

            Assert.That(result.Error, Is.EqualTo(ProgressionError.MaximumReached));
            Assert.That(data.Coins, Is.EqualTo(10000));
        }

        [Test]
        public void TwentyRanksCostTwentyFourThousandSevenHundredAndResetRefundsItExactly()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(20);
            data.Coins = 30000;
            var progression = new CommonTrainingProgression(data);
            for (var index = 0; index < 20; index++)
                Assert.That(progression.Purchase(CommonTrainingId.Vitality).Success, Is.True);

            Assert.That(data.Coins, Is.EqualTo(5300));
            Assert.That(data.CommonTrainingSpentCoins[CommonTrainingId.Vitality.ToString()], Is.EqualTo(24700));
            Assert.That(progression.Reset().Success, Is.True);
            Assert.That(data.Coins, Is.EqualTo(30000));
        }

        [Test]
        public void GuardRankTwentyReducesIncomingDamageByFifteenPercent()
        {
            var data = SaveDataV1.CreateDefaults();
            data.CommonTrainingRanks[CommonTrainingId.Guard.ToString()] = 20;

            Assert.That(new CommonTrainingProgression(data).DamageTakenMultiplier(),
                Is.EqualTo(.85f).Within(.0001f));
        }

        [Test]
        public void SuccessfulPurchasePreservesAccountExperienceThroughCopyTransaction()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(2);
            data.Coins = 1000;

            Assert.That(new CommonTrainingProgression(data).Purchase(CommonTrainingId.Power).Success, Is.True);
            Assert.That(data.AccountExperience, Is.EqualTo(100));
        }

        [Test]
        public void ResetReturnsExactlyRecordedSpendAcrossTracks()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 1000;
            var progression = new CommonTrainingProgression(data);
            progression.Purchase(CommonTrainingId.Vitality);
            progression.Purchase(CommonTrainingId.Vitality);
            progression.Purchase(CommonTrainingId.Power);

            var result = progression.Reset();

            Assert.That(result.Success, Is.True);
            Assert.That(data.Coins, Is.EqualTo(1000));
            Assert.That(data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()], Is.Zero);
            Assert.That(data.CommonTrainingRanks[CommonTrainingId.Power.ToString()], Is.Zero);
            Assert.That(data.CommonTrainingSpentCoins[CommonTrainingId.Vitality.ToString()], Is.Zero);
        }

        [Test]
        public void FailedPurchaseDoesNotPartiallyChangeRankOrCoins()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 99;

            var result = new CommonTrainingProgression(data).Purchase(CommonTrainingId.Guard);

            Assert.That(result.Error, Is.EqualTo(ProgressionError.InsufficientCoins));
            Assert.That(data.Coins, Is.EqualTo(99));
            Assert.That(data.CommonTrainingRanks[CommonTrainingId.Guard.ToString()], Is.Zero);
        }
    }
}
