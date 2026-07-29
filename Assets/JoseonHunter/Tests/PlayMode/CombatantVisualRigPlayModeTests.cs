using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatantVisualRigPlayModeTests
    {
        [UnityTest]
        public IEnumerator PresentationMotionNeverMovesLogicalCombatRoot()
        {
            var root = new GameObject("Combatant");
            root.transform.position = new Vector3(3f, -2f, 0f);
            var rig = CombatantVisualRig.Create(
                root,
                null,
                10,
                null,
                MotionWeight.Light,
                0.25f);
            var logicalPosition = root.transform.position;

            for (var index = 0; index < 12; index++)
            {
                rig.Tick(new Vector2(2f, 0.4f), 1f / 60f, MotionWeight.Light);
                yield return null;
            }

            Assert.That(root.transform.position, Is.EqualTo(logicalPosition));
            Assert.That(rig.Renderer.transform.localPosition, Is.Not.EqualTo(Vector3.zero));
            Assert.That(rig.FacingLeft, Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator HitAndDeathAreConfinedToVisualPivot()
        {
            var root = new GameObject("Combatant");
            var rig = CombatantVisualRig.Create(
                root,
                null,
                10,
                null,
                MotionWeight.Heavy);

            rig.ShowHit(Vector2.left, 0.12f);
            rig.Tick(Vector2.zero, 1f / 60f, MotionWeight.Heavy);
            Assert.That(rig.Renderer.transform.localPosition.x, Is.LessThan(0f));
            Assert.That(root.transform.localPosition, Is.EqualTo(Vector3.zero));

            rig.PlayDeath();
            for (var index = 0; index < 24; index++)
            {
                rig.Tick(Vector2.zero, 1f / 60f, MotionWeight.Heavy);
                yield return null;
            }

            Assert.That(rig.Renderer.color.a, Is.Zero.Within(0.0001f));
            Assert.That(root.transform.localPosition, Is.EqualTo(Vector3.zero));

            Object.Destroy(root);
        }
    }
}
