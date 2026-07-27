using System;
using System.IO;
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
        public void NewInstallLoadsSchemaOneDefaults()
        {
            var result = new JsonSaveRepository(directory).Load();

            Assert.That(result.Source, Is.EqualTo(LoadSource.Defaults));
            Assert.That(result.Error, Is.EqualTo(SaveError.None));
            Assert.That(result.Data.SchemaVersion, Is.EqualTo(1));
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
    }
}
