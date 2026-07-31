using System;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class ViewportSpawnGeometryTests
    {
        [TestCase(0, 0.25f, -2.25f, 9f)]
        [TestCase(1, 0.50f, 5.5f, 0f)]
        [TestCase(2, 0.75f, -2.25f, -9f)]
        [TestCase(3, 1.00f, -5.5f, -8f)]
        public void PointOnExpandedPerimeterPlacesPointOutsideOnRequestedSide(int side, float t, float expectedX, float expectedY)
        {
            var view = new Rect(-4.5f, -8f, 9f, 16f);
            var point = ViewportSpawnGeometry.PointOnExpandedPerimeter(view, side, t, 1f);
            Assert.That(view.Contains(point), Is.False);
            Assert.That(point, Is.EqualTo(new Vector2(expectedX, expectedY)));
        }

        [Test]
        public void PointOnExpandedPerimeterClampsInterpolationAndNegativeMargin()
        {
            var point = ViewportSpawnGeometry.PointOnExpandedPerimeter(new Rect(-4.5f, -8f, 9f, 16f), 0, 2f, -1f);
            Assert.That(point, Is.EqualTo(new Vector2(4.5f, 8f)));
        }

        [Test]
        public void PointOnExpandedPerimeterRejectsUnknownSide()
        {
            Assert.That(() => ViewportSpawnGeometry.PointOnExpandedPerimeter(new Rect(), 4, .5f, 1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
