using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Runtime.Audio
{
    public sealed class GameAudioPlaybackBudget
    {
        private const int MaximumTrackedAttacks = 64;

        private readonly struct WeaponAttackKey : IEquatable<WeaponAttackKey>
        {
            public WeaponAttackKey(WeaponId weaponId, int attackInstanceId)
            {
                WeaponId = weaponId;
                AttackInstanceId = attackInstanceId;
            }

            private WeaponId WeaponId { get; }
            private int AttackInstanceId { get; }

            public bool Equals(WeaponAttackKey other) =>
                WeaponId.Equals(other.WeaponId) && AttackInstanceId == other.AttackInstanceId;

            public override bool Equals(object obj) => obj is WeaponAttackKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (WeaponId.GetHashCode() * 397) ^ AttackInstanceId;
                }
            }
        }

        private readonly int sourceCapacity;
        private readonly Dictionary<GameAudioCueId, float> lastPlayed =
            new Dictionary<GameAudioCueId, float>();
        private readonly HashSet<WeaponAttackKey> weaponAttacks = new HashSet<WeaponAttackKey>();
        private readonly Queue<WeaponAttackKey> weaponAttackOrder = new Queue<WeaponAttackKey>();

        public GameAudioPlaybackBudget(int sourceCapacity)
        {
            if (sourceCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            this.sourceCapacity = sourceCapacity;
        }

        public int TrackedAttackCount => weaponAttacks.Count;

        public bool TryReserve(GameAudioCueId cue, float now, int activeSources)
        {
            if (cue == GameAudioCueId.None || !IsFinite(now) || activeSources < 0) return false;
            if (activeSources >= sourceCapacity && PriorityFor(cue) < GameAudioPriority.High) return false;

            var interval = MinimumIntervalFor(cue);
            if (lastPlayed.TryGetValue(cue, out var previous) && now - previous + .0001f < interval)
                return false;

            lastPlayed[cue] = now;
            return true;
        }

        public bool TryReserveWeapon(WeaponId weaponId, int attackInstanceId, float now, int activeSources)
        {
            if (attackInstanceId <= 0 || !IsFinite(now) || activeSources < 0 || activeSources >= sourceCapacity)
                return false;

            var key = new WeaponAttackKey(weaponId, attackInstanceId);
            if (!weaponAttacks.Add(key)) return false;
            weaponAttackOrder.Enqueue(key);
            if (weaponAttacks.Count <= MaximumTrackedAttacks) return true;

            var expired = weaponAttackOrder.Dequeue();
            weaponAttacks.Remove(expired);
            return true;
        }

        public void Reset()
        {
            lastPlayed.Clear();
            weaponAttacks.Clear();
            weaponAttackOrder.Clear();
        }

        public static GameAudioPriority PriorityFor(GameAudioCueId cue)
        {
            switch (cue)
            {
                case GameAudioCueId.UiConfirm:
                case GameAudioCueId.LevelUp:
                case GameAudioCueId.BossWarning:
                case GameAudioCueId.BossAppear:
                case GameAudioCueId.BossDefeat:
                case GameAudioCueId.Victory:
                case GameAudioCueId.Defeat:
                    return GameAudioPriority.High;
                case GameAudioCueId.UiClick:
                case GameAudioCueId.UiCancel:
                case GameAudioCueId.UpgradeSelected:
                case GameAudioCueId.CriticalHit:
                    return GameAudioPriority.Medium;
                default:
                    return GameAudioPriority.Low;
            }
        }

        private static float MinimumIntervalFor(GameAudioCueId cue)
        {
            switch (cue)
            {
                case GameAudioCueId.UiClick:
                case GameAudioCueId.UiConfirm:
                case GameAudioCueId.UiCancel:
                    return .06f;
                case GameAudioCueId.ExperiencePickup:
                    return .09f;
                case GameAudioCueId.YeopjeonPickup:
                    return .10f;
                case GameAudioCueId.NormalHit:
                    return .05f;
                default:
                    return 0f;
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
