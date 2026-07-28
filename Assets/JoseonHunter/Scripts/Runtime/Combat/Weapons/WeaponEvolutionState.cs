using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Combat;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public interface IWeaponEvolutionProfile
    {
        bool IsEvolved { get; }
    }

    public sealed class WeaponEvolutionState
    {
        private readonly HashSet<string> evolvedWeaponIds = new HashSet<string>();

        public void SetEvolved(WeaponId weaponId) => evolvedWeaponIds.Add(weaponId.Value);
        public bool IsEvolved(WeaponId weaponId) => evolvedWeaponIds.Contains(weaponId.Value);
        public void Clear() => evolvedWeaponIds.Clear();
    }

#if UNITY_INCLUDE_TESTS
    public static class EvolvedExecutorFactory
    {
        public static IWeaponExecutor CreateForTests(WeaponId weaponId, WeaponRuntimeController runtime)
        {
            const float baseDamage = 10f;
            const float cooldownSeconds = 0.1f;
            const float range = 4f;
            const float speed = 8f;

            if (weaponId.Equals(WeaponId.HwandoFlyingBlade)) return new FlyingBladeExecutor(runtime, baseDamage, cooldownSeconds, range, speed, 5, evolved: true);
            if (weaponId.Equals(WeaponId.GakgungShot)) return new GakgungExecutor(runtime, baseDamage, cooldownSeconds, range, speed, 5, evolved: true);
            if (weaponId.Equals(WeaponId.TalismanThrow)) return new TalismanExecutor(runtime, baseDamage, cooldownSeconds, range, speed, 5, 5, evolved: true);
            if (weaponId.Equals(WeaponId.ThunderCrashBomb)) return new ThunderBombExecutor(runtime, baseDamage, cooldownSeconds, range, 0.5f, 0.15f, 2f, 5, evolved: true);
            if (weaponId.Equals(WeaponId.JangseungWard)) return new JangseungWardExecutor(runtime, baseDamage, cooldownSeconds, range, 4, 4, 0.2f, 5, evolved: true);
            if (weaponId.Equals(WeaponId.SingijeonVolley)) return new SingijeonExecutor(runtime, baseDamage, cooldownSeconds, range, speed, 5, 5, evolved: true);
            if (weaponId.Equals(WeaponId.FrostFlask)) return new FrostFlaskExecutor(runtime, baseDamage, cooldownSeconds, range, 0.5f, 1f, 1.4f, 4, 5, evolved: true);
            if (weaponId.Equals(WeaponId.WindThunderFan)) return new WindThunderFanExecutor(runtime, baseDamage, cooldownSeconds, range, 1f, 5, 5, evolved: true);
            throw new System.ArgumentOutOfRangeException(nameof(weaponId), weaponId, "No evolved test executor is available.");
        }

        public static EvolutionTelemetry ReadTelemetry(IWeaponExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            switch (executor)
            {
                case FlyingBladeExecutor flyingBlade: return EmptyTelemetry(flyingBlade.IsEvolved);
                case GakgungExecutor gakgung: return EmptyTelemetry(gakgung.IsEvolved);
                case TalismanExecutor talisman: return EmptyTelemetry(talisman.IsEvolved);
                case ThunderBombExecutor thunderBomb: return EmptyTelemetry(thunderBomb.IsEvolved);
                case JangseungWardExecutor jangseungWard: return EmptyTelemetry(jangseungWard.IsEvolved);
                case SingijeonExecutor singijeon: return EmptyTelemetry(singijeon.IsEvolved);
                case FrostFlaskExecutor frostFlask: return EmptyTelemetry(frostFlask.IsEvolved);
                case WindThunderFanExecutor windThunderFan: return EmptyTelemetry(windThunderFan.IsEvolved);
                default: return EmptyTelemetry(executor is IWeaponEvolutionProfile profile && profile.IsEvolved);
            }
        }

        private static EvolutionTelemetry EmptyTelemetry(bool isEvolved) =>
            new EvolutionTelemetry(0, 0f, Array.Empty<string>(), Array.Empty<string>(), 0, 0, 0f, false, isEvolved);
    }

    public readonly struct EvolutionTelemetry
    {
        public EvolutionTelemetry(
            int lastProjectileMaximumImpacts, float lastProjectileScale,
            IReadOnlyList<string> stateOrder, IReadOnlyList<string> volleyKinds,
            int scoutProjectileCount, int focusProjectileCount, float fieldDuration,
            bool allStoredTargetsResolvedOnce, bool isEvolved = false)
        {
            LastProjectileMaximumImpacts = lastProjectileMaximumImpacts;
            LastProjectileScale = lastProjectileScale;
            StateOrder = stateOrder;
            VolleyKinds = volleyKinds;
            ScoutProjectileCount = scoutProjectileCount;
            FocusProjectileCount = focusProjectileCount;
            FieldDuration = fieldDuration;
            AllStoredTargetsResolvedOnce = allStoredTargetsResolvedOnce;
            IsEvolved = isEvolved;
        }

        public int LastProjectileMaximumImpacts { get; }
        public float LastProjectileScale { get; }
        public IReadOnlyList<string> StateOrder { get; }
        public IReadOnlyList<string> VolleyKinds { get; }
        public int ScoutProjectileCount { get; }
        public int FocusProjectileCount { get; }
        public float FieldDuration { get; }
        public bool AllStoredTargetsResolvedOnce { get; }
        public bool IsEvolved { get; }
    }
#endif
}
