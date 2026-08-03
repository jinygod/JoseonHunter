using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public readonly struct WeaponLegacyPathId : IEquatable<WeaponLegacyPathId>
    {
        public static readonly WeaponLegacyPathId HwandoVenom = new("hwando_venom");
        public static readonly WeaponLegacyPathId HwandoMoonEclipse = new("hwando_moon_eclipse");
        public static readonly WeaponLegacyPathId GakgungSunPiercer = new("gakgung_sun_piercer");
        public static readonly WeaponLegacyPathId GakgungSplitFletching = new("gakgung_split_fletching");
        public static readonly WeaponLegacyPathId TalismanHeavenSeal = new("talisman_heaven_seal");
        public static readonly WeaponLegacyPathId TalismanGhostBurst = new("talisman_ghost_burst");
        public static readonly WeaponLegacyPathId ThunderPrison = new("thunder_prison");
        public static readonly WeaponLegacyPathId ThunderEarthCurrent = new("thunder_earth_current");
        public static readonly WeaponLegacyPathId JangseungFourGuardians = new("jangseung_four_guardians");
        public static readonly WeaponLegacyPathId JangseungGuardianDescent = new("jangseung_guardian_descent");
        public static readonly WeaponLegacyPathId SingijeonFireDragon = new("singijeon_fire_dragon");
        public static readonly WeaponLegacyPathId SingijeonFireNet = new("singijeon_fire_net");
        public static readonly WeaponLegacyPathId FrostMist = new("frost_mist");
        public static readonly WeaponLegacyPathId FrostShatter = new("frost_shatter");
        public static readonly WeaponLegacyPathId FanVacuum = new("fan_vacuum");
        public static readonly WeaponLegacyPathId FanHeavenThunder = new("fan_heaven_thunder");

        public WeaponLegacyPathId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Legacy path ID is required.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(WeaponLegacyPathId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is WeaponLegacyPathId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public enum WeaponLegacyStage
    {
        None,
        Chosen,
        Reinforced,
        Completed
    }

    public enum WeaponLegacyTuningKey
    {
        DirectDamageMultiplier,
        CooldownMultiplier,
        AreaMultiplier,
        DurationMultiplier,
        AttackIntervalMultiplier,
        PrimaryDamageMultiplier,
        SecondaryDamageMultiplier,
        ReinforcedDamageMultiplier,
        CompletionDamageMultiplier,
        ProjectileCount,
        ReinforcedCount,
        CompletionCount,
        TargetCap,
        PierceBonus,
        PerPierceDamageBonus,
        StatusDurationSeconds,
        ReinforcedDurationSeconds,
        StatusStrength,
        TriggerCount,
        TickDamageMultiplier,
        TickIntervalSeconds,
        PullMultiplier,
        BossDamageMultiplier,
        IncomingDamageMultiplier,
        InnerRadiusMultiplier,
        DelaySeconds
    }

    public sealed class WeaponLegacyDefinition
    {
        private readonly IReadOnlyDictionary<WeaponLegacyTuningKey, float> tuning;

        public WeaponLegacyDefinition(
            WeaponLegacyPathId id,
            WeaponId weaponId,
            string displayName,
            string combatStyle,
            string benefit,
            string cost,
            string levelFourSummary,
            string completionName,
            string completionSummary,
            IReadOnlyDictionary<WeaponLegacyTuningKey, float> tuning)
        {
            Id = id;
            WeaponId = weaponId;
            DisplayName = Required(displayName, nameof(displayName));
            CombatStyle = Required(combatStyle, nameof(combatStyle));
            Benefit = Required(benefit, nameof(benefit));
            Cost = Required(cost, nameof(cost));
            LevelFourSummary = Required(levelFourSummary, nameof(levelFourSummary));
            CompletionName = Required(completionName, nameof(completionName));
            CompletionSummary = Required(completionSummary, nameof(completionSummary));
            this.tuning = tuning == null
                ? new ReadOnlyDictionary<WeaponLegacyTuningKey, float>(new Dictionary<WeaponLegacyTuningKey, float>())
                : new ReadOnlyDictionary<WeaponLegacyTuningKey, float>(new Dictionary<WeaponLegacyTuningKey, float>(tuning));
        }

        public WeaponLegacyPathId Id { get; }
        public WeaponId WeaponId { get; }
        public string DisplayName { get; }
        public string CombatStyle { get; }
        public string Benefit { get; }
        public string Cost { get; }
        public string LevelFourSummary { get; }
        public string CompletionName { get; }
        public string CompletionSummary { get; }
        public float DirectDamageMultiplier => Value(WeaponLegacyTuningKey.DirectDamageMultiplier, 1f);
        public float Value(WeaponLegacyTuningKey key, float fallback = 0f) =>
            tuning.TryGetValue(key, out var value) ? value : fallback;

        private static string Required(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Legacy copy is required.", parameterName);
    }

    public readonly struct WeaponLegacySnapshot : IEquatable<WeaponLegacySnapshot>
    {
        public WeaponLegacySnapshot(WeaponLegacyPathId pathId, WeaponLegacyStage stage)
        {
            PathId = pathId;
            Stage = stage;
        }

        public WeaponLegacyPathId PathId { get; }
        public WeaponLegacyStage Stage { get; }
        public bool HasPath => Stage != WeaponLegacyStage.None && !string.IsNullOrEmpty(PathId.Value);
        public bool Is(WeaponLegacyPathId pathId) => HasPath && PathId.Equals(pathId);
        public bool Equals(WeaponLegacySnapshot other) => PathId.Equals(other.PathId) && Stage == other.Stage;
        public override bool Equals(object obj) => obj is WeaponLegacySnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(PathId, Stage);
    }
}
