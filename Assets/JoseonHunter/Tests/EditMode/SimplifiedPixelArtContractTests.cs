using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class SimplifiedPixelArtContractTests
    {
        private const string HanIdle =
            "Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Idle";
        private const string HanWalk =
            "Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Walk";
        private const string BanditWalk =
            "Assets/JoseonHunter/Art/Animation/Enemies/Bandit/Walk";
        private const string RatWalk =
            "Assets/JoseonHunter/Art/Animation/Enemies/PlagueRat/Walk";
        private const string Hwando =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando";
        private static readonly string[] RuntimeReferences =
        {
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png"
        };

        [TestCase(HanIdle, "idle_", 4)]
        [TestCase(HanWalk, "walk_", 8)]
        [TestCase(BanditWalk, "walk_", 6)]
        [TestCase(RatWalk, "walk_", 6)]
        [TestCase(Hwando, "hwando_blade", 4)]
        [TestCase(Hwando, "hwando_afterimage", 4)]
        [TestCase(Hwando, "hwando_contact_spark", 4)]
        public void FinalCombatSequences_HaveTheRequiredFrameCount(
            string directory,
            string prefix,
            int expected)
        {
            Assert.That(
                Directory.GetFiles(directory, prefix + "*.png", SearchOption.TopDirectoryOnly).Length,
                Is.EqualTo(expected),
                directory);
        }

        [Test]
        public void FirstSimplifiedPack_UsesAConsistentMobilePixelContract()
        {
            var paths = new[] { HanIdle, HanWalk, BanditWalk, RatWalk }
                .SelectMany(path => Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(Hwando, "hwando_*.png", SearchOption.TopDirectoryOnly))
                .Concat(RuntimeReferences)
                .Select(path => path.Replace('\\', '/'))
                .ToArray();

            Assert.That(paths, Is.Not.Empty);
            foreach (var path in paths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(96), path);
                Assert.That(texture.height, Is.EqualTo(96), path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f), path);
            }
        }
    }
}
