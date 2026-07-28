using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public enum EvolutionDimension { Rhythm, Geometry, EnemyResponse, Payoff }

    public sealed class WeaponEvolutionDefinition
    {
        public WeaponEvolutionDefinition(
            string id,
            WeaponId requiredWeaponId,
            string displayName,
            string summary,
            params EvolutionDimension[] changedDimensions)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            RequiredWeaponId = requiredWeaponId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            ChangedDimensions = Array.AsReadOnly((changedDimensions ?? throw new ArgumentNullException(nameof(changedDimensions))).ToArray());
        }

        public string Id { get; }
        public WeaponId RequiredWeaponId { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public IReadOnlyList<EvolutionDimension> ChangedDimensions { get; }
    }

    public static class WeaponEvolutionCatalog
    {
        public static readonly IReadOnlyList<WeaponEvolutionDefinition> All = Array.AsReadOnly(new[]
        {
            new WeaponEvolutionDefinition("hwando_moon_eclipse", WeaponId.HwandoFlyingBlade, "Hwando Moon Eclipse", "The flying blade cuts a wider returning arc.", EvolutionDimension.Geometry, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("gakgung_sun_piercer", WeaponId.GakgungShot, "Gakgung Sun Piercer", "Arrows pierce through enemies at a steady rhythm.", EvolutionDimension.Rhythm, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("talisman_heaven_chain", WeaponId.TalismanThrow, "Talisman Heaven Chain", "Linked talismans bind enemies together.", EvolutionDimension.Geometry, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("thunder_prison", WeaponId.ThunderCrashBomb, "Thunder Prison", "Struck enemies are trapped in a storm field.", EvolutionDimension.EnemyResponse, EvolutionDimension.Rhythm),
            new WeaponEvolutionDefinition("twelve_guardians", WeaponId.JangseungWard, "Twelve Guardians", "Guardian wards hold enemies at bay.", EvolutionDimension.Geometry, EvolutionDimension.EnemyResponse),
            new WeaponEvolutionDefinition("fire_dragon_barrage", WeaponId.SingijeonVolley, "Fire Dragon Barrage", "Rocket volleys sweep the battlefield in succession.", EvolutionDimension.Rhythm, EvolutionDimension.Geometry),
            new WeaponEvolutionDefinition("frost_bloom_evolution", WeaponId.FrostFlask, "Frost Bloom", "Frozen enemies burst into a spreading frost bloom.", EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("returning_heaven_thunder", WeaponId.WindThunderFan, "Returning Heaven Thunder", "Returning fans call down thunder along their path.", EvolutionDimension.Geometry, EvolutionDimension.Rhythm)
        });

        private static readonly IReadOnlyDictionary<string, WeaponEvolutionDefinition> ById =
            new ReadOnlyDictionary<string, WeaponEvolutionDefinition>(All.ToDictionary(definition => definition.Id));

        public static bool TryGet(string id, out WeaponEvolutionDefinition definition) =>
            ById.TryGetValue(id, out definition);
    }
}
