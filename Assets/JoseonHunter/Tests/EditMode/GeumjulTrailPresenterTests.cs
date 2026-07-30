using System.Collections.Generic;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GeumjulTrailPresenterTests
    {
        [Test]
        public void PresenterCapsPooledKnotsAndMarksClosureReadiness()
        {
            var owner = new GameObject("Geumjul");
            var presenter = owner.AddComponent<GeumjulTrailPresenter>();
            try
            {
                presenter.Configure(CreateVisualLibrary(), presenter.transform, 4);
                presenter.SetTrail(BuildTrail(90, .14f), .48f);

                Assert.That(presenter.ActiveKnotCountForTests, Is.LessThanOrEqualTo(18));
                Assert.That(presenter.HasAnchorForTests, Is.True);
                Assert.That(presenter.IsClosureReadyForTests, Is.True);
                Assert.That(presenter.AnchorWorldSizeForTests, Is.LessThanOrEqualTo(.42f));
                Assert.That(presenter.LargestActiveKnotWorldSizeForTests, Is.LessThanOrEqualTo(.28f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ClosureScaleIsDerivedFromThePolygonBounds()
        {
            var owner = new GameObject("Geumjul closure");
            var presenter = owner.AddComponent<GeumjulTrailPresenter>();
            try
            {
                presenter.Configure(CreateVisualLibrary(), presenter.transform, 4);
                presenter.PlayClosure(new[]
                {
                    new Vector2(-2f, -1.5f), new Vector2(2f, -1.5f),
                    new Vector2(2f, 1.5f), new Vector2(-2f, 1.5f)
                });

                Assert.That(presenter.ClosureBaseScaleForTests, Is.EqualTo(2.88f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static List<Vector2> BuildTrail(int count, float spacing)
        {
            var trail = new List<Vector2>(count);
            for (var index = 0; index < count - 1; index++) trail.Add(new Vector2(index * spacing, 0f));
            trail.Add(new Vector2(.2f, 0f));
            return trail;
        }

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
