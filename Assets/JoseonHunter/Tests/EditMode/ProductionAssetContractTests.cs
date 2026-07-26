using System;
using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class ProductionAssetContractTests
    {
        private const string ManifestDirectory = "Temp/ProductionAssetContractTests";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(ToAbsolutePath(ManifestDirectory));
        }

        [TearDown]
        public void TearDown()
        {
            var directory = ToAbsolutePath(ManifestDirectory);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void AndroidReleaseContractIsPortraitApi36Arm64()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
            Assert.That(PlayerSettings.Android.minSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel26));
            Assert.That(PlayerSettings.Android.targetSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel36));
            Assert.That(PlayerSettings.Android.targetArchitectures,
                Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
                Is.EqualTo(ScriptingImplementation.IL2CPP));
        }

        [Test]
        public void ManifestDeclaresEveryRequiredApprovalBatch()
        {
            var errors = ProductionAssetValidator.Validate(
                "Docs/Assets/production-asset-manifest.json");
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateReportsDuplicateAssetId()
        {
            Assert.That(Validate(Entry("id"), Entry("id", "enemies")),
                Does.Contain("duplicate asset id: id"));
        }

        [Test]
        public void ValidateReportsUnknownBatch()
        {
            Assert.That(Validate(Entry("id", "unknown")),
                Does.Contain("unknown batch: unknown"));
        }

        [Test]
        public void ValidateReportsMissingSourcePath()
        {
            Assert.That(Validate(Entry("id", sourcePath: "")),
                Does.Contain("missing source path: id"));
        }

        [Test]
        public void ValidateReportsMissingRuntimePath()
        {
            Assert.That(Validate(Entry("id", runtimePath: "")),
                Does.Contain("missing runtime path: id"));
        }

        [Test]
        public void ValidateReportsUnapprovedLicense()
        {
            Assert.That(Validate(Entry("id", licenseStatus: "temporary")),
                Does.Contain("license other than approved: id"));
        }

        [Test]
        public void ValidateReportsInvalidApprovalStatus()
        {
            Assert.That(Validate(Entry("id", approvalStatus: "rejected")),
                Does.Contain("approval status other than pending or approved: id"));
        }

        [Test]
        public void ValidateReportsMissingSpriteMetadata()
        {
            Assert.That(Validate(Entry("id", width: 0, height: 0, frameCount: 0,
                pivotX: 0f, pivotY: 0f, pixelsPerUnit: 0, sha256: "", approvalStatus: "approved",
                promptRevision: "")),
                Does.Contain("missing dimensions: id")
                    .And.Contain("missing frame count: id")
                    .And.Contain("missing pivot: id")
                    .And.Contain("missing PPU: id")
                    .And.Contain("missing SHA-256: id")
                    .And.Contain("missing prompt revision: id"));
        }

        [Test]
        public void ValidateReportsPathsOutsideProductionRoots()
        {
            Assert.That(Validate(Entry("id", sourcePath: "Elsewhere/source.png",
                runtimePath: "Assets/Elsewhere/runtime.png")),
                Does.Contain("source path outside ArtSource: Elsewhere/source.png")
                    .And.Contain("runtime path outside Assets/JoseonHunter: Assets/Elsewhere/runtime.png"));
        }

        private static string[] Validate(params string[] entries)
        {
            var path = ToAbsolutePath(ManifestDirectory + "/" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, "{ \"schemaVersion\": 1, \"assets\": [" + string.Join(",", entries) + "] }");
            return new System.Collections.Generic.List<string>(ProductionAssetValidator.Validate(path)).ToArray();
        }

        private static string Entry(
            string id,
            string batch = "characters",
            string sourcePath = "ArtSource/Pixel/source.png",
            string runtimePath = "Assets/JoseonHunter/Art/runtime.png",
            int width = 32,
            int height = 32,
            int frameCount = 1,
            float pivotX = 0.5f,
            float pivotY = 0.5f,
            int pixelsPerUnit = 32,
            string sha256 = "",
            string licenseStatus = "approved",
            string approvalStatus = "pending",
            string promptRevision = "v1")
        {
            return string.Format(
                "{{ \"id\": \"{0}\", \"batch\": \"{1}\", \"kind\": \"sprite\", \"sourcePath\": \"{2}\", \"runtimePath\": \"{3}\", \"width\": {4}, \"height\": {5}, \"frameCount\": {6}, \"pivotX\": {7}, \"pivotY\": {8}, \"pixelsPerUnit\": {9}, \"sha256\": \"{10}\", \"licenseStatus\": \"{11}\", \"approvalStatus\": \"{12}\", \"promptRevision\": \"{13}\" }}",
                id, batch, sourcePath, runtimePath, width, height, frameCount, pivotX, pivotY,
                pixelsPerUnit, sha256, licenseStatus, approvalStatus, promptRevision);
        }

        private static string ToAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(relativePath).Replace('\\', '/');
        }
    }
}
