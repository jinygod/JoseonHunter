using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class ExperiencePickupMotionTests
    {
        [Test]
        public void AttractionAcceleratesAndForcedCollectionIsFastest()
        {
            var early = ExperiencePickupMotion.SpeedAt(.02f, false);
            var late = ExperiencePickupMotion.SpeedAt(.30f, false);

            Assert.That(late, Is.GreaterThan(early));
            Assert.That(ExperiencePickupMotion.SpeedAt(.02f, true), Is.GreaterThan(late));
        }

        [Test]
        public void StepMovesTowardTargetWithoutOvershooting()
        {
            var result = ExperiencePickupMotion.Step(Vector2.zero, Vector2.right, .30f, 1f, false);

            Assert.That(result, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void AttractionStretchLengthensTravelAxisAndNarrowsCrossAxis()
        {
            var scale = ExperiencePickupMotion.StretchAt(Vector2.right, .30f);

            Assert.That(scale.x, Is.GreaterThan(1f));
            Assert.That(scale.y, Is.LessThan(1f));
            Assert.That(scale.z, Is.EqualTo(1f));
        }
    }
}
