using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class HwandoGakgungLegacyPlayModeTests
    {
        [Test]
        public void Chosen_paths_apply_their_approved_direct_costs()
        {
            using var rig = new Rig();
            var venom = new FlyingBladeExecutor(rig.Runtime, 100f, 2f, 4f, 8f, 1, 3, false,
                Modifiers(WeaponLegacyPathId.HwandoVenom, WeaponLegacyStage.Chosen));
            var moon = new FlyingBladeExecutor(rig.Runtime, 100f, 2f, 4f, 8f, 1, 3, false,
                Modifiers(WeaponLegacyPathId.HwandoMoonEclipse, WeaponLegacyStage.Chosen));
            var sun = new GakgungExecutor(rig.Runtime, 100f, 2f, 6f, 10f, 3, false,
                Modifiers(WeaponLegacyPathId.GakgungSunPiercer, WeaponLegacyStage.Chosen));
            var split = new GakgungExecutor(rig.Runtime, 100f, 2f, 6f, 10f, 3, false,
                Modifiers(WeaponLegacyPathId.GakgungSplitFletching, WeaponLegacyStage.Chosen));

            Assert.That(venom.BaseDamage, Is.EqualTo(80f).Within(.001f));
            Assert.That(moon.CooldownSeconds, Is.EqualTo(2.4f).Within(.001f));
            Assert.That(sun.CooldownSeconds, Is.EqualTo(2.5f).Within(.001f));
            Assert.That(split.BaseDamage, Is.EqualTo(75f).Within(.001f));
            venom.Dispose(); moon.Dispose(); sun.Dispose(); split.Dispose();
        }

        [UnityTest]
        public IEnumerator Split_fletching_launches_three_five_then_completed_fourth_volley_seven()
        {
            using var chosen = new Rig(health: 100000);
            var chosenExecutor = chosen.Gakgung(WeaponLegacyPathId.GakgungSplitFletching,
                WeaponLegacyStage.Chosen, 3, cooldown: .01f);
            chosenExecutor.Tick(.02f, chosen.Context);
            Assert.That(chosenExecutor.LastLaunchCount, Is.EqualTo(3));

            using var reinforced = new Rig(health: 100000);
            var reinforcedExecutor = reinforced.Gakgung(WeaponLegacyPathId.GakgungSplitFletching,
                WeaponLegacyStage.Reinforced, 4, cooldown: .01f);
            reinforcedExecutor.Tick(.02f, reinforced.Context);
            Assert.That(reinforcedExecutor.LastLaunchCount, Is.EqualTo(5));

            using var completed = new Rig(health: 100000);
            var completedExecutor = completed.Gakgung(WeaponLegacyPathId.GakgungSplitFletching,
                WeaponLegacyStage.Completed, 5, cooldown: .01f);
            for (var volley = 0; volley < 4; volley++) completedExecutor.Tick(.02f, completed.Context);
            Assert.That(completedExecutor.LastLaunchCount, Is.EqualTo(7));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Venom_applies_four_second_poison_and_moon_return_hits_for_seventy_percent()
        {
            using var venomRig = new Rig(health: 10000);
            var venom = venomRig.Hwando(WeaponLegacyPathId.HwandoVenom, WeaponLegacyStage.Chosen);
            var venomEvents = new List<ConfirmedDamageEvent>();
            venomRig.Damage.DamageConfirmed += venomEvents.Add;
            for (var tick = 0; tick < 80 && !venomRig.Runtime.AffixStatuses.HasStatus(
                     venomRig.Target.RuntimeId, CombatStatusKind.Poison); tick++)
                venom.Tick(.05f, venomRig.Context);
            Assert.That(venomRig.Runtime.AffixStatuses.HasStatus(venomRig.Target.RuntimeId,
                CombatStatusKind.Poison), Is.True);
            Assert.That(venomEvents.Where(value => value.Phase == ContactPhase.Outbound ||
                value.Phase == ContactPhase.Inbound).All(value => value.FinalDamage == 8), Is.True);
            var transfers = new[]
            {
                venomRig.AddTarget(2, new Float2(.8f, 0f)),
                venomRig.AddTarget(3, new Float2(1f, 0f)),
                venomRig.AddTarget(4, new Float2(1.2f, 0f)),
                venomRig.AddTarget(5, new Float2(1.4f, 0f))
            };
            venomRig.Damage.TryApply(WeaponDamageRequest.Create(new AttackInstance(990,
                    RepeatHitPolicy.OncePerInstance, 0f), WeaponId.ThunderCrashBomb, venomRig.Target,
                20000, false, venomRig.Target.WorldPosition, ContactPhase.Blast, 90), out _);
            Assert.That(transfers.Count(target => venomRig.Runtime.AffixStatuses.HasStatus(target.RuntimeId,
                CombatStatusKind.Poison)), Is.EqualTo(3));

            using var moonRig = new Rig(health: 10000);
            var moon = moonRig.Hwando(WeaponLegacyPathId.HwandoMoonEclipse, WeaponLegacyStage.Chosen);
            var moonEvents = new List<ConfirmedDamageEvent>();
            moonRig.Damage.DamageConfirmed += moonEvents.Add;
            for (var tick = 0; tick < 120 && moonEvents.All(value => value.Phase != ContactPhase.PotentialChain); tick++)
                moon.Tick(.05f, moonRig.Context);
            Assert.That(moonEvents.Any(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 7), Is.True);

            using var completedMoonRig = new Rig(health: 10000);
            var completedMoon = completedMoonRig.Hwando(WeaponLegacyPathId.HwandoMoonEclipse,
                WeaponLegacyStage.Completed);
            var completedMoonEvents = new List<ConfirmedDamageEvent>();
            completedMoonRig.Damage.DamageConfirmed += completedMoonEvents.Add;
            for (var tick = 0; tick < 180 && completedMoonEvents.All(value => value.Phase != ContactPhase.Blast); tick++)
                completedMoon.Tick(.05f, completedMoonRig.Context);
            Assert.That(completedMoonEvents.Any(value => value.Phase == ContactPhase.Blast &&
                value.FinalDamage == 22), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sun_piercer_has_four_impacts_and_reinforced_armor_break_but_split_does_not()
        {
            using var sunRig = new Rig(health: 10000);
            var sun = sunRig.Gakgung(WeaponLegacyPathId.GakgungSunPiercer,
                WeaponLegacyStage.Reinforced, 4, .01f);
            for (var tick = 0; tick < 60 && !sunRig.Runtime.AffixStatuses.HasStatus(
                     sunRig.Target.RuntimeId, CombatStatusKind.ArmorBreak); tick++)
                sun.Tick(.05f, sunRig.Context);
            Assert.That(sun.LastProjectileMaximumImpacts, Is.EqualTo(4));
            Assert.That(sun.LastLaunchCount, Is.EqualTo(1));
            Assert.That(sunRig.Runtime.AffixStatuses.HasStatus(sunRig.Target.RuntimeId,
                CombatStatusKind.ArmorBreak), Is.True);

            using var splitRig = new Rig(health: 10000);
            var split = splitRig.Gakgung(WeaponLegacyPathId.GakgungSplitFletching,
                WeaponLegacyStage.Reinforced, 4, .01f);
            for (var tick = 0; tick < 60; tick++) split.Tick(.05f, splitRig.Context);
            Assert.That(splitRig.Runtime.AffixStatuses.HasStatus(splitRig.Target.RuntimeId,
                CombatStatusKind.ArmorBreak), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Completed_sun_piercer_scales_each_penetration_caps_and_adds_boss_bonus()
        {
            using var rig = new Rig(health: 10000);
            rig.AddTarget(2, new Float2(1f, 0f));
            rig.AddTarget(3, new Float2(1.4f, 0f));
            rig.AddTarget(4, new Float2(1.8f, 0f), isBoss: true);
            var executor = rig.Gakgung(WeaponLegacyPathId.GakgungSunPiercer,
                WeaponLegacyStage.Completed, 5, 5f);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;
            for (var tick = 0; tick < 80 && events.Count(value => value.Phase == ContactPhase.Direct) < 4; tick++)
                executor.Tick(.05f, rig.Context);

            var direct = events.Where(value => value.Phase == ContactPhase.Direct).Select(value => value.FinalDamage)
                .ToArray();
            Assert.That(direct, Is.EqualTo(new[] { 10, 12, 13, 20 }));
            Assert.That(events.Any(value => value.Phase == ContactPhase.Blast && value.FinalDamage == 18), Is.True);
            yield return null;
        }

        [Test]
        public void Completed_venom_focuses_poisoned_enemy_while_moon_path_does_not()
        {
            using var venomRig = new Rig(health: 10000);
            var poisoned = venomRig.AddTarget(2, new Float2(.9f, 0f));
            venomRig.Runtime.AffixStatuses.ApplyTimedStatus(poisoned.RuntimeId, CombatStatusKind.Poison,
                2f, 1, WeaponId.HwandoFlyingBlade);
            var venom = venomRig.Hwando(WeaponLegacyPathId.HwandoVenom, WeaponLegacyStage.Completed);
            venom.Tick(.01f, venomRig.Context);
            Assert.That(venom.LastSelectedTargetRuntimeId, Is.EqualTo(poisoned.RuntimeId));

            using var moonRig = new Rig(health: 10000);
            var moonPoisoned = moonRig.AddTarget(2, new Float2(.9f, 0f));
            moonRig.Runtime.AffixStatuses.ApplyTimedStatus(moonPoisoned.RuntimeId, CombatStatusKind.Poison,
                2f, 1, WeaponId.HwandoFlyingBlade);
            var moon = moonRig.Hwando(WeaponLegacyPathId.HwandoMoonEclipse, WeaponLegacyStage.Completed);
            moon.Tick(.01f, moonRig.Context);
            Assert.That(moon.LastSelectedTargetRuntimeId, Is.EqualTo(moonRig.Target.RuntimeId));
        }

        private static WeaponRuntimeModifiers Modifiers(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
            WeaponRuntimeModifiers.From(null, new WeaponLegacySnapshot(path, stage));

        private sealed class Rig : System.IDisposable
        {
            private readonly GameObject root = new("Legacy Combat Root");
            private readonly PixelHitMask mask = PixelHitMask.FromRows("1");
            public Rig(int health = 1000)
            {
                Registry = new CombatTargetRegistry();
                Damage = new CombatDamageService(Registry);
                Runtime = new WeaponRuntimeController(Registry, Damage, mask);
                Target = new Target(1, health, new Float2(.6f, 0f), mask);
                Registry.Register(Target);
                Context = new WeaponExecutionContext(new Float2(0f, 0f), root.transform, null,
                    _ => null, _ => mask, 0, 1);
            }
            public CombatTargetRegistry Registry { get; }
            public CombatDamageService Damage { get; }
            public WeaponRuntimeController Runtime { get; }
            public Target Target { get; }
            public WeaponExecutionContext Context { get; }
            public FlyingBladeExecutor Hwando(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
                new(Runtime, 10f, 5f, 1f, 2f, stage == WeaponLegacyStage.Completed ? 3 : 1,
                    stage == WeaponLegacyStage.Completed ? 5 : 3,
                    false, Modifiers(path, stage));
            public GakgungExecutor Gakgung(WeaponLegacyPathId path, WeaponLegacyStage stage, int level,
                float cooldown) => new(Runtime, 10f, cooldown, 4f, 8f, level, false, Modifiers(path, stage));
            public Target AddTarget(int id, Float2 position, bool isBoss = false)
            {
                var target = new Target(id, 10000, position, mask, isBoss);
                Registry.Register(target);
                return target;
            }
            public void Dispose()
            {
                Runtime.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private sealed class Target : ICombatTarget
        {
            private readonly bool isBoss;
            public Target(int id, int health, Float2 position, PixelHitMask mask, bool isBoss = false)
            { RuntimeId = id; Health = health; WorldPosition = position; HurtMask = mask; this.isBoss = isBoss; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => isBoss;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition { get; }
            public PixelHitMask HurtMask { get; }
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
