using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class JangseungSingijeonLegacyPlayModeTests
    {
        [Test]
        public void Chosen_paths_apply_costs_and_four_guardian_geometry()
        {
            using var rig = new Rig();
            var four = rig.Ward(WeaponLegacyPathId.JangseungFourGuardians,
                WeaponLegacyStage.Chosen);
            var descent = rig.Ward(WeaponLegacyPathId.JangseungGuardianDescent,
                WeaponLegacyStage.Chosen);
            var dragon = rig.Singijeon(WeaponLegacyPathId.SingijeonFireDragon,
                WeaponLegacyStage.Chosen);
            var net = rig.Singijeon(WeaponLegacyPathId.SingijeonFireNet,
                WeaponLegacyStage.Chosen);

            Assert.That(four.BaseDamage, Is.EqualTo(70f).Within(.001f));
            Assert.That(four.PostCount, Is.EqualTo(4));
            Assert.That(descent.LegacyWardLifetimeForTests, Is.EqualTo(6f).Within(.001f));
            Assert.That(dragon.Range, Is.EqualTo(3.25f).Within(.001f));
            Assert.That(net.BaseDamage, Is.EqualTo(70f).Within(.001f));
            four.Dispose(); descent.Dispose(); dragon.Dispose(); net.Dispose();
        }

        [UnityTest]
        public IEnumerator Completed_four_guardians_emits_three_synchronized_eighty_percent_pulses()
        {
            using var rig = new Rig(health: 10000);
            var executor = rig.Ward(WeaponLegacyPathId.JangseungFourGuardians,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;

            rig.Drive(executor, .8f);

            var pulses = events.Where(value => value.WeaponId.Equals(WeaponId.JangseungWard) &&
                value.Phase == ContactPhase.PotentialBlast).ToArray();
            Assert.That(executor.CompletedPulseCountForTests, Is.EqualTo(3));
            Assert.That(pulses.Count(value => value.TargetRuntimeId == rig.Target.RuntimeId),
                Is.EqualTo(3));
            Assert.That(pulses.All(value => value.FinalDamage == 80), Is.True);
            yield return null;
        }

        [Test]
        public void Reinforced_four_guardians_reduces_contact_damage_by_twenty_percent_while_warded()
        {
            using var rig = new Rig(health: 10000);
            var executor = rig.Ward(WeaponLegacyPathId.JangseungFourGuardians,
                WeaponLegacyStage.Reinforced);
            rig.Drive(executor, .1f);
            Assert.That(rig.Target.ContactDamageMultiplier, Is.EqualTo(.8f).Within(.001f));
            executor.Reset();
            Assert.That(rig.Target.ContactDamageMultiplier, Is.EqualTo(1f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator Guardian_descent_reinforces_second_slam_and_completed_replaces_it_with_center_slam()
        {
            using var reinforcedRig = new Rig(health: 10000);
            var reinforced = reinforcedRig.Ward(WeaponLegacyPathId.JangseungGuardianDescent,
                WeaponLegacyStage.Reinforced);
            var reinforcedEvents = new List<ConfirmedDamageEvent>();
            reinforcedRig.Damage.DamageConfirmed += reinforcedEvents.Add;
            reinforcedRig.Drive(reinforced, .7f);
            Assert.That(reinforcedEvents.Count(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 180), Is.EqualTo(2));

            using var completedRig = new Rig(health: 10000);
            var completed = completedRig.Ward(WeaponLegacyPathId.JangseungGuardianDescent,
                WeaponLegacyStage.Completed);
            var completedEvents = new List<ConfirmedDamageEvent>();
            completedRig.Damage.DamageConfirmed += completedEvents.Add;
            completedRig.Drive(completed, .7f);
            Assert.That(completedEvents.Any(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 180), Is.True);
            Assert.That(completedEvents.Any(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 320), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Fire_dragon_prioritizes_strongest_and_completed_fires_five_capped_salvos()
        {
            using var rig = new Rig(health: 10000);
            var strongest = rig.AddTarget(2, new Float2(1f, 0f), 10000, threat: 50f);
            rig.AddTarget(3, new Float2(.8f, 0f), 10000, threat: 10f);
            var executor = rig.Singijeon(WeaponLegacyPathId.SingijeonFireDragon,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;

            rig.Drive(executor, .8f);

            var salvos = events.Where(value => value.WeaponId.Equals(WeaponId.SingijeonVolley) &&
                value.Phase == ContactPhase.PotentialChain).ToArray();
            Assert.That(executor.LastFocusedTargetRuntimeIdForTests, Is.EqualTo(strongest.RuntimeId));
            Assert.That(executor.LastFocusedSalvoCountForTests, Is.EqualTo(5));
            Assert.That(salvos.Length, Is.EqualTo(5));
            Assert.That(salvos.All(value => value.TargetRuntimeId == strongest.RuntimeId &&
                value.FinalDamage == 32), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Fire_net_ticks_for_three_seconds_and_completed_detonates_connected_trail_once()
        {
            using var rig = new Rig(health: 10000);
            var executor = rig.Singijeon(WeaponLegacyPathId.SingijeonFireNet,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;

            rig.Drive(executor, 3.8f);

            Assert.That(events.Any(value => value.Phase == ContactPhase.Burn &&
                value.FinalDamage == 30), Is.True,
                string.Join(",", events.Select(value => $"{value.Phase}:{value.FinalDamage}")));
            Assert.That(events.Count(value => value.Phase == ContactPhase.Blast &&
                value.FinalDamage == 200 && value.TargetRuntimeId == rig.Target.RuntimeId),
                Is.EqualTo(1));
            Assert.That(executor.MaximumConnectedTrailEndpointsForTests,
                Is.LessThanOrEqualTo(24));
            executor.Reset();
            Assert.That(executor.ActiveTrailCountForTests, Is.Zero);
            Assert.That(rig.Damage.TrackedAttackCount, Is.Zero);
            yield return null;
        }

        [Test]
        public void Reinforced_fire_net_death_ignition_caps_at_three_nearby_targets()
        {
            using var rig = new Rig(health: 10000);
            for (var id = 2; id <= 6; id++)
                rig.AddTarget(id, new Float2(.35f + id * .08f, 0f), 10000, threat: id);
            var executor = rig.Singijeon(WeaponLegacyPathId.SingijeonFireNet,
                WeaponLegacyStage.Reinforced);
            rig.Drive(executor, 1f);
            rig.Kill(rig.TargetFor(executor.LastFireNetBurnTargetRuntimeIdForTests));
            Assert.That(executor.LastFireNetIgnitionCountForTests, Is.EqualTo(3));
        }

        [Test]
        public void Guardian_descent_uses_one_whole_silhouette_and_flat_palette()
        {
            var root = new GameObject("Guardian Presentation Test");
            var texture = new Texture2D(2, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 8).ToArray());
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 4), new Vector2(.5f, .5f), 4f);
            var presenter = new JangseungGuardianDescentPresenter(root.transform);
            presenter.Play(1, sprite, Vector2.zero, 2);

            Assert.That(presenter.ActiveSilhouetteCountForTests, Is.EqualTo(1));
            Assert.That(presenter.UsesCroppedGuardianPartsForTests, Is.False);
            Assert.That(presenter.ActivePaletteColorCountForTests, Is.LessThanOrEqualTo(3));
            Assert.That(presenter.UsesWhiteOutlineForTests, Is.False);
            presenter.Clear();
            Assert.That(presenter.ActiveSilhouetteCountForTests, Is.Zero);

            var wardPresenter = new JangseungWardPresenter(null, root.transform, 0);
            wardPresenter.ShowSet(2, new[]
            {
                new Float2(1f, 0f), new Float2(0f, 1f),
                new Float2(-1f, 0f), new Float2(0f, -1f)
            }, null);
            Assert.That(wardPresenter.ActivePaletteColorCountForTests, Is.LessThanOrEqualTo(3));
            Assert.That(wardPresenter.UsesWhiteOutlineForTests, Is.False);

            wardPresenter.Dispose();
            presenter.Dispose();
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(root);
        }

        private static WeaponRuntimeModifiers Modifiers(WeaponLegacyPathId path,
            WeaponLegacyStage stage) => WeaponRuntimeModifiers.From(null,
            new WeaponLegacySnapshot(path, stage));

        private sealed class Rig : System.IDisposable
        {
            private readonly GameObject root = new("Jangseung Singijeon Legacy Root");
            private readonly PixelHitMask mask = PixelHitMask.FromRows("111", "111", "111");
            private readonly Dictionary<int, Target> targets = new();
            private int tick;

            public Rig(int health = 1000)
            {
                Registry = new CombatTargetRegistry();
                Damage = new CombatDamageService(Registry);
                Runtime = new WeaponRuntimeController(Registry, Damage, mask);
                Target = new Target(1, health, new Float2(.35f, 0f), mask, 1f);
                Registry.Register(Target);
                targets.Add(Target.RuntimeId, Target);
            }

            public CombatTargetRegistry Registry { get; }
            public CombatDamageService Damage { get; }
            public WeaponRuntimeController Runtime { get; }
            public Target Target { get; }

            public JangseungWardExecutor Ward(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
                new(Runtime, mask, 100f, 10f, 1.5f, 2, 1, .2f,
                    stage == WeaponLegacyStage.Completed ? 5 : stage == WeaponLegacyStage.Reinforced ? 4 : 3,
                    false, Modifiers(path, stage));

            public SingijeonExecutor Singijeon(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
                new(Runtime, 100f, 10f, 5f, 12f, 1,
                    stage == WeaponLegacyStage.Completed ? 5 : stage == WeaponLegacyStage.Reinforced ? 4 : 3,
                    false, Modifiers(path, stage));

            public Target AddTarget(int id, Float2 position, int health, float threat)
            {
                var target = new Target(id, health, position, mask, threat);
                Registry.Register(target);
                targets.Add(id, target);
                return target;
            }

            public Target TargetFor(int runtimeId) => targets[runtimeId];

            public void Drive(IWeaponExecutor executor, float seconds)
            {
                var remaining = seconds;
                while (remaining > .0001f)
                {
                    var step = Mathf.Min(.05f, remaining);
                    executor.Tick(step, new WeaponExecutionContext(default, root.transform, null,
                        _ => null, _ => mask, 0, ++tick));
                    Runtime.AffixStatuses.Tick(step, tick);
                    remaining -= step;
                }
            }

            public void Kill(Target target)
            {
                const int attackId = 812345;
                Damage.TryApply(WeaponDamageRequest.Create(new AttackInstance(attackId,
                        RepeatHitPolicy.OncePerInstance, 0f), WeaponId.HwandoFlyingBlade, target,
                    100000, false, target.WorldPosition, ContactPhase.Direct, ++tick), out _);
                Damage.RetireAttack(attackId);
            }

            public void Dispose()
            {
                Runtime.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private sealed class Target : ICombatTarget, IJangseungWardStatusTarget,
            IJangseungContactDamageTarget
        {
            private readonly Dictionary<int, float> protections = new();
            public Target(int id, int health, Float2 position, PixelHitMask mask, float threat)
            { RuntimeId = id; Health = health; WorldPosition = position; HurtMask = mask;
                ThreatScore = threat; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore { get; }
            public Float2 WorldPosition { get; private set; }
            public PixelHitMask HurtMask { get; }
            public PixelMaskTransform HurtMaskTransform =>
                PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) => WorldPosition =
                new Float2(WorldPosition.X + direction.X * force,
                    WorldPosition.Y + direction.Y * force);
            public void ApplyJangseungWard(int sourceId, float strength) { }
            public void RemoveJangseungWard(int sourceId) { }
            public void ApplyJangseungContactProtection(int sourceId, float reduction) =>
                protections[sourceId] = reduction;
            public void RemoveJangseungContactProtection(int sourceId) => protections.Remove(sourceId);
            public float ContactDamageMultiplier => protections.Count == 0
                ? 1f : 1f - protections.Values.Max();
        }
    }
}
