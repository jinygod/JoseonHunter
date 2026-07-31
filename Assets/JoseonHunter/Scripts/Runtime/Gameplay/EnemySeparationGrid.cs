using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public readonly struct EnemySeparationAgent
    {
        public EnemySeparationAgent(int id, Vector2 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = Mathf.Max(0f, radius);
        }

        public int Id { get; }
        public Vector2 Position { get; }
        public float Radius { get; }
    }

    public sealed class EnemySeparationGrid
    {
        private const int MaximumSupportedNeighbors = 8;
        private readonly float cellSize;
        private readonly Dictionary<Vector2Int, List<int>> buckets = new Dictionary<Vector2Int, List<int>>();
        private readonly Stack<List<int>> reusableBuckets = new Stack<List<int>>();
        private readonly List<Vector2Int> occupiedKeys = new List<Vector2Int>();
        private IReadOnlyList<EnemySeparationAgent> agents;
        private float largestRadius;

        public EnemySeparationGrid(float cellSize)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            this.cellSize = cellSize;
        }

        public int LastNeighborCount { get; private set; }

        public void Rebuild(IReadOnlyList<EnemySeparationAgent> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            for (var index = 0; index < occupiedKeys.Count; index++)
            {
                var key = occupiedKeys[index];
                var bucket = buckets[key];
                bucket.Clear();
                reusableBuckets.Push(bucket);
            }
            buckets.Clear();
            occupiedKeys.Clear();
            agents = source;
            largestRadius = 0f;

            for (var index = 0; index < source.Count; index++)
            {
                var agent = source[index];
                largestRadius = Mathf.Max(largestRadius, agent.Radius);
                var key = CellFor(agent.Position);
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = reusableBuckets.Count > 0 ? reusableBuckets.Pop() : new List<int>();
                    buckets.Add(key, bucket);
                    occupiedKeys.Add(key);
                }
                bucket.Add(index);
            }
        }

        public Vector2 Resolve(int agentIndex, int maximumNeighbors)
        {
            if (agents == null) throw new InvalidOperationException("Rebuild must be called before resolving separation.");
            if (agentIndex < 0 || agentIndex >= agents.Count) throw new ArgumentOutOfRangeException(nameof(agentIndex));

            LastNeighborCount = 0;
            var limit = Mathf.Min(MaximumSupportedNeighbors, Mathf.Max(0, maximumNeighbors));
            if (limit == 0) return Vector2.zero;

            var agent = agents[agentIndex];
            var cell = CellFor(agent.Position);
            var range = Mathf.CeilToInt((agent.Radius + largestRadius) / cellSize);
            var result = Vector2.zero;
            for (var y = -range; y <= range && LastNeighborCount < limit; y++)
            {
                for (var x = -range; x <= range && LastNeighborCount < limit; x++)
                {
                    if (!buckets.TryGetValue(new Vector2Int(cell.x + x, cell.y + y), out var bucket)) continue;
                    for (var bucketIndex = 0; bucketIndex < bucket.Count && LastNeighborCount < limit; bucketIndex++)
                    {
                        var neighborIndex = bucket[bucketIndex];
                        if (neighborIndex == agentIndex) continue;
                        var neighbor = agents[neighborIndex];
                        var displacement = agent.Position - neighbor.Position;
                        var combinedRadius = agent.Radius + neighbor.Radius;
                        var distance = displacement.magnitude;
                        if (distance >= combinedRadius) continue;

                        var direction = distance > Mathf.Epsilon
                            ? displacement / distance
                            : CoincidentDirection(agent.Id, neighbor.Id);
                        result += direction * (combinedRadius - distance);
                        LastNeighborCount++;
                    }
                }
            }
            return Vector2.ClampMagnitude(result, 1f);
        }

        private Vector2Int CellFor(Vector2 position) => new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize));

        private static Vector2 CoincidentDirection(int firstId, int secondId)
        {
            var low = Math.Min(firstId, secondId);
            var high = Math.Max(firstId, secondId);
            var hash = unchecked(low * 397 ^ high * 7919);
            var angle = (hash & 1023) * (Mathf.PI * 2f / 1024f);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return firstId <= secondId ? direction : -direction;
        }
    }
}
