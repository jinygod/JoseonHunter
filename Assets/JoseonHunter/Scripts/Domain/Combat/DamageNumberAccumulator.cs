using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Combat
{
    public readonly struct DamageNumberDisplay
    {
        public DamageNumberDisplay(int displayedDamage, Float2 contactPoint, bool isCritical, WeaponId weaponId, int targetRuntimeId)
        {
            DisplayedDamage = displayedDamage;
            ContactPoint = contactPoint;
            IsCritical = isCritical;
            WeaponId = weaponId;
            TargetRuntimeId = targetRuntimeId;
        }

        public int DisplayedDamage { get; }
        public Float2 ContactPoint { get; }
        public bool IsCritical { get; }
        public WeaponId WeaponId { get; }
        public int TargetRuntimeId { get; }
    }

    /// <summary>Coalesces nearby confirmed hits without changing their combat results.</summary>
    public sealed class DamageNumberAccumulator
    {
        private readonly float aggregationWindow;
        private readonly Dictionary<AggregationKey, PendingDamage> pending = new Dictionary<AggregationKey, PendingDamage>();

        public DamageNumberAccumulator(float aggregationWindow = 0.25f)
        {
            if (float.IsNaN(aggregationWindow) || float.IsInfinity(aggregationWindow) || aggregationWindow < 0f)
                throw new ArgumentOutOfRangeException(nameof(aggregationWindow));

            this.aggregationWindow = aggregationWindow;
        }

        public int PendingCount => pending.Count;

        public void Add(in ConfirmedDamageEvent confirmed, float time)
        {
            if (!IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));

            var key = new AggregationKey(confirmed.AttackInstanceId, confirmed.TargetRuntimeId, confirmed.WeaponId);
            if (pending.TryGetValue(key, out var existing))
            {
                existing.Add(confirmed);
                pending[key] = existing;
                return;
            }

            pending.Add(key, new PendingDamage(confirmed, time));
        }

        public DamageNumberDisplay[] FlushReady(float time)
        {
            if (!IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));

            var ready = new List<DamageNumberDisplay>();
            var expired = new List<AggregationKey>();
            foreach (var entry in pending)
            {
                if (time < entry.Value.StartTime + aggregationWindow) continue;
                ready.Add(entry.Value.ToDisplay());
                expired.Add(entry.Key);
            }

            foreach (var key in expired) pending.Remove(key);
            return ready.Count == 0 ? Array.Empty<DamageNumberDisplay>() : ready.ToArray();
        }

        public void Clear() => pending.Clear();

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private readonly struct AggregationKey : IEquatable<AggregationKey>
        {
            public AggregationKey(int attackInstanceId, int targetRuntimeId, WeaponId weaponId)
            {
                AttackInstanceId = attackInstanceId;
                TargetRuntimeId = targetRuntimeId;
                WeaponId = weaponId;
            }

            private int AttackInstanceId { get; }
            private int TargetRuntimeId { get; }
            private WeaponId WeaponId { get; }

            public bool Equals(AggregationKey other) => AttackInstanceId == other.AttackInstanceId && TargetRuntimeId == other.TargetRuntimeId && WeaponId.Equals(other.WeaponId);
            public override bool Equals(object obj) => obj is AggregationKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(AttackInstanceId, TargetRuntimeId, WeaponId);
        }

        private struct PendingDamage
        {
            public PendingDamage(ConfirmedDamageEvent confirmed, float startTime)
            {
                Damage = confirmed.FinalDamage;
                ContactPoint = confirmed.ContactPoint;
                IsCritical = confirmed.IsCritical;
                WeaponId = confirmed.WeaponId;
                TargetRuntimeId = confirmed.TargetRuntimeId;
                StartTime = startTime;
            }

            public int Damage;
            public Float2 ContactPoint;
            public bool IsCritical;
            public WeaponId WeaponId;
            public int TargetRuntimeId;
            public float StartTime;

            public void Add(ConfirmedDamageEvent confirmed)
            {
                Damage = SaturatingAdd(Damage, confirmed.FinalDamage);
                ContactPoint = confirmed.ContactPoint;
                IsCritical |= confirmed.IsCritical;
            }

            public DamageNumberDisplay ToDisplay() => new DamageNumberDisplay(Damage, ContactPoint, IsCritical, WeaponId, TargetRuntimeId);

            private static int SaturatingAdd(int left, int right) => right > 0 && left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }
}
