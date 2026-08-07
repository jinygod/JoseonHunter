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
using UnityEngine.UI;

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
            controller.SpawnExperiencePickupForTests(Vector2.zero, 1);
            controller.TickGameplayIfRunningForTests(.02f);
            controller.AddExperienceForTests(1000);
            yield return null;

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
            controller.AdvanceStageForTests(0f, 900f);
            controller.DefeatFinalBossForTests();
            yield return null;

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossWarning),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossAppear),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossDefeat),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlayerDamageAndDefeatReachAudioDirectorOncePerStateChange()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            GameAudioDirector.Instance.ResetRequestCountsForTests();

            controller.DamagePlayerForTests(12f);
            controller.SetContactInvulnerabilityForTests(0f);
            controller.DamagePlayerForTests(10000f);
            yield return null;

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.PlayerHurt),
                Is.EqualTo(2));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.PlayerDefeat),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NormalEnemyContactAlsoUsesThePlayerHurtCue()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            controller.ConfigureSeparationLoadScenarioForTests();
            controller.SpawnEnemyForTests(Vector2.zero);
            controller.SetContactInvulnerabilityForTests(0f);
            GameAudioDirector.Instance.ResetRequestCountsForTests();

            controller.UpdateEnemiesForTests(.01f);
            yield return null;

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.PlayerHurt),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EliteDeathHasAudioButNormalEnemyDeathDoesNot()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            controller.ConfigureSeparationLoadScenarioForTests();
            GameAudioDirector.Instance.ResetRequestCountsForTests();

            controller.ConfigureViewportSpawnForTests(0, .5f, 1f, true);
            controller.SpawnEnemyForLifecycleTests();
            controller.DestroyLastEnemyForLifecycleTests();
            controller.ConfigureViewportSpawnForTests(0, .5f, 1f, false);
            controller.SpawnEnemyForLifecycleTests();
            controller.DestroyLastEnemyForLifecycleTests();
            controller.ClearViewportSpawnForTests();
            yield return null;

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.EliteAppear),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.EliteDefeat),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TreasureSpawnAndOpenReachDistinctAudioCues()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            controller.ConfigureSeparationLoadScenarioForTests();
            GameAudioDirector.Instance.ResetRequestCountsForTests();

            controller.SpawnTreasureForSeparationTests(Vector2.zero);
            controller.DestroyLastEnemyForLifecycleTests();
            yield return null;

            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.TreasureAppear),
                Is.EqualTo(1));
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.TreasureOpen),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WaveBossAttackAndPauseEventsReachAudioDirector()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = UnityEngine.Object.FindAnyObjectByType<FirstPlayableController>();
            GameAudioDirector.Instance.ResetRequestCountsForTests();

            controller.AdvanceStageForTests(0f, 900f);
            for (var tick = 0; tick < 16 && !controller.RunEndedForTests; tick++)
                controller.UpdateEnemiesForTests(.25f);
            var pause = System.Array.Find(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include),
                candidate => candidate.name == "Pause Button");
            if (!controller.RunEndedForTests) pause.onClick.Invoke();
            yield return null;

            var bossAttackRequests =
                GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossSlam) +
                GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossCharge) +
                GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.BossVolley);
            Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.WaveWarning),
                Is.GreaterThanOrEqualTo(1));
            Assert.That(bossAttackRequests, Is.GreaterThanOrEqualTo(1));
            if (!controller.RunEndedForTests)
                Assert.That(GameAudioDirector.Instance.RequestCountForTests(GameAudioCueId.PauseOpen),
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
            Assert.That(service.TryApply(WeaponDamageRequest.Create(
                71, WeaponId.GakgungShot, target, 5, false, new Float2(0f, 0f),
                ContactPhase.Direct, 1), out _), Is.True);
            yield return null;

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
