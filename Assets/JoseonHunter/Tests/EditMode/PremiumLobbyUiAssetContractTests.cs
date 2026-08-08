using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PremiumLobbyUiAssetContractTests
    {
        [TestCase("thin_outer_frame", true)]
        [TestCase("header_bar", true)]
        [TestCase("stage_title_plate", true)]
        [TestCase("content_backplate", true)]
        [TestCase("difficulty_idle", true)]
        [TestCase("difficulty_selected", true)]
        [TestCase("difficulty_locked", true)]
        [TestCase("weapon_selector_frame", true)]
        [TestCase("primary_red_button", true)]
        [TestCase("secondary_dark_button", true)]
        [TestCase("tab_idle", true)]
        [TestCase("tab_selected", true)]
        [TestCase("small_item_frame", true)]
        public void FidelitySpriteUsesDeterministicPixelImport(string name, bool sliced)
        {
            const string root = "Assets/JoseonHunter/Resources/UI/PremiumJoseon/";
            var path = root + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            AssertPlatformIsUncompressed(importer, "Standalone", path);
            AssertPlatformIsUncompressed(importer, "Android", path);
            AssertPlatformIsUncompressed(importer, "WebGL", path);
            Assert.That(importer.spriteBorder.sqrMagnitude, sliced ? Is.GreaterThan(0f) : Is.EqualTo(0f));
        }

        [TestCase("difficulty_selected")]
        [TestCase("difficulty_idle")]
        [TestCase("primary_red_button")]
        [TestCase("secondary_dark_button")]
        [TestCase("thin_outer_frame")]
        public void EdgeFillingFidelitySpriteHasVisibleArtworkAtEveryCanvasEdge(string name)
        {
            const string root = "Assets/JoseonHunter/Resources/UI/PremiumJoseon/";
            var path = root + name + ".png";
            var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(readable, File.ReadAllBytes(path)), Is.True, path);
            try
            {
                var bounds = VisibleBounds(readable);
                Assert.That(bounds.width, Is.GreaterThanOrEqualTo(readable.width * .88f),
                    name + " must not have a large horizontal transparent canvas margin");
                Assert.That(bounds.height, Is.GreaterThanOrEqualTo(readable.height * .88f),
                    name + " must not have a large vertical transparent canvas margin");
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        private static RectInt VisibleBounds(Texture2D texture)
        {
            var minimum = new Vector2Int(texture.width, texture.height);
            var maximum = new Vector2Int(-1, -1);
            for (var y = 0; y < texture.height; y++)
            for (var x = 0; x < texture.width; x++)
            {
                if (texture.GetPixel(x, y).a <= .5f) continue;
                minimum = Vector2Int.Min(minimum, new Vector2Int(x, y));
                maximum = Vector2Int.Max(maximum, new Vector2Int(x, y));
            }

            Assert.That(maximum.x, Is.GreaterThanOrEqualTo(minimum.x), "artwork must contain opaque pixels");
            return new RectInt(minimum.x, minimum.y, maximum.x - minimum.x + 1, maximum.y - minimum.y + 1);
        }

        private static void AssertPlatformIsUncompressed(TextureImporter importer, string platform, string path)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            Assert.That(settings.overridden, Is.True, path + " " + platform);
            Assert.That(settings.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed),
                path + " " + platform);
        }
    }
}
