using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PremiumLobbyUiAssetContractTests
    {
        private static readonly string[] Required =
        {
            "panel_frame", "stage_plaque_frame", "card_idle_frame", "card_selected_frame",
            "nav_idle_frame", "nav_selected_frame", "hero_oval_frame", "divider_gold",
            "icon_research", "icon_patrol", "icon_training", "icon_previous", "icon_next",
            "icon_settings", "icon_lock"
        };

        [Test]
        public void PremiumSpritesExistAndUsePointFiltering()
        {
            foreach (var name in Required)
            {
                var path = $"Assets/JoseonHunter/Resources/UI/PremiumJoseon/{name}.png";
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, name);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, name);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), name);
                Assert.That(importer.mipmapEnabled, Is.False, name);
                Assert.That(importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed), name);
            }
        }

        [TestCase("panel_frame")]
        [TestCase("stage_plaque_frame")]
        [TestCase("card_idle_frame")]
        [TestCase("card_selected_frame")]
        [TestCase("nav_idle_frame")]
        [TestCase("nav_selected_frame")]
        public void StretchableFramesHaveSpriteBorders(string name)
        {
            var path = $"Assets/JoseonHunter/Resources/UI/PremiumJoseon/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, name);
            Assert.That(importer.spriteBorder.sqrMagnitude, Is.GreaterThan(0f), name);
        }
    }
}
