using System.Collections;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameFlowCoordinatorPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Invalid_transition_is_rejected_without_changing_the_current_state()
        {
            var coordinator = new GameObject("Flow").AddComponent<GameFlowCoordinator>();

            Assert.That(coordinator.TryTransition(GameFlowState.AugmentResult), Is.False);
            Assert.That(coordinator.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(coordinator.IsGameplayRunning, Is.True);

            Object.Destroy(coordinator.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Idempotent_transition_keeps_gameplay_running_without_raising_state_changed()
        {
            var coordinator = new GameObject("Flow").AddComponent<GameFlowCoordinator>();
            var stateChanges = 0;
            coordinator.StateChanged += (_, _) => stateChanges++;

            Assert.That(coordinator.TryTransition(GameFlowState.Playing), Is.True);
            Assert.That(coordinator.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(coordinator.IsGameplayRunning, Is.True);
            Assert.That(stateChanges, Is.Zero);

            Object.Destroy(coordinator.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hit_stop_expires_using_unscaled_time()
        {
            var coordinator = new GameObject("Flow").AddComponent<GameFlowCoordinator>();

            Assert.That(coordinator.RequestHitStop(.1f), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(coordinator.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(coordinator.IsGameplayRunning, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Object.Destroy(coordinator.gameObject);
        }

        [UnityTest]
        public IEnumerator Modal_state_wins_over_hit_stop()
        {
            var coordinator = new GameObject("Flow").AddComponent<GameFlowCoordinator>();
            Assert.That(coordinator.RequestHitStop(.2f), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(coordinator.TryTransition(GameFlowState.LevelUpSelection), Is.True);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(coordinator.State, Is.EqualTo(GameFlowState.LevelUpSelection));
            Assert.That(Time.timeScale, Is.Zero);
            Object.Destroy(coordinator.gameObject);
        }

        [UnityTest]
        public IEnumerator Disable_resets_state_and_restores_time_scale()
        {
            var flow = new GameObject("Flow");
            var coordinator = flow.AddComponent<GameFlowCoordinator>();
            Assert.That(coordinator.RequestHitStop(.2f), Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            flow.SetActive(false);
            yield return null;

            Assert.That(coordinator.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(coordinator.IsGameplayRunning, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Object.Destroy(flow);
        }
    }
}
