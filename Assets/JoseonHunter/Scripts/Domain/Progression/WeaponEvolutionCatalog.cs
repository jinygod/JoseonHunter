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
            new WeaponEvolutionDefinition("hwando_moon_eclipse", WeaponId.HwandoFlyingBlade, "환도·월식", "귀환 교차점에 월식 폭발", EvolutionDimension.Geometry, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("gakgung_sun_piercer", WeaponId.GakgungShot, "각궁·관일", "일정 사격마다 거대 관통 화살", EvolutionDimension.Rhythm, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("talisman_heaven_chain", WeaponId.TalismanThrow, "천쇄부진", "연결된 봉인망 완성 시 동시 폭발", EvolutionDimension.Geometry, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("thunder_prison", WeaponId.ThunderCrashBomb, "벽력탄·뇌옥", "끌어모은 뒤 압축 낙뢰 폭발", EvolutionDimension.EnemyResponse, EvolutionDimension.Rhythm),
            new WeaponEvolutionDefinition("twelve_guardians", WeaponId.JangseungWard, "십이지신 장승진", "완성된 진 안의 적을 낙인", EvolutionDimension.Geometry, EvolutionDimension.EnemyResponse),
            new WeaponEvolutionDefinition("fire_dragon_barrage", WeaponId.SingijeonVolley, "신기전·화룡포", "표식 지점에 지연 집중 포격", EvolutionDimension.Rhythm, EvolutionDimension.Geometry),
            new WeaponEvolutionDefinition("frost_bloom_evolution", WeaponId.FrostFlask, "서리병·빙화원", "축적한 빙결을 연쇄 파쇄", EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff),
            new WeaponEvolutionDefinition("returning_heaven_thunder", WeaponId.WindThunderFan, "풍뢰선·천뢰귀환", "모은 표식 사이를 낙뢰가 왕복", EvolutionDimension.Geometry, EvolutionDimension.Rhythm)
        });

        private static readonly IReadOnlyDictionary<string, WeaponEvolutionDefinition> ById =
            new ReadOnlyDictionary<string, WeaponEvolutionDefinition>(All.ToDictionary(definition => definition.Id));

        public static bool TryGet(string id, out WeaponEvolutionDefinition definition) =>
            ById.TryGetValue(id, out definition);
    }
}
