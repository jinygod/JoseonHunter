using System;
using System.IO;
using System.Linq;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    /// <summary>
    /// Creates the initial gameplay visual authoring assets once, then treats every valid prefab as
    /// designer-owned content. Re-running this builder validates customized assets without replacing them.
    /// </summary>
    public static class GameplayVisualPrefabBuilder
    {
        public const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        public const string GameplayPrefabFolder = "Assets/JoseonHunter/Prefabs/Gameplay";
        public const string LibraryFolder = "Assets/JoseonHunter/Resources/Gameplay";
        public const string LibraryAssetPath = LibraryFolder + "/GameplayVisualPrefabLibrary.asset";

        public const string PlayerPrefabPath = GameplayPrefabFolder + "/PlayerVisual.prefab";
        public const string EnemyPrefabPath = GameplayPrefabFolder + "/EnemyVisual.prefab";
        public const string HealthBarPrefabPath = GameplayPrefabFolder + "/WorldHealthBar.prefab";
        public const string ShieldBarPrefabPath = GameplayPrefabFolder + "/WorldShieldBar.prefab";
        public const string ExperiencePickupPrefabPath = GameplayPrefabFolder + "/ExperiencePickup.prefab";
        public const string YeopjeonPickupPrefabPath = GameplayPrefabFolder + "/YeopjeonPickup.prefab";
        public const string MagnetPickupPrefabPath = GameplayPrefabFolder + "/MagnetPickup.prefab";

        private const string AuthoringWhiteSpritePath = LibraryFolder + "/GameplayAuthoringWhite.asset";
        private const string PlayerSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png";
        private const string EnemySpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png";
        private const string ExperienceSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/experience_spirit_flame.png";
        private const string CoinSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png";
        private const string MagnetSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/treasure_chest.png";

        [MenuItem("JoseonHunter/Gameplay Editing/Create or Validate Visual Prefabs")]
        public static void CreateOrValidateVisualPrefabs()
        {
            ThrowIfLoadedSceneIsDirty(GameplayScenePath);
            ThrowIfLoadedSceneIsDirty(GameplayVisualPreviewBuilder.PreviewScenePath);
            var library = CreateOrValidateProductionPrefabs();
            GameplayVisualPreviewBuilder.CreateOrValidatePreview(library);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Gameplay visual Prefabs, library, preview Prefab and preview scene are valid. " +
                $"Open {GameplayVisualPreviewBuilder.PreviewScenePath} to author their presentation.");
        }

        /// <summary>Command-line entry point intended for Unity's -executeMethod argument.</summary>
        public static void CreateOrValidateVisualPrefabsBatch()
        {
            try
            {
                CreateOrValidateVisualPrefabs();
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static GameplayVisualPrefabLibrary CreateOrValidateProductionPrefabs()
        {
            ThrowIfLoadedSceneIsDirty(GameplayScenePath);
            EnsureFolder(GameplayPrefabFolder);
            EnsureFolder(LibraryFolder);

            var authoringWhite = GetOrCreateAuthoringWhiteSprite();
            var player = EnsurePrefab(
                PlayerPrefabPath,
                () => CreateCombatantTemplate(true, RequireSprite(PlayerSpritePath)),
                prefab => ValidateCombatantPrefab(prefab, true, PlayerPrefabPath));
            var enemy = EnsurePrefab(
                EnemyPrefabPath,
                () => CreateCombatantTemplate(false, RequireSprite(EnemySpritePath)),
                prefab => ValidateCombatantPrefab(prefab, false, EnemyPrefabPath));
            var healthBar = EnsurePrefab(
                HealthBarPrefabPath,
                () => CreateWorldBarTemplate(true, authoringWhite),
                prefab => ValidateWorldBarPrefab(prefab, HealthBarPrefabPath));
            var shieldBar = EnsurePrefab(
                ShieldBarPrefabPath,
                () => CreateWorldBarTemplate(false, authoringWhite),
                prefab => ValidateWorldBarPrefab(prefab, ShieldBarPrefabPath));
            var experience = EnsurePrefab(
                ExperiencePickupPrefabPath,
                () => CreatePickupTemplate("ExperiencePickup", RequireSprite(ExperienceSpritePath), .72f, true),
                prefab => ValidatePickupPrefab(prefab, true, ExperiencePickupPrefabPath));
            var yeopjeon = EnsurePrefab(
                YeopjeonPickupPrefabPath,
                () => CreatePickupTemplate("YeopjeonPickup", RequireSprite(CoinSpritePath), .48f, false),
                prefab => ValidatePickupPrefab(prefab, false, YeopjeonPickupPrefabPath));
            var magnet = EnsurePrefab(
                MagnetPickupPrefabPath,
                () => CreatePickupTemplate("MagnetPickup", RequireSprite(MagnetSpritePath), .50f, false),
                prefab => ValidatePickupPrefab(prefab, false, MagnetPickupPrefabPath));

            var library = GetOrCreateLibrary();
            ConnectLibraryReference(library, "playerVisual", player, PlayerPrefabPath);
            ConnectLibraryReference(library, "enemyVisual", enemy, EnemyPrefabPath);
            ConnectLibraryReference(library, "worldHealthBar", healthBar, HealthBarPrefabPath);
            ConnectLibraryReference(library, "worldShieldBar", shieldBar, ShieldBarPrefabPath);
            ConnectLibraryReference(library, "experiencePickup", experience, ExperiencePickupPrefabPath);
            ConnectLibraryReference(library, "yeopjeonPickup", yeopjeon, YeopjeonPickupPrefabPath);
            ConnectLibraryReference(library, "magnetPickup", magnet, MagnetPickupPrefabPath);
            if (!library.IsComplete)
                throw new InvalidOperationException($"{LibraryAssetPath} is incomplete after reference wiring.");

            AssetDatabase.SaveAssets();
            WireGameplayScene(library);
            return library;
        }

        internal static void ThrowIfLoadedSceneIsDirty(string scenePath)
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded || scene.path != scenePath || !scene.isDirty) continue;
                throw new InvalidOperationException(
                    $"Refusing to modify '{scenePath}' because its loaded Scene has unsaved changes. " +
                    "Save or discard those changes, then run the gameplay visual builder again.");
            }
        }

        private static void WireGameplayScene(GameplayVisualPrefabLibrary library)
        {
            ThrowIfLoadedSceneIsDirty(GameplayScenePath);
            var scene = SceneManager.GetSceneByPath(GameplayScenePath);
            var openedForBuilder = !scene.IsValid() || !scene.isLoaded;
            if (openedForBuilder)
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

            try
            {
                var controllers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FirstPlayableController>(true))
                    .ToArray();
                if (controllers.Length != 1)
                    throw new InvalidOperationException(
                        $"{GameplayScenePath} must contain exactly one FirstPlayableController; found {controllers.Length}.");

                var serialized = new SerializedObject(controllers[0]);
                var property = serialized.FindProperty("gameplayVisualPrefabs");
                if (property == null)
                    throw new InvalidOperationException(
                        "FirstPlayableController is missing serialized field 'gameplayVisualPrefabs'.");
                if (property.objectReferenceValue != null && property.objectReferenceValue != library)
                    throw new InvalidOperationException(
                        $"{GameplayScenePath} already references a different gameplay visual library. " +
                        "The builder will not replace a non-empty custom reference.");

                if (property.objectReferenceValue == library) return;
                property.objectReferenceValue = library;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!EditorSceneManager.SaveScene(scene, GameplayScenePath))
                    throw new InvalidOperationException($"Failed to save gameplay prefab reference to {GameplayScenePath}.");
            }
            finally
            {
                if (openedForBuilder && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject EnsurePrefab(
            string assetPath,
            Func<GameObject> createTemplate,
            Action<GameObject> validate)
        {
            var existingMainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existingMainAsset != null)
            {
                var existingPrefab = existingMainAsset as GameObject;
                if (existingPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(existingPrefab))
                    throw new InvalidOperationException(
                        $"Existing asset at {assetPath} is not a valid Prefab. It was not overwritten.");
                validate(existingPrefab);
                return existingPrefab;
            }
            if (AssetPathExists(assetPath))
                throw new InvalidOperationException(
                    $"An existing asset at {assetPath} could not be loaded as a valid Prefab and was not overwritten.");

            var template = createTemplate();
            if (template == null)
                throw new InvalidOperationException($"Template creation returned null for {assetPath}.");
            try
            {
                var createdPrefab = PrefabUtility.SaveAsPrefabAsset(template, assetPath);
                if (createdPrefab == null)
                    throw new InvalidOperationException($"Unity failed to create gameplay visual Prefab {assetPath}.");
                validate(createdPrefab);
                return createdPrefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        private static GameObject CreateCombatantTemplate(bool player, Sprite defaultSprite)
        {
            var root = new GameObject(player ? "PlayerVisual" : "EnemyVisual");
            var view = root.AddComponent<CombatantVisualView>();
            var shadow = CreateSpriteChild(root.transform, "Soft Shadow", defaultSprite, 7);
            shadow.color = new Color(.025f, .035f, .04f, .36f);
            shadow.transform.localPosition = new Vector3(0f, -.10f, 0f);
            shadow.transform.localScale = new Vector3(.72f, .14f, 1f);

            var outline = CreateSpriteChild(root.transform, "Silhouette Outline", defaultSprite, 9);
            outline.color = new Color(.025f, .04f, .055f, .92f);
            outline.transform.localScale = Vector3.one * 1.045f;

            SpriteRenderer aura = null;
            if (player)
            {
                aura = CreateSpriteChild(root.transform, "Player Aura", defaultSprite, 8);
                aura.color = new Color(.18f, .94f, .88f, .16f);
                aura.transform.localScale = Vector3.one * 1.13f;
            }

            var pivot = new GameObject("Visual Pivot").transform;
            pivot.SetParent(root.transform, false);
            var body = pivot.gameObject.AddComponent<SpriteRenderer>();
            body.sprite = defaultSprite;
            body.sortingOrder = 10;

            var healthAnchor = new GameObject("HealthBarAnchor").transform;
            healthAnchor.SetParent(root.transform, false);
            healthAnchor.localPosition = new Vector3(0f, player ? -.30f : -.78f, 0f);
            healthAnchor.localScale = Vector3.one * (player ? .58f : .52f);

            Transform shieldAnchor = null;
            if (!player)
            {
                shieldAnchor = new GameObject("ShieldBarAnchor").transform;
                shieldAnchor.SetParent(root.transform, false);
                shieldAnchor.localPosition = new Vector3(0f, -1.48f, 0f);
            }

            view.Configure(pivot, body, shadow, outline, aura, healthAnchor, shieldAnchor);
            return root;
        }

        private static GameObject CreateWorldBarTemplate(bool health, Sprite whiteSprite)
        {
            var root = new GameObject(health ? "WorldHealthBar" : "WorldShieldBar");
            var view = root.AddComponent<WorldBarView>();
            var background = CreateSpriteChild(root.transform, "Background", whiteSprite, 20);
            background.color = health
                ? new Color(.16f, .12f, .12f, .92f)
                : new Color(.12f, .09f, .06f, .94f);
            background.transform.localScale = health
                ? new Vector3(2.2f, .24f, 1f)
                : new Vector3(2.2f, .20f, 1f);

            var fill = CreateSpriteChild(root.transform, "Fill", whiteSprite, 21);
            fill.color = health
                ? new Color(.24f, .86f, .34f, 1f)
                : new Color(.72f, .45f, .14f, 1f);
            fill.transform.localScale = health
                ? new Vector3(2f, .14f, 1f)
                : new Vector3(2f, .10f, 1f);
            fill.transform.localPosition = new Vector3(0f, 0f, -.01f);
            view.Configure(background, fill);
            return root;
        }

        private static GameObject CreatePickupTemplate(
            string rootName,
            Sprite defaultSprite,
            float baseScale,
            bool withTrail)
        {
            var root = new GameObject(rootName);
            TrailRenderer trail = null;
            if (withTrail)
            {
                trail = root.AddComponent<TrailRenderer>();
                trail.time = .14f;
                trail.minVertexDistance = .035f;
                trail.startWidth = .12f;
                trail.endWidth = 0f;
                trail.startColor = new Color(.30f, 1f, .92f, .78f);
                trail.endColor = new Color(.20f, .86f, 1f, 0f);
                trail.sortingOrder = 5;
                trail.emitting = false;
            }

            var visual = CreateSpriteChild(root.transform, "Visual", defaultSprite, 6);
            var view = root.AddComponent<PickupVisualView>();
            view.Configure(visual, trail, baseScale);
            return root;
        }

        private static SpriteRenderer CreateSpriteChild(
            Transform parent,
            string childName,
            Sprite sprite,
            int sortingOrder)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void ValidateCombatantPrefab(GameObject prefab, bool player, string assetPath)
        {
            var view = RequireSingleRootComponent<CombatantVisualView>(prefab, assetPath);
            var role = player ? CombatantVisualRole.Player : CombatantVisualRole.Enemy;
            if (!view.HasRequiredBindings(role))
                throw InvalidPrefab(assetPath, "CombatantVisualView has missing or mismatched references");
            var shadow = RequireDirectSpriteChild(prefab, "Soft Shadow", assetPath);
            var outline = RequireDirectSpriteChild(prefab, "Silhouette Outline", assetPath);
            var pivotRenderer = RequireDirectSpriteChild(prefab, "Visual Pivot", assetPath);
            var healthAnchor = RequireDirectChild(prefab, "HealthBarAnchor", assetPath);
            if (view.ShadowRenderer != shadow || view.OutlineRenderer != outline ||
                view.BodyRenderer != pivotRenderer || view.VisualPivot != pivotRenderer.transform ||
                view.HealthBarAnchor != healthAnchor)
                throw InvalidPrefab(assetPath, "CombatantVisualView references do not point to the required direct children");
            if (prefab.GetComponent<SpriteRenderer>() != null)
                throw InvalidPrefab(assetPath, "the logical root must not own a SpriteRenderer");

            var aura = prefab.transform.Find("Player Aura");
            var shield = prefab.transform.Find("ShieldBarAnchor");
            if (player)
            {
                var auraRenderer = RequireDirectSpriteChild(prefab, "Player Aura", assetPath);
                if (view.AuraRenderer != auraRenderer)
                    throw InvalidPrefab(assetPath, "CombatantVisualView aura reference is not Player Aura");
                if (shield != null) throw InvalidPrefab(assetPath, "player Prefab contains an unused ShieldBarAnchor");
                if (view.ShieldBarAnchor != null)
                    throw InvalidPrefab(assetPath, "player CombatantVisualView contains an unused shield reference");
            }
            else
            {
                if (aura != null) throw InvalidPrefab(assetPath, "enemy Prefab contains the player-only aura");
                var shieldAnchor = RequireDirectChild(prefab, "ShieldBarAnchor", assetPath);
                if (view.AuraRenderer != null || view.ShieldBarAnchor != shieldAnchor)
                    throw InvalidPrefab(assetPath, "enemy CombatantVisualView aura or shield reference is invalid");
            }
        }

        private static void ValidateWorldBarPrefab(GameObject prefab, string assetPath)
        {
            var view = RequireSingleRootComponent<WorldBarView>(prefab, assetPath);
            if (!view.HasRequiredBindings)
                throw InvalidPrefab(assetPath, "WorldBarView has missing Background or Fill references");
            var background = RequireDirectSpriteChild(prefab, "Background", assetPath);
            var fill = RequireDirectSpriteChild(prefab, "Fill", assetPath);
            if (view.BackgroundRenderer != background || view.FillRenderer != fill)
                throw InvalidPrefab(assetPath, "WorldBarView references do not point to the required direct children");
            if (background.transform.localScale.x <= fill.transform.localScale.x ||
                background.transform.localScale.y <= fill.transform.localScale.y ||
                fill.transform.localScale.x <= 0f || fill.transform.localScale.y <= 0f)
                throw InvalidPrefab(assetPath, "authored bar geometry is invalid");
            if (prefab.GetComponent<SpriteRenderer>() != null)
                throw InvalidPrefab(assetPath, "the bar root must not own a SpriteRenderer");
        }

        private static void ValidatePickupPrefab(GameObject prefab, bool requiresTrail, string assetPath)
        {
            var view = RequireSingleRootComponent<PickupVisualView>(prefab, assetPath);
            if (!view.HasRequiredBindings)
                throw InvalidPrefab(assetPath, "PickupVisualView has no Visual renderer reference");
            var visual = RequireDirectSpriteChild(prefab, "Visual", assetPath);
            if (view.VisualRenderer != visual)
                throw InvalidPrefab(assetPath, "PickupVisualView reference does not point to the direct Visual child");
            if (prefab.GetComponent<SpriteRenderer>() != null)
                throw InvalidPrefab(assetPath, "the pickup root must not own a SpriteRenderer");
            var trails = prefab.GetComponentsInChildren<TrailRenderer>(true);
            if (requiresTrail)
            {
                if (trails.Length != 1 || trails[0].gameObject != prefab || view.TrailRenderer != trails[0])
                    throw InvalidPrefab(assetPath, "experience requires exactly one root TrailRenderer wired to the view");
            }
            else if (trails.Length != 0 || view.TrailRenderer != null)
            {
                throw InvalidPrefab(assetPath, "only the experience pickup may contain a TrailRenderer");
            }
        }

        private static T RequireSingleRootComponent<T>(GameObject prefab, string assetPath) where T : Component
        {
            var matches = prefab.GetComponents<T>();
            if (matches.Length != 1)
                throw InvalidPrefab(assetPath, $"requires exactly one root {typeof(T).Name}; found {matches.Length}");
            return matches[0];
        }

        private static Transform RequireDirectChild(GameObject prefab, string childName, string assetPath)
        {
            var child = prefab.transform.Find(childName);
            if (child == null || child.parent != prefab.transform)
                throw InvalidPrefab(assetPath, $"requires direct child '{childName}'");
            return child;
        }

        private static SpriteRenderer RequireDirectSpriteChild(GameObject prefab, string childName, string assetPath)
        {
            var child = RequireDirectChild(prefab, childName, assetPath);
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
                throw InvalidPrefab(assetPath, $"child '{childName}' requires a SpriteRenderer");
            return renderer;
        }

        private static InvalidOperationException InvalidPrefab(string assetPath, string reason)
        {
            return new InvalidOperationException(
                $"Existing gameplay visual Prefab '{assetPath}' is invalid: {reason}. " +
                "It was not overwritten; repair the Prefab in Prefab Mode or move it aside before rebuilding.");
        }

        private static GameplayVisualPrefabLibrary GetOrCreateLibrary()
        {
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(LibraryAssetPath);
            if (mainAsset != null)
            {
                var existingLibrary = mainAsset as GameplayVisualPrefabLibrary;
                if (existingLibrary == null)
                    throw new InvalidOperationException(
                        $"Existing asset at {LibraryAssetPath} is not a GameplayVisualPrefabLibrary and was not overwritten.");
                return existingLibrary;
            }
            if (AssetPathExists(LibraryAssetPath))
                throw new InvalidOperationException(
                    $"An existing asset at {LibraryAssetPath} could not be loaded and was not overwritten.");

            var library = ScriptableObject.CreateInstance<GameplayVisualPrefabLibrary>();
            library.name = "GameplayVisualPrefabLibrary";
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
            return library;
        }

        private static void ConnectLibraryReference(
            GameplayVisualPrefabLibrary library,
            string propertyName,
            GameObject expectedPrefab,
            string expectedPath)
        {
            var serialized = new SerializedObject(library);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException(
                    $"{LibraryAssetPath} is missing serialized property '{propertyName}'.");
            if (property.objectReferenceValue != null && property.objectReferenceValue != expectedPrefab)
                throw new InvalidOperationException(
                    $"{LibraryAssetPath}.{propertyName} already references '{AssetDatabase.GetAssetPath(property.objectReferenceValue)}', " +
                    $"not '{expectedPath}'. The builder will not replace a non-empty custom reference.");
            if (property.objectReferenceValue == expectedPrefab) return;

            property.objectReferenceValue = expectedPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite GetOrCreateAuthoringWhiteSprite()
        {
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(AuthoringWhiteSpritePath);
            if (mainAsset != null)
            {
                if (!(mainAsset is Texture2D))
                    throw new InvalidOperationException(
                        $"Existing asset at {AuthoringWhiteSpritePath} is not the expected authoring texture.");
                var existingSprite = AssetDatabase.LoadAllAssetsAtPath(AuthoringWhiteSpritePath)
                    .OfType<Sprite>()
                    .SingleOrDefault();
                if (existingSprite == null)
                    throw new InvalidOperationException(
                        $"Existing authoring texture {AuthoringWhiteSpritePath} has no Sprite sub-asset and was not overwritten.");
                return existingSprite;
            }
            if (AssetPathExists(AuthoringWhiteSpritePath))
                throw new InvalidOperationException(
                    $"An existing asset at {AuthoringWhiteSpritePath} could not be loaded and was not overwritten.");

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GameplayAuthoringWhiteTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, AuthoringWhiteSpritePath);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 1f);
            sprite.name = "GameplayAuthoringWhiteSprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAllAssetsAtPath(AuthoringWhiteSpritePath).OfType<Sprite>().Single();
        }

        private static Sprite RequireSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                throw new InvalidOperationException(
                    $"Required representative Sprite is missing at {assetPath}; no Prefab was created.");
            return sprite;
        }

        internal static bool AssetPathExists(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                throw new InvalidOperationException("Unity project root could not be resolved from Application.dataPath.");

            var absolutePath = Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(absolutePath) || Directory.Exists(absolutePath);
        }

        private static void EnsureFolder(string assetFolder)
        {
            var segments = assetFolder.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, segments[index]);
                    if (string.IsNullOrEmpty(guid))
                        throw new InvalidOperationException($"Unity failed to create asset folder {next}.");
                }
                current = next;
            }
        }
    }
}
