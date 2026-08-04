using System;
using System.IO;
using System.Globalization;
using JoseonHunter.Domain.Save;
using JoseonHunter.Infrastructure.Save;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class SaveRecoveryTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "JoseonHunterSaveTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void NewInstallLoadsCurrentSchemaDefaults()
        {
            var result = new JsonSaveRepository(directory).Load();

            Assert.That(result.Source, Is.EqualTo(LoadSource.Defaults));
            Assert.That(result.Error, Is.EqualTo(SaveError.None));
            Assert.That(result.Data.SchemaVersion, Is.EqualTo(2));
        }

        [Test]
        public void CorruptCurrentLoadsPreviousValidBackup()
        {
            var repository = new JsonSaveRepository(directory);
            var first = SaveDataV1.CreateDefaults(); first.Coins = 7;
            var second = SaveDataV1.CreateDefaults(); second.Coins = 12;
            repository.Save(first);
            repository.Save(second);
            File.WriteAllText(Path.Combine(directory, "progression.json"), "corrupt");

            var result = repository.Load();

            Assert.That(result.Source, Is.EqualTo(LoadSource.Backup));
            Assert.That(result.Data.Coins, Is.EqualTo(7));
            Assert.That(result.Error, Is.EqualTo(SaveError.Corrupt));
        }

        [Test]
        public void SavingAfterBackupRecoveryPreservesTheValidBackup()
        {
            var repository = new JsonSaveRepository(directory);
            var first = SaveDataV1.CreateDefaults(); first.Coins = 7;
            var second = SaveDataV1.CreateDefaults(); second.Coins = 12;
            repository.Save(first);
            repository.Save(second);
            var currentPath = Path.Combine(directory, "progression.json");
            File.WriteAllText(currentPath, "corrupt");
            var recovered = repository.Load();
            recovered.Data.Coins = 8;

            Assert.That(repository.Save(recovered.Data).Success, Is.True);
            File.WriteAllText(currentPath, "corrupt again");
            var recoveredAgain = repository.Load();

            Assert.That(recoveredAgain.Source, Is.EqualTo(LoadSource.Backup));
            Assert.That(recoveredAgain.Data.Coins, Is.EqualTo(7));
        }

        [Test]
        public void CorruptCurrentAndBackupLoadsSafeDefaults()
        {
            var repository = new JsonSaveRepository(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "progression.json"), "bad");
            File.WriteAllText(Path.Combine(directory, "progression.bak"), "bad");

            var result = repository.Load();

            Assert.That(result.Source, Is.EqualTo(LoadSource.Defaults));
            Assert.That(result.Error, Is.EqualTo(SaveError.Corrupt));
        }

        [Test]
        public void InsufficientStorageKeepsThePreviousValidSave()
        {
            var original = SaveDataV1.CreateDefaults(); original.Coins = 4;
            new JsonSaveRepository(directory).Save(original);
            var replacement = SaveDataV1.CreateDefaults(); replacement.Coins = 99;
            var failing = new JsonSaveRepository(directory, (path, contents) => throw new IOException("disk space exhausted"));

            var result = failing.Save(replacement);
            var loaded = new JsonSaveRepository(directory).Load();

            Assert.That(result.Error, Is.EqualTo(SaveError.InsufficientStorage));
            Assert.That(loaded.Data.Coins, Is.EqualTo(4));
        }

        [Test]
        public void CorruptTemporaryPayloadDoesNotReplaceCurrentSave()
        {
            var original = SaveDataV1.CreateDefaults(); original.Coins = 4;
            new JsonSaveRepository(directory).Save(original);
            var repository = new JsonSaveRepository(directory, (path, contents) => File.WriteAllText(path, contents + "x"));

            var result = repository.Save(SaveDataV1.CreateDefaults());

            Assert.That(result.Error, Is.EqualTo(SaveError.Corrupt));
            Assert.That(new JsonSaveRepository(directory).Load().Data.Coins, Is.EqualTo(4));
        }

        [Test]
        public void ReplaceFailureKeepsCurrentSave()
        {
            var original = SaveDataV1.CreateDefaults(); original.Coins = 4;
            new JsonSaveRepository(directory).Save(original);
            var repository = new JsonSaveRepository(directory, (path, contents) => File.WriteAllText(path, contents), (temporary, current, backup) => throw new IOException("replace failed"));

            var result = repository.Save(SaveDataV1.CreateDefaults());

            Assert.That(result.Error, Is.EqualTo(SaveError.IoFailure));
            Assert.That(new JsonSaveRepository(directory).Load().Data.Coins, Is.EqualTo(4));
        }

        [Test]
        public void ValidButIncompletePayloadNormalizesRequiredEquipmentDefaults()
        {
            Directory.CreateDirectory(directory);
            const string payload = "{\"schemaVersion\":1,\"coins\":5}";
            var envelope = "{\"payload\":\"" + payload.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"checksum\":\"" + SaveChecksum.ForCanonicalPayload(payload) + "\"}";
            File.WriteAllText(Path.Combine(directory, "progression.json"), envelope);

            var result = new JsonSaveRepository(directory).Load();

            Assert.That(result.Source, Is.EqualTo(LoadSource.Current));
            Assert.That(result.Data.EquipmentLevels.Count, Is.EqualTo(12));
        }

        [Test]
        public void CanonicalPayloadIsOrdinalAndCultureIndependent()
        {
            var left = SaveDataV1.CreateDefaults(); left.UnlockedAppearances.Add("I"); left.UnlockedAppearances.Add("i");
            var right = SaveDataV1.CreateDefaults(); right.UnlockedAppearances.Add("i"); right.UnlockedAppearances.Add("I");
            var leftDirectory = Path.Combine(directory, "left"); var rightDirectory = Path.Combine(directory, "right");
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                new JsonSaveRepository(leftDirectory).Save(left);
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                new JsonSaveRepository(rightDirectory).Save(right);
            }
            finally { CultureInfo.CurrentCulture = previous; }

            Assert.That(File.ReadAllText(Path.Combine(leftDirectory, "progression.json")), Is.EqualTo(File.ReadAllText(Path.Combine(rightDirectory, "progression.json"))));
        }

        [Test]
        public void InvestigationPolicyAvailabilityAndSelectionRoundTrip()
        {
            var data = SaveDataV1.CreateDefaults();
            data.SelectableInvestigationPolicies.Add("next_patrol_focus");
            data.SelectedInvestigationPolicy = "next_patrol_focus";
            var repository = new JsonSaveRepository(directory);

            Assert.That(repository.Save(data).Success, Is.True);
            var loaded = repository.Load().Data;

            Assert.That(loaded.SelectableInvestigationPolicies, Is.EqualTo(new[] { "next_patrol_focus" }));
            Assert.That(loaded.SelectedInvestigationPolicy, Is.EqualTo("next_patrol_focus"));
        }
    }
}
