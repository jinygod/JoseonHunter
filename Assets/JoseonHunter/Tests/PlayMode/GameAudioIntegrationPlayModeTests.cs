using System;
using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Presentation.Combat;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Audio;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameAudioIntegrationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameAudioDirector.EnsureExists();
            yield return null;
            GameAudioDirector.Instance.ResetRequestCountsForTests();
        }

        [UnityTest]
        public IEnumerator RuntimeButtonKeepsAudioFeedbackAfterListenersAreCleared()
        {
            var root = new GameObject("Audio UI Test").transform;
            var button = RuntimeUiFactory.Button("Test Button", root, Color.black);

            button.onClick.RemoveAllListeners();

            var feedback = button.GetComponent<GameAudioButtonFeedback>();
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.CueForTests, Is.EqualTo(GameAudioCueId.UiClick));
            UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CollectedExperienceAndLevelGainReachAudioDirector()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            GameAudioDirector.Instance.ResetRequestCountsForTests();
            LogAssert.ignoreFailingMessages = true;
            try
            {
                controller.SpawnExperiencePickupForTests(Vector2.zero, 1);
                controller.TickGameplayIfRunningForTests(.02f);
                controller.AddExperienceForTests(1000);
                yield return null;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.ExperiencePickup),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.LevelUp),
                Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator BossMilestonesAndDefeatReachAudioDirector()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            GameAudioDirector.Instance.ResetRequestCountsForTests();
            LogAssert.ignoreFailingMessages = true;
            try
            {
                controller.AdvanceStageForTests(0f, 900f);
                controller.DefeatFinalBossForTests();
                yield return null;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossWarning),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossAppear),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossDefeat),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ConfirmedDamageRequestsWeaponAndImpactCues()
        {
            var audio = GameAudioDirector.Instance;
            var registry = new CombatTargetRegistry();
            var target = new AudioTestTarget(901, 100);
            Assert.That(registry.Register(target), Is.True);
            var service = new CombatDamageService(registry);
            var feedbackObject = new GameObject("Feedback");
            var feedback = feedbackObject.AddComponent<CombatFeedbackDirector>();
            feedback.Bind(service);
            audio.ResetRequestCountsForTests();
            LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.That(service.TryApply(WeaponDamageRequest.Create(
                    71, WeaponId.GakgungShot, target, 5, false, new Float2(0f, 0f),
                    ContactPhase.Direct, 1), out _), Is.True);
                yield return null;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            Assert.That(audio.RequestCountForTests(GameAudioCueId.Gakgung), Is.EqualTo(1));
            Assert.That(audio.RequestCountForTests(GameAudioCueId.NormalHit), Is.EqualTo(1));
            UnityEngine.Object.Destroy(feedbackObject);
            yield return null;
        }

        private sealed class AudioTestTarget : ICombatTarget
        {
            private int health;

            public AudioTestTarget(int runtimeId, int health)
            {
                RuntimeId = runtimeId;
                this.health = health;
            }

            public int RuntimeId { get; }
            public bool IsAlive => health > 0;
            public int Health => health;
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition => new Float2(0f, 0f);
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public void ApplyResolvedDamage(int damage) => health = Math.Max(0, health - damage);
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
