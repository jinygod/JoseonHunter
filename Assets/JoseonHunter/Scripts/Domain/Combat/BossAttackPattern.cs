using System;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Combat
{
    public enum BossCombatRole
    {
        FirstMidBoss,
        SecondMidBoss,
        FinalBoss
    }

    public enum BossAttackKind
    {
        None,
        SuppressionSlam,
        BloodCharge,
        SpiritVolley
    }

    public enum BossAttackPhase
    {
        Chase,
        Telegraph,
        Execute,
        Recover
    }

    public readonly struct BossAttackSnapshot
    {
        public BossAttackSnapshot(
            BossAttackPhase phase,
            BossAttackKind kind,
            Float2 lockedTarget,
            float phaseSecondsRemaining,
            float warningDurationSeconds,
            bool executeStarted)
        {
            Phase = phase;
            Kind = kind;
            LockedTarget = lockedTarget;
            PhaseSecondsRemaining = phaseSecondsRemaining;
            WarningDurationSeconds = warningDurationSeconds;
            ExecuteStarted = executeStarted;
        }

        public BossAttackPhase Phase { get; }
        public BossAttackKind Kind { get; }
        public Float2 LockedTarget { get; }
        public float PhaseSecondsRemaining { get; }
        public float WarningDurationSeconds { get; }
        public bool ExecuteStarted { get; }
    }

    public static class BossScaleProfile
    {
        public static float MultiplierFor(BossCombatRole role) => role switch
        {
            BossCombatRole.FirstMidBoss => 1.7f,
            BossCombatRole.SecondMidBoss => 1.9f,
            BossCombatRole.FinalBoss => 2.3f,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        public static float ContactRadius(float normalRadius, BossCombatRole role)
        {
            if (float.IsNaN(normalRadius) || float.IsInfinity(normalRadius) || normalRadius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(normalRadius));
            return normalRadius * MultiplierFor(role);
        }
    }

    public sealed class BossAttackController
    {
        private readonly BossCombatRole role;
        private BossAttackPhase phase = BossAttackPhase.Chase;
        private BossAttackKind kind;
        private Float2 lockedTarget;
        private float phaseSecondsRemaining;
        private float warningDurationSeconds;
        private int attackOrdinal;

        public BossAttackController(BossCombatRole role, float initialCooldownSeconds = .8f)
        {
            if (float.IsNaN(initialCooldownSeconds) || float.IsInfinity(initialCooldownSeconds) ||
                initialCooldownSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(initialCooldownSeconds));
            this.role = role;
            phaseSecondsRemaining = initialCooldownSeconds;
        }

        public BossAttackSnapshot Tick(
            float deltaSeconds,
            Float2 bossPosition,
            Float2 playerPosition,
            float healthFraction)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (float.IsNaN(healthFraction) || float.IsInfinity(healthFraction))
                throw new ArgumentOutOfRangeException(nameof(healthFraction));

            phaseSecondsRemaining -= deltaSeconds;
            var executeStarted = false;
            if (phaseSecondsRemaining <= 0f)
            {
                switch (phase)
                {
                    case BossAttackPhase.Chase:
                        BeginTelegraph(playerPosition);
                        break;
                    case BossAttackPhase.Telegraph:
                        phase = BossAttackPhase.Execute;
                        phaseSecondsRemaining = ExecuteDurationSeconds(kind);
                        executeStarted = true;
                        break;
                    case BossAttackPhase.Execute:
                        phase = BossAttackPhase.Recover;
                        phaseSecondsRemaining = RecoveryDurationSeconds(healthFraction);
                        break;
                    case BossAttackPhase.Recover:
                        phase = BossAttackPhase.Chase;
                        kind = BossAttackKind.None;
                        phaseSecondsRemaining = 0f;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new BossAttackSnapshot(
                phase,
                kind,
                lockedTarget,
                Math.Max(0f, phaseSecondsRemaining),
                warningDurationSeconds,
                executeStarted);
        }

        public float RecoveryDurationSeconds(float healthFraction)
        {
            var baseDuration = role == BossCombatRole.FinalBoss ? 1f : 1.2f;
            return role == BossCombatRole.FinalBoss && healthFraction < .5f
                ? baseDuration * .65f
                : baseDuration;
        }

        private void BeginTelegraph(Float2 playerPosition)
        {
            kind = SelectNextAttack();
            lockedTarget = playerPosition;
            warningDurationSeconds = WarningDurationSeconds(kind);
            phaseSecondsRemaining = warningDurationSeconds;
            phase = BossAttackPhase.Telegraph;
        }

        private BossAttackKind SelectNextAttack()
        {
            if (role == BossCombatRole.FirstMidBoss) return BossAttackKind.SuppressionSlam;
            if (role == BossCombatRole.SecondMidBoss) return BossAttackKind.BloodCharge;
            var selected = attackOrdinal++ % 3;
            return selected == 0
                ? BossAttackKind.BloodCharge
                : selected == 1
                    ? BossAttackKind.SuppressionSlam
                    : BossAttackKind.SpiritVolley;
        }

        private static float WarningDurationSeconds(BossAttackKind attack) => attack switch
        {
            BossAttackKind.SuppressionSlam => 1.1f,
            BossAttackKind.BloodCharge => .95f,
            BossAttackKind.SpiritVolley => .8f,
            _ => throw new ArgumentOutOfRangeException(nameof(attack), attack, null)
        };

        private static float ExecuteDurationSeconds(BossAttackKind attack) =>
            attack == BossAttackKind.BloodCharge ? .45f : .08f;
    }
}

