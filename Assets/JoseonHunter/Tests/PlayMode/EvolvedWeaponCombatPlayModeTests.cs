using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class EvolvedWeaponCombatPlayModeTests
    {
        [Test]
        public void Runtime_rejects_duplicate_weapon_registration_without_second_tick_or_dispose_slot()
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            var first = new CountingExecutor();
            var second = new CountingExecutor();
            var root = new GameObject("Duplicate registration test root");
            runtime.Register(WeaponId.HwandoFlyingBlade, first);

            Assert.Throws<System.InvalidOperationException>(() => runtime.Register(WeaponId.HwandoFlyingBlade, second));
            runtime.Tick(0.1f, Vector2.zero, root.transform, null, 0);
            runtime.Dispose();
            Object.DestroyImmediate(root);

            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(0));
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Evolved_factory_adapts_live_telemetry_for_every_weapon()
        {
            var executors = new HashSet<IWeaponExecutor>();
            foreach (var weaponId in WeaponRoster.All)
            {
                using (var rig = EvolvedWeaponTestRig.For(weaponId))
                {
                    rig.AddTarget(new Vector2(1f, 0f));
                    yield return rig.AdvanceSeconds(0.6f);
                    var registered = rig.Runtime.ExecutorForTests(weaponId);
                    var telemetry = EvolvedExecutorFactory.ReadTelemetry(registered);

                    Assert.That(registered, Is.SameAs(rig.Executor));
                    Assert.That(rig.Runtime.IsEvolvedForTests(weaponId), Is.True);
                    Assert.That(rig.Runtime.RegistrationCountForTests(weaponId), Is.EqualTo(1));
                    Assert.That(rig.Runtime.RegisteredExecutorSlotCountForTests, Is.EqualTo(1));
                    Assert.That(telemetry.WeaponId, Is.EqualTo(weaponId));
                    Assert.That(telemetry.IsEvolved, Is.True);
                    Assert.That(telemetry.ExecutorKind, Is.Not.Empty);
                    Assert.That(telemetry.CurrentState, Is.Not.Empty);
                    Assert.That(telemetry.PrimaryObservedCount, Is.GreaterThan(0));
                    Assert.That(executors.Add(registered), Is.True);
                }
            }
        }

        [UnityTest]
        public IEnumerator Choosing_evolution_keeps_weapon_level_and_rebuilds_evolved_executor()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            var preChoiceRuntime = controller.WeaponRuntime;
            var preChoiceExecutor = preChoiceRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade);
            Assert.That(preChoiceExecutor, Is.Not.Null);
            Assert.That(preChoiceRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(preChoiceRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            Assert.That(preChoiceRuntime.RegisteredExecutorSlotCountForTests, Is.EqualTo(1));
            controller.UnlockEvolutionForTests("hwando_moon_eclipse");
            controller.OpenUpgradeForTests();

            var index = controller.CurrentOffers
                .Select((offer, i) => (offer, i))
                .Single(pair => pair.offer.Id == "hwando_moon_eclipse").i;
            Assert.That(controller.TryChooseUpgrade(index), Is.True);

            Assert.That(controller.WeaponLevelForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(5));
            Assert.That(controller.AcquiredEvolutionIds, Contains.Item("hwando_moon_eclipse"));
            Assert.That(controller.WeaponRuntime, Is.Not.SameAs(preChoiceRuntime));
            Assert.That(controller.WeaponRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade), Is.Not.SameAs(preChoiceExecutor));
            Assert.That(preChoiceRuntime.IsDisposedForTests, Is.True);
            Assert.That(preChoiceRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(0));
            Assert.That(preChoiceRuntime.RegisteredExecutorSlotCountForTests, Is.EqualTo(0));
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
            Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            Assert.That(controller.WeaponRuntime.RegisteredExecutorSlotCountForTests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Moon_eclipse_keeps_outbound_and_return_contact_then_blasts_at_crossing()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.HwandoFlyingBlade))
            {
                rig.AddTarget(new Vector2(2f, 0f));
                rig.AddTarget(new Vector2(0.2f, 0f));
                yield return rig.AdvanceSeconds(2f);

                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Direct);
                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Inbound);
                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Blast);
            }
        }

        [UnityTest]
        public IEnumerator Sun_piercer_fires_one_high_pierce_shot_on_cadence()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.GakgungShot))
            {
                rig.AddTarget(new Vector2(3f, 0f));
                yield return rig.AdvanceCasts(4);

                Assert.That(rig.Telemetry.LastProjectileMaximumImpacts, Is.GreaterThanOrEqualTo(6));
                Assert.That(rig.Telemetry.LastProjectileScale, Is.GreaterThan(1f));
            }
        }

        private sealed class CountingExecutor : IWeaponExecutor
        {
            public int TickCount { get; private set; }
            public int DisposeCount { get; private set; }
            public void Tick(float deltaTime, in WeaponExecutionContext context) => TickCount++;
            public void Reset() { }
            public void Dispose() => DisposeCount++;
        }
    }
}
