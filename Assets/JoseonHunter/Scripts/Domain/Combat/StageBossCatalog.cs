using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Domain.Combat
{
    public readonly struct StageBossAttackStep
    {
        public StageBossAttackStep(BossAttackKind kind, float warningSeconds, float safeGapDegrees = 360f)
        {
            if (kind == BossAttackKind.None) throw new ArgumentOutOfRangeException(nameof(kind));
            if (float.IsNaN(warningSeconds) || float.IsInfinity(warningSeconds) || warningSeconds < .75f)
                throw new ArgumentOutOfRangeException(nameof(warningSeconds));
            Kind = kind;
            WarningSeconds = warningSeconds;
            SafeGapDegrees = Math.Max(0f, safeGapDegrees);
        }

        public BossAttackKind Kind { get; }
        public float WarningSeconds { get; }
        public float SafeGapDegrees { get; }
    }

    public sealed class StageBossDefinition
    {
        private readonly IReadOnlyList<StageBossAttackStep> basePattern;
        private readonly IReadOnlyList<StageBossAttackStep> linkedPattern;

        public StageBossDefinition(
            string contentId,
            string displayName,
            float atSeconds,
            BossCombatRole role,
            float visualScale,
            float maximumHealth,
            IReadOnlyList<StageBossAttackStep> basePattern,
            IReadOnlyList<StageBossAttackStep> linkedPattern = null)
        {
            if (string.IsNullOrWhiteSpace(contentId)) throw new ArgumentException(nameof(contentId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException(nameof(displayName));
            if (atSeconds < 0f || float.IsNaN(atSeconds) || float.IsInfinity(atSeconds))
                throw new ArgumentOutOfRangeException(nameof(atSeconds));
            if (visualScale <= 0f || maximumHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(visualScale));
            if (basePattern == null || basePattern.Count == 0)
                throw new ArgumentException("Boss pattern is required.", nameof(basePattern));
            ContentId = contentId;
            DisplayName = displayName;
            AtSeconds = atSeconds;
            Role = role;
            VisualScale = visualScale;
            MaximumHealth = maximumHealth;
            this.basePattern = basePattern;
            this.linkedPattern = linkedPattern ?? basePattern;
        }

        public string ContentId { get; }
        public string DisplayName { get; }
        public float AtSeconds { get; }
        public BossCombatRole Role { get; }
        public float VisualScale { get; }
        public float MaximumHealth { get; }

        public IReadOnlyList<StageBossAttackStep> PatternFor(float healthFraction, int pressureTier) =>
            healthFraction < .5f || pressureTier >= 2 ? linkedPattern : basePattern;
    }

    public static class StageBossCatalog
    {
        private static readonly IReadOnlyList<StageBossDefinition> Empty =
            Array.AsReadOnly(Array.Empty<StageBossDefinition>());
        private static readonly IReadOnlyList<StageBossDefinition> DokkaebiPass =
            Array.AsReadOnly(new[]
            {
                Boss("one_horn_captain", "외뿔 대장", 300f, BossCombatRole.FirstMidBoss, 1.7f, 850f,
                    Steps((BossAttackKind.BloodCharge, .9f), (BossAttackKind.ConeSweep, .85f))),
                Boss("iron_shield_general", "철방패 장군", 600f, BossCombatRole.SecondMidBoss, 1.9f, 1900f,
                    Steps((BossAttackKind.ShieldSlam, 1f), (BossAttackKind.ShieldPush, .85f))),
                Boss("dokkaebi_king", "도깨비 대왕", 900f, BossCombatRole.FinalBoss, 2.8f, 7800f,
                    Steps((BossAttackKind.ClubSlam, 1.1f), (BossAttackKind.TripleCharge, .9f),
                        (BossAttackKind.Rockfall, .95f)),
                    Steps((BossAttackKind.ClubSlam, 1.1f), (BossAttackKind.TripleCharge, .9f),
                        (BossAttackKind.Rockfall, .95f), (BossAttackKind.ClubSlam, 1.1f)))
            });
        private static readonly IReadOnlyList<StageBossDefinition> MoonlitTomb =
            Array.AsReadOnly(new[]
            {
                Boss("royal_guard_wraith", "왕릉 수문장", 300f, BossCombatRole.FirstMidBoss, 1.7f, 1150f,
                    Steps((BossAttackKind.CrescentSweep, 1f, 150f),
                        (BossAttackKind.SpiritHands, .95f, 48f))),
                Boss("eclipse_priest", "월식 제관", 600f, BossCombatRole.SecondMidBoss, 1.9f, 2500f,
                    Steps((BossAttackKind.SequentialCurseCells, 1.05f, 48f),
                        (BossAttackKind.RadialVolley, .95f, 44f))),
                Boss("eclipse_queen", "월식의 왕비", 900f, BossCombatRole.FinalBoss, 2.8f, 9800f,
                    Steps((BossAttackKind.CrescentSweep, 1.05f, 150f),
                        (BossAttackKind.RadialVolley, .95f, 48f),
                        (BossAttackKind.SequentialCurseCells, 1.05f, 44f)),
                    Steps((BossAttackKind.RadialVolley, .9f, 40f),
                        (BossAttackKind.SpiritHands, .9f, 36f),
                        (BossAttackKind.CrescentSweep, 1f, 140f),
                        (BossAttackKind.SequentialCurseCells, 1f, 40f)))
            });

        public static IReadOnlyList<StageBossDefinition> For(StageId stageId) =>
            stageId.Equals(StageId.DokkaebiPass) ? DokkaebiPass :
            stageId.Equals(StageId.MoonlitTomb) ? MoonlitTomb : Empty;

        public static StageBossDefinition Get(string contentId)
        {
            for (var index = 0; index < DokkaebiPass.Count; index++)
                if (string.Equals(DokkaebiPass[index].ContentId, contentId, StringComparison.Ordinal))
                    return DokkaebiPass[index];
            for (var index = 0; index < MoonlitTomb.Count; index++)
                if (string.Equals(MoonlitTomb[index].ContentId, contentId, StringComparison.Ordinal))
                    return MoonlitTomb[index];
            throw new KeyNotFoundException($"Unknown stage boss '{contentId}'.");
        }

        private static StageBossDefinition Boss(
            string id, string name, float time, BossCombatRole role, float scale, float health,
            IReadOnlyList<StageBossAttackStep> pattern,
            IReadOnlyList<StageBossAttackStep> linked = null) =>
            new StageBossDefinition(id, name, time, role, scale, health, pattern, linked);

        private static IReadOnlyList<StageBossAttackStep> Steps(
            params (BossAttackKind Kind, float Warning)[] values)
        {
            var result = new StageBossAttackStep[values.Length];
            for (var index = 0; index < values.Length; index++)
                result[index] = new StageBossAttackStep(values[index].Kind, values[index].Warning);
            return Array.AsReadOnly(result);
        }

        private static IReadOnlyList<StageBossAttackStep> Steps(
            params (BossAttackKind Kind, float Warning, float SafeGap)[] values)
        {
            var result = new StageBossAttackStep[values.Length];
            for (var index = 0; index < values.Length; index++)
                result[index] = new StageBossAttackStep(
                    values[index].Kind, values[index].Warning, values[index].SafeGap);
            return Array.AsReadOnly(result);
        }
    }

    public static class BossSafeLaneValidator
    {
        public static bool HasReachableGap(
            IReadOnlyList<StageBossAttackStep> pattern,
            float minimumDegrees)
        {
            if (pattern == null || pattern.Count == 0) return false;
            if (float.IsNaN(minimumDegrees) || float.IsInfinity(minimumDegrees) || minimumDegrees <= 0f)
                throw new ArgumentOutOfRangeException(nameof(minimumDegrees));
            for (var index = 0; index < pattern.Count; index++)
            {
                var step = pattern[index];
                if (!RequiresSafeGap(step.Kind)) continue;
                if (step.SafeGapDegrees < minimumDegrees) return false;
            }
            return true;
        }

        private static bool RequiresSafeGap(BossAttackKind kind) => kind == BossAttackKind.RadialVolley ||
            kind == BossAttackKind.SequentialCurseCells || kind == BossAttackKind.SpiritHands ||
            kind == BossAttackKind.CrescentSweep;
    }
}
