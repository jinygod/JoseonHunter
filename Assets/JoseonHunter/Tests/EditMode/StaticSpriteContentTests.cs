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

        private static readonly ExpectedEntry[] ExpectedEntries =
        {
            new("rookie_constable", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/rookie_constable.png"),
            new("shaman", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/shaman.png"),
            new("mountain_hunter", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/mountain_hunter.png"),
            new("plague_rat", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png"),
            new("vengeful_spirit", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/vengeful_spirit.png"),
            new("sakkat_specter", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/sakkat_specter.png"),
            new("dokkaebi", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/dokkaebi.png"),
            new("bandit", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png"),
            new("fallen_general", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png"),
            new("coin", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png"),
            new("experience_spirit_flame", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/experience_spirit_flame.png"),
            new("treasure_chest", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/treasure_chest.png")
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
                Assert.That(entry.prefab.GetComponent<SpriteRenderer>().sprite, Is.SameAs(entry.sprite), entry.id);
            }
        }

        [Test]
        public void CatalogEntriesUseTheExactApprovedRuntimeSpriteAndPrefabPaths()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StaticSpriteCatalog>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            foreach (var expected in ExpectedEntries)
            {
                Assert.That(catalog.TryGet(expected.Id, out var entry), Is.True, expected.Id);
                Assert.That(AssetDatabase.GetAssetPath(entry.sprite), Is.EqualTo(expected.SpritePath), expected.Id);
                Assert.That(
                    AssetDatabase.GetAssetPath(entry.prefab),
                    Is.EqualTo("Assets/JoseonHunter/Prefabs/StaticSprites/" + expected.Id + ".prefab"),
                    expected.Id);
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
                var expectedPositions = new[]
                {
                    new Vector3(-4.5f, 3f, 0f), new Vector3(-1.5f, 3f, 0f), new Vector3(1.5f, 3f, 0f), new Vector3(4.5f, 3f, 0f),
                    new Vector3(-4.5f, 0f, 0f), new Vector3(-1.5f, 0f, 0f), new Vector3(1.5f, 0f, 0f), new Vector3(4.5f, 0f, 0f),
                    new Vector3(-4.5f, -3f, 0f), new Vector3(-1.5f, -3f, 0f), new Vector3(1.5f, -3f, 0f), new Vector3(4.5f, -3f, 0f)
                };
                CollectionAssert.AreEqual(expectedPositions, proof.Cast<Transform>().Select(child => child.localPosition));
                Assert.That(proof.Cast<Transform>().Select(child => child.localPosition).Distinct().Count(), Is.EqualTo(ExpectedIds.Length));
                var catalog = AssetDatabase.LoadAssetAtPath<StaticSpriteCatalog>(CatalogPath);
                foreach (var child in proof.Cast<Transform>())
                {
                    Assert.That(catalog.TryGet(child.name, out var entry), Is.True, child.name);
                    Assert.That(
                        PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject),
                        Is.SameAs(entry.prefab),
                        child.name);
                }
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

        private readonly struct ExpectedEntry
        {
            public ExpectedEntry(string id, string spritePath)
            {
                Id = id;
                SpritePath = spritePath;
            }

            public string Id { get; }
            public string SpritePath { get; }
        }
    }
}
