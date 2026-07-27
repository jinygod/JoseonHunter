using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class MetaProgressionTests
    {
        [Test]
        public void EquipmentHasFourSlotsAndTwelveItems()
        {
            var progression = new EquipmentProgression(SaveDataV1.CreateDefaults());
            Assert.That(progression.SlotCount, Is.EqualTo(4));
            Assert.That(progression.ItemCount, Is.EqualTo(12));
        }

        [Test]
        public void EquipmentPurchaseCannotMakeCoinsNegative()
        {
            var data = SaveDataV1.CreateDefaults();
            var result = new EquipmentProgression(data).PurchaseLevel("weapon_01", 1);

            Assert.That(result.Error, Is.EqualTo(ProgressionError.InsufficientCoins));
            Assert.That(data.Coins, Is.EqualTo(0));
        }

        [Test]
        public void QualityUpgradeUsesSelectedFragmentsInsteadOfThreeItemMerge()
        {
            var data = SaveDataV1.CreateDefaults();
            data.EquipmentFragments["weapon_01"] = 10;
            var result = new EquipmentProgression(data).UpgradeQuality("weapon_01", 3);

            Assert.That(result.Success, Is.True);
            Assert.That(data.EquipmentFragments["weapon_01"], Is.EqualTo(7));
            Assert.That(data.EquipmentQualities["weapon_01"], Is.EqualTo(1));
        }

        [Test]
        public void EvolutionHasTwelveNodesAndFreeResetRefundsSpentCoins()
        {
            var data = SaveDataV1.CreateDefaults(); data.Coins = 10;
            var board = new EvolutionBoard(data);
            Assert.That(board.NodeCount, Is.EqualTo(12));
            board.Purchase("node_01", 5);

            var result = board.Reset();

            Assert.That(result.Success, Is.True);
            Assert.That(data.Coins, Is.EqualTo(10));
            Assert.That(data.EvolutionNodeRanks["node_01"], Is.EqualTo(0));
        }

        [Test]
        public void EvolutionResetRefundsEachVariablePurchaseCost()
        {
            var data = SaveDataV1.CreateDefaults(); data.Coins = 20;
            var board = new EvolutionBoard(data);
            board.Purchase("node_01", 3);
            board.Purchase("node_02", 7);

            board.Reset();

            Assert.That(data.Coins, Is.EqualTo(20));
            Assert.That(data.EvolutionSpentCoins["node_01"], Is.EqualTo(0));
            Assert.That(data.EvolutionSpentCoins["node_02"], Is.EqualTo(0));
        }

        [Test]
        public void InvestigationGivesUniqueCluesAndUnlocksMilestonesOnce()
        {
            var data = SaveDataV1.CreateDefaults();
            var investigation = new InvestigationCase(data);
            for (var index = 0; index < 9; index++) investigation.CompletePatrol(index);

            Assert.That(data.InvestigationClues.Count, Is.EqualTo(9));
            Assert.That(data.ClaimedInvestigationMilestones.Count, Is.EqualTo(3));
            Assert.That(data.UnlockedRecipes, Does.Contain("hwando_evolution"));
            Assert.That(data.UnlockedHeroes, Does.Contain("shaman"));
            Assert.That(data.UnlockedDifficulties, Does.Contain("hard"));
        }

        [Test]
        public void EverySpecifiedAutoSaveTriggerSavesTheCurrentData()
        {
            var repository = new RecordingRepository();
            var data = SaveDataV1.CreateDefaults();
            var autosave = new AutoSaveOrchestrator(repository);

            foreach (AutoSaveTrigger trigger in new[] { AutoSaveTrigger.RunResult, AutoSaveTrigger.EquipmentPurchase, AutoSaveTrigger.EvolutionPurchase, AutoSaveTrigger.SettingsChanged, AutoSaveTrigger.AppPaused })
            {
                Assert.That(autosave.SaveFor(trigger, data).Success, Is.True);
            }

            Assert.That(repository.SaveCount, Is.EqualTo(5));
            Assert.That(repository.LastSaved, Is.SameAs(data));
        }

        private sealed class RecordingRepository : ISaveRepository
        {
            public int SaveCount { get; private set; }
            public SaveDataV1 LastSaved { get; private set; }
            public LoadResult Load() { return new LoadResult(SaveDataV1.CreateDefaults(), LoadSource.Defaults, SaveError.None); }
            public SaveResult Save(SaveDataV1 data) { SaveCount++; LastSaved = data; return new SaveResult(true, SaveError.None); }
        }
    }
}
