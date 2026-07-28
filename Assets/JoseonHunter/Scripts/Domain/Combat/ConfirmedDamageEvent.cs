using System;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Combat
{
    public readonly struct ConfirmedDamageEvent : IEquatable<ConfirmedDamageEvent>
    {
        public ConfirmedDamageEvent(int attackInstanceId, WeaponId weaponId, int targetRuntimeId, DamageResult result, Float2 contactPoint, ContactPhase phase, int simulationTick)
            : this(attackInstanceId, weaponId, targetRuntimeId, result, contactPoint, phase, simulationTick, false)
        {
        }

        public ConfirmedDamageEvent(int attackInstanceId, WeaponId weaponId, int targetRuntimeId, DamageResult result, Float2 contactPoint, ContactPhase phase, int simulationTick, bool isBossTarget)
        {
            AttackInstanceId = attackInstanceId;
            WeaponId = weaponId;
            TargetRuntimeId = targetRuntimeId;
            Result = result;
            ContactPoint = contactPoint;
            Phase = phase;
            SimulationTick = simulationTick;
            IsBossTarget = isBossTarget;
        }

        public int AttackInstanceId { get; }
        public WeaponId WeaponId { get; }
        public int TargetRuntimeId { get; }
        public DamageResult Result { get; }
        public int FinalDamage => Result.FinalDamage;
        public bool IsCritical => Result.IsCritical;
        public Float2 ContactPoint { get; }
        public ContactPhase Phase { get; }
        public int SimulationTick { get; }
        /// <summary>Event-time target classification, preserved after gameplay removes a killed target.</summary>
        public bool IsBossTarget { get; }

        public bool Equals(ConfirmedDamageEvent other) =>
            AttackInstanceId == other.AttackInstanceId && WeaponId.Equals(other.WeaponId) &&
            TargetRuntimeId == other.TargetRuntimeId && Result.Equals(other.Result) &&
            ContactPoint.Equals(other.ContactPoint) && Phase == other.Phase && SimulationTick == other.SimulationTick &&
            IsBossTarget == other.IsBossTarget;

        public override bool Equals(object obj) => obj is ConfirmedDamageEvent other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(HashCode.Combine(AttackInstanceId, WeaponId, TargetRuntimeId, Result), HashCode.Combine(ContactPoint, Phase, SimulationTick, IsBossTarget));
        public static bool operator ==(ConfirmedDamageEvent left, ConfirmedDamageEvent right) => left.Equals(right);
        public static bool operator !=(ConfirmedDamageEvent left, ConfirmedDamageEvent right) => !left.Equals(right);
    }
}
