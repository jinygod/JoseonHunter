using System;
using NUnit.Framework;
using UnityEngine;
using JoseonHunter.Runtime.Gameplay;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class EnemySeparationGridTests
    {
        [Test]
        public void ResolvePushesExactOverlapsInOppositeDirections()
        {
            var grid = new EnemySeparationGrid(.84f);
            grid.Rebuild(new[]
            {
                new EnemySeparationAgent(10, Vector2.zero, .42f),
                new EnemySeparationAgent(11, Vector2.zero, .42f)
            });

            var first = grid.Resolve(0, 8);
            var second = grid.Resolve(1, 8);

            Assert.That(first.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(second.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(Vector2.Dot(first, second), Is.LessThan(0f));
        }

        [Test]
        public void ResolveUsesStableDirectionsForExactOverlapIds()
        {
            var agents = new[]
            {
                new EnemySeparationAgent(77, Vector2.zero, .42f),
                new EnemySeparationAgent(13, Vector2.zero, .42f)
            };
            var grid = new EnemySeparationGrid(.84f);
            grid.Rebuild(agents);
            var first = grid.Resolve(0, 8);
            grid.Rebuild(agents);
            var repeated = grid.Resolve(0, 8);

            Assert.That(repeated, Is.EqualTo(first));
        }

        [Test]
        public void ResolveClampsMagnitudeAndStopsAtMaximumEightNeighbors()
        {
            var agents = new EnemySeparationAgent[10];
            agents[0] = new EnemySeparationAgent(1, Vector2.zero, 1f);
            for (var index = 1; index < agents.Length; index++)
                agents[index] = new EnemySeparationAgent(index + 1, new Vector2(.1f * index, 0f), 1f);

            var grid = new EnemySeparationGrid(2f);
            grid.Rebuild(agents);
            var response = grid.Resolve(0, 8);

            Assert.That(grid.LastNeighborCount, Is.EqualTo(8));
            Assert.That(response.magnitude, Is.LessThanOrEqualTo(1f));
        }

        [TestCase(30)]
        [TestCase(50)]
        [TestCase(100)]
        public void ResolveSeparatesDenseCrowdsAtLoad(int count)
        {
            var agents = DenseAgents(count);
            var grid = new EnemySeparationGrid(.84f);
            grid.Rebuild(agents);

            for (var index = 0; index < agents.Length; index++)
            {
                var response = grid.Resolve(index, 8);
                Assert.That(response.magnitude, Is.LessThanOrEqualTo(1.0001f));
                Assert.That(grid.LastNeighborCount, Is.LessThanOrEqualTo(8));
            }
        }

        [Test]
        public void ConstructorRejectsNonPositiveCellSizes()
        {
            Assert.That(() => new EnemySeparationGrid(0f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new EnemySeparationGrid(-.1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void WarmedRebuildAndResolveAllocateNoManagedBytes()
        {
            var agents = DenseAgents(100);
            var grid = new EnemySeparationGrid(.84f);
            Warm(grid, agents);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var tick = 0; tick < 8; tick++)
            {
                grid.Rebuild(agents);
                for (var index = 0; index < agents.Length; index++) grid.Resolve(index, 8);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.EqualTo(0));
        }

        private static EnemySeparationAgent[] DenseAgents(int count)
        {
            var agents = new EnemySeparationAgent[count];
            for (var index = 0; index < count; index++)
            {
                var column = index % 10;
                var row = index / 10;
                agents[index] = new EnemySeparationAgent(index + 1, new Vector2(column * .12f, row * .12f), .42f);
            }
            return agents;
        }

        private static void Warm(EnemySeparationGrid grid, EnemySeparationAgent[] agents)
        {
            for (var tick = 0; tick < 4; tick++)
            {
                grid.Rebuild(agents);
                for (var index = 0; index < agents.Length; index++) grid.Resolve(index, 8);
            }
        }
    }
}
