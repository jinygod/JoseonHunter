using System;
using System.IO;
using System.Linq;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Domain.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoseonHunter.Editor.Scenes
{
    public static class LobbySceneBuilder
    {
        private const string ScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";
        private const string BackgroundPath = "Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png";
        private const string CoinSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png";
        private const string HeroSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png";
        private const string WeaponCatalogPath = "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset";

        [MenuItem("JoseonHunter/Setup/Build Lobby")]
        public static void Build()
        {
            RefuseDirtyLobby();
            LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
            var scene = FindLoadedLobby();
            var opened = !scene.IsValid();
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var canvas = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "Lobby Canvas");
                if (canvas == null) throw new InvalidOperationException("Lobby Canvas is missing; refusing an implicit scene repair.");
                if (PrefabUtility.IsPartOfPrefabInstance(canvas))
                    PrefabUtility.UnpackPrefabInstance(canvas, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                ComposeAuthoredHierarchy(canvas);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save the authored Lobby scene.");
                AssetDatabase.SaveAssets();
                Validate();
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("JoseonHunter/Validation/Validate Lobby")]
        public static void Validate()
        {
            var scene = FindLoadedLobby();
            var opened = !scene.IsValid();
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                RequireExactlyOne(roots, "Lobby Camera");
                var canvas = RequireExactlyOne(roots, "Lobby Canvas");
                RequireExactlyOne(roots, "EventSystem");
                var safeArea = canvas.transform.Find("Safe Area");
                if (safeArea == null) throw new InvalidOperationException("Lobby Safe Area is missing.");
                var expected = new[] { "Background", "Common Header", "Home Page", "Training Page", "Patrol Page", "Research Page", "Settings Overlay" };
                if (!safeArea.Cast<Transform>().Select(child => child.name).OrderBy(name => name)
                        .SequenceEqual(expected.OrderBy(name => name)))
                    throw new InvalidOperationException("Lobby Safe Area does not have the required authored children.");
                if (canvas.GetComponentsInChildren<LobbyRootView>(true).Length != 1 ||
                    !canvas.GetComponentInChildren<LobbyRootView>(true).HasRequiredBindings)
                    throw new InvalidOperationException("LobbyRootView is incomplete.");
                if (canvas.GetComponentsInChildren<Transform>(true)
                    .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)) != 0)
                    throw new InvalidOperationException("Lobby contains missing scripts.");
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ValidateInBatchMode()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static GameObject RequireExactlyOne(GameObject[] roots, string name)
        {
            var matches = roots.Where(root => root.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected exactly one Lobby root named '{name}'.");
            return matches[0];
        }

        private static void ComposeAuthoredHierarchy(GameObject canvas)
        {
            var safeArea = canvas.transform.Find("Safe Area") as RectTransform;
            if (safeArea == null) throw new InvalidOperationException("Lobby Safe Area is missing.");
            var background = canvas.transform.Find("Lobby Background");
            var header = safeArea.Find("Header");
            var stage = safeArea.Find("Stage Content");
            var settings = safeArea.Find("Audio Settings Overlay");
            if (background == null || header == null || stage == null || settings == null)
                throw new InvalidOperationException("Legacy Lobby bindings are incomplete; refusing a lossy migration.");
            var patrol = stage.Find("Patrol Panel");
            var training = stage.Find("Common Training Panel");
            var research = stage.Find("Weapon Research Panel");
            if (patrol == null || training == null || research == null)
                throw new InvalidOperationException("Legacy Lobby detail pages are incomplete; refusing a lossy migration.");

            var moduleRoot = "Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/";
            var commonHeader = InstantiateModule(moduleRoot + "CommonHeader.prefab", safeArea, "Common Header");
            header.SetParent(commonHeader.transform, false);
            background.SetParent(safeArea, false); background.name = "Background";
            patrol.SetParent(safeArea, false); patrol.name = "Patrol Page";
            training.SetParent(safeArea, false); training.name = "Training Page";
            research.SetParent(safeArea, false); research.name = "Research Page";
            settings.name = "Settings Overlay";
            UnityEngine.Object.DestroyImmediate(stage.gameObject);
            var navigation = safeArea.Find("Bottom Navigation");
            if (navigation != null)
            {
                navigation.SetParent(canvas.transform, false);
                navigation.name = "Lobby Navigation Binding";
            }

            var home = new GameObject("Home Page", typeof(RectTransform), typeof(LobbyHomeView), typeof(LobbyHomePresenter));
            home.transform.SetParent(safeArea, false);
            var homeView = home.GetComponent<LobbyHomeView>();
            var cards = new[]
            {
                InstantiateModule(moduleRoot + "HomeMenuCard.prefab", home.transform, "Training Card").GetComponent<LobbyMenuCardView>(),
                InstantiateModule(moduleRoot + "HomeMenuCard.prefab", home.transform, "Patrol Card").GetComponent<LobbyMenuCardView>(),
                InstantiateModule(moduleRoot + "HomeMenuCard.prefab", home.transform, "Research Card").GetComponent<LobbyMenuCardView>()
            };
            var stageText = CreateText("Stage", home.transform);
            var difficultyText = CreateText("Difficulty", home.transform);
            var weaponText = CreateText("Starting Weapon", home.transform);
            var weaponIcon = CreateImage("Starting Weapon Icon", home.transform);
            homeView.Configure(stageText, difficultyText, weaponText, weaponIcon, cards[0], cards[1], cards[2]);
            var headers = new[] { training, patrol, research }.Select(page =>
                InstantiateModule(moduleRoot + "PageHeader.prefab", page, "Page Header").GetComponent<LobbyPageHeaderView>()).ToArray();
            AuthorPageViews(moduleRoot, patrol, training, research, headers);
            var root = canvas.GetComponent<LobbyRootView>() ?? canvas.AddComponent<LobbyRootView>();
            var bootstrap = canvas.GetComponent<LobbyBootstrap>();
            var navigationPresenter = canvas.GetComponentInChildren<LobbyNavigationPresenter>(true);
            var patrolPresenter = patrol.GetComponent<PatrolPresenter>(); var patrolView = patrol.GetComponent<PatrolPageView>();
            var trainingPresenter = training.GetComponent<CommonTrainingPresenter>(); var trainingView = training.GetComponent<TrainingPageView>();
            var researchPresenter = research.GetComponent<WeaponResearchPresenter>(); var researchView = research.GetComponent<ResearchPageView>();
            var settingsButton = header.GetComponentInChildren<Button>(true);
            var audio = settings.GetComponentInChildren<AudioSettingsPresenter>(true);
            if (bootstrap == null || navigationPresenter == null || patrolPresenter == null || patrolView == null ||
                trainingPresenter == null || trainingView == null || researchPresenter == null || researchView == null ||
                settingsButton == null || audio == null) throw new InvalidOperationException("Required legacy authored binding is missing.");
            Set(root, "safeArea", safeArea); Set(root, "header", commonHeader.GetComponent<LobbyHeaderView>());
            Set(root, "home", homeView); Set(root, "homePresenter", home.GetComponent<LobbyHomePresenter>()); Set(root, "navigation", navigationPresenter);
            Set(root, "patrolView", patrolView); Set(root, "patrolPresenter", patrolPresenter); Set(root, "trainingView", trainingView);
            Set(root, "trainingPresenter", trainingPresenter); Set(root, "researchView", researchView); Set(root, "researchPresenter", researchPresenter);
            Set(root, "settingsOverlay", settings.gameObject); Set(root, "settingsButton", settingsButton); Set(root, "audioSettings", audio);
            Set(bootstrap, "rootView", root);
            Set(navigationPresenter, "homePage", home); Set(navigationPresenter, "trainingPage", training.gameObject);
            Set(navigationPresenter, "patrolPage", patrol.gameObject); Set(navigationPresenter, "researchPage", research.gameObject);
            Set(navigationPresenter, "trainingMenuButton", cards[0].Button); Set(navigationPresenter, "patrolMenuButton", cards[1].Button);
            Set(navigationPresenter, "researchMenuButton", cards[2].Button); Set(navigationPresenter, "trainingBackButton", headers[0].BackButton);
            Set(navigationPresenter, "patrolBackButton", headers[1].BackButton); Set(navigationPresenter, "researchBackButton", headers[2].BackButton);
        }

        private static void AuthorPageViews(string modules, Transform patrol, Transform training, Transform research,
            LobbyPageHeaderView[] headers)
        {
            var patrolView = patrol.gameObject.AddComponent<PatrolPageView>();
            var difficulties = Enumerable.Range(0, 3).Select(index => InstantiateModule(modules + "DifficultyCard.prefab", patrol,
                new[] { "Difficulty Normal", "Difficulty Omen", "Difficulty Great Omen" }[index]).GetComponent<LobbyDifficultyCardView>()).ToArray();
            var patrolSelector = InstantiateModule(modules + "WeaponSelectorCard.prefab", patrol, "Starting Weapon Selector").GetComponent<LobbyWeaponSelectorCardView>();
            patrolView.Configure(headers[1], FindText(patrol, "Stage Name"), FindText(patrol, "Stage Status"),
                FindButton(patrol, "Previous Stage"), FindButton(patrol, "Next Stage"), FindImage(patrol, "Patrol Hero"),
                difficulties[0], difficulties[1], difficulties[2], patrolSelector, FindText(patrol, "Patrol Feedback"),
                FindObject(patrol, "Weapon Selection Overlay").gameObject, FindButton(patrol, "Close Weapon Selection"), FindButton(patrol, "Start Patrol"));

            var trainingView = training.gameObject.AddComponent<TrainingPageView>();
            var rows = Enumerable.Range(0, 6).Select(index => InstantiateModule(modules + "TrainingRow.prefab", training,
                "Training Row " + ((CommonTrainingId)index)).GetComponent<LobbyTrainingRowView>()).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                row.Configure((CommonTrainingId)index, row.Button, row.NameText, row.IconImage, row.RankText, row.Progress);
            }
            var iconSet = AssetDatabase.LoadAssetAtPath<LobbyTrainingIconSet>("Assets/JoseonHunter/Prefabs/UI/Lobby/TrainingIconSet.asset");
            if (iconSet == null) throw new InvalidOperationException("Production TrainingIconSet is missing.");
            trainingView.Configure(rows, iconSet, FindText(training, "Current"), FindText(training, "Next"), FindText(training, "Cost"),
                FindText(training, "Training Capacity"), FindButton(training, "Purchase Training"), FindButton(training, "Reset Training"), FindText(training, "Training Feedback"));

            var researchView = research.gameObject.AddComponent<ResearchPageView>();
            var selectors = Enumerable.Range(0, 8).Select(index => InstantiateModule(modules + "WeaponSelectorCard.prefab", research,
                "Research Weapon " + index).GetComponent<LobbyWeaponSelectorCardView>()).ToArray();
            var progress = InstantiateModule(modules + "ProgressBar.prefab", research, "Mastery Progress").GetComponent<LobbyProgressBarView>();
            var researchRows = Enumerable.Range(0, 3).Select(index => InstantiateModule(modules + "ResearchRow.prefab", research,
                "Research Row " + index).GetComponent<LobbyResearchRowView>()).ToArray();
            researchView.Configure(selectors, FindImage(research, "Selected Weapon Icon"), FindText(research, "Research Title"), progress,
                researchRows, FindText(research, "Research Feedback"));
        }

        private static Transform FindObject(Transform root, string name) => FindDescendant(root, name);
        private static TMPro.TMP_Text FindText(Transform root, string name) => FindDescendant(root, name).GetComponent<TMPro.TMP_Text>();
        private static Button FindButton(Transform root, string name) => FindDescendant(root, name).GetComponent<Button>();
        private static Image FindImage(Transform root, string name) => FindDescendant(root, name).GetComponent<Image>();
        private static Transform FindDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one '{name}' beneath '{root.name}', found {matches.Length}.");
            return matches[0];
        }

        private static GameObject InstantiateModule(string path, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException($"Missing Lobby module prefab: {path}");
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) throw new InvalidOperationException($"Could not instantiate Lobby module: {path}");
            instance.name = name;
            return instance;
        }

        private static TMPro.TMP_Text CreateText(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            var text = item.GetComponent<TMPro.TextMeshProUGUI>();
            text.font = TMPro.TMP_Settings.defaultFontAsset;
            text.fontSize = 22f;
            return text;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            return item.GetComponent<Image>();
        }

        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var property = new SerializedObject(target).FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Missing serialized binding '{name}' on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BuildInBatchMode()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("JoseonHunter/Validation/Capture Lobby")]
        public static void CapturePreview()
        {
            throw new InvalidOperationException("Lobby preview capture is authored-scene work and is available after Task 8 capture wiring.");

            /*

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Lobby Preview Camera", typeof(Camera),
                typeof(UniversalAdditionalCameraData));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .025f, .018f, 1f);
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Lobby preview.");
            PopulatePatrolPreview(instance);
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            var output = Path.GetFullPath("Artifacts/LobbyPremium");
            Directory.CreateDirectory(output);
            foreach (var resolution in new[] { new Vector2Int(720, 1280), new Vector2Int(1080, 2340) })
            {
                ShowPanel(instance, "Patrol Panel");
                PopulatePatrolPreview(instance);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-patrol.png"));

                ShowPanel(instance, "Weapon Research Panel");
                PopulateResearchPreview(instance, "연구 중", 0, string.Empty);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-research-locked.png"));
                PopulateResearchPreview(instance, "해금 가능", 2000, string.Empty);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-research-ready.png"));
                PopulateResearchPreview(instance, "엽전 부족", 2000, "엽전이 부족합니다.");
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-research-coins.png"));

                ShowPanel(instance, "Common Training Panel");
                PopulateTrainingPreview(instance);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-training.png"));
            }
            UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"Captured Lobby previews to {output}.");
            */
        }

        public static void CapturePreviewInBatchMode()
        {
            try
            {
                CapturePreview();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndCaptureInBatchMode()
        {
            try
            {
                Build();
                CapturePreview();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void AssignSprites(GameObject canvasObject)
        {
            var transforms = canvasObject.GetComponentsInChildren<Transform>(true);
            var background = transforms.Single(item => item.name == "Lobby Background").GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (background.sprite == null) throw new InvalidOperationException($"Missing Lobby background: {BackgroundPath}");
            background.color = Color.white;

            var coinIcon = transforms.Single(item => item.name == "Coin Icon").GetComponent<Image>();
            coinIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);
            if (coinIcon.sprite == null) throw new InvalidOperationException($"Missing coin sprite: {CoinSpritePath}");
            coinIcon.preserveAspect = true;

            var settingsIcon = transforms.Single(item => item.name == "Settings Icon").GetComponent<Image>();
            PremiumPixelUiSkin.ApplyIcon(settingsIcon, PremiumIcon.Settings);

            var patrolHero = transforms.Single(item => item.name == "Patrol Hero").GetComponent<Image>();
            patrolHero.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeroSpritePath);
            if (patrolHero.sprite == null) throw new InvalidOperationException($"Missing hero sprite: {HeroSpritePath}");
            patrolHero.preserveAspect = true;

            foreach (var panelName in new[] { "Weapon Research Panel", "Patrol Panel", "Common Training Panel" })
            {
                var panel = transforms.Single(item => item.name == panelName).GetComponent<Image>();
                panel.sprite = null;
                panel.color = new Color(.035f, .043f, .065f, 1f);
            }

            LobbySelectionChrome.Apply(
                transforms.Single(item => item.name == "Difficulty Normal").GetComponent<Button>(), true);
            LobbySelectionChrome.Apply(
                transforms.Single(item => item.name == "Difficulty Omen").GetComponent<Button>(), false);
            LobbySelectionChrome.Apply(
                transforms.Single(item => item.name == "Difficulty Great Omen").GetComponent<Button>(), false, true);

            foreach (var button in canvasObject.GetComponentsInChildren<Button>(true))
            {
                if (UsesSemanticPremiumSkin(button.name)) continue;
                JoseonButtonSkin.Apply(button,
                    button.name is "Start Patrol" or "Purchase Training"
                        ? JoseonButtonStyle.Primary
                        : JoseonButtonStyle.Secondary);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<JoseonHunter.Content.Weapons.WeaponCatalogAsset>(
                WeaponCatalogPath);
            if (catalog == null) throw new InvalidOperationException($"Missing weapon catalog: {WeaponCatalogPath}");
            var startingWeaponIcon = transforms.Single(item => item.name == "Starting Weapon Icon").GetComponent<Image>();
            if (catalog.TryGet(JoseonHunter.Domain.Combat.WeaponId.HwandoFlyingBlade, out var startingWeapon))
            {
                startingWeaponIcon.sprite = startingWeapon.UiIcon != null
                    ? startingWeapon.UiIcon
                    : startingWeapon.PresentationSprites.FirstOrDefault();
                startingWeaponIcon.enabled = startingWeaponIcon.sprite != null;
                var researchWeaponIcon = transforms.Single(item => item.name == "Selected Weapon Icon")
                    .GetComponent<Image>();
                researchWeaponIcon.sprite = startingWeaponIcon.sprite;
                researchWeaponIcon.enabled = researchWeaponIcon.sprite != null;
            }
            canvasObject.GetComponentInChildren<PatrolPresenter>(true).ConfigureCatalog(catalog);
            canvasObject.GetComponentInChildren<WeaponResearchPresenter>(true).ConfigureCatalog(catalog);

        }

        private static bool UsesSemanticPremiumSkin(string buttonName) => buttonName is
            "Settings Button" or
            "Previous Stage" or
            "Next Stage" or
            "Difficulty Normal" or
            "Difficulty Omen" or
            "Difficulty Great Omen" or
            "Starting Weapon Selector" or
            "Weapon Research Navigation" or
            "Patrol Navigation" or
            "Common Training Navigation" ||
            buttonName.StartsWith("Style ", StringComparison.Ordinal) ||
            buttonName.StartsWith("Training ", StringComparison.Ordinal);

        private static void PopulatePatrolPreview(GameObject instance)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Starting Weapon Name", "환도 비검");
            SetPreviewText(transforms, "Coin Text", "155");
            transforms.Single(item => item.name == "Stage Status").gameObject.SetActive(false);

            var normal = transforms.Single(item => item.name == "Difficulty Normal").GetComponent<Button>();
            var omen = transforms.Single(item => item.name == "Difficulty Omen").GetComponent<Button>();
            var greatOmen = transforms.Single(item => item.name == "Difficulty Great Omen").GetComponent<Button>();
            greatOmen.GetComponentInChildren<TMPro.TMP_Text>(true).text = "대흉";
            LobbySelectionChrome.Apply(normal, true);
            LobbySelectionChrome.Apply(omen, false);
            LobbySelectionChrome.Apply(greatOmen, false, true);

            var start = transforms.Single(item => item.name == "Start Patrol").GetComponent<Button>();
            JoseonButtonSkin.Apply(start, JoseonButtonStyle.Primary);

            LobbySelectionChrome.ApplyNavigation(transforms.Single(item => item.name == "Patrol Navigation")
                .GetComponent<Button>(), PremiumIcon.Patrol, true);
            LobbySelectionChrome.ApplyNavigation(transforms.Single(item => item.name == "Weapon Research Navigation")
                .GetComponent<Button>(), PremiumIcon.Research, false);
            LobbySelectionChrome.ApplyNavigation(transforms.Single(item => item.name == "Common Training Navigation")
                .GetComponent<Button>(), PremiumIcon.Training, false);
        }

        private static void PopulateResearchPreview(GameObject instance, string state, int mastery, string feedback)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Research Title", "환도 비검 연구");
            SetPreviewText(transforms, "Mastery Summary", $"숙련도 {mastery:N0} / 2,000");
            SetPreviewText(transforms, "Research Feedback", feedback);
            var buttons = instance.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Style ", StringComparison.Ordinal))
                .OrderBy(button => button.name).ToArray();
            buttons[0].GetComponentInChildren<TMPro.TMP_Text>(true).text =
                "기본식 · 장착 중\n무기의 본래 운용법 / 추가 효과 없음\n처음부터 사용 가능";
            buttons[1].GetComponentInChildren<TMPro.TMP_Text>(true).text =
                $"맹독 비검 · {state}\n독 피해 강화 / 직접 피해 감소\n숙련도 2,000 · 엽전 800";
            buttons[2].GetComponentInChildren<TMPro.TMP_Text>(true).text =
                "월식 비검 · 연구 중\n강한 일격 / 재사용 대기 증가\n숙련도 8,000 · 엽전 2,400";
        }

        private static void PopulateTrainingPreview(GameObject instance)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Current", "현재 최대 체력 +0%");
            SetPreviewText(transforms, "Next", "강화 후 최대 체력 +2%");
            SetPreviewText(transforms, "Cost", "필요 엽전 100");
            SetPreviewText(transforms, "Training Feedback", "모든 출전에 적용되는 소규모 공통 강화입니다.");
        }

        private static void ShowPanel(GameObject instance, string selectedName)
        {
            foreach (var name in new[] { "Weapon Research Panel", "Patrol Panel", "Common Training Panel" })
            {
                var panel = instance.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == name).gameObject;
                panel.SetActive(name == selectedName);
            }
        }

        private static void SetPreviewText(Transform[] transforms, string name, string value)
        {
            var text = transforms.Single(item => item.name == name).GetComponent<TMPro.TMP_Text>();
            text.text = value;
        }

        private static void Capture(Camera camera, Vector2Int resolution, string outputPath)
        {
            var renderTexture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Scene FindLoadedLobby()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var candidate = SceneManager.GetSceneAt(index);
                if (candidate.path == ScenePath) return candidate;
            }
            return default;
        }

        private static void RefuseDirtyLobby()
        {
            var scene = FindLoadedLobby();
            if (scene.IsValid() && scene.isDirty)
                throw new InvalidOperationException("Lobby scene has unsaved changes. Save or close it before rebuilding.");
        }
    }
}
