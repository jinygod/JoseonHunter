using System.Linq;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class BattlefieldChunkLayoutTests
    {
        [TestCase(0f, 0f, 0, 0)]
        [TestCase(31.99f, 31.99f, 0, 0)]
        [TestCase(32f, 0f, 1, 0)]
        [TestCase(-0.01f, -0.01f, -1, -1)]
        [TestCase(-32f, 64f, -1, 2)]
        public void CoordinateAtUsesFloorAcrossNegativeWorldSpace(
            float x,
            float y,
            int expectedX,
            int expectedY)
        {
            Assert.That(BattlefieldChunkLayout.CoordinateAt(new Vector2(x, y)),
                Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        [Test]
        public void RequiredCoordinatesAlwaysContainAThreeByThreeNeighborhood()
        {
            var output = new Vector2Int[9];

            BattlefieldChunkLayout.FillRequired(new Vector2Int(4, -3), output);

            Assert.That(output.Distinct().Count(), Is.EqualTo(9));
            Assert.That(output, Does.Contain(new Vector2Int(3, -4)));
            Assert.That(output, Does.Contain(new Vector2Int(4, -3)));
            Assert.That(output, Does.Contain(new Vector2Int(5, -2)));
        }

        [Test]
        public void DecorationSeedIsStableForAWorldCoordinate()
        {
            var coordinate = new Vector2Int(-17, 93);

            var first = BattlefieldChunkLayout.DecorationSeed(coordinate, 0x4A4F5345);
            var second = BattlefieldChunkLayout.DecorationSeed(coordinate, 0x4A4F5345);
            var neighbor = BattlefieldChunkLayout.DecorationSeed(new Vector2Int(-16, 93), 0x4A4F5345);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(neighbor, Is.Not.EqualTo(first));
        }

        [Test]
        public void FillRequiredRejectsAnUndersizedBuffer()
        {
            Assert.That(
                () => BattlefieldChunkLayout.FillRequired(Vector2Int.zero, new Vector2Int[8]),
                Throws.TypeOf<System.ArgumentException>());
        }
    }
}
