using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatantMotionStateTests
    {
        [Test]
        public void Step_AcceleratesAndSettlesWithoutInvalidPose()
        {
            var state = new CombatantMotionState(0f);
            var moving = state.Step(Vector2.right * 2f, 0.05f, MotionWeight.Light);
            var stopped = moving;
            for (var index = 0; index < 12; index++)
            {
                stopped = state.Step(Vector2.zero, 0.05f, MotionWeight.Light);
            }

            Assert.That(moving.NormalizedSpeed, Is.GreaterThan(0f));
            Assert.That(stopped.NormalizedSpeed, Is.LessThan(moving.NormalizedSpeed));
            Assert.That(float.IsNaN(stopped.TiltDegrees), Is.False);
        }

        [Test]
        public void HeavyMotion_UsesLowerCadenceThanLightMotion()
        {
            var light = new CombatantMotionState(0f);
            var heavy = new CombatantMotionState(0f);
            var lightTransitions = 0;
            var heavyTransitions = 0;
            var previousLight = false;
            var previousHeavy = false;
            for (var index = 0; index < 60; index++)
            {
                var lightDown = light.Step(Vector2.right * 2.4f, 1f / 60f, MotionWeight.Light).FootstepPulse > 0.1f;
                var heavyDown = heavy.Step(Vector2.right * 2.4f, 1f / 60f, MotionWeight.Heavy).FootstepPulse > 0.1f;
                if (lightDown && !previousLight) lightTransitions++;
                if (heavyDown && !previousHeavy) heavyTransitions++;
                previousLight = lightDown;
                previousHeavy = heavyDown;
            }

            Assert.That(lightTransitions, Is.GreaterThan(heavyTransitions));
        }

        [Test]
        public void Hit_RecoilIsBoundedAndDecays()
        {
            var state = new CombatantMotionState(0.25f);
            state.Hit(Vector2.left, 10f);
            var first = state.Step(Vector2.zero, 0f, MotionWeight.Medium);
            var settled = first;
            for (var index = 0; index < 30; index++)
            {
                settled = state.Step(Vector2.zero, 1f / 60f, MotionWeight.Medium);
            }

            Assert.That(first.VisualOffset.magnitude, Is.LessThanOrEqualTo(0.1201f));
            Assert.That(settled.VisualOffset.magnitude, Is.LessThan(first.VisualOffset.magnitude));
        }

        [Test]
        public void Kill_ReachesCollapsedPose()
        {
            var state = new CombatantMotionState(0f);
            state.Kill();
            var pose = state.Step(Vector2.zero, 0.28f, MotionWeight.Heavy);
            Assert.That(pose.DeathProgress, Is.EqualTo(1f));
            Assert.That(pose.Scale.y, Is.LessThan(0.2f));
        }
    }
}
