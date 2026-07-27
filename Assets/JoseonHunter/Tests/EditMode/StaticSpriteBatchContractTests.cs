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
        private static readonly string[] Ids = { "rookie_constable", "shaman", "mountain_hunter", "plague_rat", "vengeful_spirit", "sakkat_specter", "dokkaebi", "bandit", "fallen_general", "coin", "experience_spirit_flame", "treasure_chest" };

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
        [Test] public void ValidateRejectsSourceHashMismatch() { var root = CreateFixture(); ReplaceManifest(root, "\"sha256\":\"\"", "\"sha256\":\"deadbeef\""); Assert.That(Validate(root).Errors, Does.Contain("source hash mismatch")); }
        [Test] public void ValidateRejectsRuntimeByteMismatchWhenRequired() { var root = CreateFixture(); var runtime = Path.Combine(root, "runtime"); Directory.CreateDirectory(Path.Combine(runtime, "Heroes")); File.Copy(Path.Combine(root, "rookie_constable", "sprite.png"), Path.Combine(runtime, "Heroes", "rookie_constable.png")); File.WriteAllBytes(Path.Combine(runtime, "Heroes", "rookie_constable.png"), new byte[] { 1 }); Assert.That(StaticSpriteBatchContract.Validate(Path.Combine(root, "batch.json"), root, runtime, true).Errors, Does.Contain("runtime byte mismatch")); }

        private static StaticSpriteBatchValidationResult Validate(string root) => StaticSpriteBatchContract.Validate(Path.Combine(root, "batch.json"), root, "", false);
        private static void ReplaceManifest(string root, string find, string replace) => File.WriteAllText(Path.Combine(root, "batch.json"), File.ReadAllText(Path.Combine(root, "batch.json")).Replace(find, replace));
        private static string CreateFixture()
        {
            var root = Path.Combine(FixtureRoot, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var assets = "";
            foreach (var id in Ids) { Directory.CreateDirectory(Path.Combine(root, id)); WriteSprite(Path.Combine(root, id, "sprite.png"), TextureFormat.RGBA32); File.WriteAllText(Path.Combine(root, id, "palette.png"), "palette"); File.WriteAllText(Path.Combine(root, id, "prompt.md"), "prompt"); File.WriteAllText(Path.Combine(root, id, "provenance.json"), "{\"jobId\":\"valid-job\"}"); assets += (assets.Length == 0 ? "" : ",") + "{\"id\":\"" + id + "\",\"role\":\"hero\",\"sourcePath\":\"" + id + "/sprite.png\",\"runtimePath\":\"Heroes/" + id + ".png\",\"width\":64,\"height\":64,\"footAnchor\":[32,56],\"pivot\":[0.5,0.125],\"pixelsPerUnit\":32,\"approvalStatus\":\"pending\",\"sha256\":\"\"}"; }
            File.WriteAllText(Path.Combine(root, "batch.json"), "{\"schemaVersion\":1,\"promptRevision\":\"static-launch-v1\",\"assets\":[" + assets + "]}"); return root;
        }
        private static void WriteSprite(string path, TextureFormat format, Color32? color = null, int x = 32, int y = 56, int colors = 1)
        {
            var texture = new Texture2D(64, 64, format, false); texture.SetPixels32(new Color32[64 * 64]);
            for (var index = 0; index < colors; index++) texture.SetPixel(x + index % 7, y - index / 7, color ?? new Color32((byte)(index + 1), 2, 3, 255));
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
