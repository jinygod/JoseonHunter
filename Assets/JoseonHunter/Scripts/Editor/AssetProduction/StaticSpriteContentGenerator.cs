using System;
using System.Collections.Generic;
using JoseonHunter.Content;
using JoseonHunter.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class StaticSpriteContentGenerator
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const string CatalogPath = "Assets/JoseonHunter/Content/StaticSpriteCatalog.asset";
        private const string PrefabFolder = "Assets/JoseonHunter/Prefabs/StaticSprites";
        private const int ProofColumns = 4;
        private const float ProofSpacing = 3f;

        private static readonly ContentDefinition[] Definitions =
        {
            new("rookie_constable", "Heroes/rookie_constable.png"),
            new("shaman", "Heroes/shaman.png"),
            new("mountain_hunter", "Heroes/mountain_hunter.png"),
            new("plague_rat", "Enemies/plague_rat.png"),
            new("vengeful_spirit", "Enemies/vengeful_spirit.png"),
            new("sakkat_specter", "Enemies/sakkat_specter.png"),
            new("dokkaebi", "Enemies/dokkaebi.png"),
            new("bandit", "Enemies/bandit.png"),
            new("fallen_general", "Bosses/fallen_general.png"),
            new("coin", "Pickups/coin.png"),
            new("experience_spirit_flame", "Pickups/experience_spirit_flame.png"),
            new("treasure_chest", "Pickups/treasure_chest.png")
        };

        [MenuItem("JoseonHunter/Assets/Generate Static Launch Content")]
        public static void Generate()
        {
            RefuseDirtyGameplayScene();
            EnsureFolder("Assets/JoseonHunter/Content");
            EnsureFolder("Assets/JoseonHunter/Prefabs");
            EnsureFolder(PrefabFolder);

            var entries = new List<StaticSpriteCatalog.Entry>(Definitions.Length);
            foreach (var definition in Definitions)
            {
                var sprite = LoadSprite(definition);
                var prefab = CreatePrefab(definition, sprite);
                entries.Add(new StaticSpriteCatalog.Entry
                {
                    id = definition.Id,
                    sprite = sprite,
                    prefab = prefab
                });
            }

            var catalog = LoadOrCreateCatalog();
            WriteEntries(catalog, entries);
            CreateProofLineup(entries);
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated static launch content: {entries.Count} catalog entries, prefabs, and Gameplay proof lineup.");
        }

        public static void GenerateFromCommandLine()
        {
            try
            {
                Generate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RefuseDirtyGameplayScene()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && scene.path == GameplayScenePath && scene.isDirty)
                {
                    throw new InvalidOperationException($"Cannot replace dirty open scene: {GameplayScenePath}");
                }
            }
        }

        private static Sprite LoadSprite(ContentDefinition definition)
        {
            var path = "Assets/JoseonHunter/Art/StaticSprites/Runtime/" + definition.RuntimePath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Missing static runtime sprite: {path}");
            }

            return sprite;
        }

        private static GameObject CreatePrefab(ContentDefinition definition, Sprite sprite)
        {
            var prefabPath = PrefabFolder + "/" + definition.Id + ".prefab";
            var instance = new GameObject(definition.Id);
            try
            {
                instance.AddComponent<SpriteRenderer>().sprite = sprite;
                instance.AddComponent<StaticSpriteMotionPresenter>();
                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static StaticSpriteCatalog LoadOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StaticSpriteCatalog>(CatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<StaticSpriteCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static void WriteEntries(StaticSpriteCatalog catalog, IReadOnlyList<StaticSpriteCatalog.Entry> entries)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var serializedEntries = serializedCatalog.FindProperty("entries");
            serializedEntries.arraySize = entries.Count;
            for (var index = 0; index < entries.Count; index++)
            {
                var serializedEntry = serializedEntries.GetArrayElementAtIndex(index);
                serializedEntry.FindPropertyRelative("id").stringValue = entries[index].id;
                serializedEntry.FindPropertyRelative("sprite").objectReferenceValue = entries[index].sprite;
                serializedEntry.FindPropertyRelative("prefab").objectReferenceValue = entries[index].prefab;
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void CreateProofLineup(IReadOnlyList<StaticSpriteCatalog.Entry> entries)
        {
            var wasOpen = false;
            Scene gameplayScene = default;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var candidate = SceneManager.GetSceneAt(index);
                if (candidate.isLoaded && candidate.path == GameplayScenePath)
                {
                    gameplayScene = candidate;
                    wasOpen = true;
                    break;
                }
            }

            if (!wasOpen)
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var world = FindWorld(gameplayScene);
                var existingProof = world.Find("StaticSpriteLaunchProof");
                if (existingProof != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingProof.gameObject);
                }

                var proof = new GameObject("StaticSpriteLaunchProof");
                proof.transform.SetParent(world, false);
                proof.SetActive(false);
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, gameplayScene);
                    instance.name = entry.id;
                    instance.transform.SetParent(proof.transform, false);
                    instance.transform.localPosition = ProofPosition(index, entries.Count);
                }

                EditorSceneManager.MarkSceneDirty(gameplayScene);
                EditorSceneManager.SaveScene(gameplayScene);
            }
            finally
            {
                if (!wasOpen && gameplayScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(gameplayScene, true);
                }
            }
        }

        private static Vector3 ProofPosition(int index, int total)
        {
            var columns = Mathf.Min(ProofColumns, total); var rows = Mathf.CeilToInt(total / (float)columns);
            return new Vector3((index % columns - (columns - 1) * 0.5f) * ProofSpacing, ((rows - 1) * 0.5f - index / columns) * ProofSpacing, 0f);
        }


        private static Transform FindWorld(Scene gameplayScene)
        {
            foreach (var root in gameplayScene.GetRootGameObjects())
            {
                if (root.name == "SceneRoot")
                {
                    var world = root.transform.Find("World");
                    if (world != null)
                    {
                        return world;
                    }
                }
            }

            throw new InvalidOperationException("Gameplay scene is missing SceneRoot/World.");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Invalid asset folder: {folder}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct ContentDefinition
        {
            public ContentDefinition(string id, string runtimePath)
            {
                Id = id;
                RuntimePath = runtimePath;
            }

            public string Id { get; }
            public string RuntimePath { get; }
        }
    }
}
