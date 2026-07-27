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
        public void EvolutionResetRejectsCoinOverflowWithoutChangingState()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = int.MaxValue;
            data.EvolutionNodeRanks["node_01"] = 1;
            data.EvolutionSpentCoins["node_01"] = 1;

            var result = new EvolutionBoard(data).Reset();

            Assert.That(result.Error, Is.EqualTo(ProgressionError.InvalidAmount));
            Assert.That(data.Coins, Is.EqualTo(int.MaxValue));
            Assert.That(data.EvolutionNodeRanks["node_01"], Is.EqualTo(1));
            Assert.That(data.EvolutionSpentCoins["node_01"], Is.EqualTo(1));
        }

        [Test]
        public void InvestigationGivesUniqueCluesAndUnlocksMilestonesOnce()
        {
            var data = SaveDataV1.CreateDefaults();
            var investigation = new InvestigationCase(data);
            for (var index = 0; index < 9; index++) investigation.CompletePatrol(0);

            Assert.That(data.InvestigationClues.Count, Is.EqualTo(9));
            Assert.That(data.ClaimedInvestigationMilestones.Count, Is.EqualTo(3));
            Assert.That(data.UnlockedRecipes, Does.Contain("hwando_evolution"));
            Assert.That(data.UnlockedHeroes, Does.Contain("shaman"));
            Assert.That(data.UnlockedDifficulties, Does.Contain("hard"));
        }

        [Test]
        public void InvestigationSelectionUsesOrdinalUndiscoveredCluesAndExactMilestoneRewards()
        {
            var data = SaveDataV1.CreateDefaults();
            data.InvestigationClues.Add("clue_09"); data.InvestigationClues.Add("clue_02");
            var investigation = new InvestigationCase(data);

            var result = investigation.CompletePatrol(0);

            Assert.That(result.Success, Is.True);
            Assert.That(data.InvestigationClues, Does.Contain("clue_01"));
            Assert.That(investigation.CompletePatrol(99).Error, Is.EqualTo(ProgressionError.InvalidSelection));
            investigation.CompletePatrol(0); investigation.CompletePatrol(0);
            Assert.That(data.FirstSolutionFlags, Does.Contain("fallen_general_first_weakness"));
            Assert.That(data.MonsterCompendiumEntries, Does.Contain("fallen_general_expanded"));
            for (var index = data.InvestigationClues.Count; index < 6; index++) investigation.CompletePatrol(0);
            Assert.That(data.SelectableInvestigationPolicies, Does.Contain("next_patrol_focus"));
            Assert.That(investigation.SelectPolicy("next_patrol_focus").Success, Is.True);
            Assert.That(data.SelectedInvestigationPolicy, Is.EqualTo("next_patrol_focus"));
        }

        [Test]
        public void FailedAutosaveLeavesLiveEquipmentAndEvolutionUnchanged()
        {
            var data = SaveDataV1.CreateDefaults(); data.Coins = 10;
            var autosave = new AutoSaveOrchestrator(new FailingRepository(), data);

            var equipment = autosave.PurchaseEquipment("weapon_01", 5);
            var evolution = autosave.PurchaseEvolution("node_01", 3);

            Assert.That(equipment.SaveError, Is.EqualTo(SaveError.IoFailure));
            Assert.That(evolution.SaveError, Is.EqualTo(SaveError.IoFailure));
            Assert.That(data.Coins, Is.EqualTo(10));
            Assert.That(data.EquipmentLevels["weapon_01"], Is.EqualTo(0));
            Assert.That(data.EvolutionNodeRanks["node_01"], Is.EqualTo(0));
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

        private sealed class FailingRepository : ISaveRepository
        {
            public LoadResult Load() { return new LoadResult(SaveDataV1.CreateDefaults(), LoadSource.Defaults, SaveError.None); }
            public SaveResult Save(SaveDataV1 data) { return new SaveResult(false, SaveError.IoFailure); }
        }
    }
}
