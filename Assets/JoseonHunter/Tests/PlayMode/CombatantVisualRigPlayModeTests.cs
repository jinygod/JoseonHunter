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

        [UnityTest]
        public IEnumerator PlayerRoleCreatesReadableLayersWithoutChangingCollisionScale()
        {
            var root = new GameObject("Player");
            root.transform.localScale = Vector3.one * 0.62f;

            var rig = CombatantVisualRig.Create(
                root,
                null,
                10,
                null,
                MotionWeight.Light,
                0f,
                CombatantVisualRole.Player);
            yield return null;

            Assert.That(root.transform.Find("Soft Shadow"), Is.Not.Null);
            Assert.That(root.transform.Find("Player Aura"), Is.Not.Null);
            Assert.That(root.transform.Find("Visual Pivot"), Is.Not.Null);
            Assert.That(rig.CollisionTransform(default).Scale.x, Is.EqualTo(0.62f).Within(0.001f));
            Assert.That(rig.CollisionTransform(default).Scale.y, Is.EqualTo(0.62f).Within(0.001f));

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator EnemyRoleHasShadowButNoPlayerAura()
        {
            var root = new GameObject("Enemy");
            CombatantVisualRig.Create(
                root,
                null,
                8,
                null,
                MotionWeight.Medium,
                0f,
                CombatantVisualRole.Enemy);
            yield return null;

            Assert.That(root.transform.Find("Soft Shadow"), Is.Not.Null);
            Assert.That(root.transform.Find("Player Aura"), Is.Null);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator HitFlashIsLocalAndRestoresTheCombatantColor()
        {
            var root = new GameObject("Hit Flash Enemy");
            var rig = CombatantVisualRig.Create(
                root,
                null,
                8,
                null,
                MotionWeight.Medium);
            rig.Renderer.color = new Color(.72f, .84f, .92f, 1f);
            var baseline = rig.Renderer.color;

            rig.ShowHit(Vector2.left, .1f);
            rig.Tick(Vector2.zero, 1f / 60f, MotionWeight.Medium);
            Assert.That(rig.Renderer.color, Is.Not.EqualTo(baseline));
            Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));

            for (var index = 0; index < 8; index++)
                rig.Tick(Vector2.zero, 1f / 60f, MotionWeight.Medium);

            Assert.That(rig.Renderer.color.r, Is.EqualTo(baseline.r).Within(.01f));
            Assert.That(rig.Renderer.color.g, Is.EqualTo(baseline.g).Within(.01f));
            Assert.That(rig.Renderer.color.b, Is.EqualTo(baseline.b).Within(.01f));
            Object.Destroy(root);
            yield return null;
        }
    }
}
