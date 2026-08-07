using System;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Combat
{
    public enum EnemyAttackKind
    {
        DirectChase,
        DirectionalGuard,
        WarnedLineCharge,
        WarnedSingleProjectile,
        CircleSlam,
        ConeSweep,
        Rockfall,
        SpinSweep,
        WarnedLineProjectile,
        PredictedCurseField,
        WarnedBurrowEmergence
    }

    public enum EnemyAttackPhase
    {
        Chase,
        Telegraph,
        Execute,
        Recover
    }

    public struct DirectionalGuardState
    {
        private readonly float blockedDamageMultiplier;
        private int remainingCharges;

        public DirectionalGuardState(int charges, float blockedDamageMultiplier)
        {
            if (charges <= 0) throw new ArgumentOutOfRangeException(nameof(charges));
            if (float.IsNaN(blockedDamageMultiplier) || float.IsInfinity(blockedDamageMultiplier) ||
                blockedDamageMultiplier < 0f || blockedDamageMultiplier > 1f)
                throw new ArgumentOutOfRangeException(nameof(blockedDamageMultiplier));
            remainingCharges = charges;
            this.blockedDamageMultiplier = blockedDamageMultiplier;
        }

        public bool IsBroken => remainingCharges <= 0;
        public int RemainingCharges => Math.Max(0, remainingCharges);
        public float IncomingDamageMultiplier => IsBroken ? 1f : blockedDamageMultiplier;

        public bool ConfirmBlockedHit()
        {
            if (IsBroken) return false;
            remainingCharges--;
            return remainingCharges == 0;
        }
    }

    public readonly struct EnemyAttackSnapshot
    {
        public EnemyAttackSnapshot(
            EnemyAttackPhase phase,
            EnemyAttackKind kind,
            Float2 lockedTarget,
            float warningSeconds,
            bool executeStarted)
        {
            Phase = phase;
            Kind = kind;
            LockedTarget = lockedTarget;
            WarningSeconds = warningSeconds;
            ExecuteStarted = executeStarted;
        }

        public EnemyAttackPhase Phase { get; }
        public EnemyAttackKind Kind { get; }
        public Float2 LockedTarget { get; }
        public float WarningSeconds { get; }
        public bool ExecuteStarted { get; }
    }

    public sealed class EnemyAttackController
    {
        private readonly EnemyAttackKind kind;
        private EnemyAttackPhase phase = EnemyAttackPhase.Chase;
        private float remaining;
        private Float2 lockedTarget;

        public EnemyAttackController(EnemyAttackKind kind, float initialCooldownSeconds = 1f)
        {
            if (float.IsNaN(initialCooldownSeconds) || float.IsInfinity(initialCooldownSeconds) ||
                initialCooldownSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(initialCooldownSeconds));
            this.kind = kind;
            remaining = initialCooldownSeconds;
        }

        public EnemyAttackSnapshot Tick(
            float deltaSeconds,
            Float2 enemyPosition,
            Float2 playerPosition,
            bool canAcquireTarget)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            remaining -= deltaSeconds;
            var executeStarted = false;
            if (remaining <= 0f)
            {
                switch (phase)
                {
                    case EnemyAttackPhase.Chase when canAcquireTarget:
                        lockedTarget = playerPosition;
                        remaining = WarningSecondsFor(kind);
                        phase = EnemyAttackPhase.Telegraph;
                        break;
                    case EnemyAttackPhase.Chase:
                        remaining = .1f;
                        break;
                    case EnemyAttackPhase.Telegraph:
                        remaining = ExecuteSecondsFor(kind);
                        phase = EnemyAttackPhase.Execute;
                        executeStarted = true;
                        break;
                    case EnemyAttackPhase.Execute:
                        remaining = RecoverySecondsFor(kind);
                        phase = EnemyAttackPhase.Recover;
                        break;
                    case EnemyAttackPhase.Recover:
                        remaining = CooldownSecondsFor(kind);
                        phase = EnemyAttackPhase.Chase;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new EnemyAttackSnapshot(phase, kind, lockedTarget, WarningSecondsFor(kind), executeStarted);
        }

        private static float WarningSecondsFor(EnemyAttackKind attack) => attack switch
        {
            EnemyAttackKind.WarnedSingleProjectile => .75f,
            EnemyAttackKind.WarnedLineProjectile => .85f,
            EnemyAttackKind.PredictedCurseField => 1f,
            EnemyAttackKind.WarnedBurrowEmergence => 1.05f,
            EnemyAttackKind.WarnedLineCharge => .8f,
            EnemyAttackKind.CircleSlam => 1f,
            EnemyAttackKind.ConeSweep => .85f,
            EnemyAttackKind.Rockfall => .9f,
            EnemyAttackKind.SpinSweep => .75f,
            _ => .7f
        };

        private static float ExecuteSecondsFor(EnemyAttackKind attack) =>
            attack == EnemyAttackKind.WarnedLineCharge ? .42f : .08f;

        private static float RecoverySecondsFor(EnemyAttackKind attack) =>
            attack == EnemyAttackKind.WarnedLineCharge ? 1.15f : .8f;

        private static float CooldownSecondsFor(EnemyAttackKind attack) =>
            attack switch
            {
                EnemyAttackKind.WarnedSingleProjectile => 2.2f,
                EnemyAttackKind.WarnedLineProjectile => 2.45f,
                EnemyAttackKind.PredictedCurseField => 3.2f,
                EnemyAttackKind.WarnedBurrowEmergence => 3.6f,
                _ => 1.35f
            };
    }

    public static class RangedAttackRules
    {
        public static bool CanAcquireTarget(bool insideCameraMargin, float distance, float maximumDistance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f)
                throw new ArgumentOutOfRangeException(nameof(distance));
            if (float.IsNaN(maximumDistance) || float.IsInfinity(maximumDistance) || maximumDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumDistance));
            return insideCameraMargin && distance <= maximumDistance;
        }
    }
}
