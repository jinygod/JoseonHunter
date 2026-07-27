using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Combat
{
    public readonly struct CombatTargetSnapshot : IEquatable<CombatTargetSnapshot>
    {
        public CombatTargetSnapshot(int runtimeId, float health, float threat, bool isElite, bool isBoss, Float2 position)
        {
            RuntimeId = runtimeId;
            Health = health;
            Threat = threat;
            IsElite = isElite;
            IsBoss = isBoss;
            Position = position;
        }

        public int RuntimeId { get; }
        public float Health { get; }
        public float Threat { get; }
        public bool IsElite { get; }
        public bool IsBoss { get; }
        public Float2 Position { get; }

        public bool Equals(CombatTargetSnapshot other) =>
            RuntimeId == other.RuntimeId && Health.Equals(other.Health) && Threat.Equals(other.Threat) &&
            IsElite == other.IsElite && IsBoss == other.IsBoss && Position.Equals(other.Position);

        public override bool Equals(object obj) => obj is CombatTargetSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RuntimeId, Health, Threat, IsElite, IsBoss, Position);
    }

    public static class CombatTargetSelector
    {
        public static CombatTargetSnapshot? Select(WeaponTargeting targeting, Float2 playerPosition, IReadOnlyList<CombatTargetSnapshot> candidates)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (candidates.Count == 0) return null;
            if (!Enum.IsDefined(typeof(WeaponTargeting), targeting))
                throw new ArgumentOutOfRangeException(nameof(targeting), targeting, "Unknown weapon targeting mode.");

            var selected = candidates[0];
            for (var index = 1; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (IsPreferred(targeting, playerPosition, candidate, selected, candidates)) selected = candidate;
            }

            return selected;
        }

        private static bool IsPreferred(WeaponTargeting targeting, Float2 playerPosition, CombatTargetSnapshot candidate, CombatTargetSnapshot current, IReadOnlyList<CombatTargetSnapshot> candidates)
        {
            switch (targeting)
            {
                case WeaponTargeting.HighestThreat:
                    return CompareHighestThreat(candidate, current) < 0;
                case WeaponTargeting.DensestCenter:
                case WeaponTargeting.PredictedCrowd:
                    return CompareFloat(DistanceSum(candidate, candidates), DistanceSum(current, candidates), candidate.RuntimeId, current.RuntimeId) < 0;
                case WeaponTargeting.DensestDirection:
                    return CompareDensestDirection(playerPosition, candidate, current, candidates) < 0;
                case WeaponTargeting.DangerousSector:
                    return CompareFloat(-DangerScore(playerPosition, candidate), -DangerScore(playerPosition, current), candidate.RuntimeId, current.RuntimeId) < 0;
                case WeaponTargeting.Nearest:
                case WeaponTargeting.NearestUnmarked:
                case WeaponTargeting.PlayerBoundary:
                    return CompareFloat(DistanceSquared(playerPosition, candidate.Position), DistanceSquared(playerPosition, current.Position), candidate.RuntimeId, current.RuntimeId) < 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targeting), targeting, "Unknown weapon targeting mode.");
            }
        }

        private static int CompareHighestThreat(CombatTargetSnapshot candidate, CombatTargetSnapshot current)
        {
            var result = current.IsBoss.CompareTo(candidate.IsBoss);
            if (result != 0) return result;
            result = current.IsElite.CompareTo(candidate.IsElite);
            if (result != 0) return result;
            result = current.Threat.CompareTo(candidate.Threat);
            if (result != 0) return result;
            return candidate.RuntimeId.CompareTo(current.RuntimeId);
        }

        private static int CompareDensestDirection(Float2 playerPosition, CombatTargetSnapshot candidate, CombatTargetSnapshot current, IReadOnlyList<CombatTargetSnapshot> candidates)
        {
            var candidateCount = SameDirectionCount(playerPosition, candidate, candidates);
            var currentCount = SameDirectionCount(playerPosition, current, candidates);
            var result = currentCount.CompareTo(candidateCount);
            if (result != 0) return result;
            return candidate.RuntimeId.CompareTo(current.RuntimeId);
        }

        private static int SameDirectionCount(Float2 playerPosition, CombatTargetSnapshot target, IReadOnlyList<CombatTargetSnapshot> candidates)
        {
            var directionX = target.Position.X - playerPosition.X;
            var directionY = target.Position.Y - playerPosition.Y;
            var count = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                var offsetX = candidates[index].Position.X - playerPosition.X;
                var offsetY = candidates[index].Position.Y - playerPosition.Y;
                if (directionX * offsetX + directionY * offsetY >= 0f) count++;
            }

            return count;
        }

        private static float DangerScore(Float2 playerPosition, CombatTargetSnapshot target)
        {
            var priority = target.Threat + (target.IsElite ? 25f : 0f) + (target.IsBoss ? 100f : 0f);
            return priority / (1f + DistanceSquared(playerPosition, target.Position));
        }

        private static float DistanceSum(CombatTargetSnapshot target, IReadOnlyList<CombatTargetSnapshot> candidates)
        {
            var total = 0f;
            for (var index = 0; index < candidates.Count; index++) total += Distance(target.Position, candidates[index].Position);
            return total;
        }

        private static float Distance(Float2 first, Float2 second) => (float)Math.Sqrt(DistanceSquared(first, second));
        private static float DistanceSquared(Float2 first, Float2 second)
        {
            var x = first.X - second.X;
            var y = first.Y - second.Y;
            return x * x + y * y;
        }

        private static int CompareFloat(float candidate, float current, int candidateId, int currentId)
        {
            var result = candidate.CompareTo(current);
            return result != 0 ? result : candidateId.CompareTo(currentId);
        }
    }
}
