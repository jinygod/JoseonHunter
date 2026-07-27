using JoseonHunter.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StaticSpriteMotionStateTests
    {
        [Test]
        public void SetVelocityUpdatesFacingAndPreservesLastFacingWhenIdle()
        {
            var state = new StaticSpriteMotionState();

            state.SetVelocity(new Vector2(1f, 0f));
            Assert.That(state.FlipX, Is.False);

            state.SetVelocity(new Vector2(-1f, 0f));
            Assert.That(state.FlipX, Is.True);

            state.SetVelocity(Vector2.zero);
            Assert.That(state.FlipX, Is.True);
        }

        [Test]
        public void StepAppliesMovingBobAndTiltWithinSpecifiedLimits()
        {
            var state = new StaticSpriteMotionState();
            state.SetVelocity(new Vector2(100f, 0f));

            state.Step(1f / 24f);

            Assert.That(state.BobOffset, Is.EqualTo(1f / 32f).Within(0.0001f));
            Assert.That(Mathf.Abs(state.TiltDegrees), Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void StepKeepsIdleBobAtZero()
        {
            var state = new StaticSpriteMotionState();
            state.SetVelocity(Vector2.zero);

            state.Step(1f / 24f);

            Assert.That(state.BobOffset, Is.Zero);
            Assert.That(state.TiltDegrees, Is.Zero);
        }

        [Test]
        public void ResetClearsMotionWithoutChangingLastFacing()
        {
            var state = new StaticSpriteMotionState();
            state.SetVelocity(new Vector2(-1f, 0f));
            state.Step(1f / 24f);

            state.Reset();

            Assert.That(state.FlipX, Is.True);
            Assert.That(state.BobOffset, Is.Zero);
            Assert.That(state.TiltDegrees, Is.Zero);
        }
    }
}
