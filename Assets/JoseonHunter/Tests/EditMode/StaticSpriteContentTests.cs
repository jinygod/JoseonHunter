using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoseonHunter.Content;
using JoseonHunter.Editor.AssetProduction;
using JoseonHunter.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StaticSpriteContentTests
    {
        private const string CatalogPath = "Assets/JoseonHunter/Content/StaticSpriteCatalog.asset";
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";

        private static readonly string[] ExpectedIds =
        {
            "rookie_constable", "shaman", "mountain_hunter", "plague_rat", "vengeful_spirit", "sakkat_specter",
            "dokkaebi", "bandit", "fallen_general", "coin", "experience_spirit_flame", "treasure_chest"
        };

        [Test]
        public void CatalogContainsExactlyTheApprovedStaticSpriteEntries()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StaticSpriteCatalog>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Entries.Count, Is.EqualTo(ExpectedIds.Length));
            CollectionAssert.AreEquivalent(ExpectedIds, catalog.Entries.Select(entry => entry.id));
            Assert.That(catalog.Entries.Select(entry => entry.id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(ExpectedIds.Length));
            Assert.That(catalog.Entries.All(entry => entry.sprite != null && entry.prefab != null), Is.True);
        }

        [Test]
        public void CatalogEntriesReferenceSingleRendererAndMotionPresenterPrefabs()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StaticSpriteCatalog>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            foreach (var entry in catalog.Entries)
            {
                Assert.That(entry.prefab.GetComponentsInChildren<SpriteRenderer>(true), Has.Length.EqualTo(1), entry.id);
                Assert.That(entry.prefab.GetComponentsInChildren<StaticSpriteMotionPresenter>(true), Has.Length.EqualTo(1), entry.id);
            }
        }

        [Test]
        public void GameplaySceneContainsInactiveStaticSpriteLaunchProofLineup()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var world = scene.GetRootGameObjects().Single(root => root.name == "SceneRoot")
                    .transform.Find("World");
                var proof = world.Find("StaticSpriteLaunchProof");

                Assert.That(proof, Is.Not.Null);
                Assert.That(proof.gameObject.activeSelf, Is.False);
                Assert.That(proof.childCount, Is.EqualTo(ExpectedIds.Length));
                CollectionAssert.AreEquivalent(ExpectedIds, proof.Cast<Transform>().Select(child => child.name));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratorRefusesToOverwriteAnOpenDirtyGameplayScene()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var unsavedChange = new GameObject("UnsavedStaticSpriteProofChange");
                SceneManager.MoveGameObjectToScene(unsavedChange, scene);
                EditorSceneManager.MarkSceneDirty(scene);

                var refusalMethod = typeof(StaticSpriteContentGenerator).GetMethod(
                    "RefuseDirtyGameplayScene",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.That(refusalMethod, Is.Not.Null);
                var exception = Assert.Throws<TargetInvocationException>(
                    () => refusalMethod.Invoke(null, null));
                Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(scene.isDirty, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
