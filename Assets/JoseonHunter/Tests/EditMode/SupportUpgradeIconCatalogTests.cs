using System.Linq;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class SupportUpgradeIconCatalogTests
    {
        private static readonly string[] SupportIds = { "talisman", "boots", "warding_bell" };

        [TestCase("talisman")]
        [TestCase("boots")]
        [TestCase("warding_bell")]
        public void Every_support_upgrade_resolves_a_pixel_art_sprite(string id)
        {
            var sprite = SupportUpgradeIconCatalog.Resolve(id);

            Assert.That(sprite, Is.Not.Null, id);
            Assert.That(sprite.texture.width, Is.EqualTo(96), id);
            Assert.That(sprite.texture.height, Is.EqualTo(96), id);
            Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point), id);

            var assetPath = AssetDatabase.GetAssetPath(sprite.texture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, id);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), id);
            Assert.That(importer.mipmapEnabled, Is.False, id);
            Assert.That(importer.alphaIsTransparency, Is.True, id);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), id);
        }

        [Test]
        public void Support_upgrade_icons_are_visually_distinct_assets()
        {
            var sprites = SupportIds.Select(SupportUpgradeIconCatalog.Resolve).ToArray();

            Assert.That(sprites, Has.All.Not.Null);
            Assert.That(sprites.Select(sprite => AssetDatabase.GetAssetPath(sprite)).Distinct().Count(),
                Is.EqualTo(SupportIds.Length));
        }

        [Test]
        public void Unknown_support_upgrade_uses_the_existing_glyph_fallback()
        {
            Assert.That(SupportUpgradeIconCatalog.Resolve("unknown_support"), Is.Null);
            Assert.That(SupportUpgradeIconCatalog.Resolve(null), Is.Null);
        }
    }
}
