using System.Linq;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class FlatWardVisualTests
    {
        [Test]
        public void WardSparkBurstUsesOnlyMutedOchreWithoutWhiteContours()
        {
            var root = new GameObject("Flat ward spark root");
            var material = new Material(Shader.Find("Sprites/Default"));
            var pool = new FlatWardSparkPool(root.transform, material, 4, 8);
            try
            {
                pool.PlayBurst(Vector2.zero, 8, .3f);

                Assert.That(pool.ActiveCountForTests, Is.EqualTo(8));
                Assert.That(pool.CreatedCountForTests, Is.EqualTo(8));
                Assert.That(pool.UsesOnlyApprovedColorsForTests, Is.True);
                Assert.That(pool.HasWhiteContourForTests, Is.False);
                Assert.That(
                    root.GetComponentsInChildren<MeshRenderer>()
                        .Max(renderer => Vector2.Distance(Vector2.zero, renderer.transform.position)),
                    Is.GreaterThan(.1f));
            }
            finally
            {
                pool.Dispose();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WardSparkPoolReusesExistingDiamondsAfterTheyExpire()
        {
            var root = new GameObject("Flat ward spark reuse root");
            var material = new Material(Shader.Find("Sprites/Default"));
            var pool = new FlatWardSparkPool(root.transform, material, 4, 8);
            try
            {
                pool.PlayBurst(Vector2.zero, 8, .3f);
                pool.Tick(1f);
                pool.PlayBurst(Vector2.one, 3, .2f);

                Assert.That(pool.ActiveCountForTests, Is.EqualTo(3));
                Assert.That(pool.CreatedCountForTests, Is.EqualTo(8));
            }
            finally
            {
                pool.Dispose();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(root);
            }
        }
    }
}
