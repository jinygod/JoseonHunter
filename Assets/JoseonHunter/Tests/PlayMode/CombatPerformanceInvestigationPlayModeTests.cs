using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatPerformanceInvestigationPlayModeTests
    {
        [UnityTest]
        public IEnumerator EachLevelFiveWeaponAgainstTwentyTargetsRecordsItsIsolatedCombatCost()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            var mask = PixelHitMask.FromRows("1");
            var overBudget = new List<string>();
            foreach (var weaponId in WeaponRoster.All)
            {
                controller.ResetRunForTests();
                controller.ConfigureSeparationLoadScenarioForTests();
                controller.SetWeaponLevelForTests(weaponId, 5);
                for (var index = 0; index < 20; index++)
                {
                    var angle = index * Mathf.PI * 2f / 20f;
                    var radius = 1.25f + index % 4 * .55f;
                    var target = new InvestigationTarget(
                        index + 10000,
                        new Float2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius),
                        mask);
                    Assert.That(controller.RegisterCombatTargetForTests(target), Is.True);
                }

                var allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();
                var timer = Stopwatch.StartNew();
                for (var tick = 0; tick < 40; tick++)
                    Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);
                timer.Stop();
                var allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                TestContext.WriteLine(
                    $"WEAPON_ISOLATION id={weaponId.Value}; targets=20; ticks=40; " +
                    $"totalMs={timer.Elapsed.TotalMilliseconds:F4}; " +
                    $"averageTickMs={timer.Elapsed.TotalMilliseconds / 40d:F4}; allocatedBytes={allocatedBytes}");
                var averageTickMs = timer.Elapsed.TotalMilliseconds / 40d;
                if (averageTickMs > 16.67d)
                    overBudget.Add($"{weaponId.Value}={averageTickMs:F4}ms");
                yield return null;
            }

            Assert.That(overBudget, Is.Empty,
                "Each isolated level-five weapon combination must fit one 60 Hz CPU frame: " +
                string.Join(", ", overBudget));
            controller.Flow.ResetToPlaying();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator EightLevelFiveWeaponsAgainstOneHundredTargetsLeaveRenderingHeadroom()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            controller.ConfigureSeparationLoadScenarioForTests();
            foreach (var weaponId in WeaponRoster.All)
                controller.SetWeaponLevelForTests(weaponId, 5);

            var mask = PixelHitMask.FromRows("1");
            for (var index = 0; index < 100; index++)
            {
                var angle = index * Mathf.PI * 2f / 100f;
                var radius = 1.25f + index % 8 * .32f;
                Assert.That(controller.RegisterCombatTargetForTests(new InvestigationTarget(
                    index + 20000,
                    new Float2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius),
                    mask)), Is.True);
            }

            var allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var tick = 0; tick < 40; tick++)
                Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);
            timer.Stop();
            var averageTickMs = timer.Elapsed.TotalMilliseconds / 40d;
            var allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            TestContext.WriteLine(
                $"EIGHT_WEAPON_LOAD targets=100; ticks=40; totalMs={timer.Elapsed.TotalMilliseconds:F4}; " +
                $"averageTickMs={averageTickMs:F4}; allocatedBytes={allocatedBytes}");

            Assert.That(averageTickMs, Is.LessThanOrEqualTo(12d),
                "Combat CPU work must leave at least 4.67 ms of a 60 Hz frame for rendering and presentation.");
            controller.Flow.ResetToPlaying();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator GameplaySceneRecordsFirstAndRepeatedLoadTime()
        {
            var firstLoad = Stopwatch.StartNew();
            yield return LoadScene("Gameplay");
            firstLoad.Stop();

            yield return LoadScene("Lobby");
            var repeatedLoad = Stopwatch.StartNew();
            yield return LoadScene("Gameplay");
            repeatedLoad.Stop();

            TestContext.WriteLine(
                $"GAMEPLAY_FIRST_RENDER coldLoadMs={firstLoad.Elapsed.TotalMilliseconds:F3}; " +
                $"repeatLoadMs={repeatedLoad.Elapsed.TotalMilliseconds:F3}");

            Assert.That(firstLoad.Elapsed.TotalMilliseconds, Is.GreaterThan(0d));
            Assert.That(repeatedLoad.Elapsed.TotalMilliseconds, Is.GreaterThan(0d));
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone)
                yield return null;
            yield return null;
        }

        private sealed class InvestigationTarget : ICombatTarget, IFrostStatusTarget, IJangseungWardStatusTarget
        {
            private readonly PixelHitMask mask;

            public InvestigationTarget(int runtimeId, Float2 position, PixelHitMask mask)
            {
                RuntimeId = runtimeId;
                Position = position;
                this.mask = mask;
            }

            public int RuntimeId { get; }
            public bool IsAlive => true;
            public int Health => int.MaxValue;
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => 1f;
            public Float2 Position { get; private set; }
            public Float2 WorldPosition => Position;
            public PixelHitMask HurtMask => mask;
            public PixelMaskTransform HurtMaskTransform =>
                PixelMaskTransform.Translation(Position.X, Position.Y);

            public void ApplyResolvedDamage(int damage) { }

            public void ApplyKnockback(Float2 direction, float force)
            {
                Position = new Float2(
                    Position.X + direction.X * force,
                    Position.Y + direction.Y * force);
            }

            public void ApplyFrostSlow(int sourceId, float strength) { }
            public void RemoveFrostSlow(int sourceId, float decaySeconds) { }
            public void ApplyFreeze(int sourceId, float durationSeconds) { }
            public void ApplyJangseungWard(int sourceId, float strength) { }
            public void RemoveJangseungWard(int sourceId) { }
        }
    }
}
