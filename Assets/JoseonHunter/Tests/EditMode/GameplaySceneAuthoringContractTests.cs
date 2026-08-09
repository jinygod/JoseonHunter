using System;
using System.Linq;
using System.Reflection;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameplaySceneAuthoringContractTests
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const string PreviewScenePath = "Assets/JoseonHunter/Scenes/GameplayVisualPreview.unity";

        [Test]
        public void GameplaySceneContainsOneCompleteAuthoredComposition()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var cameraRoot = FindSingleRoot(scene, "Main Camera");
                var firstPlayable = FindSingleRoot(scene, "FirstPlayable");
                var uiRoot = FindSingleRoot(scene, "First Playable UI");
                var eventSystemRoot = FindSingleRoot(scene, "EventSystem");
                Assert.That(cameraRoot, Is.Not.Null);
                var camera = cameraRoot.GetComponent<Camera>();
                Assert.That(camera, Is.Not.Null);
                Assert.That(camera.orthographic, Is.True);
                Assert.That(camera.orthographicSize,
                    Is.EqualTo(CombatVisualScaleProfile.MobilePortrait.CameraOrthographicSize));
                Assert.That(firstPlayable, Is.Not.Null);
                Assert.That(uiRoot, Is.Not.Null);
                Assert.That(eventSystemRoot, Is.Not.Null);

                var composition = firstPlayable.GetComponents<GameplaySceneComposition>().SingleOrDefault();
                Assert.That(composition, Is.Not.Null);
                Assert.That(composition.IsComplete, Is.True);
                Assert.That(firstPlayable.transform.Find("FlatField"), Is.Not.Null);
                Assert.That(firstPlayable.transform.Find("RuntimeObjects"), Is.Not.Null);
                Assert.That(firstPlayable.transform.Find("RuntimeSystems"), Is.Not.Null);
                Assert.That(firstPlayable.transform.Find("Spawn Guides"), Is.Not.Null);

                var preview = firstPlayable.transform.Find("FlatField/Authoring Preview");
                Assert.That(preview, Is.Not.Null);
                var presentation = AssetDatabase.LoadAssetAtPath<BattlefieldPresentationLibrary>(
                    "Assets/JoseonHunter/Resources/Presentation/BattlefieldPresentationLibrary.asset");
                Assert.That(presentation, Is.Not.Null);
                var previewChunks = preview.GetComponentsInChildren<BattlefieldChunkView>(true);
                Assert.That(previewChunks, Has.Length.EqualTo(BattlefieldChunkLayout.ActiveChunkCount));
                foreach (var chunk in previewChunks)
                {
                    Assert.That(PrefabUtility.GetCorrespondingObjectFromOriginalSource(chunk.gameObject),
                        Is.EqualTo(presentation.ChunkPrefab.gameObject));
                    Assert.That(chunk.transform.Find("Ground").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                }
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(-1, -1)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(0, -1)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(1, -1)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(-1, 0)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(0, 0)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(1, 0)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(-1, 1)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(0, 1)),
                        BattlefieldChunkLayout.WorldCenter(new Vector2Int(1, 1))
                    },
                    previewChunks.Select(chunk => chunk.transform.position).ToArray());

                var player = firstPlayable.transform.Find("RuntimeObjects/Han Yeonhwa");
                Assert.That(player, Is.Not.Null);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromOriginalSource(player.gameObject), Is.Not.Null);
                var playerView = player.GetComponent<CombatantVisualView>();
                Assert.That(playerView, Is.Not.Null);
                Assert.That(playerView.HealthBarAnchor, Is.Not.Null);
                var healthBars = playerView.HealthBarAnchor.GetComponentsInChildren<WorldBarView>(true);
                Assert.That(healthBars, Has.Length.EqualTo(1));
                Assert.That(healthBars[0].transform.parent, Is.EqualTo(playerView.HealthBarAnchor));
                Assert.That(firstPlayable.GetComponentsInChildren<GameplayBattlefieldHost>(true), Has.Length.EqualTo(1));
                Assert.That(uiRoot.GetComponents<FirstPlayableUiBootstrap>(), Has.Length.EqualTo(1));
                Assert.That(scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<FirstPlayableUiBootstrap>(true)).ToArray(), Has.Length.EqualTo(1));
                Assert.That(eventSystemRoot.GetComponents<EventSystem>(), Has.Length.EqualTo(1));
                Assert.That(eventSystemRoot.GetComponents<BaseInputModule>(), Has.Length.EqualTo(1));
                var inputSystemModuleType = Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                Assert.That(inputSystemModuleType, Is.Not.Null);
                Assert.That(eventSystemRoot.GetComponents(inputSystemModuleType), Has.Length.EqualTo(1));
                Assert.That(scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<EventSystem>(true)).ToArray(), Has.Length.EqualTo(1));
                Assert.That(firstPlayable.GetComponents<FirstPlayableController>(), Has.Length.EqualTo(1));
                Assert.That(firstPlayable.GetComponents<GameFlowCoordinator>(), Has.Length.EqualTo(1));
                Assert.That(AssetDatabase.LoadAssetAtPath<GameplayVisualPrefabLibrary>(
                    GameplayVisualPrefabBuilder.LibraryAssetPath), Is.Not.Null);
                var controller = firstPlayable.GetComponent<FirstPlayableController>();
                var controllerProperties = new SerializedObject(controller);
                Assert.That(controllerProperties.FindProperty("sceneComposition").objectReferenceValue,
                    Is.EqualTo(composition));
                Assert.That(controllerProperties.FindProperty("enemySprites").arraySize, Is.GreaterThan(0));
                Assert.That(controllerProperties.FindProperty("battlefieldDecals").arraySize, Is.GreaterThan(0));
                Assert.That(controllerProperties.FindProperty("jangseungGeumjulVisuals").objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratorPreservesExistingAuthoringOnRepeatedValidation()
        {
            FirstPlayableSceneGenerator.Generate();
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().Single(root => root.name == "FirstPlayable")
                    .transform.Find("RuntimeObjects/Han Yeonhwa");
                var position = player.localPosition;
                var scale = player.localScale;
                var prefabPath = AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(player.gameObject));
                var dependencyHash = AssetDatabase.GetAssetDependencyHash(prefabPath);

                FirstPlayableSceneGenerator.Generate();

                Assert.That(player, Is.Not.Null);
                Assert.That(player.localPosition, Is.EqualTo(position));
                Assert.That(player.localScale, Is.EqualTo(scale));
                Assert.That(AssetDatabase.GetAssetDependencyHash(prefabPath), Is.EqualTo(dependencyHash));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratorValidationPreservesEveryExistingAuthoredCameraInspectorValue()
        {
            var scene = CreateTemporaryScene();
            try
            {
                var library = RequireVisualLibrary();
                var cameraRoot = CreateInScene(scene, "Main Camera");
                var camera = cameraRoot.AddComponent<Camera>();
                camera.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.Euler(17f, 31f, 43f));
                camera.orthographic = false;
                camera.fieldOfView = 53f;
                camera.orthographicSize = 8.75f;
                camera.clearFlags = CameraClearFlags.Depth;
                camera.backgroundColor = new Color(.13f, .27f, .41f, .59f);

                var expectedPosition = camera.transform.position;
                var expectedRotation = camera.transform.rotation;
                var expectedOrthographic = camera.orthographic;
                var expectedFieldOfView = camera.fieldOfView;
                var expectedOrthographicSize = camera.orthographicSize;
                var expectedClearFlags = camera.clearFlags;
                var expectedBackground = camera.backgroundColor;

                InvokePrivateGenerator("GenerateScene", scene, library);

                Assert.That(camera.transform.position, Is.EqualTo(expectedPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(expectedRotation));
                Assert.That(camera.orthographic, Is.EqualTo(expectedOrthographic));
                Assert.That(camera.fieldOfView, Is.EqualTo(expectedFieldOfView));
                Assert.That(camera.orthographicSize, Is.EqualTo(expectedOrthographicSize));
                Assert.That(camera.clearFlags, Is.EqualTo(expectedClearFlags));
                Assert.That(camera.backgroundColor, Is.EqualTo(expectedBackground));
            }
            finally
            {
                CloseTemporaryScene(scene);
            }
        }

        [Test]
        public void GeneratorRejectsMismatchedCompositionBeforeMutatingTheScene()
        {
            var scene = CreateTemporaryScene();
            try
            {
                var library = RequireVisualLibrary();
                var controllerRoot = CreateInScene(scene, "FirstPlayable");
                var controller = controllerRoot.AddComponent<FirstPlayableController>();
                var expectedComposition = controllerRoot.AddComponent<GameplaySceneComposition>();
                var mismatchedComposition = CreateInScene(scene, "Mismatched Composition")
                    .AddComponent<GameplaySceneComposition>();
                var authoredMarker = CreateInScene(scene, "Authored Marker");
                authoredMarker.transform.SetPositionAndRotation(
                    new Vector3(12f, -4f, 7f),
                    Quaternion.Euler(11f, 22f, 33f));
                var markerPosition = authoredMarker.transform.position;
                var markerRotation = authoredMarker.transform.rotation;
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("sceneComposition").objectReferenceValue = mismatchedComposition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var sceneWasDirty = scene.isDirty;

                AssertPrivateGeneratorFailure(
                    "GenerateScene",
                    "different scene composition",
                    scene,
                    library);

                serialized.Update();
                Assert.That(serialized.FindProperty("sceneComposition").objectReferenceValue,
                    Is.EqualTo(mismatchedComposition));
                Assert.That(controllerRoot.GetComponent<GameplaySceneComposition>(), Is.EqualTo(expectedComposition));
                Assert.That(authoredMarker.transform.position, Is.EqualTo(markerPosition));
                Assert.That(authoredMarker.transform.rotation, Is.EqualTo(markerRotation));
                Assert.That(scene.GetRootGameObjects().Any(root => root.name == "Main Camera"), Is.False);
                Assert.That(scene.isDirty, Is.EqualTo(sceneWasDirty));
            }
            finally
            {
                CloseTemporaryScene(scene);
            }
        }

        [Test]
        public void GeneratorRefusesDirtyLoadedGameplayBeforeMutation()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var dummy = new GameObject("Unsaved Gameplay Authoring Change");
                SceneManager.MoveGameObjectToScene(dummy, scene);
                EditorSceneManager.MarkSceneDirty(scene);

                Assert.That(() => FirstPlayableSceneGenerator.Generate(), Throws.TypeOf<InvalidOperationException>());
                Assert.That(scene.isDirty, Is.True);
                Assert.That(scene.GetRootGameObjects().Any(root => root == dummy), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratorRejectsNestedConnectedWorldHealthBar()
        {
            var scene = CreateTemporaryScene();
            try
            {
                var library = RequireVisualLibrary();
                var anchor = CreateInScene(scene, "HealthBarAnchor").transform;
                var nestedParent = CreateInScene(scene, "Unexpected Nest").transform;
                nestedParent.SetParent(anchor, false);
                PrefabUtility.InstantiatePrefab(library.WorldHealthBar, nestedParent);

                AssertPrivateGeneratorFailure(
                    "RequireOrCreateConnectedHealthBar",
                    "direct connected child",
                    anchor,
                    library.WorldHealthBar);
            }
            finally
            {
                CloseTemporaryScene(scene);
            }
        }

        [Test]
        public void GeneratorRejectsWrongSoleInputModule()
        {
            var scene = CreateTemporaryScene();
            try
            {
                var root = CreateInScene(scene, "EventSystem");
                root.AddComponent<EventSystem>();
                root.AddComponent<StandaloneInputModule>();

                AssertPrivateGeneratorFailure(
                    "RequireSingleEventSystem",
                    "exactly one InputSystemUIInputModule",
                    scene,
                    root);
            }
            finally
            {
                CloseTemporaryScene(scene);
            }
        }

        [Test]
        public void GeneratorRejectsInactiveDuplicateUiBootstrapAndEventSystem()
        {
            var scene = CreateTemporaryScene();
            try
            {
                var uiRoot = CreateInScene(scene, "First Playable UI");
                uiRoot.AddComponent<FirstPlayableUiBootstrap>();
                var inactiveUi = CreateInScene(scene, "Inactive Extra UI");
                inactiveUi.AddComponent<FirstPlayableUiBootstrap>();
                inactiveUi.SetActive(false);
                AssertPrivateGeneratorFailure(
                    "RequireSingleUiBootstrap",
                    "unexpected FirstPlayableUiBootstrap",
                    scene,
                    uiRoot);

                var eventRoot = CreateInScene(scene, "EventSystem");
                eventRoot.AddComponent<EventSystem>();
                var inactiveEvent = CreateInScene(scene, "Inactive Extra EventSystem");
                inactiveEvent.AddComponent<EventSystem>();
                inactiveEvent.SetActive(false);
                AssertPrivateGeneratorFailure(
                    "RequireSingleEventSystem",
                    "unexpected EventSystem",
                    scene,
                    eventRoot);
            }
            finally
            {
                CloseTemporaryScene(scene);
            }
        }

        [Test]
        public void GeneratorRepairsEmptyLegacyArraysButPreservesNonNullCustomReferences()
        {
            var scene = CreateTemporaryScene();
            try
            {
                var library = RequireVisualLibrary();
                var controller = CreateInScene(scene, "Temporary Controller").AddComponent<FirstPlayableController>();
                var serialized = new SerializedObject(controller);
                var motionLibrary = AssetDatabase.LoadAssetAtPath<CombatMotionLibrary>(
                    CombatMotionLibraryBuilder.AssetPath);
                Assert.That(motionLibrary, Is.Not.Null);
                serialized.FindProperty("motionLibrary").objectReferenceValue = motionLibrary;
                serialized.FindProperty("enemySprites").arraySize = 0;
                serialized.FindProperty("battlefieldDecals").arraySize = 0;
                serialized.FindProperty("jangseungGeumjulVisuals").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var motionLibraryWasDirty = EditorUtility.IsDirty(motionLibrary);

                InvokePrivateGenerator("ConfigureControllerAssets", controller, library);
                serialized.Update();
                Assert.That(serialized.FindProperty("motionLibrary").objectReferenceValue, Is.EqualTo(motionLibrary));
                Assert.That(EditorUtility.IsDirty(motionLibrary), Is.EqualTo(motionLibraryWasDirty));
                Assert.That(serialized.FindProperty("enemySprites").arraySize, Is.EqualTo(5));
                Assert.That(serialized.FindProperty("battlefieldDecals").arraySize, Is.EqualTo(4));
                Assert.That(serialized.FindProperty("jangseungGeumjulVisuals").objectReferenceValue, Is.Not.Null);

                var customSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png");
                var customGeumjul = serialized.FindProperty("jangseungGeumjulVisuals").objectReferenceValue;
                serialized.FindProperty("enemySprites").arraySize = 1;
                serialized.FindProperty("enemySprites").GetArrayElementAtIndex(0).objectReferenceValue = customSprite;
                serialized.FindProperty("battlefieldDecals").arraySize = 1;
                serialized.FindProperty("battlefieldDecals").GetArrayElementAtIndex(0).objectReferenceValue = customSprite;
                serialized.FindProperty("jangseungGeumjulVisuals").objectReferenceValue = customGeumjul;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                InvokePrivateGenerator("ConfigureControllerAssets", controller, library);
                serialized.Update();
                Assert.That(serialized.FindProperty("enemySprites").arraySize, Is.EqualTo(1));
                Assert.That(serialized.FindProperty("enemySprites").GetArrayElementAtIndex(0).objectReferenceValue,
                    Is.EqualTo(customSprite));
                Assert.That(serialized.FindProperty("battlefieldDecals").arraySize, Is.EqualTo(1));
                Assert.That(serialized.FindProperty("battlefieldDecals").GetArrayElementAtIndex(0).objectReferenceValue,
                    Is.EqualTo(customSprite));
                Assert.That(serialized.FindProperty("jangseungGeumjulVisuals").objectReferenceValue,
                    Is.EqualTo(customGeumjul));
            }
            finally
            {
                CloseTemporaryScene(scene);
            }
        }

        private static GameplayVisualPrefabLibrary RequireVisualLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<GameplayVisualPrefabLibrary>(
                GameplayVisualPrefabBuilder.LibraryAssetPath);
            Assert.That(library, Is.Not.Null);
            return library;
        }

        private static Scene CreateTemporaryScene()
        {
            // The runner may retain an unsaved empty Scene between fixtures; Unity refuses another
            // additive untitled Scene in that state. A saved preview Scene provides isolated,
            // discarded additive test ownership without ever saving the production Gameplay asset.
            return EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Additive);
        }

        private static GameObject CreateInScene(Scene scene, string name)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject;
        }

        private static void CloseTemporaryScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static void AssertPrivateGeneratorFailure(string methodName, string expectedMessage, params object[] arguments)
        {
            var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateGenerator(methodName, arguments));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerException.Message, Does.Contain(expectedMessage));
        }

        private static void InvokePrivateGenerator(string methodName, params object[] arguments)
        {
            var method = typeof(FirstPlayableSceneGenerator).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private generator hook '{methodName}'.");
            method.Invoke(null, arguments);
        }

        private static GameObject FindSingleRoot(Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(root => root.name == name).ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), $"Expected exactly one root named '{name}'.");
            return matches.SingleOrDefault();
        }
    }
}
