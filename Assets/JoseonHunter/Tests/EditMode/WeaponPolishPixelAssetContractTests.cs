using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponPolishPixelAssetContractTests
    {
        private const string KnownFixturePath =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png";
        private const string PolishRoot =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish";

        [TestCase("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png")]
        [TestCase("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Gakgung/gakgung_arrow.png")]
        public void ExistingPolishFrame_UsesMobilePixelImportContract(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(
                WeaponPixelAssetContract.ValidatePolishFrame(texture, importer, path),
                Is.Empty);
        }

        [Test]
        public void PolishFrame_RejectsSpriteSheetMode()
        {
            var importer = AssetImporter.GetAtPath(KnownFixturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var originalMode = importer.spriteImportMode;

            try
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;

                Assert.That(
                    WeaponPixelAssetContract.ValidatePolishFrame(
                        AssetDatabase.LoadAssetAtPath<Texture2D>(KnownFixturePath),
                        importer,
                        KnownFixturePath),
                    Does.Contain("polish frame must be a single sprite"));
            }
            finally
            {
                importer.spriteImportMode = originalMode;
            }
        }

        [Test]
        public void GeneratedPolishBatch_UsesContractOnEveryIndividualFrame()
        {
            var paths = Directory.GetFiles(PolishRoot, "*.png", SearchOption.AllDirectories);
            Assert.That(paths, Has.Length.EqualTo(119));

            foreach (var path in paths)
            {
                var assetPath = path.Replace('\\', '/');
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(
                    WeaponPixelAssetContract.ValidatePolishFrame(texture, importer, assetPath),
                    Is.Empty,
                    assetPath);
            }
        }
    }
}
