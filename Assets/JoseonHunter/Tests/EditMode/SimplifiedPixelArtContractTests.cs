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
        private const string DokkaebiWalk =
            "Assets/JoseonHunter/Art/Animation/Enemies/Dokkaebi/Walk";
        private const string SakkatWalk =
            "Assets/JoseonHunter/Art/Animation/Enemies/SakkatSpecter/Walk";
        private const string SpiritWalk =
            "Assets/JoseonHunter/Art/Animation/Enemies/VengefulSpirit/Walk";
        private const string CaptainIdle =
            "Assets/JoseonHunter/Art/Animation/Elites/DokkaebiCaptain/Idle";
        private const string CaptainWalk =
            "Assets/JoseonHunter/Art/Animation/Elites/DokkaebiCaptain/Walk";
        private const string GeneralIdle =
            "Assets/JoseonHunter/Art/Animation/Bosses/FallenGeneral/Idle";
        private const string GeneralWalk =
            "Assets/JoseonHunter/Art/Animation/Bosses/FallenGeneral/Walk";
        private const string Hwando =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando";
        private const string WeaponPolish =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish";
        private const string Pickups =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups";
        private static readonly string[] RuntimeReferences =
        {
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png"
        };
        private static readonly string[] NormalEnemyReferences =
        {
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/dokkaebi.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/sakkat_specter.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/vengeful_spirit.png"
        };

        [TestCase(HanIdle, "idle_", 4)]
        [TestCase(HanWalk, "walk_", 8)]
        [TestCase(BanditWalk, "walk_", 6)]
        [TestCase(RatWalk, "walk_", 6)]
        [TestCase(DokkaebiWalk, "walk_", 6)]
        [TestCase(SakkatWalk, "walk_", 6)]
        [TestCase(SpiritWalk, "walk_", 6)]
        [TestCase(CaptainIdle, "idle_", 4)]
        [TestCase(CaptainWalk, "walk_", 6)]
        [TestCase(GeneralIdle, "idle_", 4)]
        [TestCase(GeneralWalk, "walk_", 8)]
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

        [TestCase("Fan", "fan_gust", 5)]
        [TestCase("Fan", "fan_lightning", 6)]
        [TestCase("Fan", "fan_target", 4)]
        [TestCase("Frost", "frost_flask", 6)]
        [TestCase("Frost", "frost_growth", 5)]
        [TestCase("Frost", "frost_shatter", 6)]
        [TestCase("Gakgung", "gakgung_aim_glint", 3)]
        [TestCase("Gakgung", "gakgung_arrow", 3)]
        [TestCase("Gakgung", "gakgung_impact_splinter", 5)]
        [TestCase("Jangseung", "jangseung_rise", 5)]
        [TestCase("Jangseung", "jangseung_strike", 5)]
        [TestCase("Jangseung", "jangseung_ward", 4)]
        [TestCase("Singijeon", "singijeon_ember", 5)]
        [TestCase("Singijeon", "singijeon_explosion", 6)]
        [TestCase("Singijeon", "singijeon_rocket", 4)]
        [TestCase("Talisman", "talisman_binding", 5)]
        [TestCase("Talisman", "talisman_rotate", 4)]
        [TestCase("Talisman", "talisman_seal_pulse", 5)]
        [TestCase("Thunder", "thunder_blast", 6)]
        [TestCase("Thunder", "thunder_ground_current", 5)]
        [TestCase("Thunder", "thunder_lob", 6)]
        [TestCase("Thunder", "thunder_warning", 4)]
        public void WeaponSequences_PreserveTheirRuntimeFrameContracts(
            string weapon,
            string prefix,
            int expected)
        {
            var directory = Path.Combine(WeaponPolish, weapon);
            Assert.That(
                Directory.GetFiles(directory, prefix + "*.png", SearchOption.TopDirectoryOnly).Length,
                Is.EqualTo(expected),
                directory + "/" + prefix);
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

        [Test]
        public void CompleteNormalEnemyPack_UsesNinetySixPixelCanvases()
        {
            AssertPixelContract(
                new[] { DokkaebiWalk, SakkatWalk, SpiritWalk }
                    .SelectMany(path => Directory.GetFiles(path, "*.png"))
                    .Concat(NormalEnemyReferences),
                96);
        }

        [Test]
        public void EliteAndBossPacks_UseTierSpecificCanvases()
        {
            AssertPixelContract(
                new[] { CaptainIdle, CaptainWalk }
                    .SelectMany(path => Directory.GetFiles(path, "*.png"))
                    .Append("Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites/dokkaebi_captain.png"),
                112);
            AssertPixelContract(
                new[] { GeneralIdle, GeneralWalk }
                    .SelectMany(path => Directory.GetFiles(path, "*.png"))
                    .Append("Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png"),
                128);
        }

        [Test]
        public void PickupsAndAllWeaponPolishFrames_UseTheirFinalContracts()
        {
            AssertPixelContract(Directory.GetFiles(Pickups, "*.png"), 64);
            AssertPixelContract(
                Directory.GetFiles(WeaponPolish, "*.png", SearchOption.AllDirectories),
                96);
        }

        private static void AssertPixelContract(
            System.Collections.Generic.IEnumerable<string> sourcePaths,
            int size)
        {
            foreach (var sourcePath in sourcePaths)
            {
                var path = sourcePath.Replace('\\', '/');
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(size), path);
                Assert.That(texture.height, Is.EqualTo(size), path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f), path);
            }
        }
    }
}
