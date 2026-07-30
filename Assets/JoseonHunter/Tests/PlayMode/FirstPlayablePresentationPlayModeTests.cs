using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class FirstPlayablePresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ClosureAnimationReturnsAllTemporarySpritesToPool()
        {
            var presenter = CreatePresenter();
            try
            {
                presenter.PlayClosure(UnitSquare());
                yield return new WaitForSeconds(.8f);
                Assert.That(presenter.ActiveClosureVisualCountForTests, Is.Zero);
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

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var previousPresenter = Object.FindFirstObjectByType<GeumjulTrailPresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(previousPresenter, Is.Not.Null);
            var previousMaterial = previousPresenter.CachedMaterialForTests;
            Assert.That(previousMaterial, Is.Not.Null);

            controller.ResetRunForTests();
            controller.ResetRunForTests();
            yield return null;

            Assert.That(previousPresenter == null, Is.True);
            Assert.That(previousMaterial == null, Is.True);
            Assert.That(Object.FindObjectsByType<GeumjulTrailPresenter>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ResetRunLoadsResourcesVisualLibraryWhenSerializedAssignmentIsMissing()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
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
