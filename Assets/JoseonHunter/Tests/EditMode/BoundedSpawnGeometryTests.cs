using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class BoundedSpawnGeometryTests
    {
        [TestCase(-15f, 0f, 0.05f)]
        [TestCase(15f, 0f, 0.30f)]
        [TestCase(0f, -35f, 0.55f)]
        [TestCase(0f, 35f, 0.80f)]
        public void FindsInsideFieldOutsideViewportWhenPlayerHugsCameraSafeEdge(
            float playerX, float playerY, float t)
        {
            var cameraObject = new GameObject("Bounded Spawn Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 18f;
            camera.aspect = 9f / 16f;
            camera.transform.position = new Vector3(playerX, playerY, -10f);
            var bounds = Rect.MinMaxRect(-36f, -56f, 36f, 56f);

            try
            {
                Assert.That(BoundedSpawnGeometry.TrySelect(
                    new Vector2(playerX, playerY), bounds, camera, t, out var position), Is.True);
                Assert.That(bounds.Contains(position), Is.True);
                Assert.That(ViewportBounds(camera).Contains(position), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ReturnsFalseWhenNoOffscreenAreaExistsInsideBounds()
        {
            var cameraObject = new GameObject("Oversized Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 20f;
            camera.aspect = 1f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            try
            {
                Assert.That(BoundedSpawnGeometry.TrySelect(
                    Vector2.zero, Rect.MinMaxRect(-5f, -5f, 5f, 5f), camera, .5f, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static Rect ViewportBounds(Camera camera)
        {
            var bottomLeft = camera.ViewportToWorldPoint(Vector3.zero);
            var topRight = camera.ViewportToWorldPoint(Vector3.one);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }
    }
}
