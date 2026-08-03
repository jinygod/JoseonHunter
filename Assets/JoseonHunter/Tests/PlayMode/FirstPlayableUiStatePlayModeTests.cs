using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class FirstPlayableUiStatePlayModeTests
    {
        [UnityTest]
        public IEnumerator EndedRunStateCanOnlyRestartThroughThePublicEntryPoint()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.EndRunForTests(false);
            Assert.That(controller.UiState.RunEnded, Is.True);
            Assert.That(controller.UiState.Victory, Is.False);

            controller.RestartRun();
            Assert.That(controller.UiState.RunEnded, Is.False);
            Assert.That(controller.RunEndedForTests, Is.False);
        }

        [UnityTest]
        public IEnumerator Upgrade_presentation_contract_guards_choices_and_queues_rewards()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            var opened = 0;
            var chosen = 0;
            var resets = 0;
            controller.UpgradeOpened += _ => opened++;
            controller.UpgradeChosen += _ => chosen++;
            controller.RunReset += () => resets++;

            Assert.That(controller.TryChooseUpgrade(0), Is.False);
            Assert.That(controller.NotifyUpgradePresentationClosed(), Is.False);

            controller.OpenUpgradeForTests();
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(controller.TryChooseUpgrade(-1), Is.False);
            Assert.That(controller.TryChooseUpgrade(controller.CurrentOffers.Count), Is.False);
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(chosen, Is.EqualTo(1));
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(1));
            Assert.That(controller.IsUpgradeOpen, Is.False);
            Assert.That(controller.NotifyUpgradePresentationClosed(), Is.True);
            Assert.That(controller.NotifyUpgradePresentationClosed(), Is.False);

            controller.ResetRunForTests();
            Assert.That(resets, Is.EqualTo(1));
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(0));

            controller.AddExperienceForTests(100);
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(chosen, Is.EqualTo(2));
            Assert.That(controller.IsUpgradeOpen, Is.False, "the next queued choice waits for presentation close");
            Assert.That(opened, Is.EqualTo(2));

            Assert.That(controller.NotifyUpgradePresentationClosed(), Is.True);
            Assert.That(controller.Flow.State, Is.EqualTo(JoseonHunter.Domain.Runs.GameFlowState.Playing));
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(controller.IsUpgradeOpen, Is.False);
            controller.TickGameplayIfRunningForTests(.5f);
            Assert.That(opened, Is.EqualTo(2));
            controller.TickGameplayIfRunningForTests(.51f);
            Assert.That(opened, Is.EqualTo(3));
            Assert.That(controller.IsUpgradeOpen, Is.True);
        }
    }
}
