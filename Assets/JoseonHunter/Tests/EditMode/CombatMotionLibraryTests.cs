using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatMotionLibraryTests
    {
        [Test]
        public void Find_ReturnsConfiguredSetByReferenceSprite()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f, 64f);
            var set = new CombatMotionSet();
            set.Configure("hero", sprite, new[] { sprite }, new[] { sprite }, 3f, 8f, MotionWeight.Light);
            var library = ScriptableObject.CreateInstance<CombatMotionLibrary>();
            library.Configure(new[] { set });

            Assert.That(library.Find(sprite), Is.SameAs(set));

            Object.DestroyImmediate(library);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void EmptyFrames_FallBackToReferenceSprite()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f, 64f);
            var set = new CombatMotionSet();
            set.Configure("fallback", sprite, null, null, 3f, 8f, MotionWeight.Medium);

            Assert.That(set.Frame(false, 200), Is.SameAs(sprite));
            Assert.That(set.Frame(true, -1), Is.SameAs(sprite));

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }
}
