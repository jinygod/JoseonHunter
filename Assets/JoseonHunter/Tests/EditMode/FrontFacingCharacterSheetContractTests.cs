using System;
using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class FrontFacingCharacterSheetContractTests
    {
        private const string FixtureRoot = "Temp/FrontFacingCharacterSheetContractTests";

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(FixtureRoot);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(FixtureRoot)) Directory.Delete(FixtureRoot, true);
        }

        [Test]
        public void ValidateAcceptsCanonicalFrontFacingSheet()
        {
            var root = CreateFixture();
            var result = FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png"));

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.FrameCount, Is.EqualTo(12));
            Assert.That(result.CellSize, Is.EqualTo(new Vector2Int(64, 64)));
            Assert.That(result.SheetSize, Is.EqualTo(new Vector2Int(256, 192)));
            Assert.That(result.HeadHeightRatio, Is.InRange(0.45f, 0.55f));
            Assert.That(FrontFacingCharacterSheetContract.HasAnimationVariation(root, 2, 4), Is.True);
        }

        [TestCase("\"directions\":[\"front\"]", "\"directions\":[\"down\"]", "invalid directions")]
        [TestCase("\"frames\":6,\"fps\":8", "\"frames\":32,\"fps\":8", "invalid frame count")]
        [TestCase("{\"name\":\"death\",\"start\":6,\"frames\":6,\"fps\":8}", "{\"name\":\"attack\",\"start\":6,\"frames\":6,\"fps\":8}", "invalid animation contract")]
        [TestCase("\"headHeightRatio\":0.5", "\"headHeightRatio\":0.6", "invalid head height ratio")]
        public void ValidateRejectsManifestContractViolations(string find, string replace, string expected)
        {
            var root = CreateFixture();
            var manifest = Path.Combine(root, "manifest.json");
            File.WriteAllText(manifest, File.ReadAllText(manifest).Replace(find, replace));

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain(expected));
        }

        [Test]
        public void ValidateRejectsOpaqueBackgroundCorner()
        {
            var root = CreateFixture();
            WriteSheet(Path.Combine(root, "flattened.png"), new Color32(40, 34, 45, 255), (0, 0, new Color32(1, 2, 3, 255)));
            File.Copy(Path.Combine(root, "flattened.png"), Path.Combine(root, "runtime.png"), true);

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("opaque sheet corner"));
        }

        [Test]
        public void ValidateRejectsSemiTransparentPixels()
        {
            var root = CreateFixture();
            WriteSheet(Path.Combine(root, "flattened.png"), new Color32(40, 34, 45, 255), (10, 10, new Color32(40, 34, 45, 128)));
            File.Copy(Path.Combine(root, "flattened.png"), Path.Combine(root, "runtime.png"), true);

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("semi-transparent pixel"));
        }

        [Test]
        public void ValidateRejectsIdenticalMoveFrames()
        {
            var root = CreateFixture();
            WriteSheet(Path.Combine(root, "flattened.png"), new Color32(40, 34, 45, 255));
            File.Copy(Path.Combine(root, "flattened.png"), Path.Combine(root, "runtime.png"), true);

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("identical move frames"));
        }

        [Test]
        public void ValidateRejectsWrongDimensions()
        {
            var root = CreateFixture();
            WritePng(Path.Combine(root, "flattened.png"), 255, 192, new Color32(40, 34, 45, 255));
            File.Copy(Path.Combine(root, "flattened.png"), Path.Combine(root, "runtime.png"), true);

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("invalid sheet size"));
        }

        [Test]
        public void ValidateRejectsColorsOutsidePalette()
        {
            var root = CreateFixture();
            WriteSheet(Path.Combine(root, "flattened.png"), new Color32(40, 34, 45, 255), (10, 10, new Color32(1, 2, 3, 255)));
            File.Copy(Path.Combine(root, "flattened.png"), Path.Combine(root, "runtime.png"), true);

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("color outside palette"));
        }

        [Test]
        public void ValidateRejectsRuntimeSourceMismatch()
        {
            var root = CreateFixture();
            WriteSheet(Path.Combine(root, "runtime.png"), new Color32(40, 34, 45, 255), (10, 10, new Color32(1, 2, 3, 255)));

            Assert.That(FrontFacingCharacterSheetContract.Validate(root, Path.Combine(root, "runtime.png")).Errors,
                Does.Contain("runtime does not match flattened source"));
        }

        private static string CreateFixture()
        {
            var root = Path.Combine(FixtureRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "manifest.json"), "{\"id\":\"rookie-constable\",\"cellSize\":[64,64],\"sheetSize\":[256,192],\"footAnchor\":[32,56],\"pivot\":[0.5,0.125],\"pixelsPerUnit\":32,\"view\":\"front\",\"directions\":[\"front\"],\"headHeightRatio\":0.5,\"promptRevision\":\"pixellab-rookie-v1\",\"animations\":[{\"name\":\"idle\",\"start\":0,\"frames\":2,\"fps\":4},{\"name\":\"move\",\"start\":2,\"frames\":4,\"fps\":8},{\"name\":\"death\",\"start\":6,\"frames\":6,\"fps\":8}]}" );
            WritePng(Path.Combine(root, "palette.png"), 1, 1, new Color32(40, 34, 45, 255));
            WriteSheet(Path.Combine(root, "flattened.png"), new Color32(40, 34, 45, 255),
                (2 * 64 + 1, 1, new Color32(40, 34, 45, 255)),
                (3 * 64 + 2, 2, new Color32(40, 34, 45, 255)),
                (1, 64 + 3, new Color32(40, 34, 45, 255)),
                (64 + 4, 64 + 4, new Color32(40, 34, 45, 255)));
            File.Copy(Path.Combine(root, "flattened.png"), Path.Combine(root, "runtime.png"));
            return root;
        }

        private static void WriteSheet(string path, Color32 color, params (int x, int y, Color32 color)[] pixels)
        {
            var texture = new Texture2D(256, 192, TextureFormat.RGBA32, false);
            texture.SetPixels32(new Color32[256 * 192]);
            foreach (var pixel in pixels) texture.SetPixel(pixel.x, pixel.y, pixel.color);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void WritePng(string path, int width, int height, Color32 color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(new Color32[width * height]);
            texture.SetPixel(1, 1, color);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
