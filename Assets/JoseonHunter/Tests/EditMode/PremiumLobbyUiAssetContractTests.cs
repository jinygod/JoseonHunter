using NUnit.Framework;
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

        private static void AssertPlatformIsUncompressed(TextureImporter importer, string platform, string path)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            Assert.That(settings.overridden, Is.True, path + " " + platform);
            Assert.That(settings.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed),
                path + " " + platform);
        }
    }
}
