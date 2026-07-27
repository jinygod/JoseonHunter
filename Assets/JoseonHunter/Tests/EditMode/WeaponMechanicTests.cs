using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponMechanicTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("FlyingBladeExecutorTests");

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void FlyingBladeDealsNothingBeforeItsFirstMaskOverlap()
        {
            var fixture = CreateFixture(new Float2(1f, 0f), 1);

            fixture.Executor.Tick(0.05f, fixture.Context(1));

            Assert.That(fixture.Target.Health, Is.EqualTo(100));
            Assert.That(fixture.Events, Is.Empty);
        }

        [Test]
        public void FlyingBladeHitsEachPhaseOnceAndReturnsToPoolWithinRange()
        {
            var fixture = CreateFixture(new Float2(1f, 0f), 1);

            for (var tick = 1; tick <= 50; tick++) fixture.Executor.Tick(0.1f, fixture.Context(tick));

            Assert.Multiple(() =>
            {
                Assert.That(Count(fixture.Events, ContactPhase.Outbound), Is.EqualTo(1));
                Assert.That(Count(fixture.Events, ContactPhase.Inbound), Is.EqualTo(1));
                Assert.That(fixture.Executor.MaximumDistanceFromLaunch, Is.LessThanOrEqualTo(2f));
                Assert.That(fixture.Executor.ReturnedToPoolCount, Is.EqualTo(1));
                Assert.That(fixture.Executor.ActiveBladeCount, Is.Zero);
            });
        }

        [Test]
        public void LevelFiveVolleyUsesThreeStaggeredBlades()
        {
            var fixture = CreateFixture(new Float2(1f, 0f), 3);

            fixture.Executor.Tick(0.01f, fixture.Context(1));

            Assert.Multiple(() =>
            {
                Assert.That(fixture.Executor.LastVolleyLaunchCount, Is.EqualTo(3));
                Assert.That(fixture.Executor.ActiveBladeCount, Is.EqualTo(3));
                Assert.That(fixture.Executor.DelayedBladeCount, Is.EqualTo(2));
            });
        }

        private Fixture CreateFixture(Float2 targetPosition, int bladeCount)
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var executor = new FlyingBladeExecutor(runtime, 10f, 10f, 2f, 2f, bladeCount);
            runtime.Register(executor);
            var target = new TestTarget(5, targetPosition, mask);
            registry.Register(target);
            var events = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += events.Add;
            return new Fixture(executor, target, events, root.transform);
        }

        private static int Count(IEnumerable<ConfirmedDamageEvent> events, ContactPhase phase)
        {
            var result = 0;
            foreach (var confirmed in events) if (confirmed.Phase == phase) result++;
            return result;
        }

        private readonly struct Fixture
        {
            public Fixture(FlyingBladeExecutor executor, TestTarget target, List<ConfirmedDamageEvent> events, Transform presentationRoot)
            {
                Executor = executor; Target = target; Events = events; PresentationRoot = presentationRoot;
            }
            public FlyingBladeExecutor Executor { get; }
            public TestTarget Target { get; }
            public List<ConfirmedDamageEvent> Events { get; }
            private Transform PresentationRoot { get; }
            public WeaponExecutionContext Context(int tick) => new WeaponExecutionContext(new Float2(0f, 0f), PresentationRoot, null, 0, tick);
        }

        private sealed class TestTarget : ICombatTarget
        {
            private readonly PixelHitMask mask;
            public TestTarget(int runtimeId, Float2 position, PixelHitMask mask)
            {
                RuntimeId = runtimeId; WorldPosition = position; this.mask = mask; Health = 100;
            }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public Float2 WorldPosition { get; }
            public PixelHitMask HurtMask => mask;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
