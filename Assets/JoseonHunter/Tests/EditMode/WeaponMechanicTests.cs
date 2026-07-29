using System.Collections.Generic;
using System.Linq;
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

            NUnitMultipleCompat.Run(() =>
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

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(fixture.Executor.LastVolleyLaunchCount, Is.EqualTo(3));
                Assert.That(fixture.Executor.ActiveBladeCount, Is.EqualTo(3));
                Assert.That(fixture.Executor.DelayedBladeCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void FlyingBlade_LevelFive_UsesDistinctCurvedOutboundPositions()
        {
            var fixture = CreateFixture(new Float2(1f, 0f), 3);

            fixture.Executor.Tick(.08f, fixture.Context(1));
            var outbound = fixture.Executor.FirstActivePositionForTests;
            fixture.Executor.Tick(.08f, fixture.Context(2));
            var later = fixture.Executor.FirstActivePositionForTests;

            Assert.That(Mathf.Abs(later.Y - outbound.Y), Is.GreaterThan(.01f));
        }

        [Test]
        public void FlyingBlade_LevelFive_CrossesToTheOppositeWorldSpaceSideOnInbound()
        {
            var fixture = CreateFixture(new Float2(1f, 0f), 3);

            fixture.Executor.Tick(.1f, fixture.Context(1));
            var outbound = fixture.Executor.FirstActivePositionForTests;
            while (!fixture.Executor.FirstActiveInboundForTests)
                fixture.Executor.Tick(.1f, fixture.Context(2));
            var turnaround = fixture.Executor.FirstActivePositionForTests;

            fixture.Executor.Tick(.1f, fixture.Context(3));
            var inbound = fixture.Executor.FirstActivePositionForTests;

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(outbound.Y * inbound.Y, Is.LessThan(0f));
                Assert.That(inbound.X, Is.LessThan(turnaround.X));
                Assert.That(Mathf.Abs(inbound.Y), Is.GreaterThan(.01f));
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

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(bow.LastSelectedTargetRuntimeId, Is.EqualTo(2));
                Assert.That(bow.LastLaunchCount, Is.EqualTo(1));
                Assert.That(boss.Health, Is.EqualTo(100));
            });
        }

        [Test]
        public void EvolvedGakgungKeepsLevelFourPrimaryToOneImpactBeforeSunPiercerCadence()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var bow = new GakgungExecutor(runtime, 10f, 10f, 2f, 10f, 4, evolved: true);
            registry.Register(new TestTarget(1, new Float2(0.2f, 0f), mask));
            registry.Register(new TestTarget(2, new Float2(0.4f, 0f), mask));
            registry.Register(new TestTarget(3, new Float2(0.6f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += events.Add;

            bow.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));

            Assert.That(events, Has.Count.EqualTo(1));
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

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(singijeon.LastDirection.X, Is.GreaterThan(0f));
                Assert.That(singijeon.LastLaunchCount, Is.EqualTo(3));
                Assert.That(singijeon.ActiveProjectileCount, Is.EqualTo(1));
                Assert.That(singijeon.PendingLaunchCountForTests, Is.EqualTo(2));
            });
        }

        [Test]
        public void Singijeon_LevelFive_LaunchesAcrossMultipleTicks()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var singijeon = new SingijeonExecutor(runtime, 10f, 1f, 2f, 10f, 3, 5);
            registry.Register(new TestTarget(1, new Float2(10f, 0f), mask));
            var context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1);

            singijeon.Tick(.01f, context);
            var first = singijeon.ActiveProjectileCount;
            singijeon.Tick(.06f, context);

            Assert.That(singijeon.ActiveProjectileCount, Is.GreaterThan(first));
        }

        [Test]
        public void LinearProjectileSpec_ClampsVisualAnimationTiming()
        {
            var attack = new AttackInstance(1, RepeatHitPolicy.OncePerInstance, 0f);

            var spec = new LinearProjectileSpec(
                attack,
                WeaponId.GakgungShot,
                default,
                new Float2(1f, 0f),
                1f,
                1f,
                1,
                1,
                "Animated Arrow",
                visualPartStart: 4,
                visualFrameCount: 0,
                visualFrameSeconds: 0f);

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(spec.VisualPartStart, Is.EqualTo(4));
                Assert.That(spec.VisualFrameCount, Is.EqualTo(1));
                Assert.That(spec.VisualFrameSeconds, Is.EqualTo(.01f));
            });
        }

        [Test]
        public void LinearProjectile_RecordsOnlyAConfirmedImpactContact()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var linear = new LinearProjectileExecutor(runtime);
            registry.Register(new TestTarget(1, new Float2(.2f, 0f), mask));
            var context = new WeaponExecutionContext(default, root.transform, null, 0, 1);
            linear.Launch(context, new LinearProjectileSpec(
                new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.GakgungShot,
                default,
                new Float2(1f, 0f),
                10f,
                .1f,
                10,
                1,
                "Contact Probe"));

            linear.Tick(.1f, context);

            Assert.That(linear.HasLastImpactContactForTests, Is.True);
            Assert.That(linear.LastImpactContactForTests.X, Is.InRange(-.5f, .5f));
        }

        [Test]
        public void EvolvedGakgung_ClampsSunPiercerVisualScale()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var bow = new GakgungExecutor(runtime, 10f, .01f, 4f, 10f, 5, evolved: true);
            registry.Register(new TestTarget(1, new Float2(3f, 0f), mask, health: 10000));

            for (var tick = 1; tick <= 4; tick++)
                bow.Tick(.02f, new WeaponExecutionContext(default, root.transform, null, 0, tick));

            Assert.That(bow.LastProjectileScale, Is.InRange(.72f, 1.08f));
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

            NUnitMultipleCompat.Run(() =>
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

            NUnitMultipleCompat.Run(() =>
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

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(singijeon.LaneCount, Is.EqualTo(SingijeonExecutor.MaxLaneCount));
                Assert.That(singijeon.LastLaunchCount, Is.EqualTo(SingijeonExecutor.MaxLaneCount * 3));
                Assert.That(singijeon.ActiveProjectileCount, Is.EqualTo(1));
                Assert.That(
                    singijeon.ActiveProjectileCount + singijeon.PendingLaunchCountForTests,
                    Is.EqualTo(SingijeonExecutor.MaxLaneCount * 3));
            });

            var linear = new LinearProjectileExecutor(runtime);
            var context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2);
            var requested = new LinearProjectileSpec(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.GakgungShot, new Float2(0f, 0f), new Float2(1f, 0f), 1f, 0.01f, 1, 999, "Cap Test");
            for (var index = 0; index < LinearProjectileExecutor.MaxActiveProjectiles + 10; index++) linear.Launch(context, requested);
            Assert.That(linear.ActiveCount, Is.EqualTo(LinearProjectileExecutor.MaxActiveProjectiles));
            linear.Tick(0.01f, context);

            NUnitMultipleCompat.Run(() =>
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

            NUnitMultipleCompat.Run(() =>
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

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.Health, Is.EqualTo(1900));
                Assert.That(damage.TrackedAttackCount, Is.Zero);
            });
        }

        [Test]
        public void TalismanSequencesDirectAttachAndSealBeforeUniqueTransfersThenBursts()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var talisman = new TalismanExecutor(runtime, 10f, 10f, 3f, 20f, 3, 1);
            var first = new TestTarget(1, new Float2(0.2f, 0f), mask);
            var second = new TestTarget(2, new Float2(0.4f, 0f), mask);
            var third = new TestTarget(3, new Float2(0.6f, 0f), mask);
            registry.Register(first); registry.Register(second); registry.Register(third);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            for (var tick = 1; tick <= 12; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(events[0].Phase, Is.EqualTo(ContactPhase.Direct));
                Assert.That(events[1].Phase, Is.EqualTo(ContactPhase.Attach));
                Assert.That(events[2].Phase, Is.EqualTo(ContactPhase.Seal));
                Assert.That(events.Select(confirmed => confirmed.TargetRuntimeId).Distinct().Count(), Is.EqualTo(3));
                Assert.That(events[events.Count - 1].Phase, Is.EqualTo(ContactPhase.Blast));
                Assert.That(talisman.ActiveCastCount, Is.Zero);
                Assert.That(damage.TrackedAttackCount, Is.Zero);
            });
        }

        [Test]
        public void TalismanSafelyBurstsOnceWhenNoTransferTargetExists()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var talisman = new TalismanExecutor(runtime, 10f, 10f, 2f, 20f, 5, 1);
            registry.Register(new TestTarget(1, new Float2(0.2f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            for (var tick = 1; tick <= 5; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Blast), Is.EqualTo(1));
            Assert.That(talisman.LastFinalBurstCount, Is.EqualTo(1));
        }

        [Test]
        public void LevelFiveTalismansHoldSeveralSealsThenResolveOneBindingBurst()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var talisman = new TalismanExecutor(runtime, 10f, 10f, 2f, 20f, 1, 5);
            registry.Register(new TestTarget(1, new Float2(0.2f, 0f), mask)); registry.Register(new TestTarget(2, new Float2(0.4f, 0f), mask));
            registry.Register(new TestTarget(3, new Float2(0.6f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            for (var tick = 1; tick <= 3; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Attach), Is.EqualTo(3));
                Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Blast), Is.EqualTo(3));
                Assert.That(events.Where(confirmed => confirmed.Phase == ContactPhase.Blast).Select(confirmed => confirmed.SimulationTick).Distinct().Count(), Is.EqualTo(1));
                Assert.That(talisman.LastFinalBurstCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void LevelFiveShortCooldownDoesNotMixAnActiveBindingCastWithItsNextCast()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var talisman = new TalismanExecutor(runtime, 10f, 0.01f, 2f, 20f, 1, 5);
            registry.Register(new TestTarget(1, new Float2(0.2f, 0f), mask)); registry.Register(new TestTarget(2, new Float2(0.4f, 0f), mask));
            registry.Register(new TestTarget(3, new Float2(0.6f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            for (var tick = 1; tick <= 4; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            var bursts = events.Where(confirmed => confirmed.Phase == ContactPhase.Blast).ToArray();
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(bursts, Has.Length.EqualTo(3));
                Assert.That(bursts.Select(confirmed => confirmed.AttackInstanceId).Distinct().Count(), Is.EqualTo(1));
                Assert.That(bursts.Select(confirmed => confirmed.SimulationTick).Distinct(), Is.EqualTo(new[] { 3 }));
                Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Attach && confirmed.SimulationTick == 1), Is.EqualTo(3));
                Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Attach && confirmed.SimulationTick == 4), Is.EqualTo(3));
            });
        }

        [Test]
        public void LevelFiveNonOverlappingLiveTargetCompletesAndCanLaunchAgain()
        {
            var attackMask = PixelHitMask.FromRows("1");
            var nonOverlappingHurtMask = PixelHitMask.FromRows("0");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, attackMask);
            var talisman = new TalismanExecutor(runtime, 10f, 0.01f, 2f, 20f, 1, 5);
            registry.Register(new TestTarget(1, new Float2(0.2f, 0f), nonOverlappingHurtMask));

            talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(talisman.ActiveCastCount, Is.Zero);
                Assert.That(damage.TrackedAttackCount, Is.Zero);
                Assert.That(talisman.LastFinalBurstCount, Is.Zero);
                Assert.That(talisman.TotalLaunchedTalismanCount, Is.EqualTo(1));
            });

            talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(talisman.ActiveCastCount, Is.Zero);
                Assert.That(damage.TrackedAttackCount, Is.Zero);
                Assert.That(talisman.TotalLaunchedTalismanCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void LevelFiveBindingExcludesTargetWhoseSealCannotBeConfirmed()
        {
            var mask = PixelHitMask.FromRows("1");
            var noContactMask = PixelHitMask.FromRows("0");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var talisman = new TalismanExecutor(runtime, 10f, 10f, 2f, 20f, 1, 5);
            var failedSeal = new TestTarget(1, new Float2(0.2f, 0f), mask);
            registry.Register(failedSeal); registry.Register(new TestTarget(2, new Float2(0.4f, 0f), mask));
            registry.Register(new TestTarget(3, new Float2(0.6f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));
            talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2));
            failedSeal.SetHurtMask(noContactMask);
            talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 3));
            for (var tick = 4; tick <= 6; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(events.Any(confirmed => confirmed.Phase == ContactPhase.Seal && confirmed.TargetRuntimeId == 1), Is.False);
                Assert.That(events.Any(confirmed => confirmed.Phase == ContactPhase.Blast && confirmed.TargetRuntimeId == 1), Is.False);
                Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Blast), Is.EqualTo(2));
            });
        }

        [Test]
        public void FailedTransferContactSkipsTheAttemptedTargetAndMovesToAnotherLegalTarget()
        {
            var mask = PixelHitMask.FromRows("1");
            var noContactMask = PixelHitMask.FromRows("0");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var talisman = new TalismanExecutor(runtime, 10f, 10f, 2f, 20f, 2, 1);
            var first = new TestTarget(1, new Float2(0.2f, 0f), mask);
            var failedTransfer = new TestTarget(2, new Float2(0.4f, 0f), mask);
            var replacement = new TestTarget(3, new Float2(0.6f, 0f), mask);
            registry.Register(first); registry.Register(failedTransfer); registry.Register(replacement);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            for (var tick = 1; tick <= 3; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));
            failedTransfer.SetHurtMask(noContactMask);
            for (var tick = 4; tick <= 7; tick++) talisman.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(events.Any(confirmed => confirmed.TargetRuntimeId == 2), Is.False);
                Assert.That(events.Any(confirmed => confirmed.TargetRuntimeId == 3 && confirmed.Phase == ContactPhase.Direct), Is.True);
                Assert.That(events.Count(confirmed => confirmed.TargetRuntimeId == 3 && confirmed.Phase == ContactPhase.Blast), Is.EqualTo(1));
                Assert.That(talisman.ActiveCastCount, Is.Zero);
            });
        }

        [Test]
        public void WindThunderFanKnocksBackWindContactsBeforeSimultaneousMarkedLightning()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var fan = new WindThunderFanExecutor(runtime, 10f, 10f, 2f, 3f, 2, 1);
            var first = new TestTarget(1, new Float2(0.5f, 0f), mask, threatScore: 10f);
            var second = new TestTarget(2, new Float2(0.8f, 0.1f), mask, threatScore: 5f);
            var outside = new TestTarget(3, new Float2(-0.5f, 0f), mask, threatScore: 0f);
            registry.Register(first); registry.Register(second); registry.Register(outside);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            fan.Tick(0.01f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));
            fan.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2));
            fan.Tick(0.01f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 3));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(first.KnockbackCount, Is.EqualTo(1));
                Assert.That(second.KnockbackCount, Is.EqualTo(1));
                Assert.That(outside.KnockbackCount, Is.Zero);
                Assert.That(events.Take(2).All(confirmed => confirmed.Phase == ContactPhase.Wind), Is.True);
                Assert.That(events.Skip(2).All(confirmed => confirmed.Phase == ContactPhase.Lightning && confirmed.SimulationTick == 3), Is.True);
                Assert.That(events.Skip(2).Select(confirmed => confirmed.TargetRuntimeId), Is.EquivalentTo(new[] { 1, 2 }));
                Assert.That(fan.LastLightningSimulationTick, Is.EqualTo(3));
            });
        }

        [Test]
        public void LevelFiveFanEmitsFourCardinalGustsBeforeOneBoundedEcho()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var fan = new WindThunderFanExecutor(runtime, 10f, 10f, 2f, 1f, 4, 5);
            registry.Register(new TestTarget(1, new Float2(0.5f, 0f), mask)); registry.Register(new TestTarget(2, new Float2(0f, 0.5f), mask));
            registry.Register(new TestTarget(3, new Float2(-0.5f, 0f), mask)); registry.Register(new TestTarget(4, new Float2(0f, -0.5f), mask));
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            for (var tick = 1; tick <= 6; tick++) fan.Tick(0.2f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, tick));

            Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Wind), Is.EqualTo(4));
            Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Lightning), Is.EqualTo(4));
        }

        [Test]
        public void ThunderBombDealsDamageOnlyWhenItsExpandingPixelRingReachesTheTarget()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var bomb = new ThunderBombExecutor(runtime, 10f, 10f, 3f, 0.1f, 0.1f, 1f, 1);
            var target = new TestTarget(1, new Float2(1.3f, 0f), mask); registry.Register(target);
            registry.Register(new TestTarget(2, new Float2(0f, 0f), mask));
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            bomb.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));
            bomb.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2));
            bomb.Tick(0.06f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 3));
            Assert.That(events, Is.Empty, "Fuse completion and an undersized ring must not deal center damage.");

            bomb.Tick(0.12f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 4));
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(events, Has.Count.EqualTo(2));
                Assert.That(events[0].Phase, Is.EqualTo(ContactPhase.Blast));
                Assert.That(target.Health, Is.EqualTo(90));
            });
        }

        [Test]
        public void FrostFieldSlowsTicksFreezesDecaysAndExpiresItsOldestFieldAtCapacity()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var frost = new FrostFlaskExecutor(runtime, 10f, 10f, 2f, 0.1f, 2f, 1f, 1, 1);
            var target = new TestTarget(1, new Float2(0.4f, 0f), mask); registry.Register(target);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            frost.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 1));
            frost.Tick(0.25f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 2));
            frost.Tick(0.25f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 3));
            frost.Tick(0.25f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 4));
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.SlowApplications, Is.GreaterThan(0));
                Assert.That(events.Count(confirmed => confirmed.Phase == ContactPhase.Tick), Is.EqualTo(3));
                Assert.That(target.FreezeCount, Is.EqualTo(1));
            });

            target.MoveTo(new Float2(4f, 0f));
            frost.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 5));
            frost.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 6));
            target.MoveTo(new Float2(0.4f, 0f));
            var capacityFrost = new FrostFlaskExecutor(runtime, 10f, 0.1f, 2f, 0.1f, 2f, 1f, 1, 1);
            capacityFrost.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 7));
            capacityFrost.Tick(0.1f, new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null, 0, 8));
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.LastSlowStrength, Is.Zero);
                Assert.That(target.LastSlowDecay, Is.GreaterThan(0f));
                Assert.That(capacityFrost.ExpiredFieldCount, Is.EqualTo(1));
                Assert.That(capacityFrost.ActiveFieldCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void FrostStatusSourcesRetainTheStrongestOverlapUntilTheirOwnFieldExits()
        {
            var target = new TestTarget(1, default, PixelHitMask.FromRows("1"));

            target.ApplyFrostSlow(101, 0.6f);
            target.ApplyFrostSlow(202, 0.35f);
            target.RemoveFrostSlow(101, 0.35f);
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.LastSlowStrength, Is.EqualTo(0.35f));
                Assert.That(target.ActiveSlowSourceCount, Is.EqualTo(1));
            });

            target.RemoveFrostSlow(202, 0.35f);
            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.LastSlowStrength, Is.Zero);
                Assert.That(target.ActiveSlowSourceCount, Is.Zero);
                Assert.That(target.LastSlowDecay, Is.EqualTo(0.35f));
            });
        }

        [Test]
        public void FrostResetRemovesEveryActiveFieldSourceAndRetiresItsAttacks()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var frost = new FrostFlaskExecutor(runtime, 10f, 0.2f, 2f, 0.1f, 2f, 1f, 2, 1);
            var target = new TestTarget(1, new Float2(0.4f, 0f), mask); registry.Register(target);

            for (var tick = 1; tick <= 4; tick++) frost.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, tick));
            Assert.That(target.ActiveSlowSourceCount, Is.EqualTo(2));

            frost.Reset();

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.ActiveSlowSourceCount, Is.Zero);
                Assert.That(damage.TrackedAttackCount, Is.Zero);
                Assert.That(frost.ActiveFieldCount, Is.Zero);
            });
        }

        [Test]
        public void FrostCapacityEvictionRemovesTheOldestSourceBeforeThatTickCanAdvanceIt()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var frost = new FrostFlaskExecutor(runtime, 10f, 0.2f, 2f, 0.1f, 2f, 1f, 1, 1);
            var target = new TestTarget(1, new Float2(0.4f, 0f), mask); registry.Register(target);

            frost.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            frost.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            Assert.That(target.ActiveSlowSourceCount, Is.EqualTo(1));

            frost.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 3));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(target.ActiveSlowSourceCount, Is.Zero);
                Assert.That(damage.TrackedAttackCount, Is.Zero);
                Assert.That(frost.ActiveFieldCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void ThunderBombLargeBlastStepSweepsAnIntermediateRingContact()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var bomb = new ThunderBombExecutor(runtime, 10f, 10f, 3f, 0.01f, 0f, 1f, 1);
            var target = new TestTarget(1, new Float2(1.3f, 0f), mask); registry.Register(target);
            registry.Register(new TestTarget(2, new Float2(0f, 0f), mask));

            bomb.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            bomb.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            bomb.Tick(1f, new WeaponExecutionContext(default, root.transform, null, 0, 3));

            Assert.That(target.Health, Is.EqualTo(90));
        }

        [Test]
        public void JangseungWardDamagesOnlyAConfirmedBoundaryCrossingAndRequiresLeaveBeforeReentry()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var ward = new JangseungWardExecutor(runtime, 10f, 10f, 1f, 2, 1, 0.6f, 1);
            var target = new TestTarget(1, new Float2(0f, -0.5f), mask); registry.Register(target);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            target.MoveTo(new Float2(0.2f, -0.4f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            Assert.That(events, Is.Empty, "Movement on one side must not become area damage.");

            target.MoveTo(new Float2(0.2f, 0.5f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 3));
            target.MoveTo(new Float2(0.3f, 0f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 4));
            target.MoveTo(new Float2(0.3f, -0.5f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 5));
            target.MoveTo(new Float2(0.3f, 0.5f));
            ward.Tick(0.3f, new WeaponExecutionContext(default, root.transform, null, 0, 6));
            Assert.That(events, Has.Count.EqualTo(1), "Three simulation ticks are insufficient when only 0.5 real seconds elapsed.");
            target.MoveTo(new Float2(0.3f, -0.5f));
            ward.Tick(0.3f, new WeaponExecutionContext(default, root.transform, null, 0, 7));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(events.Count, Is.EqualTo(2));
                Assert.That(events.All(confirmed => confirmed.Phase == ContactPhase.BoundaryCrossing), Is.True);
                Assert.That(target.KnockbackCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void JangseungWardEvictsTheOldestFiniteSetAndRetiresItsAttack()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var ward = new JangseungWardExecutor(runtime, 10f, 0.1f, 1f, 2, 1, 0f, 1);

            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(ward.ActiveWardSetCount, Is.EqualTo(1));
                Assert.That(ward.EvictedWardSetCount, Is.EqualTo(1));
                Assert.That(damage.TrackedAttackCount, Is.Zero, "Unhit ward attacks must not become tracked and evicted attacks are retired either way.");
            });
        }

        [Test]
        public void LevelFiveJangseungMaintainsFourCardinalPosts()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var ward = new JangseungWardExecutor(runtime, 10f, 10f, 1f, 2, 2, 0f, 5);

            ward.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));

            NUnitMultipleCompat.Run(() =>
            {
                Assert.That(ward.ActiveWardSetCount, Is.EqualTo(1));
                Assert.That(ward.ActivePostCount, Is.EqualTo(4));
            });
        }

        [Test]
        public void JangseungWardResamplesCenteredPpu32MaskAndRejectsTransparentCrossings()
        {
            var hurtMask = new PixelHitMask(1, 1, Vector2.zero, 32f, new uint[] { 1u });
            // The middle source column is transparent; the two outer columns survive nearest-neighbor stretching.
            var wardMask = new PixelHitMask(3, 3, new Vector2(1f, 1f), 32f, new uint[] { 365u });
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, hurtMask);
            var ward = new JangseungWardExecutor(runtime, wardMask, 10f, 10f, 1f, 2, 1, 0f, 1);
            var target = new TestTarget(1, new Float2(0f, -0.5f), hurtMask); registry.Register(target);

            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            target.MoveTo(new Float2(0f, 0.5f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            Assert.That(target.Health, Is.EqualTo(100), "A transparent authored segment column must not confirm a crossing.");

            target.MoveTo(new Float2(0.75f, 0.5f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 3));
            target.MoveTo(new Float2(0.75f, -0.5f));
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 4));
            Assert.That(target.Health, Is.EqualTo(90), "An opaque PPU32 resampled column should confirm the finite crossing.");
        }

        [Test]
        public void JangseungWardUsesExactMovementCrossingTimeForLargeFrames()
        {
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry(); var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var ward = new JangseungWardExecutor(runtime, 10f, 10f, 1f, 2, 1, 0.5f, 1);
            var target = new TestTarget(1, new Float2(0f, -0.8f), mask); registry.Register(target);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;

            ward.Tick(0f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            target.MoveTo(new Float2(0f, 0.2f));
            ward.Tick(1f, new WeaponExecutionContext(default, root.transform, null, 0, 2)); // crosses at 0.8s
            target.MoveTo(new Float2(0f, -0.8f));
            ward.Tick(1f, new WeaponExecutionContext(default, root.transform, null, 0, 3)); // crosses at 1.2s

            Assert.That(events, Has.Count.EqualTo(1), "The 0.4 second re-entry gap must not be rounded to the two frame-end timestamps.");
        }

        [Test]
        public void JangseungWardPpu32MaskIncludesEndpointsAndRotatedFiniteSegments()
        {
            var mask = new PixelHitMask(1, 1, Vector2.zero, 32f, new uint[] { 1u });
            var opaque = new PixelHitMask(3, 3, new Vector2(1f, 1f), 32f, new uint[] { 511u });
            var endpointRegistry = new CombatTargetRegistry(); var endpointDamage = new CombatDamageService(endpointRegistry);
            var endpointWard = new JangseungWardExecutor(new WeaponRuntimeController(endpointRegistry, endpointDamage, mask), opaque, 10f, 10f, 1f, 2, 1, 0f, 1);
            var endpointTarget = new TestTarget(1, new Float2(1f, -0.5f), mask); endpointRegistry.Register(endpointTarget);

            endpointWard.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            endpointTarget.MoveTo(new Float2(1f, 0.5f));
            endpointWard.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            Assert.That(endpointTarget.Health, Is.EqualTo(90), "The stretched mask must contain its finite endpoint.");

            var diagonalRegistry = new CombatTargetRegistry(); var diagonalDamage = new CombatDamageService(diagonalRegistry);
            var diagonalWard = new JangseungWardExecutor(new WeaponRuntimeController(diagonalRegistry, diagonalDamage, mask), opaque, 10f, 10f, 1f, 3, 1, 0f, 1);
            var diagonalTarget = new TestTarget(2, new Float2(0.5f, 0f), mask); diagonalRegistry.Register(diagonalTarget);
            diagonalWard.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            diagonalTarget.MoveTo(new Float2(0.5f, 1f));
            diagonalWard.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));

            Assert.That(diagonalTarget.Health, Is.EqualTo(90), "A 45-degree finite segment must preserve its PPU32 endpoint-aligned mask geometry.");
        }

        [Test]
        public void RuntimeDisposeDisposesRegisteredExecutorForLevelReplacement()
        {
            var registry = new CombatTargetRegistry();
            var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), PixelHitMask.FromRows("1"));
            var executor = new DisposeProbeExecutor();
            runtime.Register(executor);

            runtime.Dispose();

            Assert.That(executor.DisposeCount, Is.EqualTo(1));
            runtime.Dispose();
            Assert.That(executor.DisposeCount, Is.EqualTo(1), "A repeated teardown must not dispose presentation pools twice.");
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

        private sealed class DisposeProbeExecutor : IWeaponExecutor
        {
            public int DisposeCount { get; private set; }
            public void Tick(float deltaTime, in WeaponExecutionContext context) { }
            public void Reset() { }
            public void Dispose() => DisposeCount++;
        }

        private sealed class TestTarget : ICombatTarget, IFrostStatusTarget
        {
            private PixelHitMask mask;
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
            public int KnockbackCount { get; private set; }
            public int SlowApplications { get; private set; }
            public int FreezeCount { get; private set; }
            public int ActiveSlowSourceCount => slowSources.Count;
            public float LastSlowStrength { get; private set; }
            public float LastSlowDecay { get; private set; }
            private readonly Dictionary<int, float> slowSources = new Dictionary<int, float>();
            public PixelHitMask HurtMask => mask;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public void MoveTo(Float2 position) => WorldPosition = position;
            public void SetHurtMask(PixelHitMask value) => mask = value;
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) => KnockbackCount++;
            public void ApplyFrostSlow(int sourceId, float strength) { slowSources[sourceId] = strength; SlowApplications++; LastSlowStrength = StrongestSlow(); }
            public void RemoveFrostSlow(int sourceId, float decaySeconds) { slowSources.Remove(sourceId); LastSlowStrength = StrongestSlow(); LastSlowDecay = decaySeconds; }
            public void ApplyFreeze(int sourceId, float durationSeconds) => FreezeCount++;
            private float StrongestSlow() { var result = 1f; foreach (var source in slowSources) result = Mathf.Min(result, source.Value); return slowSources.Count == 0 ? 0f : result; }
        }
    }
}
