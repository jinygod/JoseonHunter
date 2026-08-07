using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StageAttackPoolTests
    {
        [Test]
        public void ProjectilePoolNeverExceedsCapacityAndExpiresHandles()
        {
            var root = new GameObject("Projectile Pool Test");
            try
            {
                var pool = root.AddComponent<EnemyProjectilePool>();
                pool.Configure(2, null);

                pool.Launch(Vector2.zero, Vector2.right, 1f, 3f, Color.white);
                pool.Launch(Vector2.zero, Vector2.up, 1f, 3f, Color.white);
                pool.Launch(Vector2.zero, Vector2.left, 1f, 3f, Color.white);

                Assert.That(pool.Capacity, Is.EqualTo(2));
                Assert.That(pool.ActiveCount, Is.EqualTo(2));

                pool.Tick(1.01f, new Vector2(100f, 100f), .2f, null);
                Assert.That(pool.ActiveCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HazardPoolExpiresAndAppliesDamageOnlyOnItsCadence()
        {
            var root = new GameObject("Hazard Pool Test");
            try
            {
                var pool = root.AddComponent<StageHazardPool>();
                pool.Configure(2, null);
                var damageEvents = 0;
                pool.Activate(Vector2.zero, 2f, 1f, .4f, 5f, Color.magenta);

                pool.Tick(.1f, Vector2.zero, _ => damageEvents++);
                pool.Tick(.1f, Vector2.zero, _ => damageEvents++);
                Assert.That(damageEvents, Is.EqualTo(1));

                pool.Tick(.25f, Vector2.zero, _ => damageEvents++);
                Assert.That(damageEvents, Is.EqualTo(2));
                pool.Tick(.6f, Vector2.zero, _ => damageEvents++);
                Assert.That(pool.ActiveCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearingPoolsRemovesAllActiveStageAttacks()
        {
            var root = new GameObject("Stage Pool Reset Test");
            try
            {
                var projectiles = root.AddComponent<EnemyProjectilePool>();
                var hazards = root.AddComponent<StageHazardPool>();
                projectiles.Configure(3, null);
                hazards.Configure(3, null);
                projectiles.Launch(Vector2.zero, Vector2.one, 9f, 1f, Color.white);
                hazards.Activate(Vector2.zero, 1f, 9f, 1f, 1f, Color.white);

                projectiles.Clear();
                hazards.Clear();

                Assert.That(projectiles.ActiveCount, Is.Zero);
                Assert.That(hazards.ActiveCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
