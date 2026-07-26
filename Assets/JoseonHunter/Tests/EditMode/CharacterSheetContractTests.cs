using System;
using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CharacterSheetContractTests
    {
        private const string FixtureRoot = "Temp/CharacterSheetContractTests";

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(FixtureRoot);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(FixtureRoot)) Directory.Delete(FixtureRoot, true);
        }

        [Test]
        public void ValidateAcceptsCanonicalMannequin()
        {
            var result = CharacterSheetContract.Validate(
                "ArtSource/Pixel/Characters/mannequin",
                "Assets/JoseonHunter/Art/Characters/Runtime/mannequin.png");

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CellSize, Is.EqualTo(new Vector2Int(64, 64)));
            Assert.That(result.FootAnchor, Is.EqualTo(new Vector2Int(32, 56)));
            Assert.That(result.Pivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
            Assert.That(result.FrameCount, Is.EqualTo(38));
        }

        [Test]
        public void CanonicalActiveFramesHaveReadableBoundsAndAnimationVariation()
        {
            var root = "ArtSource/Pixel/Characters/mannequin";
            var runtime = "Assets/JoseonHunter/Art/Characters/Runtime/mannequin.png";
            Assert.That(CharacterSheetContract.Validate(root, runtime).Errors, Is.Empty);
            Assert.That(CharacterSheetContract.ActiveFrameBounds(root), Has.Length.EqualTo(38));
            Assert.That(CharacterSheetContract.ActiveFrameBounds(root)[0].height, Is.InRange(44, 52));
            Assert.That(CharacterSheetContract.ActiveFrameBounds(root)[0].yMax, Is.InRange(57, 59));
            Assert.That(CharacterSheetContract.HasAnimationVariation(root, 0, 12), Is.True);
            Assert.That(CharacterSheetContract.HasAnimationVariation(root, 12, 18), Is.True);
            Assert.That(CharacterSheetContract.HasAnimationVariation(root, 30, 8), Is.True);
        }

        [Test]
        public void ValidateRejectsRuntimeThatDoesNotMatchLayerComposite()
        {
            var root = CreateFixture();
            var runtime = Path.Combine(root, "runtime.png");
            WritePng(runtime, 384, 448, new Color32(1, 2, 3, 255));

            Assert.That(CharacterSheetContract.Validate(root, runtime).Errors,
                Does.Contain("runtime does not match layer composite"));
        }

        [TestCase("\"directions\":[\"down\",\"right\",\"up\"]", "\"directions\":[\"down\",\"up\",\"right\"]", "invalid directions")]
        [TestCase("\"mirrorLeftFrom\":\"right\"", "\"mirrorLeftFrom\":\"up\"", "invalid mirror source")]
        [TestCase("\"promptRevision\":\"mannequin-v1\"", "\"promptRevision\":\"\"", "missing prompt revision")]
        [TestCase("\"fps\":6", "\"fps\":7", "invalid animation: idle")]
        public void ValidateRejectsExactManifestContractViolations(string find, string replace, string expected)
        {
            var root = CreateFixture();
            var path = Path.Combine(root, "manifest.json");
            File.WriteAllText(path, File.ReadAllText(path).Replace(find, replace));

            Assert.That(CharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain(expected));
        }

        [Test]
        public void ValidateRejectsWrongPaletteSlotsAndLayerOrder()
        {
            var root = CreateFixture();
            var path = Path.Combine(root, "manifest.json");
            var json = File.ReadAllText(path)
                .Replace("\"skin\",\"primary-cloth\"", "\"primary-cloth\",\"skin\"")
                .Replace("\"shadow\",\"back-equipment\"", "\"back-equipment\",\"shadow\"");
            File.WriteAllText(path, json);

            Assert.That(CharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("invalid palette slots").And.Contain("invalid layer contract"));
        }

        [TestCase("wrong canvas size", "body", 383, 448, "invalid canvas")]
        [TestCase("mismatched layer dimensions", "body", 384, 447, "invalid canvas")]
        public void ValidateRejectsInvalidLayerCanvas(string name, string layer, int width, int height, string expected)
        {
            var root = CreateFixture();
            WritePng(Path.Combine(root, "layers", layer + ".png"), width, height, new Color32(40, 34, 45, 255));

            Assert.That(CharacterSheetContract.Validate(root, "runtime.png").Errors,
                Does.Contain(expected + ": " + layer));
        }

        [Test]
        public void ValidateRejectsSemiTransparentStrayPixel()
        {
            var root = CreateFixture();
            WritePng(Path.Combine(root, "layers", "body.png"), 384, 448, new Color32(40, 34, 45, 128));

            Assert.That(CharacterSheetContract.Validate(root, "runtime.png").Errors,
                Does.Contain("semi-transparent pixel: body"));
        }

        [Test]
        public void ValidateRejectsMoreThanDeclaredPalette()
        {
            var root = CreateFixture();
            WritePng(Path.Combine(root, "layers", "body.png"), 384, 448, new Color32(1, 2, 3, 255));

            Assert.That(CharacterSheetContract.Validate(root, "runtime.png").Errors,
                Does.Contain("color outside palette: body"));
        }

        [Test]
        public void ValidateRejectsMissingLayer()
        {
            var root = CreateFixture();
            File.Delete(Path.Combine(root, "layers", "face.png"));

            Assert.That(CharacterSheetContract.Validate(root, "runtime.png").Errors,
                Does.Contain("missing layer: face"));
        }

        [Test]
        public void ValidateRejectsNonTransparentUnusedCell()
        {
            var root = CreateFixture();
            WritePng(Path.Combine(root, "layers", "body.png"), 384, 448, new Color32(40, 34, 45, 255), 320, 384);

            Assert.That(CharacterSheetContract.Validate(root, "runtime.png").Errors,
                Does.Contain("non-transparent unused cell: body"));
        }

        private static string CreateFixture()
        {
            var root = Path.Combine(FixtureRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "layers"));
            File.WriteAllText(Path.Combine(root, "manifest.json"), "{\"id\":\"mannequin\",\"cellSize\":[64,64],\"footAnchor\":[32,56],\"pivot\":[0.5,0.125],\"pixelsPerUnit\":32,\"directions\":[\"down\",\"right\",\"up\"],\"mirrorLeftFrom\":\"right\",\"animations\":[{\"name\":\"idle\",\"start\":0,\"frames\":12,\"fps\":6},{\"name\":\"move\",\"start\":12,\"frames\":18,\"fps\":10},{\"name\":\"death\",\"start\":30,\"frames\":8,\"fps\":10}],\"layers\":[\"shadow\",\"back-equipment\",\"body\",\"back-hair\",\"lower-clothing\",\"upper-clothing\",\"armor\",\"face\",\"front-hair\",\"headwear\",\"left-weapon\",\"right-prop\",\"front-overlay\"],\"paletteSlots\":[\"skin\",\"primary-cloth\",\"secondary-cloth\",\"accent\",\"metal\",\"outline\"],\"promptRevision\":\"mannequin-v1\"}");
            foreach (var layer in new[] { "shadow", "back-equipment", "body", "back-hair", "lower-clothing", "upper-clothing", "armor", "face", "front-hair", "headwear", "left-weapon", "right-prop", "front-overlay" })
                WritePng(Path.Combine(root, "layers", layer + ".png"), 384, 448, new Color32(40, 34, 45, 255));
            WritePng(Path.Combine(root, "palette.png"), 6, 1, new Color32(40, 34, 45, 255));
            return root;
        }

        private static void WritePng(string path, int width, int height, Color32 color, int x = 0, int y = 0)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixel(x, y, color);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
