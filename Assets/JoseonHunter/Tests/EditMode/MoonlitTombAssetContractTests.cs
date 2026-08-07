using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class MoonlitTombAssetContractTests
    {
        private const string EnemyRoot = "Assets/JoseonHunter/Art/Enemies/MoonlitTomb/";
        private const string BossRoot = "Assets/JoseonHunter/Art/Bosses/MoonlitTomb/";

        [TestCase(EnemyRoot + "tomb_attendant.png", 48)]
        [TestCase(EnemyRoot + "tomb_archer_ghost.png", 48)]
        [TestCase(EnemyRoot + "red_lantern_wraith.png", 48)]
        [TestCase(EnemyRoot + "curse_shaman.png", 48)]
        [TestCase(EnemyRoot + "grave_ambusher_elite.png", 64)]
        [TestCase(BossRoot + "royal_guard_wraith.png", 80)]
        [TestCase(BossRoot + "eclipse_priest.png", 80)]
        [TestCase(BossRoot + "eclipse_queen.png", 112)]
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
        public void StageCatalogResolvesMoonlitTombWithoutFallback()
        {
            var catalog = Resources.Load<StagePresentationCatalog>("StagePresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (var id in new[]
                     {
                         "tomb_attendant", "tomb_archer_ghost", "red_lantern_wraith",
                         "curse_shaman", "grave_ambusher_elite", "royal_guard_wraith",
                         "eclipse_priest", "eclipse_queen"
                     })
                Assert.That(catalog.TryGetSprite(id, out var sprite) && sprite != null, Is.True, id);

            Assert.That(catalog.TryGetStage(StageId.MoonlitTomb, out var stage), Is.True);
            Assert.That(stage.Ground, Is.Not.Null);
            Assert.That(stage.AlternateGround, Is.Not.Null);
            Assert.That(stage.Decorations.Count, Is.EqualTo(4));
        }

        [Test]
        public void GroundRemainsDistinctFromCrimsonAndVioletWarnings()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/JoseonHunter/Art/Stages/MoonlitTomb/moonlit_tomb_ground.png");
            Assert.That(texture, Is.Not.Null);
            var ground = AverageOpaque(texture);
            Assert.That(ColorDistance(ground, new Color(.78f, .04f, .07f)), Is.GreaterThan(.32f));
            Assert.That(ColorDistance(ground, new Color(.62f, .08f, .44f)), Is.GreaterThan(.25f));
        }

        private static float ColorDistance(Color first, Color second)
        {
            var red = first.r - second.r;
            var green = first.g - second.g;
            var blue = first.b - second.b;
            return Mathf.Sqrt(red * red + green * green + blue * blue);
        }

        private static Color AverageOpaque(Texture texture)
        {
            var copy = CopyReadable(texture);
            try
            {
                var pixels = copy.GetPixels32();
                var sum = Vector3.zero;
                var count = 0;
                foreach (var pixel in pixels)
                {
                    if (pixel.a < 32) continue;
                    sum += new Vector3(pixel.r, pixel.g, pixel.b) / 255f;
                    count++;
                }
                return count == 0 ? Color.black : new Color(sum.x / count, sum.y / count, sum.z / count, 1f);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        private static void AssertNoNearWhiteTransparentEdge(Texture2D texture, string path)
        {
            var readable = CopyReadable(texture);
            try
            {
                var pixels = readable.GetPixels32();
                for (var y = 0; y < readable.height; y++)
                for (var x = 0; x < readable.width; x++)
                {
                    var pixel = pixels[y * readable.width + x];
                    if (pixel.a < 32 || pixel.r < 235 || pixel.g < 235 || pixel.b < 235) continue;
                    if (TouchesTransparency(pixels, readable.width, readable.height, x, y))
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
