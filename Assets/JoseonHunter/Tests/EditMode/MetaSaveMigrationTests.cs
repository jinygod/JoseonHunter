using System;
using System.Collections.Generic;
using System.IO;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
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

            Assert.That(loaded.Data.SchemaVersion, Is.EqualTo(2));
            Assert.That(loaded.Data.Coins, Is.EqualTo(777));
            Assert.That(loaded.Data.EquipmentLevels["weapon_01"], Is.EqualTo(4));
            Assert.That(loaded.Data.InvestigationClues, Contains.Item("clue_03"));
            Assert.That(loaded.Data.PatrolLoadouts, Has.Count.EqualTo(3));
        }

        [Test]
        public void SchemaTwoRoundTripPreservesMasteryStylesTrainingAndLoadouts()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 321;
            data.WeaponMasteryPoints[WeaponId.FrostFlask.Value] = 4567;
            data.UnlockedWeaponStyles.Add(WeaponLegacyPathId.FrostMist.Value);
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 3;
            data.CommonTrainingSpentCoins[CommonTrainingId.Power.ToString()] = 560;
            data.PatrolLoadouts[1].Name = "빙무 순찰";
            data.PatrolLoadouts[1].StartingWeaponId = WeaponId.FrostFlask.Value;
            data.PatrolLoadouts[1].WeaponStyleIds[WeaponId.FrostFlask.Value] = WeaponLegacyPathId.FrostMist.Value;
            data.ActivePatrolLoadoutIndex = 1;

            var repository = new JsonSaveRepository(directory);
            Assert.That(repository.Save(data).Success, Is.True);
            var loaded = repository.Load().Data;

            Assert.That(loaded.SchemaVersion, Is.EqualTo(2));
            Assert.That(loaded.WeaponMasteryPoints[WeaponId.FrostFlask.Value], Is.EqualTo(4567));
            Assert.That(loaded.UnlockedWeaponStyles, Contains.Item(WeaponLegacyPathId.FrostMist.Value));
            Assert.That(loaded.CommonTrainingRanks[CommonTrainingId.Power.ToString()], Is.EqualTo(3));
            Assert.That(loaded.PatrolLoadouts[1].Name, Is.EqualTo("빙무 순찰"));
            Assert.That(loaded.ActivePatrolLoadoutIndex, Is.EqualTo(1));
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
