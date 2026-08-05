using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class FirstPlayablePresentationPlayModeTests
    {
        [Test]
        public void GuardianDescentUsesOneCoherentSpriteAndReusesItsPooledVisuals()
        {
            var root = new GameObject("Guardian Descent Test");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, 0f), 2f);
            var presenter = new JangseungGuardianDescentPresenter(root.transform);
            try
            {
                presenter.Play(7, sprite, Vector2.zero, 12);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(1));
                var createdChildren = root.transform.childCount;

                presenter.Tick(.60f);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);

                presenter.Play(8, sprite, Vector2.one, 12);
                Assert.That(root.transform.childCount, Is.EqualTo(createdChildren));
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(1));
            }
            finally
            {
                presenter.Dispose();
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator Paused_flow_freezes_elapsed_enemy_and_camera_follow()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            try
            {
                Assert.That(controller, Is.Not.Null);
                var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
                Assert.That(player, Is.Not.Null);
                player.position = new Vector3(2f, 0f, 0f);
                yield return null;

                var enemy = controller.SpawnEnemyForTests(new Vector2(4f, 0f));
                var beforeCamera = Camera.main.transform.position;
                var beforeElapsed = controller.UiState.Elapsed;
                var beforeEnemy = enemy.WorldPosition;
                Assert.That(controller.Flow.TryTransition(GameFlowState.Paused), Is.True);
                yield return new WaitForSecondsRealtime(.2f);

                Assert.That(controller.UiState.Elapsed, Is.EqualTo(beforeElapsed));
                Assert.That(Camera.main.transform.position, Is.EqualTo(beforeCamera));
                Assert.That(enemy.WorldPosition, Is.EqualTo(beforeEnemy));
            }
            finally
            {
                if (controller != null) controller.Flow?.ResetToPlaying();
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator ClosureAnimationReturnsFillAndSparksToPool()
        {
            var presenter = CreatePresenter();
            try
            {
                presenter.PlayClosure(UnitSquare());
                yield return new WaitForSeconds(.8f);
                Assert.That(presenter.ActiveClosureVisualCountForTests, Is.Zero);
                Assert.That(presenter.ClosureSparkCountForTests, Is.Zero);
                Assert.That(presenter.ClosureMeshVertexCountForTests, Is.Zero);
            }
            finally
            {
                Object.Destroy(presenter.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator ResetRunDestroysThePreviousPresenterAndCachedMaterial()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var previousPresenter = Object.FindAnyObjectByType<GeumjulTrailPresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(previousPresenter, Is.Not.Null);
            var previousMaterial = previousPresenter.CachedMaterialForTests;
            Assert.That(previousMaterial, Is.Not.Null);

            controller.ResetRunForTests();
            controller.ResetRunForTests();
            yield return null;

            Assert.That(previousPresenter == null, Is.True);
            Assert.That(previousMaterial == null, Is.True);
            Assert.That(Object.FindObjectsByType<GeumjulTrailPresenter>(), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ResetRunLoadsResourcesVisualLibraryWhenSerializedAssignmentIsMissing()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var libraryField = typeof(FirstPlayableController)
                .GetField("jangseungGeumjulVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(controller, Is.Not.Null);
            Assert.That(libraryField, Is.Not.Null);

            libraryField.SetValue(controller, null);
            controller.ResetRunForTests();
            yield return null;

            var library = Resources.Load<JangseungGeumjulVisualLibrary>("Presentation/JangseungGeumjulVisualLibrary");
            Assert.That(controller.ResolvedJangseungGeumjulVisualLibraryForTests, Is.SameAs(library));
            Assert.That(controller.WeaponRuntime.JangseungGeumjulVisualLibraryForTests, Is.SameAs(library));
            Assert.That(controller.GeumjulPresenterForTests.ConfiguredVisualLibraryForTests, Is.SameAs(library));
        }

        private static GeumjulTrailPresenter CreatePresenter()
        {
            var owner = new GameObject("Geumjul");
            var presenter = owner.AddComponent<GeumjulTrailPresenter>();
            presenter.Configure(CreateVisualLibrary(), owner.transform, 4);
            return presenter;
        }

        private static IReadOnlyList<Vector2> UnitSquare() => new[]
        {
            new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f),
            new Vector2(.5f, .5f), new Vector2(-.5f, .5f)
        };

        private static JangseungGeumjulVisualLibrary CreateVisualLibrary()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, .5f), 2f);
            var library = ScriptableObject.CreateInstance<JangseungGeumjulVisualLibrary>();
            library.ConfigureForImport(texture, sprite, new[] { sprite },
                new[] { sprite, sprite, sprite, sprite, sprite, sprite }, null, null);
            return library;
        }
    }
}
