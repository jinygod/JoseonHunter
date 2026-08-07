using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameMusicAssetContractTests
    {
        private const string Root = "Assets/JoseonHunter/Audio/Music/CC0";

        [TestCase(Root + "/lobby_yoiyami.ogg")]
        [TestCase(Root + "/gwigok_early_asianoriental.ogg")]
        [TestCase(Root + "/gwigok_mid_frozen_desert.ogg")]
        [TestCase(Root + "/gwigok_late_hope.ogg")]
        [TestCase(Root + "/dokkaebi_pass_oriented.ogg")]
        [TestCase(Root + "/moonlit_tomb_creepy_loop.ogg")]
        [TestCase(Root + "/midboss_determined_pursuit.ogg")]
        [TestCase(Root + "/finalboss_epic_battle.ogg")]
        public void RequiredMusicUsesTheStreamingStereoProfile(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Assert.That(clip, Is.Not.Null, path);
            Assert.That(clip.length, Is.GreaterThanOrEqualTo(30f), path);

            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.forceToMono, Is.False, path);
            Assert.That(importer.loadInBackground, Is.True, path);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.False, path);
            Assert.That(importer.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.Streaming), path);
            Assert.That(importer.defaultSampleSettings.compressionFormat,
                Is.EqualTo(AudioCompressionFormat.Vorbis), path);
            Assert.That(importer.defaultSampleSettings.quality, Is.EqualTo(.55f).Within(.001f), path);
        }

        [Test]
        public void RuntimeMusicFolderContainsOnlyTheEightApprovedClips()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { Root });
            Assert.That(guids, Has.Length.EqualTo(8));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.That(Path.GetExtension(path), Is.EqualTo(".ogg"));
            }
        }
    }
}
