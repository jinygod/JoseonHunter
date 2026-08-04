using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CommonTrainingProgressionTests
    {
        [Test]
        public void FivePurchasesCostApprovedAmountsAndCapAtTenPercent()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 2000;
            var progression = new CommonTrainingProgression(data);

            for (var rank = 0; rank < 5; rank++)
                Assert.That(progression.Purchase(CommonTrainingId.Vitality).Success, Is.True);

            Assert.That(data.Coins, Is.EqualTo(420));
            Assert.That(progression.Multiplier(CommonTrainingId.Vitality), Is.EqualTo(1.10f).Within(.0001f));
            Assert.That(progression.Purchase(CommonTrainingId.Vitality).Error,
                Is.EqualTo(ProgressionError.MaximumReached));
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
