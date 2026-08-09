using System;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    public static class FirstPlayableSceneGenerator
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const string BattlefieldPresentationPath =
            "Assets/JoseonHunter/Resources/Presentation/BattlefieldPresentationLibrary.asset";

        [MenuItem("JoseonHunter/Gameplay Editing/Open Authored Gameplay Scene")]
        public static void OpenAuthoredGameplayScene()
        {
            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(GameplayScenePath);
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        [MenuItem("JoseonHunter/Gameplay Editing/Create or Validate Authored Gameplay Scene")]
        public static void CreateOrValidateAuthoredGameplayScene() => Generate();

        [MenuItem("JoseonHunter/Setup/Generate First Playable")]
        public static void Generate()
        {
            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(GameplayScenePath);
            var library = RequireVisualLibrary();
            var scene = SceneManager.GetSceneByPath(GameplayScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

            try
            {
                GenerateScene(scene, library);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, GameplayScenePath))
                    throw new InvalidOperationException($"Unity failed to save {GameplayScenePath}.");
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = FindSingleRootOrNull(scene, "FirstPlayable");
                Debug.Log("JoseonHunter authored Gameplay scene is valid.");
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void GenerateScene(Scene scene, GameplayVisualPrefabLibrary library)
        {
            ValidateExistingControllerSceneComposition(scene);

            var cameraRoot = FindSingleRootOrNull(scene, "Main Camera");
            var createdCameraRoot = cameraRoot == null;
            if (createdCameraRoot) cameraRoot = RequireOrCreateRoot(scene, "Main Camera");
            var camera = cameraRoot.GetComponent<Camera>();
            var createdCameraComponent = camera == null;
            if (createdCameraComponent) camera = RequireOrAdd<Camera>(cameraRoot);
            if (cameraRoot.tag != "MainCamera") cameraRoot.tag = "MainCamera";
            if (createdCameraRoot || createdCameraComponent) ConfigureNewCamera(camera);

            var controllerRoot = RequireOrCreateRoot(scene, "FirstPlayable");
            var controller = RequireOrAdd<FirstPlayableController>(controllerRoot);
            RequireOrAdd<GameFlowCoordinator>(controllerRoot);
            var composition = RequireOrAdd<GameplaySceneComposition>(controllerRoot);

            var field = RequireOrCreateChild(controllerRoot.transform, "FlatField");
            var authoringPreview = RequireOrCreateChild(field, "Authoring Preview");
            if (authoringPreview.tag != "EditorOnly") authoringPreview.tag = "EditorOnly";
            var runtimeBattlefield = RequireOrCreateChild(field, "Runtime Battlefield");
            var host = RequireOrAdd<GameplayBattlefieldHost>(field.gameObject);
            host.ConfigureAuthoringRoots(runtimeBattlefield, authoringPreview.gameObject);
            EnsureAuthoringPreview(authoringPreview);

            var runtimeObjects = RequireOrCreateChild(controllerRoot.transform, "RuntimeObjects");
            var runtimeSystems = RequireOrCreateChild(controllerRoot.transform, "RuntimeSystems");
            var spawnGuides = RequireOrCreateChild(controllerRoot.transform, "Spawn Guides");
            var spawnGuide = RequireOrAdd<GameplaySpawnGuide>(spawnGuides.gameObject);
            spawnGuide.Configure(camera, 1f, 3f);

            var player = RequireOrCreateConnectedPlayer(runtimeObjects, library.PlayerVisual);
            var playerView = player.GetComponent<CombatantVisualView>();
            if (playerView == null || playerView.HealthBarAnchor == null)
                throw new InvalidOperationException("Authored PlayerVisual is missing CombatantVisualView.HealthBarAnchor.");
            RequireOrCreateConnectedHealthBar(playerView.HealthBarAnchor, library.WorldHealthBar);

            var uiRoot = RequireOrCreateRoot(scene, "First Playable UI");
            RequireSingleUiBootstrap(scene, uiRoot);
            var eventSystemRoot = RequireOrCreateRoot(scene, "EventSystem");
            RequireSingleEventSystem(scene, eventSystemRoot);

            ConfigureControllerAssets(controller, library);
            composition.Configure(camera, field, runtimeObjects, runtimeSystems, spawnGuides, playerView, uiRoot);
            ConfigureControllerSceneComposition(controller, composition);
        }

        public static void GenerateInBatchMode()
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

        private static GameplayVisualPrefabLibrary RequireVisualLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<GameplayVisualPrefabLibrary>(
                GameplayVisualPrefabBuilder.LibraryAssetPath);
            if (library == null || !library.IsComplete)
                throw new InvalidOperationException(
                    "Gameplay visual library is missing or incomplete. Run Create or Validate Visual Prefabs first.");
            return library;
        }

        private static GameObject RequireOrCreateRoot(Scene scene, string name)
        {
            var existing = FindSingleRootOrNull(scene, name);
            if (existing != null) return existing;
            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static GameObject FindSingleRootOrNull(Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(root => root.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"{GameplayScenePath} contains duplicate root '{name}'.");
            return matches.SingleOrDefault();
        }

        private static Transform RequireOrCreateChild(Transform parent, string name)
        {
            var matches = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"{parent.name} contains duplicate child '{name}'.");
            if (matches.Length == 1) return matches[0];
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static T RequireOrAdd<T>(GameObject gameObject) where T : Component
        {
            var matches = gameObject.GetComponents<T>();
            if (matches.Length > 1)
                throw new InvalidOperationException($"{gameObject.name} contains duplicate {typeof(T).Name} components.");
            return matches.Length == 1 ? matches[0] : gameObject.AddComponent<T>();
        }

        private static void ConfigureNewCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = CombatVisualScaleProfile.MobilePortrait.CameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .07f, .08f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureAuthoringPreview(Transform previewRoot)
        {
            var presentation = AssetDatabase.LoadAssetAtPath<BattlefieldPresentationLibrary>(BattlefieldPresentationPath);
            if (presentation == null || presentation.ChunkPrefab == null || presentation.GroundTile == null)
                throw new InvalidOperationException("Battlefield presentation library is missing its chunk prefab or ground tile.");
            var chunks = previewRoot.GetComponentsInChildren<BattlefieldChunkView>(true);
            if (chunks.Length != 0 && chunks.Length != BattlefieldChunkLayout.ActiveChunkCount)
                throw new InvalidOperationException("Authoring Preview must contain exactly nine BattlefieldChunkView instances.");
            if (chunks.Length == 0)
            {
                chunks = new BattlefieldChunkView[BattlefieldChunkLayout.ActiveChunkCount];
                var index = 0;
                for (var y = -1; y <= 1; y++)
                for (var x = -1; x <= 1; x++)
                {
                    var chunk = PrefabUtility.InstantiatePrefab(
                        presentation.ChunkPrefab.gameObject,
                        previewRoot.gameObject.scene) as GameObject;
                    if (chunk == null) throw new InvalidOperationException("Unity could not instantiate the battlefield chunk preview.");
                    chunk.transform.SetParent(previewRoot, false);
                    chunks[index++] = chunk.GetComponent<BattlefieldChunkView>();
                }
            }

            if (chunks.Any(chunk => chunk == null ||
                                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(chunk.gameObject) !=
                                    presentation.ChunkPrefab.gameObject))
                throw new InvalidOperationException("Authoring Preview must use connected production BattlefieldChunkView instances.");

            var coordinateIndex = 0;
            for (var y = -1; y <= 1; y++)
            for (var x = -1; x <= 1; x++)
            {
                chunks[coordinateIndex++].Assign(
                    new Vector2Int(x, y),
                    presentation.GroundTile,
                    presentation.AlternateGroundTile,
                    presentation.Decorations,
                    presentation.GroundTile,
                    0x4A4F5345);
            }
        }

        private static GameObject RequireOrCreateConnectedPlayer(Transform parent, GameObject prefab)
        {
            var matches = parent.Cast<Transform>().Where(child => child.name == "Han Yeonhwa").ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("RuntimeObjects contains duplicate Han Yeonhwa objects.");
            if (matches.Length == 1)
            {
                RequireExpectedPrefab(matches[0].gameObject, prefab, "Han Yeonhwa");
                return matches[0].gameObject;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) throw new InvalidOperationException("Unity could not instantiate PlayerVisual.prefab.");
            instance.name = "Han Yeonhwa";
            return instance;
        }

        private static void RequireOrCreateConnectedHealthBar(Transform anchor, GameObject prefab)
        {
            var allBars = anchor.GetComponentsInChildren<WorldBarView>(true);
            if (allBars.Length > 1)
                throw new InvalidOperationException("Han Yeonhwa has duplicate or nested WorldHealthBar instances.");
            if (allBars.Length == 1)
            {
                var existing = allBars[0];
                if (existing.transform.parent != anchor ||
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(existing.gameObject) != prefab)
                    throw new InvalidOperationException("Han Yeonhwa WorldHealthBar must be a direct connected child of HealthBarAnchor.");
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab, anchor) as GameObject;
            if (instance == null) throw new InvalidOperationException("Unity could not instantiate WorldHealthBar.prefab.");
            instance.name = "Health Bar";
        }

        private static void RequireExpectedPrefab(GameObject instance, GameObject expectedPrefab, string objectName)
        {
            if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance) != expectedPrefab)
                throw new InvalidOperationException($"{objectName} must remain a connected instance of {AssetDatabase.GetAssetPath(expectedPrefab)}.");
        }

        private static void RequireSingleUiBootstrap(Scene scene, GameObject root)
        {
            var bootstraps = scene.GetRootGameObjects().SelectMany(candidate =>
                candidate.GetComponentsInChildren<FirstPlayableUiBootstrap>(true)).ToArray();
            if (bootstraps.Length > 1 || (bootstraps.Length == 1 && bootstraps[0].gameObject != root))
                throw new InvalidOperationException("Gameplay scene contains an unexpected FirstPlayableUiBootstrap.");
            if (bootstraps.Length == 0) root.AddComponent<FirstPlayableUiBootstrap>();
        }

        private static void RequireSingleEventSystem(Scene scene, GameObject root)
        {
            var eventSystems = scene.GetRootGameObjects().SelectMany(candidate =>
                candidate.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if (eventSystems.Length > 1 || (eventSystems.Length == 1 && eventSystems[0].gameObject != root))
                throw new InvalidOperationException("Gameplay scene contains an unexpected EventSystem.");
            if (eventSystems.Length == 0) root.AddComponent<EventSystem>();
            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType == null) throw new InvalidOperationException("InputSystemUIInputModule is unavailable.");
            var modules = root.GetComponents<BaseInputModule>();
            if (modules.Length == 0)
            {
                root.AddComponent(inputModuleType);
                return;
            }
            if (modules.Length != 1 || modules[0].GetType() != inputModuleType)
                throw new InvalidOperationException("EventSystem requires exactly one InputSystemUIInputModule and no other input module.");
        }

        private static void ConfigureControllerAssets(FirstPlayableController controller, GameplayVisualPrefabLibrary library)
        {
            var serialized = new SerializedObject(controller);
            AssignSprite(serialized, "playerSprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png");
            AssignSprite(serialized, "enemySprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png");
            AssignSprite(serialized, "enemySpriteAlt", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png");
            AssignSprite(serialized, "eliteSprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites/dokkaebi_captain.png");
            AssignSprite(serialized, "bossSprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png");
            AssignSprite(serialized, "experienceSprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/experience_spirit_flame.png");
            AssignSprite(serialized, "coinSprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png");
            AssignSprite(serialized, "treasureChestSprite", "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/treasure_chest.png");
            AssignSprite(serialized, "battlefieldTilePrimary", "Assets/JoseonHunter/Art/World/Runtime/Battlefield/occult_battlefield.png");
            AssignSpritesIfEmpty(serialized, "enemySprites", new[]
            {
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/dokkaebi.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/sakkat_specter.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/vengeful_spirit.png"
            });
            AssignSpritesIfEmpty(serialized, "battlefieldDecals", new[]
            {
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/ward_paper_scraps.png",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/shrine_roof_fragment.png",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/dry_reed_clump.png",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/ritual_stone.png"
            });
            var geumjulVisuals = serialized.FindProperty("jangseungGeumjulVisuals");
            if (geumjulVisuals.objectReferenceValue == null)
                geumjulVisuals.objectReferenceValue = AssetDatabase.LoadAssetAtPath<JangseungGeumjulVisualLibrary>(
                    "Assets/JoseonHunter/Resources/Presentation/JangseungGeumjulVisualLibrary.asset");
            var catalog = serialized.FindProperty("weaponCatalog");
            if (catalog.objectReferenceValue == null)
                catalog.objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>("Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            var motion = serialized.FindProperty("motionLibrary");
            if (motion.objectReferenceValue == null) motion.objectReferenceValue = CombatMotionLibraryBuilder.Build();
            var visuals = serialized.FindProperty("gameplayVisualPrefabs");
            if (visuals.objectReferenceValue != null && visuals.objectReferenceValue != library)
                throw new InvalidOperationException("Gameplay controller references a different visual prefab library.");
            visuals.objectReferenceValue = library;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureControllerSceneComposition(
            FirstPlayableController controller,
            GameplaySceneComposition composition)
        {
            var serialized = new SerializedObject(controller);
            var sceneComposition = serialized.FindProperty("sceneComposition");
            if (sceneComposition.objectReferenceValue != null && sceneComposition.objectReferenceValue != composition)
                throw new InvalidOperationException(
                    "Gameplay controller references a different scene composition.");
            if (sceneComposition.objectReferenceValue == null)
            {
                sceneComposition.objectReferenceValue = composition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ValidateExistingControllerSceneComposition(Scene scene)
        {
            var controllerRoot = FindSingleRootOrNull(scene, "FirstPlayable");
            if (controllerRoot == null) return;
            var controller = controllerRoot.GetComponent<FirstPlayableController>();
            var composition = controllerRoot.GetComponent<GameplaySceneComposition>();
            if (controller == null) return;

            var serialized = new SerializedObject(controller);
            var sceneComposition = serialized.FindProperty("sceneComposition");
            if (sceneComposition.objectReferenceValue != null && sceneComposition.objectReferenceValue != composition)
                throw new InvalidOperationException(
                    "Gameplay controller references a different scene composition.");
        }

        private static void AssignSprite(SerializedObject serialized, string propertyName, string path)
        {
            var property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignSpritesIfEmpty(SerializedObject serialized, string propertyName, string[] paths)
        {
            var property = serialized.FindProperty(propertyName);
            if (property.arraySize != 0 && Enumerable.Range(0, property.arraySize)
                    .Any(index => property.GetArrayElementAtIndex(index).objectReferenceValue != null))
                return;
            property.arraySize = paths.Length;
            for (var index = 0; index < paths.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(paths[index]);
        }
    }
}
