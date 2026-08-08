using System;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    /// <summary>Builds a non-shipping authoring scene from nested instances of the production visual Prefabs.</summary>
    public static class GameplayVisualPreviewBuilder
    {
        public const string PreviewPrefabPath =
            GameplayVisualPrefabBuilder.GameplayPrefabFolder + "/GameplayAuthoringPreview.prefab";
        public const string PreviewScenePath = "Assets/JoseonHunter/Scenes/GameplayVisualPreview.unity";
        private const string BossSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png";

        [MenuItem("JoseonHunter/Gameplay Editing/Open Visual Preview")]
        public static void OpenVisualPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before opening the gameplay visual authoring preview.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var library = GameplayVisualPrefabBuilder.CreateOrValidateProductionPrefabs();
            CreateOrValidatePreview(library);
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
            var previewRoot = scene.GetRootGameObjects().SingleOrDefault();
            Selection.activeGameObject = previewRoot;
            if (previewRoot != null && SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        [MenuItem("JoseonHunter/Gameplay Editing/Rebuild Visual Preview From Production Prefabs")]
        public static void RebuildVisualPreviewFromProductionPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before rebuilding the gameplay visual authoring preview.");
                return;
            }

            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
            ThrowIfPreviewPrefabStageIsOpen();
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Visual Preview",
                    "This replaces only GameplayAuthoringPreview.prefab and GameplayVisualPreview.unity. " +
                    "The seven production gameplay visual Prefabs are validated and will not be overwritten.",
                    "Rebuild Preview",
                    "Cancel"))
                return;

            var library = GameplayVisualPrefabBuilder.CreateOrValidateProductionPrefabs();
            RebuildPreviewAssets(library);
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = scene.GetRootGameObjects().SingleOrDefault();
            Debug.Log("Gameplay visual Preview was rebuilt from connected production Prefab instances.");
        }

        /// <summary>Optional command-line entry point when only the preview needs validation.</summary>
        public static void CreateOrValidatePreviewBatch()
        {
            try
            {
                GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
                var library = GameplayVisualPrefabBuilder.CreateOrValidateProductionPrefabs();
                CreateOrValidatePreview(library);
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Explicit destructive preview-only rebuild for CI/task verification. Production Prefabs are
        /// validated first and are never deleted or saved by this operation.
        /// </summary>
        public static void RebuildVisualPreviewFromProductionPrefabsBatch()
        {
            try
            {
                GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
                ThrowIfPreviewPrefabStageIsOpen();
                var library = GameplayVisualPrefabBuilder.CreateOrValidateProductionPrefabs();
                RebuildPreviewAssets(library);
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static GameObject CreateOrValidatePreview(GameplayVisualPrefabLibrary library)
        {
            if (library == null || !library.IsComplete)
                throw new InvalidOperationException(
                    "A complete GameplayVisualPrefabLibrary is required before the authoring preview can be built.");

            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
            var previewPrefab = GetOrCreatePreviewPrefab(library);
            EnsurePreviewScene(previewPrefab);
            ExcludePreviewFromBuildSettings();
            AssetDatabase.SaveAssets();
            return previewPrefab;
        }

        private static GameObject RebuildPreviewAssets(GameplayVisualPrefabLibrary library)
        {
            if (library == null || !library.IsComplete)
                throw new InvalidOperationException(
                    "A complete GameplayVisualPrefabLibrary is required before rebuilding the authoring preview.");

            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
            ThrowIfPreviewPrefabStageIsOpen();
            CloseLoadedPreviewSceneForRebuild();
            DeletePreviewAssetIfPresent(PreviewScenePath);
            DeletePreviewAssetIfPresent(PreviewPrefabPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return CreateOrValidatePreview(library);
        }

        private static GameObject GetOrCreatePreviewPrefab(GameplayVisualPrefabLibrary library)
        {
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(PreviewPrefabPath);
            if (mainAsset != null)
            {
                var existingPrefab = mainAsset as GameObject;
                if (existingPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(existingPrefab))
                    throw new InvalidOperationException(
                        $"Existing asset at {PreviewPrefabPath} is not a valid Prefab and was not overwritten.");
                ValidatePreviewPrefab(existingPrefab, library);
                return existingPrefab;
            }
            if (GameplayVisualPrefabBuilder.AssetPathExists(PreviewPrefabPath))
                throw new InvalidOperationException(
                    $"An existing asset at {PreviewPrefabPath} could not be loaded as a valid Prefab and was not overwritten.");

            var template = CreatePreviewTemplate(library);
            try
            {
                var createdPrefab = PrefabUtility.SaveAsPrefabAsset(template, PreviewPrefabPath);
                if (createdPrefab == null)
                    throw new InvalidOperationException($"Unity failed to create preview Prefab {PreviewPrefabPath}.");
                ValidatePreviewPrefab(createdPrefab, library);
                return createdPrefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        private static GameObject CreatePreviewTemplate(GameplayVisualPrefabLibrary library)
        {
            var root = new GameObject("GameplayAuthoringPreview");
            var visualScale = CombatVisualScaleProfile.MobilePortrait;

            var cameraObject = new GameObject("Portrait Camera Guide");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .07f, .08f, 1f);

            var player = AddNestedPrefab(library.PlayerVisual, root.transform, "Player Preview", new Vector3(-3f, 1.4f, 0f));
            player.transform.localScale = Vector3.Scale(
                player.transform.localScale,
                Vector3.one * visualScale.PlayerScale);
            var playerView = RequireCombatantView(player);
            AddNestedBarPrefab(
                library.WorldHealthBar,
                playerView.HealthBarAnchor,
                "Player Health Bar Preview");

            var enemy = AddNestedPrefab(
                library.EnemyVisual,
                root.transform,
                "Normal Enemy Preview",
                new Vector3(0f, 1.4f, 0f));
            var enemyView = RequireCombatantView(enemy);
            enemy.transform.localScale = Vector3.Scale(
                enemy.transform.localScale,
                Vector3.one * visualScale.NormalEnemyScale);
            AddNestedBarPrefab(
                library.WorldHealthBar,
                enemyView.HealthBarAnchor,
                "Enemy Health Bar Preview");
            var boss = AddNestedPrefab(library.EnemyVisual, root.transform, "Elite Boss Scale Preview", new Vector3(3f, 1.4f, 0f));
            boss.transform.localScale = Vector3.Scale(
                boss.transform.localScale,
                Vector3.one * visualScale.NormalEnemyScale *
                BossScaleProfile.MultiplierFor(BossCombatRole.FinalBoss));
            ApplyRepresentativeSprite(boss, BossSpritePath);
            var bossView = RequireCombatantView(boss);
            if (bossView.ShieldBarAnchor == null)
                throw new InvalidOperationException(
                    "Elite Boss Scale Preview requires EnemyVisual.ShieldBarAnchor for its nested shield bar.");
            AddNestedBarPrefab(
                library.WorldHealthBar,
                bossView.HealthBarAnchor,
                "Boss Health Bar Preview");
            AddNestedBarPrefab(
                library.WorldShieldBar,
                bossView.ShieldBarAnchor,
                "Boss Shield Bar Preview");

            var experience = AddNestedPrefab(
                library.ExperiencePickup,
                root.transform,
                "Experience Preview",
                new Vector3(-1.4f, -2.6f, 0f));
            experience.transform.localScale = Vector3.one * experience.GetComponent<PickupVisualView>().BaseScale;
            var yeopjeon = AddNestedPrefab(
                library.YeopjeonPickup,
                root.transform,
                "Yeopjeon Preview",
                new Vector3(0f, -2.6f, 0f));
            yeopjeon.transform.localScale = Vector3.one * yeopjeon.GetComponent<PickupVisualView>().BaseScale;
            var magnet = AddNestedPrefab(
                library.MagnetPickup,
                root.transform,
                "Magnet Preview",
                new Vector3(1.4f, -2.6f, 0f));
            magnet.transform.localScale = Vector3.one * magnet.GetComponent<PickupVisualView>().BaseScale;
            return root;
        }

        private static void ApplyRepresentativeSprite(GameObject combatantInstance, string spritePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                throw new InvalidOperationException($"Preview representative Sprite is missing at {spritePath}.");
            var view = combatantInstance.GetComponent<CombatantVisualView>();
            if (view == null)
                throw new InvalidOperationException($"{combatantInstance.name} is missing CombatantVisualView.");
            view.BodyRenderer.sprite = sprite;
            view.ShadowRenderer.sprite = sprite;
            view.OutlineRenderer.sprite = sprite;
            if (view.AuraRenderer != null) view.AuraRenderer.sprite = sprite;
        }

        private static CombatantVisualView RequireCombatantView(GameObject combatantInstance)
        {
            var view = combatantInstance == null ? null : combatantInstance.GetComponent<CombatantVisualView>();
            if (view == null || view.HealthBarAnchor == null)
                throw new InvalidOperationException(
                    $"Preview combatant '{combatantInstance?.name ?? "<null>"}' has no usable CombatantVisualView health anchor.");
            return view;
        }

        private static GameObject AddNestedPrefab(
            GameObject prefab,
            Transform parent,
            string instanceName,
            Vector3 localPosition)
        {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException(
                    $"Cannot add invalid production Prefab '{AssetDatabase.GetAssetPath(prefab)}' to the preview.");
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Unity failed to instantiate {AssetDatabase.GetAssetPath(prefab)}.");
            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        private static GameObject AddNestedBarPrefab(
            GameObject prefab,
            Transform parent,
            string instanceName)
        {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException(
                    $"Cannot add invalid production Prefab '{AssetDatabase.GetAssetPath(prefab)}' to the preview.");
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Unity failed to instantiate {AssetDatabase.GetAssetPath(prefab)}.");
            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void ValidatePreviewPrefab(GameObject previewPrefab, GameplayVisualPrefabLibrary library)
        {
            if (previewPrefab.GetComponentsInChildren<FirstPlayableController>(true).Length != 0)
                throw InvalidPreview("contains FirstPlayableController and could start live combat flow");
            if (previewPrefab.GetComponentsInChildren<MetaGameSession>(true).Length != 0)
                throw InvalidPreview("contains MetaGameSession and could modify save/session state");
            if (previewPrefab.GetComponentsInChildren<Camera>(true).Length != 1)
                throw InvalidPreview("requires exactly one portrait Camera guide");

            RequireNestedInstances(previewPrefab, library.PlayerVisual, 1);
            RequireNestedInstances(previewPrefab, library.EnemyVisual, 2);
            RequireNestedInstances(previewPrefab, library.WorldHealthBar, 1);
            RequireNestedInstances(previewPrefab, library.WorldShieldBar, 1);
            RequireNestedInstances(previewPrefab, library.ExperiencePickup, 1);
            RequireNestedInstances(previewPrefab, library.YeopjeonPickup, 1);
            RequireNestedInstances(previewPrefab, library.MagnetPickup, 1);

            var playerView = RequireNamedCombatantView(previewPrefab, "Player Preview");
            var enemyView = RequireNamedCombatantView(previewPrefab, "Normal Enemy Preview");
            var bossView = RequireNamedCombatantView(previewPrefab, "Elite Boss Scale Preview");
            RequireNestedBarUnderAnchor(playerView.HealthBarAnchor, library.WorldHealthBar, "Player Preview");
            RequireNestedBarUnderAnchor(enemyView.HealthBarAnchor, library.WorldHealthBar, "Normal Enemy Preview");
            RequireNestedBarUnderAnchor(bossView.HealthBarAnchor, library.WorldHealthBar, "Elite Boss Scale Preview");
            RequireNestedBarUnderAnchor(bossView.ShieldBarAnchor, library.WorldShieldBar, "Elite Boss Scale Preview");
        }

        private static CombatantVisualView RequireNamedCombatantView(GameObject previewPrefab, string instanceName)
        {
            var matches = previewPrefab.GetComponentsInChildren<CombatantVisualView>(true)
                .Where(view => view.gameObject.name == instanceName)
                .ToArray();
            if (matches.Length != 1)
                throw InvalidPreview($"requires exactly one '{instanceName}' CombatantVisualView; found {matches.Length}");
            return matches[0];
        }

        private static void RequireNestedBarUnderAnchor(
            Transform anchor,
            GameObject expectedBarPrefab,
            string combatantName)
        {
            if (anchor == null)
                throw InvalidPreview($"{combatantName} is missing the required world-bar anchor");
            var count = anchor.GetComponentsInChildren<WorldBarView>(true).Count(view =>
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(view.gameObject) == expectedBarPrefab);
            if (count != 1)
                throw InvalidPreview(
                    $"{combatantName}/{anchor.name} requires one connected nested instance of " +
                    $"{AssetDatabase.GetAssetPath(expectedBarPrefab)}; found {count}");
        }

        private static void RequireNestedInstances(GameObject previewPrefab, GameObject productionPrefab, int minimum)
        {
            var count = previewPrefab.GetComponentsInChildren<Transform>(true).Count(candidate =>
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(candidate.gameObject) == productionPrefab);
            if (count < minimum)
                throw InvalidPreview(
                    $"requires at least {minimum} nested instance(s) of {AssetDatabase.GetAssetPath(productionPrefab)}; found {count}");
        }

        private static InvalidOperationException InvalidPreview(string reason)
        {
            return new InvalidOperationException(
                $"Existing preview Prefab '{PreviewPrefabPath}' is invalid: {reason}. " +
                "It was not overwritten; repair it in Prefab Mode or move it aside before rebuilding.");
        }

        private static void EnsurePreviewScene(GameObject previewPrefab)
        {
            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScenePath);
            if (sceneAsset != null)
            {
                ValidateExistingPreviewScene(previewPrefab);
                return;
            }
            if (AssetDatabase.LoadMainAssetAtPath(PreviewScenePath) != null ||
                GameplayVisualPrefabBuilder.AssetPathExists(PreviewScenePath))
                throw new InvalidOperationException(
                    $"Existing asset at {PreviewScenePath} is not a Scene and was not overwritten.");

            var singleEmptyUntitledScene = SceneManager.sceneCount == 1 &&
                                          string.IsNullOrEmpty(SceneManager.GetSceneAt(0).path) &&
                                          !SceneManager.GetSceneAt(0).isDirty &&
                                          SceneManager.GetSceneAt(0).rootCount == 0;
            if (!singleEmptyUntitledScene && Enumerable.Range(0, SceneManager.sceneCount)
                    .Select(SceneManager.GetSceneAt)
                    .Any(scene => scene.isLoaded && string.IsNullOrEmpty(scene.path)))
                throw new InvalidOperationException(
                    "Cannot create the gameplay preview while an untitled Scene is loaded. " +
                    "Save or close the untitled Scene, then run the builder again.");

            var previewScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                singleEmptyUntitledScene ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                var instance = PrefabUtility.InstantiatePrefab(previewPrefab, previewScene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Unity failed to instantiate the authoring preview into its Scene.");
                if (!EditorSceneManager.SaveScene(previewScene, PreviewScenePath))
                    throw new InvalidOperationException($"Unity failed to save authoring preview Scene {PreviewScenePath}.");
            }
            finally
            {
                if (!singleEmptyUntitledScene && previewScene.IsValid() && previewScene.isLoaded)
                    EditorSceneManager.CloseScene(previewScene, true);
            }
        }

        private static void ValidateExistingPreviewScene(GameObject previewPrefab)
        {
            GameplayVisualPrefabBuilder.ThrowIfLoadedSceneIsDirty(PreviewScenePath);
            var scene = SceneManager.GetSceneByPath(PreviewScenePath);
            var openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
                scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Additive);

            try
            {
                var roots = scene.GetRootGameObjects();
                if (roots.SelectMany(root => root.GetComponentsInChildren<FirstPlayableController>(true)).Any())
                    throw new InvalidOperationException($"{PreviewScenePath} contains live FirstPlayableController flow.");
                if (roots.SelectMany(root => root.GetComponentsInChildren<MetaGameSession>(true)).Any())
                    throw new InvalidOperationException($"{PreviewScenePath} contains MetaGameSession state.");

                var connectedPreviewRoots = roots.Count(root =>
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(root) == previewPrefab);
                if (connectedPreviewRoots != 1 || roots.Length != 1)
                    throw new InvalidOperationException(
                        $"Existing preview Scene '{PreviewScenePath}' must contain exactly one connected " +
                        $"GameplayAuthoringPreview root; found {connectedPreviewRoots} connected roots and {roots.Length} total roots. " +
                        "The Scene was not overwritten.");
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ExcludePreviewFromBuildSettings()
        {
            var original = EditorBuildSettings.scenes;
            var filtered = original.Where(scene => scene.path != PreviewScenePath).ToArray();
            if (filtered.Length != original.Length) EditorBuildSettings.scenes = filtered;
        }

        private static void ThrowIfPreviewPrefabStageIsOpen()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == PreviewPrefabPath)
                throw new InvalidOperationException(
                    $"Refusing to rebuild {PreviewPrefabPath} while it is open in Prefab Mode. " +
                    "Close Prefab Mode, then run the rebuild again.");
        }

        private static void CloseLoadedPreviewSceneForRebuild()
        {
            var scene = SceneManager.GetSceneByPath(PreviewScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (scene.isDirty)
                throw new InvalidOperationException(
                    $"Refusing to rebuild dirty loaded Scene {PreviewScenePath}.");

            if (SceneManager.sceneCount == 1)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return;
            }

            if (!EditorSceneManager.CloseScene(scene, true))
                throw new InvalidOperationException($"Could not close loaded preview Scene {PreviewScenePath}.");
        }

        private static void DeletePreviewAssetIfPresent(string assetPath)
        {
            if (!GameplayVisualPrefabBuilder.AssetPathExists(assetPath) &&
                AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                return;
            if (!AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException($"Unity could not delete preview-only asset {assetPath}.");
        }
    }
}
