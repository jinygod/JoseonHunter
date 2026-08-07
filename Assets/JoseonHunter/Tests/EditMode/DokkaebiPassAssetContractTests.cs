using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class DokkaebiPassAssetContractTests
    {
        private const string EnemyRoot = "Assets/JoseonHunter/Art/Enemies/DokkaebiPass/";
        private const string BossRoot = "Assets/JoseonHunter/Art/Bosses/DokkaebiPass/";

        [TestCase(EnemyRoot + "club_dokkaebi.png", 48)]
        [TestCase(EnemyRoot + "shield_guard_dokkaebi.png", 48)]
        [TestCase(EnemyRoot + "iron_horn_dokkaebi.png", 48)]
        [TestCase(EnemyRoot + "stone_thrower_dokkaebi.png", 48)]
        [TestCase(EnemyRoot + "red_horn_elite.png", 64)]
        [TestCase(BossRoot + "one_horn_captain.png", 80)]
        [TestCase(BossRoot + "iron_shield_general.png", 80)]
        [TestCase(BossRoot + "dokkaebi_king.png", 112)]
        public void CharacterSpritesMeetApprovedCanvasAndImportContract(string path, int pixels)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(texture, Is.Not.Null, path);
            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(pixels), path);
            Assert.That(texture.height, Is.EqualTo(pixels), path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(pixels / 1.5f).Within(.02f), path);
            AssertNoNearWhiteTransparentEdge(texture, path);
        }

        [Test]
        public void StageCatalogResolvesEveryDokkaebiSpriteWithoutFallback()
        {
            var catalog = Resources.Load<StagePresentationCatalog>("StagePresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (var id in new[]
                     {
                         "club_dokkaebi", "shield_guard_dokkaebi", "iron_horn_dokkaebi",
                         "stone_thrower_dokkaebi", "red_horn_elite", "one_horn_captain",
                         "iron_shield_general", "dokkaebi_king"
                     })
                Assert.That(catalog.TryGetSprite(id, out var sprite) && sprite != null, Is.True, id);
        }

        [Test]
        public void StageCatalogResolvesDokkaebiPassGroundAndFourDecorations()
        {
            var catalog = Resources.Load<StagePresentationCatalog>("StagePresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetStage(StageId.DokkaebiPass, out var stage), Is.True);
            Assert.That(stage.Ground, Is.Not.Null);
            Assert.That(stage.AlternateGround, Is.Not.Null);
            Assert.That(stage.Decorations.Count, Is.EqualTo(4));
            Assert.That(stage.Decorations.All(sprite => sprite != null), Is.True);
        }

        private static void AssertNoNearWhiteTransparentEdge(Texture2D texture, string path)
        {
            var readable = CopyReadable(texture);
            try
            {
                var pixels = readable.GetPixels32();
                var width = readable.width;
                var height = readable.height;
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * width + x];
                    if (pixel.a < 32 || pixel.r < 235 || pixel.g < 235 || pixel.b < 235) continue;
                    if (TouchesTransparency(pixels, width, height, x, y))
                        Assert.Fail(path + $" contains a near-white outline pixel at {x},{y}");
                }
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        private static bool TouchesTransparency(IReadOnlyList<Color32> pixels, int width, int height, int x, int y)
        {
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                var sampleX = x + offsetX;
                var sampleY = y + offsetY;
                if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height) return true;
                if (pixels[sampleY * width + sampleX].a < 32) return true;
            }
            return false;
        }

        private static Texture2D CopyReadable(Texture source)
        {
            var temporary = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, temporary);
            var previous = RenderTexture.active;
            RenderTexture.active = temporary;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            return copy;
        }
    }
}
