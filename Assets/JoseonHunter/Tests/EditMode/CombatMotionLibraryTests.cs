using System.Collections.Generic;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatMotionLibraryTests
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";

        [Test]
        public void GameplayControllerActiveCombatBaseSpritesResolveToCompletePointFiltered64PpuMotionSets()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var controller = FindController(scene);
                var serialized = new SerializedObject(controller);
                var library = serialized.FindProperty("motionLibrary").objectReferenceValue as CombatMotionLibrary;
                Assert.That(library, Is.Not.Null, "Gameplay FirstPlayableController must consume the checked-in motion library.");

                var baseSprites = ActiveCombatBaseSprites(serialized);
                Assert.That(baseSprites, Is.Not.Empty);
                foreach (var baseSprite in baseSprites)
                {
                    Assert.That(baseSprite, Is.Not.Null);
                    var motion = library.Find(baseSprite);
                    Assert.That(motion, Is.Not.Null, $"{baseSprite.name} must resolve through the controller-consumed motion library.");
                    Assert.That(motion.IdleFrames, Is.Not.Empty, $"{baseSprite.name} requires an idle sequence.");
                    Assert.That(motion.MoveFrames, Is.Not.Empty, $"{baseSprite.name} requires a move sequence.");
                    AssertFramesUseMobilePixelImport(motion.IdleFrames, baseSprite.name + " idle");
                    AssertFramesUseMobilePixelImport(motion.MoveFrames, baseSprite.name + " move");
                }

                var han = serialized.FindProperty("playerSprite").objectReferenceValue as Sprite;
                var hanMotion = library.Find(han);
                Assert.That(hanMotion.IdleFrames.Count, Is.EqualTo(4));
                Assert.That(hanMotion.MoveFrames.Count, Is.EqualTo(8));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

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

        private static FirstPlayableController FindController(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<FirstPlayableController>(true);
                if (controller != null) return controller;
            }

            Assert.Fail("Gameplay scene is missing FirstPlayableController.");
            return null;
        }

        private static List<Sprite> ActiveCombatBaseSprites(SerializedObject controller)
        {
            var result = new List<Sprite>();
            Add(controller.FindProperty("playerSprite"), result);
            Add(controller.FindProperty("enemySprite"), result);
            Add(controller.FindProperty("enemySpriteAlt"), result);
            var normalEnemies = controller.FindProperty("enemySprites");
            for (var index = 0; index < normalEnemies.arraySize; index++) Add(normalEnemies.GetArrayElementAtIndex(index), result);
            Add(controller.FindProperty("eliteSprite"), result);
            Add(controller.FindProperty("bossSprite"), result);
            return result;
        }

        private static void Add(SerializedProperty property, ICollection<Sprite> sprites)
        {
            var sprite = property.objectReferenceValue as Sprite;
            Assert.That(sprite, Is.Not.Null, $"Gameplay controller field '{property.propertyPath}' must be assigned.");
            if (!sprites.Contains(sprite)) sprites.Add(sprite);
        }

        private static void AssertFramesUseMobilePixelImport(IReadOnlyList<Sprite> frames, string description)
        {
            foreach (var frame in frames)
            {
                Assert.That(frame, Is.Not.Null, description + " contains a null frame.");
                Assert.That(frame.pixelsPerUnit, Is.EqualTo(64f), description + " must use exactly 64 PPU.");
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frame)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, description + " must be imported as a texture.");
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), description + " must use Point filtering.");
            }
        }
    }
}
