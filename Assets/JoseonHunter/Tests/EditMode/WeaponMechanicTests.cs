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

        [Test]
        public void GakgungPrioritizesBossOverCloserNormalAndMissesMovedTarget()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var bow = new GakgungExecutor(runtime, 10f, 1f, 2f, 10f, 1);
            var normal = new TestTarget(1, new Float2(0.2f, 0f), mask, false, false, 999f);
            var boss = new TestTarget(2, new Float2(1f, 0f), mask, true, false, 0f);
            registry.Register(normal); registry.Register(boss);

            bow.Tick(0.01f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));
            boss.MoveTo(new Float2(1f, 4f));
            for (var tick = 2; tick < 20; tick++) bow.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            Assert.Multiple(() =>
            {
                Assert.That(bow.LastSelectedTargetRuntimeId, Is.EqualTo(2));
                Assert.That(bow.LastLaunchCount, Is.EqualTo(1));
                Assert.That(boss.Health, Is.EqualTo(100));
            });
        }

        [Test]
        public void SingijeonUsesDensestDirectionAndConfiguredNonHomingLanes()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var singijeon = new SingijeonExecutor(runtime, 10f, 1f, 2f, 10f, 3, 1);
            registry.Register(new TestTarget(1, new Float2(1f, 0f), mask, true, false, 100f));
            registry.Register(new TestTarget(2, new Float2(1f, 0.1f), mask, false, false, 0f));
            registry.Register(new TestTarget(3, new Float2(1f, -0.1f), mask, false, false, 0f));
            registry.Register(new TestTarget(4, new Float2(-1f, 0f), mask, false, false, 0f));

            singijeon.Tick(0.01f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));

            Assert.Multiple(() =>
            {
                Assert.That(singijeon.LastDirection.X, Is.GreaterThan(0f));
                Assert.That(singijeon.LastLaunchCount, Is.EqualTo(3));
                Assert.That(singijeon.ActiveProjectileCount, Is.EqualTo(3));
            });
        }

        [Test]
        public void FlyingBladeGakgungAndSingijeonAllocateDistinctAttackInstanceIds()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var blade = new FlyingBladeExecutor(runtime, 10f, 10f, 2f, 2f, 1);
            var bow = new GakgungExecutor(runtime, 10f, 10f, 2f, 10f, 1);
            var volley = new SingijeonExecutor(runtime, 10f, 10f, 2f, 10f, 1, 1);
            registry.Register(new TestTarget(1, new Float2(0.2f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += events.Add;
            var context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1);

            blade.Tick(0.1f, context);
            bow.Tick(0.1f, context);
            volley.Tick(0.1f, context);

            Assert.That(events, Has.Count.EqualTo(3));
            Assert.That(events[0].AttackInstanceId, Is.Not.EqualTo(events[1].AttackInstanceId));
            Assert.That(events[0].AttackInstanceId, Is.Not.EqualTo(events[2].AttackInstanceId));
            Assert.That(events[1].AttackInstanceId, Is.Not.EqualTo(events[2].AttackInstanceId));
        }

        [Test]
        public void HighSpeedProjectileEventuallySweepsItsFullRangeWithoutEarlyExpiry()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var bow = new GakgungExecutor(runtime, 10f, 10f, 100f, 1000f, 1);
            var target = new TestTarget(1, new Float2(100f, 0f), mask);
            registry.Register(target);

            for (var tick = 1; tick <= 4; tick++)
                bow.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            Assert.Multiple(() =>
            {
                Assert.That(target.Health, Is.EqualTo(90));
                Assert.That(bow.ActiveProjectileCount, Is.Zero);
            });
        }

        [Test]
        public void SingijeonClustersDirectionsAcrossThePlusMinus180DegreeWrap()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var singijeon = new SingijeonExecutor(runtime, 10f, 10f, 2f, 10f, 1, 1);
            registry.Register(new TestTarget(1, new Float2(-1f, 0.05f), mask));
            registry.Register(new TestTarget(2, new Float2(-1f, -0.05f), mask));
            registry.Register(new TestTarget(3, new Float2(-1f, 0f), mask));
            registry.Register(new TestTarget(4, new Float2(1f, 0.05f), mask));
            registry.Register(new TestTarget(5, new Float2(1f, -0.05f), mask));

            singijeon.Tick(0.01f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));

            Assert.Multiple(() =>
            {
                Assert.That(singijeon.LastDirectionBucket, Is.EqualTo(6));
                Assert.That(singijeon.LastDirection.X, Is.LessThan(0f));
            });
        }

        [Test]
        public void SingijeonAndLinearProjectilesCapLanesActivePoolAndImpacts()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var singijeon = new SingijeonExecutor(runtime, 10f, 10f, 2f, 10f, 999, 5);
            registry.Register(new TestTarget(1, new Float2(1f, 0f), mask));

            singijeon.Tick(0.01f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));

            Assert.Multiple(() =>
            {
                Assert.That(singijeon.LaneCount, Is.EqualTo(SingijeonExecutor.MaxLaneCount));
                Assert.That(singijeon.LastLaunchCount, Is.EqualTo(SingijeonExecutor.MaxLaneCount * 3));
                Assert.That(singijeon.ActiveProjectileCount, Is.EqualTo(SingijeonExecutor.MaxLaneCount * 3));
            });

            var linear = new LinearProjectileExecutor(runtime);
            var context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2);
            var requested = new LinearProjectileSpec(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.GakgungShot, new Float2(0f, 0f), new Float2(1f, 0f), 1f, 0.01f, 1, 999, "Cap Test");
            for (var index = 0; index < LinearProjectileExecutor.MaxActiveProjectiles + 10; index++) linear.Launch(context, requested);
            Assert.That(linear.ActiveCount, Is.EqualTo(LinearProjectileExecutor.MaxActiveProjectiles));
            linear.Tick(0.01f, context);

            Assert.Multiple(() =>
            {
                Assert.That(requested.MaxImpacts, Is.EqualTo(LinearProjectileExecutor.MaxImpactsPerProjectile));
                Assert.That(linear.ActiveCount, Is.Zero);
                Assert.That(linear.ReturnedToPoolCount, Is.EqualTo(LinearProjectileExecutor.MaxActiveProjectiles));
                Assert.That(linear.PooledCount, Is.LessThanOrEqualTo(LinearProjectileExecutor.MaxPooledProjectiles));
            });
        }

        [Test]
        public void PiercingLinearProjectileHitsOnlyThreeAlignedTargetsThenRetires()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var linear = new LinearProjectileExecutor(runtime);
            var targets = new[]
            {
                new TestTarget(1, new Float2(1f, 0f), mask), new TestTarget(2, new Float2(2f, 0f), mask),
                new TestTarget(3, new Float2(3f, 0f), mask), new TestTarget(4, new Float2(4f, 0f), mask)
            };
            foreach (var target in targets) registry.Register(target);
            var events = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += events.Add;
            var context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1);
            linear.Launch(context, new LinearProjectileSpec(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.GakgungShot, new Float2(0f, 0f), new Float2(1f, 0f), 10f, 1f, 10, 999, "Pierce Test"));

            linear.Tick(0.5f, context);

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(3));
                Assert.That(targets[3].Health, Is.EqualTo(100));
                Assert.That(linear.ActiveCount, Is.Zero);
                Assert.That(damage.TrackedAttackCount, Is.Zero);
            });
        }

        [Test]
        public void CompletedLinearAttacksAreRetiredInsteadOfAccumulating()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var linear = new LinearProjectileExecutor(runtime);
            var target = new TestTarget(1, new Float2(1f, 0f), mask, health: 2000);
            registry.Register(target);
            var context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1);

            for (var index = 0; index < 100; index++)
            {
                linear.Launch(context, new LinearProjectileSpec(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f),
                    WeaponId.GakgungShot, new Float2(0f, 0f), new Float2(1f, 0f), 10f, 0.1f, 1, 1, "Retire Test"));
                linear.Tick(0.1f, context);
            }

            Assert.Multiple(() =>
            {
                Assert.That(target.Health, Is.EqualTo(1900));
                Assert.That(damage.TrackedAttackCount, Is.Zero);
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
            public TestTarget(int runtimeId, Float2 position, PixelHitMask mask, bool isBoss = false, bool isElite = false, float threatScore = 0f, int health = 100)
            {
                RuntimeId = runtimeId; WorldPosition = position; this.mask = mask; Health = health;
                IsBoss = isBoss; IsElite = isElite; ThreatScore = threatScore;
            }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss { get; }
            public bool IsElite { get; }
            public float ThreatScore { get; }
            public Float2 WorldPosition { get; private set; }
            public PixelHitMask HurtMask => mask;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public void MoveTo(Float2 position) => WorldPosition = position;
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
