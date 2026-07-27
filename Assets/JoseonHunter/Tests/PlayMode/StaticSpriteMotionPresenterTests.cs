using System.Collections;
using JoseonHunter.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class StaticSpriteMotionPresenterTests
    {
        [UnityTest]
        public IEnumerator InitialFramesPreserveTheOriginalColorWithoutShowingHit()
        {
            var gameObject = CreatePresenter(out var renderer, out _);
            var originalColor = renderer.color;

            yield return null;
            yield return null;

            Assert.That(renderer.color, Is.EqualTo(originalColor));

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator SetVelocityUpdatesFacingAndMovingOrIdleOffset()
        {
            var gameObject = CreatePresenter(out var renderer, out var presenter);
            var originalPosition = gameObject.transform.localPosition;

            presenter.SetVelocity(Vector2.right);
            yield return null;
            Assert.That(renderer.flipX, Is.False);
            Assert.That(gameObject.transform.localPosition.y, Is.Not.EqualTo(originalPosition.y).Within(0.0001f));

            presenter.SetVelocity(Vector2.left);
            yield return null;
            Assert.That(renderer.flipX, Is.True);

            presenter.SetVelocity(Vector2.zero);
            yield return null;
            Assert.That(renderer.flipX, Is.True);
            Assert.That(gameObject.transform.localPosition.x, Is.EqualTo(originalPosition.x));
            Assert.That(gameObject.transform.localPosition.y, Is.EqualTo(originalPosition.y).Within(0.0001f));
            Assert.That(gameObject.transform.localPosition.z, Is.EqualTo(originalPosition.z));

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator ShowHitRestoresTheOriginalColorAfterItsDuration()
        {
            var gameObject = CreatePresenter(out var renderer, out var presenter);
            var originalColor = renderer.color;

            presenter.ShowHit();
            Assert.That(renderer.color, Is.EqualTo(Color.white));

            yield return new WaitForSeconds(0.1f);
            Assert.That(renderer.color, Is.EqualTo(originalColor));

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator PlayDeathFadesWithoutDeactivatingTheGameObject()
        {
            var gameObject = CreatePresenter(out var renderer, out var presenter);

            presenter.PlayDeath();
            yield return new WaitForSeconds(0.4f);

            Assert.That(renderer.color.a, Is.Zero.Within(0.0001f));
            Assert.That(gameObject.activeSelf, Is.True);

            Object.Destroy(gameObject);
        }

        private static GameObject CreatePresenter(out SpriteRenderer renderer, out StaticSpriteMotionPresenter presenter)
        {
            var gameObject = new GameObject("StaticSpriteMotionPresenterTests");
            renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = Color.green;
            presenter = gameObject.AddComponent<StaticSpriteMotionPresenter>();
            return gameObject;
        }
    }
}
