using System.Collections;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameAudioDirectorPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var director in Object.FindObjectsByType<GameAudioDirector>(FindObjectsInactive.Include))
                Object.Destroy(director.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var director in Object.FindObjectsByType<GameAudioDirector>(FindObjectsInactive.Include))
                Object.Destroy(director.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnsureCreatedTwiceKeepsOneDirectorAndTwelveSources()
        {
            GameAudioDirector.EnsureExists();
            GameAudioDirector.EnsureExists();
            yield return null;

            Assert.That(Object.FindObjectsByType<GameAudioDirector>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(GameAudioDirector.Instance, Is.Not.Null);
            Assert.That(GameAudioDirector.Instance.SourceCount, Is.EqualTo(12));
        }

        [UnityTest]
        public IEnumerator AddingDuplicateDirectorKeepsOriginalInstance()
        {
            GameAudioDirector.EnsureExists();
            var original = GameAudioDirector.Instance;
            new GameObject("Duplicate Game Audio").AddComponent<GameAudioDirector>();
            yield return null;

            Assert.That(GameAudioDirector.Instance, Is.SameAs(original));
            Assert.That(Object.FindObjectsByType<GameAudioDirector>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SourcesAreReusableTwoDimensionalOneShotChannels()
        {
            GameAudioDirector.EnsureExists();
            yield return null;

            var sources = GameAudioDirector.Instance.GetComponents<AudioSource>();
            Assert.That(sources, Has.Length.EqualTo(12));
            foreach (var source in sources)
            {
                Assert.That(source.playOnAwake, Is.False);
                Assert.That(source.loop, Is.False);
                Assert.That(source.spatialBlend, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator MissingNoneCueReturnsFalseWithoutThrowing()
        {
            GameAudioDirector.EnsureExists();
            yield return null;

            Assert.DoesNotThrow(() => GameAudioDirector.Instance.TryPlay(GameAudioCueId.None));
            Assert.That(GameAudioDirector.Instance.TryPlay(GameAudioCueId.None), Is.False);
        }

        [UnityTest]
        public IEnumerator CombatGateRejectsCombatCuesWhileKeepingUiRequestsValid()
        {
            GameAudioDirector.EnsureExists();
            yield return null;
            var director = GameAudioDirector.Instance;
            director.SetCombatEnabled(false);

            Assert.That(director.CanRequest(GameAudioCueId.NormalHit), Is.False);
            Assert.That(director.CanRequest(GameAudioCueId.Gakgung), Is.False);
            Assert.That(director.CanRequest(GameAudioCueId.UiClick), Is.True);
        }
    }
}
