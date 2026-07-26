using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JoseonHunter.Editor.AssetImport;
using NUnit.Framework;
using UnityEditor;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class AssetMigrationValidatorTests
    {
        private const string ManifestDirectory = "Temp/AssetMigrationValidatorTests";
        private const string PixelFixturePath =
            "Assets/JoseonHunter/ValidationFixtures/asset-migration-validator-pixel.png";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(ToAbsolutePath(ManifestDirectory));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(PixelFixturePath);
            DeleteEmptyAssetDirectory("Assets/JoseonHunter/ValidationFixtures");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var manifestDirectory = ToAbsolutePath(ManifestDirectory);
            if (Directory.Exists(manifestDirectory))
            {
                Directory.Delete(manifestDirectory, true);
            }
        }

        [Test]
        public void ValidateReportsDuplicateDestination()
        {
            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", PixelFixturePath),
                Entry("assets/images/monsters/fallen_general_64.png", PixelFixturePath));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("duplicate destination: " + PixelFixturePath));
        }

        [Test]
        public void ValidateReportsDestinationOutsideApprovedRoots()
        {
            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", "Assets/Elsewhere/hero.png"));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("destination outside approved roots: Assets/Elsewhere/hero.png"));
        }

        [Test]
        public void ValidateReportsTraversalOutsideApprovedRoots()
        {
            const string destination = "Assets/JoseonHunter/../../Packages/manifest.json";
            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", destination));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("destination outside approved roots: " + destination));
        }

        [Test]
        public void ValidateTreatsEquivalentDestinationsAsDuplicates()
        {
            const string destination = "Assets/JoseonHunter/Art/Characters/rookie_constable_player_32.png";
            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", destination),
                Entry("assets/images/player/rookie_constable_player_32.png",
                    "Assets/JoseonHunter/Art/Characters/Equivalent/../rookie_constable_player_32.png"));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("duplicate destination: " + destination));
        }

        [Test]
        public void ValidateReportsMissingDestinationFile()
        {
            const string destination = "Assets/JoseonHunter/Art/Characters/missing-validator-file.png";
            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", destination));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("missing destination file: " + destination));
        }

        [Test]
        public void ValidateReportsNonApprovedLicenseStatus()
        {
            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", PixelFixturePath, "pixel", "temporary"));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("license status is not approved: " + PixelFixturePath));
        }

        [Test]
        public void ValidateBlocksApprovedManifestAudioWhenAudioLedgerIsTemporary()
        {
            const string destination = "Assets/JoseonHunter/Audio/SFX/validator-temporary.wav";
            var manifestPath = WriteManifest(
                Entry("assets/audio/sfx/hwando.ogg", destination, "sfx", "approved"));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("audio rights ledger status is not approved: assets/audio/sfx/hwando.ogg"));
        }

        [Test]
        public void ValidateBlocksTemporaryManifestAudioEvenWhenAudioSourceExistsInLedger()
        {
            const string destination = "Assets/JoseonHunter/Audio/SFX/validator-temporary.wav";
            var manifestPath = WriteManifest(
                Entry("assets/audio/sfx/hwando.ogg", destination, "sfx", "temporary"));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("license status is not approved: " + destination));
        }

        [Test]
        public void ValidateReportsPixelProfileWithMipmaps()
        {
            Directory.CreateDirectory(ToAbsolutePath("Assets/JoseonHunter/ValidationFixtures"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.CopyAsset(
                "Assets/JoseonHunter/Art/Characters/rookie_constable_player_32.png", PixelFixturePath);
            AssetDatabase.ImportAsset(PixelFixturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(PixelFixturePath) as TextureImporter;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            var manifestPath = WriteManifest(
                Entry("assets/images/player/rookie_constable_player_32.png", PixelFixturePath));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("pixel profile has mipmaps enabled: " + PixelFixturePath));
        }

        [TestCase("SongMyung-Regular.ttf", "SongMyung-OFL.txt")]
        [TestCase("GowunBatang-Regular.ttf", "GowunBatang-OFL.txt")]
        public void ValidateReportsMissingRequiredFontLicense(string fontName, string licenseName)
        {
            var fontDestination = "Assets/JoseonHunter/Art/Fonts/" + fontName;
            var manifestPath = WriteManifest(
                Entry("assets/fonts/" + fontName, fontDestination, "raw"));

            var errors = AssetMigrationValidator.Validate(manifestPath);

            Assert.That(errors, Does.Contain("missing font license: " + licenseName));
        }

        [Test]
        public void CheckedInManifestIsValid()
        {
            var errors = AssetMigrationValidator.Validate(
                ToAbsolutePath("Tools/AssetMigration/asset-migration-manifest.json"));

            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
        }

        private static string WriteManifest(params ManifestEntry[] entries)
        {
            var path = ToAbsolutePath(
                ManifestDirectory + "/" + Guid.NewGuid().ToString("N") + ".json");
            var jsonEntries = entries.Select(entry => string.Format(
                "{{ \"source\": \"{0}\", \"destination\": \"{1}\", \"profile\": \"{2}\", \"licenseStatus\": \"{3}\" }}",
                entry.Source, entry.Destination, entry.Profile, entry.LicenseStatus));
            File.WriteAllText(path, "{ \"version\": 1, \"entries\": [ " + string.Join(", ", jsonEntries) + " ] }");
            return path;
        }

        private static ManifestEntry Entry(
            string source,
            string destination,
            string profile = "pixel",
            string licenseStatus = "approved")
        {
            return new ManifestEntry(source, destination, profile, licenseStatus);
        }

        private static string ToAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(relativePath).Replace('\\', '/');
        }

        private static void DeleteEmptyAssetDirectory(string assetPath)
        {
            var absolutePath = ToAbsolutePath(assetPath);
            if (Directory.Exists(absolutePath) && Directory.GetFileSystemEntries(absolutePath).Length == 0)
            {
                Directory.Delete(absolutePath);
            }

            var metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath) && !Directory.Exists(absolutePath))
            {
                File.Delete(metaPath);
            }
        }

        private readonly struct ManifestEntry
        {
            public ManifestEntry(string source, string destination, string profile, string licenseStatus)
            {
                Source = source;
                Destination = destination;
                Profile = profile;
                LicenseStatus = licenseStatus;
            }

            public string Source { get; }
            public string Destination { get; }
            public string Profile { get; }
            public string LicenseStatus { get; }
        }
    }
}
