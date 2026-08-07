using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameAudioAssetContractTests
    {
        private const string Root = "Assets/JoseonHunter/Resources/Audio/CC0";

        [TestCase(Root + "/UI/ui_click.ogg")]
        [TestCase(Root + "/UI/ui_confirm.ogg")]
        [TestCase(Root + "/Pickups/experience.ogg")]
        [TestCase(Root + "/Pickups/yeopjeon_1.ogg")]
        [TestCase(Root + "/Pickups/yeopjeon_2.ogg")]
        [TestCase(Root + "/Pickups/magnet.ogg")]
        [TestCase(Root + "/Pickups/level_up.ogg")]
        [TestCase(Root + "/Weapons/gakgung.wav")]
        [TestCase(Root + "/Weapons/hwando.wav")]
        [TestCase(Root + "/Weapons/thunder_bomb.ogg")]
        [TestCase(Root + "/Weapons/frost_flask.ogg")]
        [TestCase(Root + "/Weapons/wind_fan.wav")]
        [TestCase(Root + "/Weapons/jangseung.ogg")]
        [TestCase(Root + "/Weapons/geumjul.ogg")]
        [TestCase(Root + "/Weapons/singijeon.ogg")]
        [TestCase(Root + "/Combat/hit_soft_1.ogg")]
        [TestCase(Root + "/Combat/hit_critical.ogg")]
        [TestCase(Root + "/Combat/boss_defeat.ogg")]
        [TestCase(Root + "/Combat/player_hurt_1.ogg")]
        [TestCase(Root + "/Combat/player_hurt_2.ogg")]
        [TestCase(Root + "/Combat/player_defeat.ogg")]
        [TestCase(Root + "/Combat/elite_defeat.ogg")]
        [TestCase(Root + "/Combat/boss_slam.ogg")]
        [TestCase(Root + "/Combat/boss_charge.wav")]
        [TestCase(Root + "/Combat/boss_volley.ogg")]
        [TestCase(Root + "/Events/wave_warning.ogg")]
        [TestCase(Root + "/Events/elite_appear.ogg")]
        [TestCase(Root + "/Events/treasure_appear.ogg")]
        [TestCase(Root + "/Events/treasure_open.ogg")]
        [TestCase(Root + "/UI/pause_open.ogg")]
        [TestCase(Root + "/UI/appraisal_tick.ogg")]
        [TestCase(Root + "/UI/appraisal_reveal.ogg")]
        public void RequiredClipExistsAndUsesMobileShortSfxProfile(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Assert.That(clip, Is.Not.Null, path);
            Assert.That(clip.length, Is.LessThanOrEqualTo(4f), path);

            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.forceToMono, Is.True, path);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True, path);
            Assert.That(importer.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.DecompressOnLoad), path);
            Assert.That(importer.defaultSampleSettings.compressionFormat,
                Is.EqualTo(AudioCompressionFormat.Vorbis), path);
        }

        [Test]
        public void RuntimeAudioFolderContainsOnlyTheThirtyTwoApprovedClips()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { Root });
            Assert.That(guids, Has.Length.EqualTo(32));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.That(Path.GetExtension(path), Is.EqualTo(".ogg").Or.EqualTo(".wav"));
            }
        }
    }
}
