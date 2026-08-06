using System;
using System.Collections.Generic;
using System.IO;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Infrastructure.Save;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class MetaSaveMigrationTests
    {
        private string directory;

        [SetUp]
        public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "JoseonHunterMetaMigration", Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void SchemaOnePayloadMigratesWithoutLosingExistingProgress()
        {
            Directory.CreateDirectory(directory);
            const string payload = "{\"schemaVersion\":1,\"coins\":777,\"equipmentLevels\":[{\"key\":\"weapon_01\",\"value\":4}],\"investigationClues\":[\"clue_03\"]}";
            var envelope = "{\"payload\":\"" + payload.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                           "\",\"checksum\":\"" + SaveChecksum.ForCanonicalPayload(payload) + "\"}";
            File.WriteAllText(Path.Combine(directory, "progression.json"), envelope);

            var loaded = new JsonSaveRepository(directory).Load();

            Assert.That(loaded.Data.SchemaVersion, Is.EqualTo(4));
            Assert.That(loaded.Data.Coins, Is.EqualTo(777));
            Assert.That(loaded.Data.EquipmentLevels["weapon_01"], Is.EqualTo(4));
            Assert.That(loaded.Data.InvestigationClues, Contains.Item("clue_03"));
            Assert.That(loaded.Data.PatrolLoadouts, Has.Count.EqualTo(3));
        }

        [Test]
        public void SchemaFourRoundTripPreservesProgressStageSelectionAndClearRecords()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 321;
            data.AccountExperience = 12958;
            data.WeaponMasteryPoints[WeaponId.FrostFlask.Value] = 4567;
            data.UnlockedWeaponStyles.Add(WeaponLegacyPathId.FrostMist.Value);
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 3;
            data.CommonTrainingSpentCoins[CommonTrainingId.Power.ToString()] = 560;
            data.PatrolLoadouts[1].Name = "빙무 순찰";
            data.PatrolLoadouts[1].StartingWeaponId = WeaponId.FrostFlask.Value;
            data.PatrolLoadouts[1].WeaponStyleIds[WeaponId.FrostFlask.Value] = WeaponLegacyPathId.FrostMist.Value;
            data.ActivePatrolLoadoutIndex = 1;
            data.SelectedStageId = StageId.GwigokField.Value;
            data.SelectedStageDifficulty = StageDifficultyNames.StorageId(StageDifficulty.Omen);
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 612, 35)));

            var repository = new JsonSaveRepository(directory);
            Assert.That(repository.Save(data).Success, Is.True);
            var loaded = repository.Load().Data;

            Assert.That(loaded.SchemaVersion, Is.EqualTo(4));
            Assert.That(loaded.AccountExperience, Is.EqualTo(12958));
            Assert.That(loaded.WeaponMasteryPoints[WeaponId.FrostFlask.Value], Is.EqualTo(4567));
            Assert.That(loaded.UnlockedWeaponStyles, Contains.Item(WeaponLegacyPathId.FrostMist.Value));
            Assert.That(loaded.CommonTrainingRanks[CommonTrainingId.Power.ToString()], Is.EqualTo(3));
            Assert.That(loaded.PatrolLoadouts[1].Name, Is.EqualTo("빙무 순찰"));
            Assert.That(loaded.ActivePatrolLoadoutIndex, Is.EqualTo(1));
            Assert.That(loaded.SelectedStageId, Is.EqualTo(StageId.GwigokField.Value));
            Assert.That(loaded.SelectedStageDifficulty, Is.EqualTo("omen"));
            Assert.That(loaded.StageClearRecords.Count, Is.EqualTo(1));
            Assert.That(loaded.StageClearRecords[0].BestKills, Is.EqualTo(612));
        }

        [Test]
        public void SchemaTwoTrainingMigratesToEnoughAccountExperienceWithoutLosingProgress()
        {
            Directory.CreateDirectory(directory);
            const string payload = "{\"schemaVersion\":2,\"coins\":555,\"commonTrainingRanks\":[{\"key\":\"Vitality\",\"value\":5},{\"key\":\"Power\",\"value\":5},{\"key\":\"Footwork\",\"value\":5},{\"key\":\"Learning\",\"value\":2}]}";
            var envelope = "{\"payload\":\"" + payload.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                           "\",\"checksum\":\"" + SaveChecksum.ForCanonicalPayload(payload) + "\"}";
            File.WriteAllText(Path.Combine(directory, "progression.json"), envelope);

            var loaded = new JsonSaveRepository(directory).Load().Data;

            Assert.That(loaded.SchemaVersion, Is.EqualTo(4));
            Assert.That(AccountProgression.StateFor(loaded.AccountExperience).Level, Is.EqualTo(4));
            Assert.That(loaded.AccountExperience, Is.EqualTo(AccountProgression.TotalExperienceForLevel(4)));
            Assert.That(loaded.CommonTrainingRanks[CommonTrainingId.Learning.ToString()], Is.EqualTo(2));
            Assert.That(loaded.Coins, Is.EqualTo(555));
        }

        [Test]
        public void SaveDataCopyAndCopyFromPreserveAccountExperience()
        {
            var source = SaveDataV1.CreateDefaults();
            source.AccountExperience = 777;
            source.SelectedStageId = StageId.DokkaebiPass.Value;
            source.SelectedStageDifficulty = "normal";
            source.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 400, 30)));

            var copy = source.Copy();
            var destination = SaveDataV1.CreateDefaults();
            destination.CopyFrom(copy);

            Assert.That(copy.AccountExperience, Is.EqualTo(777));
            Assert.That(destination.AccountExperience, Is.EqualTo(777));
            Assert.That(destination.SelectedStageId, Is.EqualTo(StageId.DokkaebiPass.Value));
            Assert.That(destination.StageClearRecords.Count, Is.EqualTo(1));
            Assert.That(destination.StageClearRecords, Is.Not.SameAs(source.StageClearRecords));
            Assert.That(destination.StageClearRecords[0], Is.Not.SameAs(source.StageClearRecords[0]));
        }

        [Test]
        public void SchemaThreeVictoryMigratesToStageOneNormalClear()
        {
            Directory.CreateDirectory(directory);
            const string payload = "{\"schemaVersion\":3,\"bestPatrolResults\":[{\"key\":\"victory_kills\",\"value\":481}]}";
            var envelope = "{\"payload\":\"" + payload.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                           "\",\"checksum\":\"" + SaveChecksum.ForCanonicalPayload(payload) + "\"}";
            File.WriteAllText(Path.Combine(directory, "progression.json"), envelope);

            var loaded = new JsonSaveRepository(directory).Load().Data;

            Assert.That(loaded.SchemaVersion, Is.EqualTo(4));
            Assert.That(loaded.StageClearRecords.Count, Is.EqualTo(1));
            Assert.That(loaded.StageClearRecords[0].StageId, Is.EqualTo(StageId.GwigokField.Value));
            Assert.That(loaded.StageClearRecords[0].Difficulty, Is.EqualTo("normal"));
            Assert.That(loaded.StageClearRecords[0].Victory, Is.True);
            Assert.That(loaded.StageClearRecords[0].BestKills, Is.EqualTo(481));
        }

        [Test]
        public void LockedStageSelectionIsRejectedWithoutSavingOrChangingLiveData()
        {
            var data = SaveDataV1.CreateDefaults();
            var repository = new RecordingRepository();
            var autosave = new AutoSaveOrchestrator(repository, data);

            var result = autosave.SaveStageSelection(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal));

            Assert.That(result.Error, Is.EqualTo(ProgressionError.InvalidSelection));
            Assert.That(repository.SaveCount, Is.Zero);
            Assert.That(data.SelectedStageId, Is.EqualTo(StageId.GwigokField.Value));
            Assert.That(data.SelectedStageDifficulty, Is.EqualTo("normal"));
        }

        [Test]
        public void UnlockedStageSelectionSavesAtomically()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            var repository = new RecordingRepository();
            var autosave = new AutoSaveOrchestrator(repository, data);

            var result = autosave.SaveStageSelection(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal));

            Assert.That(result.Success, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
            Assert.That(data.SelectedStageId, Is.EqualTo(StageId.DokkaebiPass.Value));
            Assert.That(data.SelectedStageDifficulty, Is.EqualTo("normal"));
        }

        [Test]
        public void OmenVictoryAppliesEveryRewardMultiplierAndWritesOneClearRecord()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            var repository = new RecordingRepository();
            var autosave = new AutoSaveOrchestrator(repository, data);
            var settlement = new RunSettlement(
                new Dictionary<WeaponId, int> { [WeaponId.GakgungShot] = 10 },
                10, 800, 900f, true, false,
                new StageSelection(StageId.GwigokField, StageDifficulty.Omen), 35);

            var result = autosave.CommitRun(settlement);

            Assert.That(result.Success, Is.True);
            Assert.That(data.Coins, Is.EqualTo(14));
            Assert.That(data.AccountExperience, Is.EqualTo(750));
            Assert.That(data.WeaponMasteryPoints[WeaponId.GakgungShot.Value], Is.EqualTo(12));
            Assert.That(data.StageClearRecords.Count, Is.EqualTo(2));
            Assert.That(data.StageClearRecords[1].Difficulty, Is.EqualTo("omen"));
            Assert.That(data.StageClearRecords[1].BestKills, Is.EqualTo(800));
            Assert.That(data.StageClearRecords[1].BestLevel, Is.EqualTo(35));
        }

        [Test]
        public void FailedDifficultySettlementRollsBackRewardsAndClearRecord()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            var autosave = new AutoSaveOrchestrator(new AlwaysFailRepository(), data);
            var settlement = new RunSettlement(
                new Dictionary<WeaponId, int> { [WeaponId.GakgungShot] = 10 },
                10, 800, 900f, true, false,
                new StageSelection(StageId.GwigokField, StageDifficulty.Omen), 35);

            var result = autosave.CommitRun(settlement);

            Assert.That(result.SaveError, Is.EqualTo(SaveError.IoFailure));
            Assert.That(data.Coins, Is.Zero);
            Assert.That(data.AccountExperience, Is.Zero);
            Assert.That(data.WeaponMasteryPoints[WeaponId.GakgungShot.Value], Is.Zero);
            Assert.That(data.StageClearRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public void FailedStyleSaveLeavesLiveCoinsAndUnlocksUnchanged()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 800;
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 2000;
            var autosave = new AutoSaveOrchestrator(new AlwaysFailRepository(), data);

            var result = autosave.PurchaseWeaponStyle(
                WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSunPiercer);

            Assert.That(result.SaveError, Is.EqualTo(SaveError.IoFailure));
            Assert.That(data.Coins, Is.EqualTo(800));
            Assert.That(data.UnlockedWeaponStyles, Is.Empty);
        }

        [Test]
        public void SavingLoadoutRejectsLockedStyleWithoutReplacingPreset()
        {
            var data = SaveDataV1.CreateDefaults();
            var originalName = data.PatrolLoadouts[0].Name;
            var autosave = new AutoSaveOrchestrator(new RecordingRepository(), data);
            var locked = new PatrolLoadout(
                "잠긴 각궁",
                WeaponId.GakgungShot,
                new Dictionary<WeaponId, WeaponLegacyPathId>
                {
                    [WeaponId.GakgungShot] = WeaponLegacyPathId.GakgungSunPiercer
                },
                "normal");

            var result = autosave.SaveLoadout(0, locked);

            Assert.That(result.Error, Is.EqualTo(ProgressionError.InvalidSelection));
            Assert.That(data.PatrolLoadouts[0].Name, Is.EqualTo(originalName));
        }

        [Test]
        public void RunSettlementAddsEveryRewardOnceThroughAtomicSave()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 10;
            var repository = new RecordingRepository();
            var autosave = new AutoSaveOrchestrator(repository, data);
            var settlement = new RunSettlement(
                new Dictionary<WeaponId, int> { [WeaponId.FrostFlask] = 9 },
                7, 21, 42f, false, true);

            var result = autosave.CommitRun(settlement);

            Assert.That(result.Success, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
            Assert.That(data.Coins, Is.EqualTo(17));
            Assert.That(data.WeaponMasteryPoints[WeaponId.FrostFlask.Value], Is.EqualTo(9));
            Assert.That(data.BestPatrolResults["patrol_kills"], Is.EqualTo(21));
            Assert.That(data.AccountExperience, Is.EqualTo(3));
        }

        [Test]
        public void FailedRunSaveLeavesEveryLiveRewardIncludingAccountExperienceUnchanged()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 10;
            data.AccountExperience = 77;
            var autosave = new AutoSaveOrchestrator(new AlwaysFailRepository(), data);
            var settlement = new RunSettlement(
                new Dictionary<WeaponId, int> { [WeaponId.GakgungShot] = 4 },
                5, 800, 900f, true, false);

            var result = autosave.CommitRun(settlement);

            Assert.That(result.SaveError, Is.EqualTo(SaveError.IoFailure));
            Assert.That(data.Coins, Is.EqualTo(10));
            Assert.That(data.AccountExperience, Is.EqualTo(77));
            Assert.That(data.WeaponMasteryPoints[WeaponId.GakgungShot.Value], Is.Zero);
        }

        private sealed class AlwaysFailRepository : ISaveRepository
        {
            public LoadResult Load() => new LoadResult(SaveDataV1.CreateDefaults(), LoadSource.Defaults, SaveError.None);
            public SaveResult Save(SaveDataV1 data) => new SaveResult(false, SaveError.IoFailure);
        }

        private sealed class RecordingRepository : ISaveRepository
        {
            public int SaveCount { get; private set; }
            public LoadResult Load() => new LoadResult(SaveDataV1.CreateDefaults(), LoadSource.Defaults, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                SaveCount++;
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
