using System;
using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StaticSpriteBatchContractTests
    {
        private const string FixtureRoot = "Temp/StaticSpriteBatchContractTests";
        private static readonly FixtureAsset[] Assets =
        {
            new("rookie_constable", "hero", "Heroes/rookie_constable.png"), new("shaman", "hero", "Heroes/shaman.png"), new("mountain_hunter", "hero", "Heroes/mountain_hunter.png"),
            new("plague_rat", "enemy", "Enemies/plague_rat.png"), new("vengeful_spirit", "enemy", "Enemies/vengeful_spirit.png"), new("sakkat_specter", "enemy", "Enemies/sakkat_specter.png"),
            new("dokkaebi", "enemy", "Enemies/dokkaebi.png"), new("bandit", "enemy", "Enemies/bandit.png"), new("fallen_general", "boss", "Bosses/fallen_general.png"),
            new("coin", "pickup", "Pickups/coin.png"), new("experience_spirit_flame", "pickup", "Pickups/experience_spirit_flame.png"), new("treasure_chest", "pickup", "Pickups/treasure_chest.png")
        };

        [SetUp] public void SetUp() => Directory.CreateDirectory(FixtureRoot);
        [TearDown] public void TearDown() { if (Directory.Exists(FixtureRoot)) Directory.Delete(FixtureRoot, true); }

        [Test]
        public void ValidateAcceptsCanonicalTwelveAssetBatch()
        {
            var root = CreateFixture();
            var result = StaticSpriteBatchContract.Validate(Path.Combine(root, "batch.json"), root, "", false);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.AssetCount, Is.EqualTo(12));
        }

        [TestCase("rookie_constable", "", "missing id")]
        [TestCase("shaman", "rookie_constable", "duplicate id")]
        public void ValidateRejectsInvalidIds(string target, string replacement, string expected)
        {
            var root = CreateFixture();
            ReplaceManifest(root, "\"id\":\"" + target + "\"", "\"id\":\"" + replacement + "\"");
            Assert.That(Validate(root).Errors, Does.Contain(expected));
        }

        [Test]
        public void ValidateRejectsUnexpectedThirteenthAsset()
        {
            var root = CreateFixture();
            File.AppendAllText(Path.Combine(root, "batch.json"), " ");
            var json = File.ReadAllText(Path.Combine(root, "batch.json"));
            File.WriteAllText(Path.Combine(root, "batch.json"), json.Replace("]}", ",{\"id\":\"extra\"}]}"));
            Assert.That(Validate(root).Errors, Does.Contain("unexpected asset id"));
        }

        [TestCase("width", "63", "invalid dimensions")]
        [TestCase("height", "63", "invalid dimensions")]
        public void ValidateRejectsWrongDimensions(string field, string value, string expected)
        {
            var root = CreateFixture(); ReplaceManifest(root, "\"" + field + "\":64", "\"" + field + "\":" + value);
            Assert.That(Validate(root).Errors, Does.Contain(expected));
        }

        [Test] public void ValidateAssetRejectsNonRgbaInput() { var root = CreateFixture(); WriteSprite(Path.Combine(root, "rookie_constable", "sprite.png"), TextureFormat.RGB24); Assert.That(StaticSpriteBatchContract.ValidateAsset("rookie_constable", Path.Combine(root, "rookie_constable")), Does.Contain("non-RGBA input")); }
        [Test] public void ValidateAssetRejectsSemiTransparentPixel() { var root = CreateFixture(); WriteSprite(Path.Combine(root, "rookie_constable", "sprite.png"), TextureFormat.RGBA32, new Color32(10, 10, 10, 128)); Assert.That(StaticSpriteBatchContract.ValidateAsset("rookie_constable", Path.Combine(root, "rookie_constable")), Does.Contain("semi-transparent pixel")); }
        [Test] public void ValidateAssetRejectsOpaqueCorner() { var root = CreateFixture(); WriteSprite(Path.Combine(root, "rookie_constable", "sprite.png"), TextureFormat.RGBA32, new Color32(10, 10, 10, 255), 0, 0); Assert.That(StaticSpriteBatchContract.ValidateAsset("rookie_constable", Path.Combine(root, "rookie_constable")), Does.Contain("opaque corner")); }
        [Test] public void ValidateAssetRejectsTooManyOpaqueColors() { var root = CreateFixture(); WriteSprite(Path.Combine(root, "rookie_constable", "sprite.png"), TextureFormat.RGBA32, null, 30, 56, 49); Assert.That(StaticSpriteBatchContract.ValidateAsset("rookie_constable", Path.Combine(root, "rookie_constable")), Does.Contain("too many opaque colors")); }
        [Test] public void ValidateAssetRejectsWrongBottomAnchor() { var root = CreateFixture(); WriteSprite(Path.Combine(root, "rookie_constable", "sprite.png"), TextureFormat.RGBA32, new Color32(1, 1, 1, 255), 32, 55); Assert.That(StaticSpriteBatchContract.ValidateAsset("rookie_constable", Path.Combine(root, "rookie_constable")), Does.Contain("invalid maximum opaque y")); }
        [Test] public void ValidateAssetRejectsOffCenterBounds() { var root = CreateFixture(); WriteSprite(Path.Combine(root, "rookie_constable", "sprite.png"), TextureFormat.RGBA32, new Color32(1, 1, 1, 255), 29, 56); Assert.That(StaticSpriteBatchContract.ValidateAsset("rookie_constable", Path.Combine(root, "rookie_constable")), Does.Contain("invalid horizontal center")); }
        [Test] public void ValidateRejectsMissingPrompt() { var root = CreateFixture(); File.Delete(Path.Combine(root, "rookie_constable", "prompt.md")); Assert.That(Validate(root).Errors, Does.Contain("missing prompt")); }
        [Test] public void ValidateRejectsMissingProvenance() { var root = CreateFixture(); File.Delete(Path.Combine(root, "rookie_constable", "provenance.json")); Assert.That(Validate(root).Errors, Does.Contain("missing provenance")); }
        [Test] public void ValidateRejectsTokenLikeProvenance() { var root = CreateFixture(); File.WriteAllText(Path.Combine(root, "rookie_constable", "provenance.json"), "{\"apiKey\":\"safe\"}"); Assert.That(Validate(root).Errors, Does.Contain("token-like provenance value")); }
        [Test] public void ValidateAllowsJobIdUuidButRejectsOtherUuid() { var root = CreateFixture(); File.WriteAllText(Path.Combine(root, "rookie_constable", "provenance.json"), "{\"jobId\":\"01234567-89ab-cdef-0123-456789abcdef\",\"request\":\"01234567-89ab-cdef-0123-456789abcdef\"}"); Assert.That(Validate(root).Errors, Does.Contain("token-like provenance value")); }
        [Test] public void ValidateRejectsTokenLikeValuesInsideProvenanceArrays() { var root = CreateFixture(); File.WriteAllText(Path.Combine(root, "rookie_constable", "provenance.json"), "{\"history\":[\"Bearer sample\"]}"); Assert.That(Validate(root).Errors, Does.Contain("token-like provenance value")); }
        [Test] public void ValidateRejectsEscapedTokenLikeProvenanceValues() { var root = CreateFixture(); File.WriteAllText(Path.Combine(root, "rookie_constable", "provenance.json"), "{\"note\":\"api\\u005fkey\"}"); Assert.That(Validate(root).Errors, Does.Contain("token-like provenance value")); }
        [Test] public void ValidateRejectsSourceHashMismatch() { var root = CreateFixture(); ReplaceManifest(root, "\"sha256\":\"\"", "\"sha256\":\"deadbeef\""); Assert.That(Validate(root).Errors, Does.Contain("source hash mismatch")); }
        [Test] public void ValidateRejectsRuntimeByteMismatchWhenRequired() { var root = CreateFixture(); var runtime = Path.Combine(root, "runtime"); Directory.CreateDirectory(Path.Combine(runtime, "Heroes")); File.Copy(Path.Combine(root, "rookie_constable", "sprite.png"), Path.Combine(runtime, "Heroes", "rookie_constable.png")); File.WriteAllBytes(Path.Combine(runtime, "Heroes", "rookie_constable.png"), new byte[] { 1 }); Assert.That(StaticSpriteBatchContract.Validate(Path.Combine(root, "batch.json"), root, runtime, true).Errors, Does.Contain("runtime byte mismatch")); }

        [TestCase("shaman", "role", "enemy")]
        [TestCase("plague_rat", "sourcePath", "shaman/sprite.png")]
        [TestCase("fallen_general", "runtimePath", "Enemies/fallen_general.png")]
        public void ValidateRejectsWrongCanonicalRoleOrPath(string id, string property, string value)
        {
            var root = CreateFixture(); var asset = Asset(id);
            ReplaceManifest(root, "\"id\":\"" + id + "\",\"role\":\"" + asset.Role + "\",\"sourcePath\":\"" + id + "/sprite.png\",\"runtimePath\":\"" + asset.RuntimePath + "\"", "\"id\":\"" + id + "\",\"role\":\"" + (property == "role" ? value : asset.Role) + "\",\"sourcePath\":\"" + (property == "sourcePath" ? value : id + "/sprite.png") + "\",\"runtimePath\":\"" + (property == "runtimePath" ? value : asset.RuntimePath) + "\"");
            Assert.That(Validate(root).Errors, Does.Contain("invalid canonical mapping"));
        }

        [Test]
        public void ValidateRejectsApprovedManifestAssetWithPendingProvenance()
        {
            var root = CreateFixture("approved");
            Assert.That(Validate(root).Errors, Does.Contain("provenance approval mismatch"));
        }

        private static StaticSpriteBatchValidationResult Validate(string root) => StaticSpriteBatchContract.Validate(Path.Combine(root, "batch.json"), root, "", false);
        private static void ReplaceManifest(string root, string find, string replace) => File.WriteAllText(Path.Combine(root, "batch.json"), File.ReadAllText(Path.Combine(root, "batch.json")).Replace(find, replace));
        private static string CreateFixture(string approvalStatus = "pending")
        {
            var root = Path.Combine(FixtureRoot, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var assets = "";
            foreach (var asset in Assets) { Directory.CreateDirectory(Path.Combine(root, asset.Id)); WriteSprite(Path.Combine(root, asset.Id, "sprite.png"), TextureFormat.RGBA32); File.WriteAllText(Path.Combine(root, asset.Id, "palette.png"), "palette"); File.WriteAllText(Path.Combine(root, asset.Id, "prompt.md"), "prompt"); File.WriteAllText(Path.Combine(root, asset.Id, "provenance.json"), "{\"jobId\":\"valid-job\",\"status\":\"pending\"}"); assets += (assets.Length == 0 ? "" : ",") + "{\"id\":\"" + asset.Id + "\",\"role\":\"" + asset.Role + "\",\"sourcePath\":\"" + asset.Id + "/sprite.png\",\"runtimePath\":\"" + asset.RuntimePath + "\",\"width\":64,\"height\":64,\"footAnchor\":[32,56],\"pivot\":[0.5,0.125],\"pixelsPerUnit\":32,\"approvalStatus\":\"" + approvalStatus + "\",\"sha256\":\"\"}"; }
            File.WriteAllText(Path.Combine(root, "batch.json"), "{\"schemaVersion\":1,\"promptRevision\":\"static-launch-v1\",\"assets\":[" + assets + "]}"); return root;
        }
        private static FixtureAsset Asset(string id) { foreach (var asset in Assets) if (asset.Id == id) return asset; throw new ArgumentOutOfRangeException(nameof(id)); }
        private readonly struct FixtureAsset { public FixtureAsset(string id, string role, string runtimePath) { Id = id; Role = role; RuntimePath = runtimePath; } public string Id { get; } public string Role { get; } public string RuntimePath { get; } }
        private static void WriteSprite(string path, TextureFormat format, Color32? color = null, int x = 32, int y = 56, int colors = 1)
        {
            var texture = new Texture2D(64, 64, format, false); texture.SetPixels32(new Color32[64 * 64]);
            for (var index = 0; index < colors; index++) texture.SetPixel(x + index % 7, 63 - (y - index / 7), color ?? new Color32((byte)(index + 1), 2, 3, 255));
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
