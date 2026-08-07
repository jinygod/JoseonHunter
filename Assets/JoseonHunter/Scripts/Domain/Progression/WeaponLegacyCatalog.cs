using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public static class WeaponLegacyCatalog
    {
        private static readonly IReadOnlyList<WeaponLegacyDefinition> AllDefinitions = Array.AsReadOnly(new[]
        {
            Path(WeaponLegacyPathId.HwandoVenom, WeaponId.HwandoFlyingBlade, "독니", "독을 퍼뜨리는 지속 전투", "중독과 처치 전염", "직접 피해 -20%", "전염 대상 증가", "혈독난무", "중독된 적을 연속 베고 독을 폭발시킵니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .8f), (WeaponLegacyTuningKey.StatusDurationSeconds, 4f), (WeaponLegacyTuningKey.TickIntervalSeconds, .5f), (WeaponLegacyTuningKey.TickDamageMultiplier, .2f), (WeaponLegacyTuningKey.TargetCap, 3f), (WeaponLegacyTuningKey.CompletionCount, 6f), (WeaponLegacyTuningKey.SecondaryDamageMultiplier, .4f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 1.6f)),
            Path(WeaponLegacyPathId.HwandoMoonEclipse, WeaponId.HwandoFlyingBlade, "월식", "되돌아오는 잔영으로 교차 공격", "귀환 잔영과 교차 폭발", "재사용 대기시간 +20%", "두 번째 교차 잔영", "환도·월식", "잔영 교차점에서 큰 폭발을 일으킵니다.",
                (WeaponLegacyTuningKey.CooldownMultiplier, 1.2f), (WeaponLegacyTuningKey.SecondaryDamageMultiplier, .7f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 2.2f), (WeaponLegacyTuningKey.AreaMultiplier, 1.25f)),
            Path(WeaponLegacyPathId.GakgungSunPiercer, WeaponId.GakgungShot, "관일", "느리지만 강한 장거리 관통", "관통 누적 피해와 방어 파괴", "공격 속도 -20%", "방어력 25% 감소", "각궁·관일", "마지막 관통점에서 폭발하고 우두머리에게 강해집니다.",
                (WeaponLegacyTuningKey.AttackIntervalMultiplier, 1.25f), (WeaponLegacyTuningKey.PierceBonus, 3f), (WeaponLegacyTuningKey.PerPierceDamageBonus, .15f), (WeaponLegacyTuningKey.StatusDurationSeconds, 2.5f), (WeaponLegacyTuningKey.StatusStrength, .25f), (WeaponLegacyTuningKey.BossDamageMultiplier, 1.3f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 1.8f)),
            Path(WeaponLegacyPathId.GakgungSplitFletching, WeaponId.GakgungShot, "갈래깃", "부채꼴 다중 화살", "넓은 방향을 동시에 제압", "화살 피해 -25%", "화살 다섯 발", "천우산개", "네 번째 사격마다 일곱 발을 펼칩니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .75f), (WeaponLegacyTuningKey.ProjectileCount, 3f), (WeaponLegacyTuningKey.SecondaryDamageMultiplier, .7f), (WeaponLegacyTuningKey.ReinforcedCount, 5f), (WeaponLegacyTuningKey.ReinforcedDamageMultiplier, .6f), (WeaponLegacyTuningKey.CompletionCount, 7f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, .55f)),
            Path(WeaponLegacyPathId.TalismanHeavenSeal, WeaponId.TalismanThrow, "천쇄봉인", "봉인을 유지하다 연쇄 폭발", "봉인 전염과 받는 피해 증가", "직접 피해 -25%", "봉인 대상 받는 피해 +15%", "천쇄부진", "봉인된 적의 죽음이 주변 봉인을 터뜨립니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .75f), (WeaponLegacyTuningKey.StatusDurationSeconds, 2f), (WeaponLegacyTuningKey.IncomingDamageMultiplier, 1.15f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 1.6f), (WeaponLegacyTuningKey.TargetCap, 4f)),
            Path(WeaponLegacyPathId.TalismanGhostBurst, WeaponId.TalismanThrow, "원귀폭발", "짧은 예고 뒤 강한 범위 폭발", "집중 폭발과 연쇄 폭진", "봉인 전염 제거", "범위 +30%, 두 번째 폭발", "백귀폭진", "원귀 폭발이 최대 세 번 이어집니다.",
                (WeaponLegacyTuningKey.DelaySeconds, .6f), (WeaponLegacyTuningKey.PrimaryDamageMultiplier, 2f), (WeaponLegacyTuningKey.AreaMultiplier, 1.3f), (WeaponLegacyTuningKey.SecondaryDamageMultiplier, 1f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 1.2f), (WeaponLegacyTuningKey.TargetCap, 3f)),
            Path(WeaponLegacyPathId.ThunderPrison, WeaponId.ThunderCrashBomb, "뇌옥", "적을 모아 중심에서 폭파", "끌어당김과 중심부 피해", "재사용 대기시간 +25%", "중심부 피해 +60%", "벽력탄·뇌옥", "압축한 적을 중심에서 크게 폭발시킵니다.",
                (WeaponLegacyTuningKey.CooldownMultiplier, 1.25f), (WeaponLegacyTuningKey.StatusDurationSeconds, 1f), (WeaponLegacyTuningKey.ReinforcedDamageMultiplier, 1.6f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 3f), (WeaponLegacyTuningKey.InnerRadiusMultiplier, .45f)),
            Path(WeaponLegacyPathId.ThunderEarthCurrent, WeaponId.ThunderCrashBomb, "지맥", "지면에 남는 연쇄 전류", "지속 전류와 처치 확산", "최초 폭발 피해 -30%", "지속시간 4초, 연쇄 3마리", "뇌맥연쇄", "처치 시 전류가 최대 다섯 마리로 퍼집니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .7f), (WeaponLegacyTuningKey.StatusDurationSeconds, 3f), (WeaponLegacyTuningKey.ReinforcedDurationSeconds, 4f), (WeaponLegacyTuningKey.TickIntervalSeconds, .5f), (WeaponLegacyTuningKey.TickDamageMultiplier, .3f), (WeaponLegacyTuningKey.ReinforcedCount, 3f), (WeaponLegacyTuningKey.TargetCap, 5f)),
            Path(WeaponLegacyPathId.JangseungFourGuardians, WeaponId.JangseungWard, "사방수호", "사방 결계로 밀쳐내며 방어", "군중 제어와 접촉 피해 감소", "공격 피해 -30%", "접촉 피해 20% 감소", "십이지신 장승진", "네 방향에서 세 차례 수호 파동을 보냅니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .7f), (WeaponLegacyTuningKey.ProjectileCount, 4f), (WeaponLegacyTuningKey.StatusStrength, .2f), (WeaponLegacyTuningKey.CompletionCount, 3f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, .8f)),
            Path(WeaponLegacyPathId.JangseungGuardianDescent, WeaponId.JangseungWard, "수호신강림", "결계를 줄이고 수호령 내려찍기", "강한 반복 내려찍기", "결계 유지시간 -40%", "두 번째 내려찍기", "산신강림", "중심부를 거대한 수호령이 짓누릅니다.",
                (WeaponLegacyTuningKey.DurationMultiplier, .6f), (WeaponLegacyTuningKey.PrimaryDamageMultiplier, 1.8f), (WeaponLegacyTuningKey.SecondaryDamageMultiplier, 1.8f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 3.2f)),
            Path(WeaponLegacyPathId.SingijeonFireDragon, WeaponId.SingijeonVolley, "화룡포", "강한 적에게 집중 포격", "표적 우선과 단일 화력", "공격 범위 -35%", "집중 총피해 +60%", "신기전·화룡포", "한 표적에 다섯 차례 집중 포격합니다.",
                (WeaponLegacyTuningKey.AreaMultiplier, .65f), (WeaponLegacyTuningKey.CompletionCount, 5f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, .32f)),
            Path(WeaponLegacyPathId.SingijeonFireNet, WeaponId.SingijeonVolley, "화망", "화약 흔적과 화상 전염", "지속 피해와 처치 점화", "최초 폭발 피해 -30%", "처치 시 주변 3마리 점화", "연화화망", "연결된 화약 흔적이 동시에 폭발합니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .7f), (WeaponLegacyTuningKey.StatusDurationSeconds, 3f), (WeaponLegacyTuningKey.TickIntervalSeconds, .5f), (WeaponLegacyTuningKey.TickDamageMultiplier, .3f), (WeaponLegacyTuningKey.TargetCap, 3f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 2f)),
            Path(WeaponLegacyPathId.FrostMist, WeaponId.FrostFlask, "빙무", "넓은 서리 안개로 제어", "넓은 둔화와 빙결", "직접 피해 -35%", "세 번 적중 시 빙결", "서리병·빙화원", "서리꽃이 세 차례 피어나며 빙결시킵니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .65f), (WeaponLegacyTuningKey.AreaMultiplier, 1.35f), (WeaponLegacyTuningKey.StatusStrength, .45f), (WeaponLegacyTuningKey.TriggerCount, 3f), (WeaponLegacyTuningKey.IncomingDamageMultiplier, 1.1f), (WeaponLegacyTuningKey.CompletionCount, 3f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, .6f)),
            Path(WeaponLegacyPathId.FrostShatter, WeaponId.FrostFlask, "파쇄", "짧고 강한 착지와 빙결 파쇄", "순간 폭발과 연쇄 파쇄", "장판 지속시간 -50%", "빙결 파쇄 최대 3마리", "빙결파쇄", "빙결 파쇄가 최대 다섯 마리까지 이어집니다.",
                (WeaponLegacyTuningKey.DurationMultiplier, .5f), (WeaponLegacyTuningKey.PrimaryDamageMultiplier, 1.5f), (WeaponLegacyTuningKey.SecondaryDamageMultiplier, 1.8f), (WeaponLegacyTuningKey.ReinforcedCount, 3f), (WeaponLegacyTuningKey.TargetCap, 5f)),
            Path(WeaponLegacyPathId.FanVacuum, WeaponId.WindThunderFan, "진공", "끌어당기며 출혈을 쌓는 근접 제어", "강한 흡입과 출혈 파열", "번개 피해 -30%", "출혈 3중첩 시 파열", "회천풍진", "거대한 진공 소용돌이가 반복 파열합니다.",
                (WeaponLegacyTuningKey.DirectDamageMultiplier, .7f), (WeaponLegacyTuningKey.PullMultiplier, 1.5f), (WeaponLegacyTuningKey.TriggerCount, 3f), (WeaponLegacyTuningKey.TickIntervalSeconds, .5f), (WeaponLegacyTuningKey.TickDamageMultiplier, .15f), (WeaponLegacyTuningKey.StatusDurationSeconds, 2f), (WeaponLegacyTuningKey.ReinforcedDamageMultiplier, 1f)),
            Path(WeaponLegacyPathId.FanHeavenThunder, WeaponId.WindThunderFan, "천뢰", "먼 적을 잇는 연쇄 번개", "다중 튕김과 귀환 폭발", "끌어당김 제거", "천뢰 표식과 80% 귀환타", "풍뢰선·천뢰귀환", "일곱 번 연쇄한 뒤 표식 중심을 폭발시킵니다.",
                (WeaponLegacyTuningKey.SecondaryDamageMultiplier, .7f), (WeaponLegacyTuningKey.TargetCap, 4f), (WeaponLegacyTuningKey.ReinforcedDamageMultiplier, .8f), (WeaponLegacyTuningKey.CompletionCount, 7f), (WeaponLegacyTuningKey.CompletionDamageMultiplier, 2f))
        });

        private static readonly IReadOnlyDictionary<WeaponLegacyPathId, WeaponLegacyDefinition> ById = BuildById();
        private static readonly IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponLegacyDefinition>> ByWeapon = BuildByWeapon();

        public static IReadOnlyList<WeaponLegacyDefinition> All => AllDefinitions;

        public static IReadOnlyList<WeaponLegacyDefinition> PathsFor(WeaponId weaponId) =>
            ByWeapon.TryGetValue(weaponId, out var paths) ? paths : Array.Empty<WeaponLegacyDefinition>();

        public static bool TryGet(WeaponLegacyPathId id, out WeaponLegacyDefinition definition) =>
            ById.TryGetValue(id, out definition);

        private static WeaponLegacyDefinition Path(
            WeaponLegacyPathId id,
            WeaponId weaponId,
            string displayName,
            string combatStyle,
            string benefit,
            string cost,
            string levelFourSummary,
            string completionName,
            string completionSummary,
            params (WeaponLegacyTuningKey Key, float Value)[] values)
        {
            var tuning = new Dictionary<WeaponLegacyTuningKey, float>();
            foreach (var value in values) tuning.Add(value.Key, value.Value);
            return new WeaponLegacyDefinition(id, weaponId, displayName, combatStyle, benefit, cost,
                levelFourSummary, completionName, completionSummary, DimensionsFor(id), tuning);
        }

        private static IReadOnlyList<EvolutionDimension> DimensionsFor(WeaponLegacyPathId id)
        {
            if (id.Equals(WeaponLegacyPathId.HwandoVenom))
                return Pair(EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.HwandoMoonEclipse))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.GakgungSunPiercer))
                return Pair(EvolutionDimension.Rhythm, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.GakgungSplitFletching))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.Rhythm);
            if (id.Equals(WeaponLegacyPathId.TalismanHeavenSeal))
                return Pair(EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.TalismanGhostBurst))
                return Pair(EvolutionDimension.Rhythm, EvolutionDimension.Geometry);
            if (id.Equals(WeaponLegacyPathId.ThunderPrison))
                return Pair(EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.ThunderEarthCurrent))
                return Pair(EvolutionDimension.Rhythm, EvolutionDimension.EnemyResponse);
            if (id.Equals(WeaponLegacyPathId.JangseungFourGuardians))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.EnemyResponse);
            if (id.Equals(WeaponLegacyPathId.JangseungGuardianDescent))
                return Pair(EvolutionDimension.Rhythm, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.SingijeonFireDragon))
                return Pair(EvolutionDimension.Rhythm, EvolutionDimension.Geometry);
            if (id.Equals(WeaponLegacyPathId.SingijeonFireNet))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.FrostMist))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.EnemyResponse);
            if (id.Equals(WeaponLegacyPathId.FrostShatter))
                return Pair(EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff);
            if (id.Equals(WeaponLegacyPathId.FanVacuum))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.EnemyResponse);
            if (id.Equals(WeaponLegacyPathId.FanHeavenThunder))
                return Pair(EvolutionDimension.Geometry, EvolutionDimension.Rhythm);
            throw new ArgumentOutOfRangeException(nameof(id), id.Value, "Unknown weapon legacy path.");
        }

        private static IReadOnlyList<EvolutionDimension> Pair(
            EvolutionDimension first,
            EvolutionDimension second) =>
            Array.AsReadOnly(new[] { first, second });

        private static IReadOnlyDictionary<WeaponLegacyPathId, WeaponLegacyDefinition> BuildById()
        {
            var result = new Dictionary<WeaponLegacyPathId, WeaponLegacyDefinition>();
            foreach (var definition in AllDefinitions) result.Add(definition.Id, definition);
            return new ReadOnlyDictionary<WeaponLegacyPathId, WeaponLegacyDefinition>(result);
        }

        private static IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponLegacyDefinition>> BuildByWeapon()
        {
            var mutable = new Dictionary<WeaponId, List<WeaponLegacyDefinition>>();
            foreach (var definition in AllDefinitions)
            {
                if (!mutable.TryGetValue(definition.WeaponId, out var paths))
                    mutable.Add(definition.WeaponId, paths = new List<WeaponLegacyDefinition>());
                paths.Add(definition);
            }

            var result = new Dictionary<WeaponId, IReadOnlyList<WeaponLegacyDefinition>>();
            foreach (var pair in mutable) result.Add(pair.Key, pair.Value.AsReadOnly());
            return new ReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponLegacyDefinition>>(result);
        }
    }
}
