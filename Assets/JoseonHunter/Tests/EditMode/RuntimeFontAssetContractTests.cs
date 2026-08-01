using NUnit.Framework;
using TMPro;
using UnityEngine;
using JoseonHunter.Presentation.UI;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class RuntimeFontAssetContractTests
    {
        [TestCase("Fonts/ChosunGs-Dynamic SDF", "ChosunGs-Dynamic SDF")]
        [TestCase("Fonts/MaruBuri-Regular-Dynamic SDF", "MaruBuri-Regular-Dynamic SDF")]
        [TestCase("Fonts/MaruBuri-SemiBold-Dynamic SDF", "MaruBuri-SemiBold-Dynamic SDF")]
        [TestCase("Fonts/BlackAndWhitePicture-Dynamic SDF", "BlackAndWhitePicture-Dynamic SDF")]
        public void LicensedRuntimeFontExistsWithDynamicAtlas(string path, string expectedName)
        {
            var font = Resources.Load<TMP_FontAsset>(path);
            var fallback = Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-Dynamic SDF");

            Assert.That(font, Is.Not.Null, path);
            Assert.That(font.name, Is.EqualTo(expectedName));
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(font.fallbackFontAssetTable, Does.Contain(fallback));
        }

        [TestCase(RuntimeFontRole.Body, "MaruBuri-Regular-Dynamic SDF")]
        [TestCase(RuntimeFontRole.BodyEmphasis, "MaruBuri-SemiBold-Dynamic SDF")]
        [TestCase(RuntimeFontRole.Title, "ChosunGs-Dynamic SDF")]
        [TestCase(RuntimeFontRole.Damage, "BlackAndWhitePicture-Dynamic SDF")]
        public void RuntimeFontCatalogMapsSemanticRole(RuntimeFontRole role, string expectedName)
        {
            Assert.That(RuntimeFontCatalog.For(role).name, Is.EqualTo(expectedName));
        }
    }
}
