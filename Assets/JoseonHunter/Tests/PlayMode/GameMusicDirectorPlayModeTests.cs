using System.Collections;
using System.Linq;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameMusicDirectorPlayModeTests
    {
        private GameObject listenerObject;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var existing in Object.FindObjectsByType<GameMusicDirector>(FindObjectsInactive.Include))
                Object.Destroy(existing.gameObject);
            yield return null;
            listenerObject = new GameObject("Game Music Test Listener", typeof(AudioListener));
            GameMusicDirector.EnsureExists();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (listenerObject != null) Object.Destroy(listenerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PersistentDirectorOwnsExactlyTwoLoopingMusicSources()
        {
            var director = GameMusicDirector.Instance;
            Assert.That(director, Is.Not.Null);
            var sources = director.GetComponents<AudioSource>();
            Assert.That(sources, Has.Length.EqualTo(2));
            Assert.That(sources.All(source => source.loop && !source.playOnAwake && source.spatialBlend == 0f),
                Is.True);

            GameMusicDirector.EnsureExists();
            yield return null;
            Assert.That(Object.FindObjectsByType<GameMusicDirector>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DuplicateRoleRequestKeepsTheCurrentSourcePlaying()
        {
            var director = GameMusicDirector.Instance;
            Assert.That(director.Request(GameMusicRole.Lobby, 0f), Is.True);
            yield return null;
            var source = director.GetComponents<AudioSource>().Single(candidate => candidate.isPlaying);
            var clip = source.clip;

            Assert.That(director.Request(GameMusicRole.Lobby, 0f), Is.True);
            yield return null;

            Assert.That(director.CurrentRole, Is.EqualTo(GameMusicRole.Lobby));
            Assert.That(source.isPlaying, Is.True);
            Assert.That(source.clip, Is.SameAs(clip));
            Assert.That(director.GetComponents<AudioSource>().Count(candidate => candidate.isPlaying), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NewRoleCrossfadesToTheOtherSourceAndStopsTheOutgoingSource()
        {
            var director = GameMusicDirector.Instance;
            director.Request(GameMusicRole.Lobby, 0f);
            yield return null;
            var outgoing = director.GetComponents<AudioSource>().Single(candidate => candidate.isPlaying);

            Assert.That(director.Request(GameMusicRole.CombatEarly, .08f), Is.True);
            yield return new WaitForSecondsRealtime(.12f);

            var sources = director.GetComponents<AudioSource>();
            Assert.That(director.CurrentRole, Is.EqualTo(GameMusicRole.CombatEarly));
            Assert.That(outgoing.isPlaying, Is.False);
            Assert.That(sources.Count(candidate => candidate.isPlaying), Is.EqualTo(1));
            Assert.That(sources.Single(candidate => candidate.isPlaying).volume, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator FadeOutStopsBothSourcesAndClearsTheRole()
        {
            var director = GameMusicDirector.Instance;
            director.Request(GameMusicRole.CombatMid, 0f);
            yield return null;

            director.FadeOut(.06f);
            yield return new WaitForSecondsRealtime(.1f);

            Assert.That(director.CurrentRole, Is.EqualTo(GameMusicRole.None));
            Assert.That(director.GetComponents<AudioSource>().Any(source => source.isPlaying), Is.False);
        }

        [UnityTest]
        public IEnumerator NoneRoleIsRejectedWithoutChangingPlayback()
        {
            var director = GameMusicDirector.Instance;
            director.Request(GameMusicRole.Lobby, 0f);
            yield return null;

            Assert.That(director.Request(GameMusicRole.None, .1f), Is.False);
            Assert.That(director.CurrentRole, Is.EqualTo(GameMusicRole.Lobby));
        }
    }
}
