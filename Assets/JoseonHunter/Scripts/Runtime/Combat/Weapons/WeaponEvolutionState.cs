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
                case FlyingBladeExecutor value:
                    return Snapshot(WeaponId.HwandoFlyingBlade, "FlyingBlade", value.IsEvolved, value.ActiveBladeCount > 0 ? "Active" : "Idle", value.LastVolleyLaunchCount, value.ActiveBladeCount, value.LastVolleyLaunchCount, 0, 0f);
                case GakgungExecutor value:
                    return Snapshot(WeaponId.GakgungShot, "Gakgung", value.IsEvolved, value.LastSelectedTargetRuntimeId > 0 ? "Tracking" : "Idle", value.LastLaunchCount, value.ActiveProjectileCount, value.LastLaunchCount, 0, 0f);
                case TalismanExecutor value:
                    return Snapshot(WeaponId.TalismanThrow, "Talisman", value.IsEvolved, value.LastState.ToString(), value.TotalLaunchedTalismanCount, value.ActiveCastCount, value.LastLaunchCount, value.LastFinalBurstCount, 0f, value.LastContactPhases);
                case ThunderBombExecutor value:
                    return Snapshot(WeaponId.ThunderCrashBomb, "ThunderBomb", value.IsEvolved, value.LastState.ToString(), value.ActiveBombCount, value.Level, value.ActiveBombCount, 0, value.BlastRadius);
                case JangseungWardExecutor value:
                    return Snapshot(WeaponId.JangseungWard, "JangseungWard", value.IsEvolved, value.ActiveWardSetCount > 0 ? "Active" : "Idle", value.ActiveWardSetCount, value.ActivePostCount, value.ActivePostCount, value.EvictedWardSetCount, value.Radius);
                case SingijeonExecutor value:
                    return Snapshot(WeaponId.SingijeonVolley, "Singijeon", value.IsEvolved, value.LastDirectionBucket >= 0 ? "Volley" : "Idle", value.LastLaunchCount, value.ActiveProjectileCount, value.LastLaunchCount, value.LastDirectionBucket, value.Range);
                case FrostFlaskExecutor value:
                    return Snapshot(WeaponId.FrostFlask, "FrostFlask", value.IsEvolved, value.ActiveFieldCount > 0 ? "Field" : "Idle", value.ActiveFieldCount, value.ExpiredFieldCount, value.ActiveFieldCount, value.ExpiredFieldCount, value.Duration);
                case WindThunderFanExecutor value:
                    return Snapshot(WeaponId.WindThunderFan, "WindThunderFan", value.IsEvolved, value.State.ToString(), value.LastWindContactCount, value.LastLightningContactCount, value.LastWindContactCount, value.LastLightningContactCount, value.Range);
                default:
                    return Snapshot(default, executor.GetType().Name, executor is IWeaponEvolutionProfile profile && profile.IsEvolved, "Unknown", 0, 0, 0, 0, 0f);
            }
        }

        private static EvolutionTelemetry Snapshot(WeaponId weaponId, string executorKind, bool isEvolved, string state, int primaryCount, int secondaryCount, int scoutProjectileCount, int focusProjectileCount, float fieldDuration, IReadOnlyList<ContactPhase> contactPhases = null) =>
            new EvolutionTelemetry(0, 0f, new[] { state }, ContactPhaseNames(contactPhases), scoutProjectileCount, focusProjectileCount, fieldDuration, false, isEvolved, weaponId, executorKind, state, primaryCount, secondaryCount);

        private static IReadOnlyList<string> ContactPhaseNames(IReadOnlyList<ContactPhase> phases)
        {
            if (phases == null || phases.Count == 0) return Array.Empty<string>();
            var result = new string[phases.Count];
            for (var index = 0; index < phases.Count; index++) result[index] = phases[index].ToString();
            return result;
        }
    }

    public readonly struct EvolutionTelemetry
    {
        public EvolutionTelemetry(
            int lastProjectileMaximumImpacts, float lastProjectileScale,
            IReadOnlyList<string> stateOrder, IReadOnlyList<string> volleyKinds,
            int scoutProjectileCount, int focusProjectileCount, float fieldDuration,
            bool allStoredTargetsResolvedOnce, bool isEvolved = false, WeaponId weaponId = default,
            string executorKind = "", string currentState = "", int primaryObservedCount = 0, int secondaryObservedCount = 0)
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
            WeaponId = weaponId;
            ExecutorKind = executorKind;
            CurrentState = currentState;
            PrimaryObservedCount = primaryObservedCount;
            SecondaryObservedCount = secondaryObservedCount;
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
        public WeaponId WeaponId { get; }
        public string ExecutorKind { get; }
        public string CurrentState { get; }
        public int PrimaryObservedCount { get; }
        public int SecondaryObservedCount { get; }
    }
#endif
}
