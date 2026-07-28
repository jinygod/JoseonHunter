using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using UnityEngine;

namespace JoseonHunter.Tests.PlayMode
{
    internal sealed class EvolvedWeaponTestRig : IDisposable
    {
        private readonly GameObject root = new GameObject("Evolved Weapon Test Root");
        private readonly List<ConfirmedDamageEvent> events = new List<ConfirmedDamageEvent>();
        private readonly List<TestTarget> targets = new List<TestTarget>();
        private int tick;

        private EvolvedWeaponTestRig(WeaponId weaponId)
        {
            Registry = new CombatTargetRegistry();
            Damage = new CombatDamageService(Registry);
            Runtime = new WeaponRuntimeController(Registry, Damage, PixelHitMask.FromRows("1"));
            Executor = EvolvedExecutorFactory.CreateForTests(weaponId, Runtime);
            Runtime.Register(weaponId, Executor);
            Damage.DamageConfirmed += events.Add;
        }

        public CombatTargetRegistry Registry { get; }
        public CombatDamageService Damage { get; }
        public WeaponRuntimeController Runtime { get; }
        public IWeaponExecutor Executor { get; }
        public IReadOnlyList<ContactPhase> ContactPhases => events.Select(value => value.Phase).ToArray();
        public IReadOnlyList<ContactPhase> DistinctPhaseOrder => events.Select(value => value.Phase).Distinct().ToArray();
        public int UniqueDamagedTargets => events.Select(value => value.TargetRuntimeId).Distinct().Count();
        public EvolutionTelemetry Telemetry => EvolvedExecutorFactory.ReadTelemetry(Executor);

        public static EvolvedWeaponTestRig For(WeaponId weaponId) => new EvolvedWeaponTestRig(weaponId);
        public int Count(ContactPhase phase) => events.Count(value => value.Phase == phase);

        public TestTarget AddTarget(Vector2 position)
        {
            var target = new TestTarget(targets.Count + 1, new Float2(position.x, position.y), PixelHitMask.FromRows("1"));
            targets.Add(target);
            Registry.Register(target);
            return target;
        }

        public void AddTargets(int count)
        {
            for (var index = 0; index < count; index++) AddTarget(new Vector2(1f + index * 0.2f, 0f));
        }

        public void AddTargets(int count, bool insideField)
        {
            var distance = insideField ? 0.4f : 8f;
            for (var index = 0; index < count; index++) AddTarget(new Vector2(distance + index * 0.1f, 0f));
        }

        public IEnumerator AdvanceSeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                const float delta = 0.05f;
                Executor.Tick(delta, new WeaponExecutionContext(default, root.transform, null, 0, ++tick));
                elapsed += delta;
                yield return null;
            }
        }

        public IEnumerator AdvanceCasts(int count)
        {
            for (var index = 0; index < count; index++)
            {
                var delta = index == 0 ? 0.01f : 0.1f;
                Executor.Tick(delta, new WeaponExecutionContext(default, root.transform, null, 0, ++tick));
                yield return null;
            }
        }

        public void Dispose()
        {
            Damage.DamageConfirmed -= events.Add;
            Runtime.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    internal sealed class TestTarget : ICombatTarget, IFrostStatusTarget, IJangseungWardStatusTarget
    {
        private readonly PixelHitMask mask;
        public TestTarget(int runtimeId, Float2 position, PixelHitMask mask)
        {
            RuntimeId = runtimeId;
            Position = position;
            this.mask = mask;
        }

        public int RuntimeId { get; }
        public bool IsAlive => Health > 0;
        public int Health { get; private set; } = 100;
        public bool IsBoss => false;
        public bool IsElite => false;
        public float ThreatScore => 0f;
        public Float2 Position { get; set; }
        public Float2 WorldPosition => Position;
        public PixelHitMask HurtMask => mask;
        public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Translation(Position.X, Position.Y);
        public ISet<string> Statuses { get; } = new HashSet<string>();

        public void ApplyResolvedDamage(int damage) => Health -= damage;
        public void ApplyKnockback(Float2 direction, float force) => Position = new Float2(Position.X + direction.X * force, Position.Y + direction.Y * force);
        public void ApplyFrostSlow(int sourceId, float strength) => Statuses.Add($"frost:{sourceId}");
        public void RemoveFrostSlow(int sourceId, float decaySeconds) => Statuses.Remove($"frost:{sourceId}");
        public void ApplyFreeze(int sourceId, float durationSeconds) => Statuses.Add($"freeze:{sourceId}");
        public void ApplyJangseungWard(int sourceId, float strength) => Statuses.Add($"ward:{sourceId}");
        public void RemoveJangseungWard(int sourceId) => Statuses.Remove($"ward:{sourceId}");
    }
}
