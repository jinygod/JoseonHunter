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
                if (canvas.GetComponent<LobbyRootView>() != null)
                {
                    RepairAuthoredHierarchy(canvas);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save authored Lobby.");
                    Validate();
                    return;
                }
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

        private static void RepairAuthoredHierarchy(GameObject canvas)
        {
            ValidateAuthoredRepairPreconditions(canvas);
            var safe = canvas.transform.Find("Safe Area");
            if (safe == null) throw new InvalidOperationException("Authored Lobby Safe Area is missing.");
            Set(canvas.GetComponent<LobbyBootstrap>(), "weaponCatalog",
                AssetDatabase.LoadAssetAtPath<JoseonHunter.Content.Weapons.WeaponCatalogAsset>(WeaponCatalogPath));
            var order = new[] { "Background", "Common Header", "Home Page", "Training Page", "Patrol Page", "Research Page", "Settings Overlay" };
            for (var index = 0; index < order.Length; index++) safe.Find(order[index]).SetSiblingIndex(index);
            foreach (var child in canvas.GetComponentsInChildren<HorizontalLayoutGroup>(true)) UnityEngine.Object.DestroyImmediate(child);
            var legacy = canvas.transform.Find("Lobby Navigation Binding");
            if (legacy != null)
                foreach (Transform child in legacy) UnityEngine.Object.DestroyImmediate(child.gameObject);
            safe.Find("Home Page").gameObject.SetActive(true);
            safe.Find("Training Page").gameObject.SetActive(false);
            safe.Find("Patrol Page").gameObject.SetActive(false);
            safe.Find("Research Page").gameObject.SetActive(false);
            var cards = safe.Find("Home Page").GetComponentsInChildren<LobbyMenuCardView>(true);
            var cardTitles = new[] { "수련", "출전", "연구" };
            var cardDescriptions = new[] { "기초 능력을 단련합니다", "순찰을 시작합니다", "무기 연구를 확인합니다" };
            var iconPaths = new[]
            {
                "Assets/JoseonHunter/Resources/UI/PremiumJoseon/icon_training.png",
                "Assets/JoseonHunter/Resources/UI/PremiumJoseon/icon_patrol.png",
                "Assets/JoseonHunter/Resources/UI/PremiumJoseon/icon_research.png"
            };
            for (var index = 0; index < cards.Length && index < 3; index++)
            {
                cards[index].Title.text = cardTitles[index];
                cards[index].Description.text = cardDescriptions[index];
                cards[index].Icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[index]);
                cards[index].Icon.rectTransform.anchorMin = new Vector2(.32f, .66f);
                cards[index].Icon.rectTransform.anchorMax = new Vector2(.68f, .94f);
                cards[index].Icon.rectTransform.anchoredPosition = Vector2.zero;
                cards[index].Icon.rectTransform.sizeDelta = Vector2.zero;
                cards[index].Title.fontSize = 24f;
                cards[index].Title.rectTransform.anchorMin = new Vector2(.07f, .42f);
                cards[index].Title.rectTransform.anchorMax = new Vector2(.93f, .60f);
                cards[index].Title.rectTransform.anchoredPosition = Vector2.zero;
                cards[index].Title.rectTransform.sizeDelta = Vector2.zero;
                cards[index].Description.fontSize = 16f;
                cards[index].Description.rectTransform.anchorMin = new Vector2(.07f, .12f);
                cards[index].Description.rectTransform.anchorMax = new Vector2(.93f, .38f);
                cards[index].Description.rectTransform.anchoredPosition = Vector2.zero;
                cards[index].Description.rectTransform.sizeDelta = Vector2.zero;
                var rect = cards[index].GetComponent<RectTransform>();
                var minX = new[] { .05f, .365f, .650f };
                var maxX = new[] { .350f, .635f, .950f };
                rect.anchorMin = new Vector2(minX[index], .460f);
                rect.anchorMax = new Vector2(maxX[index], .735f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
            var pageTitles = new[] { "수련", "출전", "연구" };
            var pageNames = new[] { "Training Page", "Patrol Page", "Research Page" };
            for (var index = 0; index < pageNames.Length; index++)
            {
                var header = safe.Find(pageNames[index]).GetComponentInChildren<LobbyPageHeaderView>(true);
                header.Title.text = pageTitles[index];
                header.Icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[index]);
                var rect = header.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(.04f, .825f); rect.anchorMax = new Vector2(.96f, .890f);
                rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
            }
            var commonHeader = safe.Find("Common Header").GetComponent<RectTransform>();
            commonHeader.anchorMin = new Vector2(.03f, .915f); commonHeader.anchorMax = new Vector2(.97f, .985f);
            commonHeader.anchoredPosition = Vector2.zero; commonHeader.sizeDelta = Vector2.zero;
            RemoveLegacyDetailDuplicateRoots(safe);
            RemoveLegacyPatrolDuplicateRoots(safe.Find("Patrol Page"));
            ApplyDetailLayouts(safe);
            AuthorSettingsOverlay(safe.Find("Settings Overlay"));
        }

        private static void ValidateAuthoredRepairPreconditions(GameObject canvas)
        {
            if (canvas == null) throw new InvalidOperationException("Authored Lobby repair preflight failed: Lobby Canvas is missing.");
            var safe = canvas.transform.Find("Safe Area");
            if (safe == null) throw new InvalidOperationException("Authored Lobby repair preflight failed: Safe Area is missing.");
            var expected = new[] { "Background", "Common Header", "Home Page", "Training Page", "Patrol Page", "Research Page", "Settings Overlay" };
            foreach (var name in expected)
                if (safe.Cast<Transform>().Count(child => child.name == name) != 1)
                    throw new InvalidOperationException($"Authored Lobby repair preflight failed: required direct Safe Area child '{name}' is missing or duplicated.");
            if (safe.childCount != expected.Length)
                throw new InvalidOperationException($"Authored Lobby repair preflight failed: Safe Area requires exactly {expected.Length} direct children, found {safe.childCount}.");

            var bootstrap = canvas.GetComponent<LobbyBootstrap>();
            var root = canvas.GetComponent<LobbyRootView>();
            if (bootstrap == null || root == null || !root.HasRequiredBindings)
                throw new InvalidOperationException("Authored Lobby repair preflight failed: Bootstrap or root bindings are incomplete.");
            if (new SerializedObject(bootstrap).FindProperty("weaponCatalog") == null)
                throw new InvalidOperationException("Authored Lobby repair preflight failed: Bootstrap weapon catalog binding is unavailable.");

            var catalog = AssetDatabase.LoadAssetAtPath<JoseonHunter.Content.Weapons.WeaponCatalogAsset>(WeaponCatalogPath);
            var iconPaths = new[]
            {
                "Assets/JoseonHunter/Resources/UI/PremiumJoseon/icon_training.png",
                "Assets/JoseonHunter/Resources/UI/PremiumJoseon/icon_patrol.png",
                "Assets/JoseonHunter/Resources/UI/PremiumJoseon/icon_research.png"
            };
            if (catalog == null || iconPaths.Any(path => AssetDatabase.LoadAssetAtPath<Sprite>(path) == null))
                throw new InvalidOperationException("Authored Lobby repair preflight failed: required catalog or navigation icon asset is missing.");

            var homeTransform = safe.Find("Home Page");
            var trainingTransform = safe.Find("Training Page");
            var patrolTransform = safe.Find("Patrol Page");
            var researchTransform = safe.Find("Research Page");
            var settingsTransform = safe.Find("Settings Overlay");
            if (root.SafeArea != safe || root.Home == null || root.Home.transform != homeTransform ||
                root.TrainingView == null || root.TrainingView.transform != trainingTransform ||
                root.PatrolView == null || root.PatrolView.transform != patrolTransform ||
                root.ResearchView == null || root.ResearchView.transform != researchTransform ||
                root.SettingsOverlay == null || root.SettingsOverlay.transform != settingsTransform)
                throw new InvalidOperationException("Authored Lobby repair preflight failed: root bindings do not match the direct authored hierarchy.");

            var home = root.Home;
            var cards = homeTransform.GetComponentsInChildren<LobbyMenuCardView>(true);
            var boundCards = new[] { home.TrainingCard, home.PatrolCard, home.ResearchCard };
            if (home.StageText == null || home.DifficultyText == null || home.StartingWeaponText == null ||
                home.StartingWeaponIcon == null || cards.Length != 3 || cards.Any(card =>
                    card.Button == null || card.Title == null || card.Description == null || card.Icon == null) ||
                boundCards.Distinct().Count() != 3 || cards.Any(card => !boundCards.Contains(card)))
                throw new InvalidOperationException("Authored Lobby repair preflight failed: Home bindings or menu-card modules are incomplete.");

            foreach (var page in new[] { trainingTransform, patrolTransform, researchTransform })
            {
                var headers = page.GetComponentsInChildren<LobbyPageHeaderView>(true);
                if (headers.Length != 1 || headers[0].BackButton == null || headers[0].Title == null || headers[0].Icon == null)
                    throw new InvalidOperationException($"Authored Lobby repair preflight failed: page header beneath '{page.name}' is incomplete or duplicated.");
            }

            var training = trainingTransform.GetComponent<TrainingPageView>();
            var patrol = patrolTransform.GetComponent<PatrolPageView>();
            var research = researchTransform.GetComponent<ResearchPageView>();
            var settings = settingsTransform.GetComponent<LobbyAudioSettingsView>();
            if (training == null || !training.HasRequiredBindings || patrol == null || !patrol.HasRequiredBindings ||
                research == null || !research.HasRequiredBindings || settings == null || !settings.HasRequiredBindings)
                throw new InvalidOperationException("Authored Lobby repair preflight failed: page or settings bindings are incomplete.");

            var difficultyModules = patrolTransform.GetComponentsInChildren<LobbyDifficultyCardView>(true);
            var patrolSelectors = patrolTransform.GetComponentsInChildren<LobbyWeaponSelectorCardView>(true);
            var boundDifficulties = new[] { patrol.NormalDifficulty, patrol.OmenDifficulty, patrol.GreatOmenDifficulty };
            if (difficultyModules.Length != 3 || patrolSelectors.Length != 1 ||
                boundDifficulties.Distinct().Count() != 3 || difficultyModules.Any(card => !boundDifficulties.Contains(card)) ||
                patrol.WeaponSelector != patrolSelectors[0] ||
                trainingTransform.GetComponentsInChildren<LobbyTrainingRowView>(true).Length != 6 ||
                researchTransform.GetComponentsInChildren<LobbyWeaponSelectorCardView>(true).Length != 8 ||
                researchTransform.GetComponentsInChildren<LobbyResearchRowView>(true).Length != 3 ||
                researchTransform.GetComponentsInChildren<LobbyProgressBarView>(true).Length != 1)
                throw new InvalidOperationException("Authored Lobby repair preflight failed: required module instances are incomplete.");

            RequireUniqueAuthoredDescendant(trainingTransform, "Training Summary Backplate");
            RequireUniqueAuthoredDescendant(patrolTransform, "Stage Plaque");
            var dialog = RequireUniqueAuthoredDescendant(settingsTransform, "Audio Settings Panel") as RectTransform;
            var sliders = settingsTransform.GetComponentsInChildren<Slider>(true);
            if (settingsTransform.GetComponent<Image>() == null || dialog == null || sliders.Length != 2 ||
                settingsTransform.GetComponentsInChildren<Button>(true).Count(button => button.name == "Close Audio Settings") != 1 ||
                settingsTransform.GetComponentsInChildren<TMPro.TMP_Text>(true).Count(text => text.name.EndsWith(" Value")) != 2 ||
                sliders.Any(slider => slider.fillRect == null || slider.handleRect == null ||
                    slider.targetGraphic == null || slider.handleRect.parent is not RectTransform))
                throw new InvalidOperationException("Authored Lobby repair preflight failed: audio settings controls are incomplete.");
        }

        private static Transform RequireUniqueAuthoredDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item != root && item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Authored Lobby repair preflight failed: expected one '{name}' beneath '{root.name}', found {matches.Length}.");
            return matches[0];
        }

        private static void AuthorSettingsOverlay(Transform overlay)
        {
            var view = overlay.GetComponent<LobbyAudioSettingsView>() ?? overlay.gameObject.AddComponent<LobbyAudioSettingsView>();
            var dim = overlay.GetComponent<Image>();
            var dialog = overlay.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(item => item.name == "Audio Settings Panel");
            var title = overlay.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault();
            if (title == null) title = CreateText("Audio Settings Title", dialog);
            title.text = "소리 설정"; SetRect(title.rectTransform, .15f, .78f, .85f, .90f);
            var sliders = overlay.GetComponentsInChildren<Slider>(true);
            if (sliders.Length == 0) sliders = new[] { CreateAudioSlider(dialog, "Music Volume Slider", .60f), CreateAudioSlider(dialog, "Sound Effect Volume Slider", .38f) };
            var close = overlay.GetComponentsInChildren<Button>(true).FirstOrDefault(item => item.name == "Close Audio Settings") ?? CreateAudioClose(dialog);
            if (dim == null || dialog == null || title == null || sliders.Length != 2 || close == null)
                throw new InvalidOperationException($"Authored Settings Overlay bindings are incomplete: dim={dim != null}, dialog={dialog != null}, title={title != null}, sliders={sliders.Length}, close={close != null}.");
            foreach (var slider in sliders) NormalizeAudioSlider(slider);
            var values = overlay.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(item => item.name.EndsWith(" Value")).ToArray();
            if (values.Length != 2)
            {
                values = new[] { EnsureAudioValue(dialog, "Music Volume Value"), EnsureAudioValue(dialog, "Sound Effect Volume Value") };
            }
            view.Configure(title, sliders[0], sliders[1], values[0], values[1], close, dim, dialog);
            SetRect(dialog, .08f, .245f, .92f, .755f);
        }

        private static Slider CreateAudioSlider(RectTransform parent, string name, float y)
        {
            var track = CreateImage(name, parent); SetRect(track.rectTransform, .12f, y, .88f, y + .06f);
            var slider = track.gameObject.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f;
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>(); fillArea.SetParent(track.transform, false); SetRect(fillArea, .02f, .2f, .98f, .8f);
            var fill = CreateImage("Fill", fillArea); SetRect(fill.rectTransform, 0, 0, 1, 1); slider.fillRect = fill.rectTransform;
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>(); handleArea.SetParent(track.transform, false); SetRect(handleArea, 0, 0, 1, 1);
            var handle = CreateImage("Handle", handleArea); handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(.5f,.5f); handle.rectTransform.sizeDelta = new Vector2(20,26); slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            return slider;
        }

        private static void NormalizeAudioSlider(Slider slider)
        {
            if (slider.fillRect == null || slider.handleRect == null || slider.targetGraphic == null)
                throw new InvalidOperationException($"Authored slider '{slider.name}' is missing Fill, Handle, or target graphic.");
            var handleArea = slider.handleRect.parent as RectTransform;
            if (handleArea == null) throw new InvalidOperationException($"Authored slider '{slider.name}' Handle has no RectTransform parent.");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            var handle = slider.handleRect;
            handle.anchorMin = new Vector2(0f, .5f);
            handle.anchorMax = new Vector2(0f, .5f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(20f, 26f);
        }

        private static Button CreateAudioClose(RectTransform parent)
        {
            var image = CreateImage("Close Audio Settings", parent); SetRect(image.rectTransform, .25f, .08f, .75f, .18f);
            var button = image.gameObject.AddComponent<Button>(); var label = CreateText("Label", image.transform); label.text = "닫기"; SetRect(label.rectTransform,0,0,1,1); return button;
        }

        private static TMPro.TMP_Text EnsureAudioValue(RectTransform parent, string name)
        {
            var existing = parent.Find(name)?.GetComponent<TMPro.TMP_Text>();
            if (existing != null) return existing;
            var value = CreateText(name, parent);
            value.text = "100%";
            SetRect(value.rectTransform, .68f, name.StartsWith("Music") ? .58f : .38f, .90f, name.StartsWith("Music") ? .66f : .46f);
            return value;
        }

        private static void ApplyDetailLayouts(Transform safe)
        {
            var training = safe.Find("Training Page").GetComponent<TrainingPageView>();
            var rowY = new[] { (.710f, .800f), (.615f, .705f), (.520f, .610f), (.425f, .515f), (.330f, .420f), (.235f, .325f) };
            for (var i = 0; i < training.Rows.Length; i++)
            {
                var row = training.Rows[i].GetComponent<RectTransform>();
                if (row.parent != training.transform) row.SetParent(training.transform, false);
                SetRect(row, .06f, rowY[i].Item1, .94f, rowY[i].Item2);
            }
            var purchase = training.PurchaseButton.GetComponent<RectTransform>();
            if (purchase.parent != training.transform) purchase.SetParent(training.transform, false);
            SetRect(purchase, .06f, .050f, .61f, .150f);
            var reset = training.ResetButton.GetComponent<RectTransform>();
            if (reset.parent != training.transform) reset.SetParent(training.transform, false);
            SetRect(reset, .64f, .050f, .94f, .150f);
            var summary = FindObject(training.transform, "Training Summary Backplate").GetComponent<RectTransform>();
            if (summary.parent != training.transform) summary.SetParent(training.transform, false);
            SetRect(summary, .06f, .160f, .94f, .230f);
            var feedback = training.FeedbackText.rectTransform;
            if (feedback.parent != summary) feedback.SetParent(summary, false);
            SetRect(feedback, .08f, .170f, .92f, .220f);
            var patrol = safe.Find("Patrol Page").GetComponent<PatrolPageView>();
            SetRect(FindObject(patrol.transform, "Stage Plaque").GetComponent<RectTransform>(), .04f, .735f, .96f, .815f);
            SetRect(patrol.StageName.rectTransform, .20f, .755f, .80f, .805f); SetRect(patrol.PreviousStageButton.GetComponent<RectTransform>(), .06f, .742f, .18f, .810f); SetRect(patrol.NextStageButton.GetComponent<RectTransform>(), .82f, .742f, .94f, .810f);
            SetRect(patrol.HeroImage.rectTransform, .29f, .535f, .71f, .720f); SetRect(patrol.NormalDifficulty.GetComponent<RectTransform>(), .06f, .405f, .353f, .495f); SetRect(patrol.OmenDifficulty.GetComponent<RectTransform>(), .353f, .405f, .647f, .495f); SetRect(patrol.GreatOmenDifficulty.GetComponent<RectTransform>(), .647f, .405f, .94f, .495f); SetRect(patrol.WeaponSelector.GetComponent<RectTransform>(), .06f, .275f, .94f, .365f); SetRect(patrol.StartButton.GetComponent<RectTransform>(), .18f, .105f, .82f, .205f);
            var research = safe.Find("Research Page").GetComponent<ResearchPageView>();
            var selectorY = new[] { (.710f,.800f),(.615f,.705f),(.520f,.610f),(.425f,.515f) };
            for (var i = 0; i < research.WeaponSelectors.Length; i++)
            {
                var selector = research.WeaponSelectors[i].GetComponent<RectTransform>();
                if (selector.parent != research.transform) selector.SetParent(research.transform, false);
                SetRect(selector, i % 2 == 0 ? .06f : .515f, selectorY[i / 2].Item1,
                    i % 2 == 0 ? .485f : .94f, selectorY[i / 2].Item2);
            }
            var researchRowY = new[] { (.310f,.400f),(.215f,.305f),(.120f,.210f) };
            for (var i = 0; i < research.Rows.Length; i++)
            {
                var row = research.Rows[i].GetComponent<RectTransform>();
                if (row.parent != research.transform) row.SetParent(research.transform, false);
                SetRect(row, .06f, researchRowY[i].Item1, .94f, researchRowY[i].Item2);
            }
            var selectedIcon = research.SelectedWeaponIcon.rectTransform;
            if (selectedIcon.parent != research.transform) selectedIcon.SetParent(research.transform, false);
            SetRect(selectedIcon, .06f, .025f, .14f, .100f);
            var selectedTitle = research.SelectedWeaponName.rectTransform;
            if (selectedTitle.parent != research.transform) selectedTitle.SetParent(research.transform, false);
            SetRect(selectedTitle, .16f, .055f, .48f, .100f);
            var masteryProgress = research.MasteryProgress.GetComponent<RectTransform>();
            if (masteryProgress.parent != research.transform) masteryProgress.SetParent(research.transform, false);
            SetRect(masteryProgress, .50f, .025f, .94f, .100f);
            var researchFeedback = research.FeedbackText.rectTransform;
            if (researchFeedback.parent != research.transform) researchFeedback.SetParent(research.transform, false);
            SetRect(researchFeedback, .06f, .105f, .94f, .115f);
        }

        private static void RemoveLegacyDetailDuplicateRoots(Transform safe)
        {
            var training = safe.Find("Training Page").GetComponent<TrainingPageView>();
            var research = safe.Find("Research Page").GetComponent<ResearchPageView>();
            if (training == null || !training.HasRequiredBindings || research == null || !research.HasRequiredBindings)
                throw new InvalidOperationException("Modular Training and Research bindings are required before legacy cleanup.");

            DestroyNamedDescendants(training.transform, "Training Grid");
            foreach (var legacyRoot in new[]
                     { "Research Progress Backplate", "Weapon Grid", "Style Card 0", "Style Card 1", "Style Card 2" })
                DestroyNamedDescendants(research.transform, legacyRoot);

            foreach (var row in training.Rows)
                PremiumPixelUiSkin.ApplyFrame(row.Button.GetComponent<Image>(), PremiumFrame.SmallItem);
        }

        private static void RemoveLegacyPatrolDuplicateRoots(Transform patrol)
        {
            var view = patrol.GetComponent<PatrolPageView>();
            if (view == null || !view.HasRequiredBindings)
                throw new InvalidOperationException("Modular Patrol bindings are required before legacy cleanup.");
            foreach (var legacy in patrol.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name is "Difficulty Normal" or "Difficulty Omen" or "Difficulty Great Omen" or "Starting Weapon Selector")
                         .Where(item => item.GetComponent<LobbyDifficultyCardView>() == null && item.GetComponent<LobbyWeaponSelectorCardView>() == null)
                         .ToArray())
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);
        }

        private static void DestroyNamedDescendants(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => item != root && item.name == name).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void SetRect(RectTransform rect, float minX, float minY, float maxX, float maxY)
        { rect.anchorMin=new Vector2(minX,minY); rect.anchorMax=new Vector2(maxX,maxY); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=Vector2.zero; }

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
                if (roots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).Count() != 1 ||
                    roots.SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).Count() != 1)
                    throw new InvalidOperationException("Lobby requires exactly one EventSystem and AudioListener.");
                if (EditorBuildSettings.scenes.Where(item => item.enabled).Select(item => item.path).SequenceEqual(new[]
                    { "Assets/JoseonHunter/Scenes/Bootstrap.unity", ScenePath, "Assets/JoseonHunter/Scenes/Gameplay.unity" }) == false)
                    throw new InvalidOperationException("Lobby Build Settings order drifted.");
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
            AuthorSettingsOverlay(settings);
            UnityEngine.Object.DestroyImmediate(stage.gameObject);
            var navigation = safeArea.Find("Bottom Navigation");
            if (navigation != null)
            {
                var presenter = navigation.GetComponent<LobbyNavigationPresenter>();
                UnityEngine.Object.DestroyImmediate(navigation.gameObject);
                navigation = new GameObject("Lobby Navigation Binding").transform;
                navigation.SetParent(canvas.transform, false);
                presenter = navigation.gameObject.AddComponent<LobbyNavigationPresenter>();
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
            RemoveLegacyDetailDuplicateRoots(safeArea);
            RemoveLegacyPatrolDuplicateRoots(patrol);
            ApplyDetailLayouts(safeArea);
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
            Set(bootstrap, "weaponCatalog", AssetDatabase.LoadAssetAtPath<JoseonHunter.Content.Weapons.WeaponCatalogAsset>(WeaponCatalogPath));
            Set(navigationPresenter, "homePage", home); Set(navigationPresenter, "trainingPage", training.gameObject);
            Set(navigationPresenter, "patrolPage", patrol.gameObject); Set(navigationPresenter, "researchPage", research.gameObject);
            Set(navigationPresenter, "trainingMenuButton", cards[0].Button); Set(navigationPresenter, "patrolMenuButton", cards[1].Button);
            Set(navigationPresenter, "researchMenuButton", cards[2].Button); Set(navigationPresenter, "trainingBackButton", headers[0].BackButton);
            Set(navigationPresenter, "patrolBackButton", headers[1].BackButton); Set(navigationPresenter, "researchBackButton", headers[2].BackButton);
            home.SetActive(true); training.gameObject.SetActive(false); patrol.gameObject.SetActive(false); research.gameObject.SetActive(false);
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
                if (UsesSemanticPremiumSkin(button)) continue;
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

        private static bool UsesSemanticPremiumSkin(Button button) =>
            button.GetComponentInParent<LobbyTrainingRowView>() != null || UsesSemanticPremiumSkin(button.name);

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
