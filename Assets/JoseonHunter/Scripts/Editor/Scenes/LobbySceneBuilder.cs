using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoseonHunter.Editor.Scenes
{
    public static class LobbySceneBuilder
    {
        private const string ScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";
        private const string CaptureDirectory = "Artifacts/LobbyPremium";
        private const string BackgroundPath = "Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png";
        private const string CoinSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png";
        private const string HeroSpritePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png";
        private const string WeaponCatalogPath = "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset";
        private static readonly LobbyCaptureRequest[] CaptureRequests =
        {
            new(new Vector2Int(720, 1280), "Home"),
            new(new Vector2Int(720, 1280), "Training"),
            new(new Vector2Int(720, 1280), "Patrol"),
            new(new Vector2Int(720, 1280), "Research-ready"),
            new(new Vector2Int(1080, 1920), "Home"),
            new(new Vector2Int(1080, 1920), "Training"),
            new(new Vector2Int(1080, 1920), "Patrol"),
            new(new Vector2Int(1080, 1920), "Research-ready"),
            new(new Vector2Int(1080, 2340), "Home"),
            new(new Vector2Int(1080, 2340), "Training"),
            new(new Vector2Int(1080, 2340), "Patrol"),
            new(new Vector2Int(1080, 2340), "Research-ready")
        };

        public static IReadOnlyList<LobbyCaptureRequest> CapturePlan => CaptureRequests;

        public readonly struct LobbyCaptureRequest
        {
            public LobbyCaptureRequest(Vector2Int resolution, string page)
            {
                Resolution = resolution;
                Page = page;
            }

            public Vector2Int Resolution { get; }
            public string Page { get; }
            public string FileName => $"{Resolution.x}x{Resolution.y}-{Page.ToLowerInvariant()}.png";
        }

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
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one;
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
            foreach (var pageName in new[] { "Home Page", "Training Page", "Patrol Page", "Research Page" })
                NormalizeAuthoredPage(safe.Find(pageName));

            var commonHeaderView = safe.Find("Common Header").GetComponent<LobbyHeaderView>();
            var rootView = canvas.GetComponent<LobbyRootView>();
            Set(rootView, "header", commonHeaderView);
            Set(rootView, "settingsButton", commonHeaderView.SettingsButton);
            var legacyHeader = commonHeaderView.transform.Find("Header");
            if (legacyHeader != null) UnityEngine.Object.DestroyImmediate(legacyHeader.gameObject);
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
                PrefabUtility.RecordPrefabInstancePropertyModifications(cards[index].Title);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cards[index].Description);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cards[index].Icon);
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
            AuthorHomeSummary(safe.Find("Home Page").GetComponent<LobbyHomeView>());
            var pageTitles = new[] { "수련", "출전", "연구" };
            var pageNames = new[] { "Training Page", "Patrol Page", "Research Page" };
            for (var index = 0; index < pageNames.Length; index++)
            {
                var header = safe.Find(pageNames[index]).GetComponentInChildren<LobbyPageHeaderView>(true);
                header.Title.text = pageTitles[index];
                header.Icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[index]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(header.Title);
                PrefabUtility.RecordPrefabInstancePropertyModifications(header.Icon);
                var rect = header.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(.04f, .825f); rect.anchorMax = new Vector2(.96f, .890f);
                rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
            }
            var commonHeader = safe.Find("Common Header").GetComponent<RectTransform>();
            commonHeader.anchorMin = new Vector2(.03f, .915f); commonHeader.anchorMax = new Vector2(.97f, .985f);
            commonHeader.anchoredPosition = Vector2.zero; commonHeader.sizeDelta = Vector2.zero;
            RemoveLegacyDetailDuplicateRoots(safe);
            RemoveLegacyPatrolDuplicateRoots(safe.Find("Patrol Page"));
            DestroyNamedDescendants(safe.Find("Training Page"), "Training Title");
            DestroyNamedDescendants(safe.Find("Training Page"), "Training Description");
            ApplyDetailLayouts(safe);
            AuthorSettingsOverlay(safe.Find("Settings Overlay"));
        }

        private static void ValidateAuthoredRepairPreconditions(GameObject canvas)
        {
            if (canvas == null) throw new InvalidOperationException("Authored Lobby repair preflight failed: Lobby Canvas is missing.");
            if (canvas.GetComponent<RectTransform>() == null)
                throw new InvalidOperationException("Authored Lobby repair preflight failed: Lobby Canvas has no RectTransform.");
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
            if (bootstrap == null || root == null)
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
            var header = safe.Find("Common Header").GetComponent<LobbyHeaderView>();
            if (header == null || !header.HasRequiredBindings || root.Header != header ||
                root.SafeArea != safe || root.Home == null || root.Home.transform != homeTransform ||
                root.TrainingView == null || root.TrainingView.transform != trainingTransform ||
                root.PatrolView == null || root.PatrolView.transform != patrolTransform ||
                root.ResearchView == null || root.ResearchView.transform != researchTransform ||
                root.Navigation == null || root.HomePresenter == null || root.TrainingPresenter == null ||
                root.PatrolPresenter == null || root.ResearchPresenter == null || root.AudioSettings == null ||
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
            var selectorX = new[] { (.06f,.265f),(.285f,.49f),(.51f,.715f),(.735f,.94f) };
            var selectorY = new[] { (.700f,.800f),(.585f,.685f) };
            for (var i = 0; i < research.WeaponSelectors.Length; i++)
            {
                var selector = research.WeaponSelectors[i].GetComponent<RectTransform>();
                if (selector.parent != research.transform) selector.SetParent(research.transform, false);
                SetRect(selector, selectorX[i % 4].Item1, selectorY[i / 4].Item1,
                    selectorX[i % 4].Item2, selectorY[i / 4].Item2);
                research.WeaponSelectors[i].Caption.gameObject.SetActive(false);
                research.WeaponSelectors[i].Chevron.gameObject.SetActive(false);
                SetRect(research.WeaponSelectors[i].Icon.rectTransform, .06f, .20f, .30f, .80f);
                SetRect(research.WeaponSelectors[i].WeaponName.rectTransform, .33f, .17f, .95f, .83f);
                research.WeaponSelectors[i].WeaponName.fontSize = 14f;
                research.WeaponSelectors[i].WeaponName.textWrappingMode = TextWrappingModes.NoWrap;
                research.WeaponSelectors[i].WeaponName.overflowMode = TextOverflowModes.Ellipsis;
            }
            var researchRowY = new[] { (.440f,.550f),(.315f,.425f),(.190f,.300f) };
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
            SetRect(researchFeedback, .06f, .105f, .94f, .175f);
        }

        private static void AuthorHomeSummary(LobbyHomeView home)
        {
            if (home == null || home.StageText == null || home.DifficultyText == null ||
                home.StartingWeaponText == null || home.StartingWeaponIcon == null)
                throw new InvalidOperationException("Authored Home summary bindings are incomplete.");
            var summary = home.transform.Find("Current Deployment") as RectTransform;
            if (summary == null)
            {
                var background = CreateImage("Current Deployment", home.transform);
                PremiumPixelUiSkin.ApplyFrame(background, PremiumFrame.ContentBackplate);
                summary = background.rectTransform;
            }
            SetRect(summary, .08f, .135f, .92f, .320f);
            if (home.StageText.transform.parent != summary) home.StageText.transform.SetParent(summary, false);
            if (home.DifficultyText.transform.parent != summary) home.DifficultyText.transform.SetParent(summary, false);
            if (home.StartingWeaponText.transform.parent != summary) home.StartingWeaponText.transform.SetParent(summary, false);
            if (home.StartingWeaponIcon.transform.parent != summary) home.StartingWeaponIcon.transform.SetParent(summary, false);
            SetRect(home.StageText.rectTransform, .04f, .18f, .31f, .82f);
            SetRect(home.DifficultyText.rectTransform, .35f, .18f, .62f, .82f);
            SetRect(home.StartingWeaponIcon.rectTransform, .66f, .24f, .75f, .76f);
            SetRect(home.StartingWeaponText.rectTransform, .75f, .18f, .97f, .82f);
            foreach (var text in new[] { home.StageText, home.DifficultyText, home.StartingWeaponText })
            {
                text.fontSize = 18f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 13f;
                text.fontSizeMax = 19f;
                text.alignment = TMPro.TextAlignmentOptions.Center;
            }
            home.StartingWeaponIcon.preserveAspect = true;
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

        private static void NormalizeAuthoredPage(Transform page)
        {
            if (page is not RectTransform rect)
                throw new InvalidOperationException("Authored Lobby page is missing a RectTransform: " + page?.name);
            SetRect(rect, 0f, 0f, 1f, 1f);
            var background = page.GetComponent<Image>();
            if (background == null) return;
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
            background.raycastTarget = false;
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
            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) throw new InvalidOperationException("Lobby Canvas RectTransform is missing.");
            canvasRect.localScale = Vector3.one;
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
            UnityEngine.Object.DestroyImmediate(header.gameObject);
            background.SetParent(safeArea, false); background.name = "Background";
            patrol.SetParent(safeArea, false); patrol.name = "Patrol Page";
            training.SetParent(safeArea, false); training.name = "Training Page";
            research.SetParent(safeArea, false); research.name = "Research Page";
            NormalizeAuthoredPage(patrol);
            NormalizeAuthoredPage(training);
            NormalizeAuthoredPage(research);
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
            NormalizeAuthoredPage(home.transform);
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
            DestroyNamedDescendants(training, "Training Title");
            DestroyNamedDescendants(training, "Training Description");
            ApplyDetailLayouts(safeArea);
            var root = canvas.GetComponent<LobbyRootView>() ?? canvas.AddComponent<LobbyRootView>();
            var bootstrap = canvas.GetComponent<LobbyBootstrap>();
            var navigationPresenter = canvas.GetComponentInChildren<LobbyNavigationPresenter>(true);
            var patrolPresenter = patrol.GetComponent<PatrolPresenter>(); var patrolView = patrol.GetComponent<PatrolPageView>();
            var trainingPresenter = training.GetComponent<CommonTrainingPresenter>(); var trainingView = training.GetComponent<TrainingPageView>();
            var researchPresenter = research.GetComponent<WeaponResearchPresenter>(); var researchView = research.GetComponent<ResearchPageView>();
            var settingsButton = commonHeader.GetComponent<LobbyHeaderView>().SettingsButton;
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
            RepairAuthoredHierarchy(canvas);
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
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Lobby capture refuses to run while the editor is in Play Mode.");
            RefuseDirtyLobby();
            var previousSession = MetaGameSession.Current;
            if (previousSession != null)
                throw new InvalidOperationException("Lobby capture refuses to replace an existing MetaGameSession.");

            var activeScene = SceneManager.GetActiveScene();
            var sourceScene = FindLoadedLobby();
            var openedSource = !sourceScene.IsValid();
            var replaceUntitledBatchScene = openedSource && Application.isBatchMode && string.IsNullOrEmpty(activeScene.path);
            var temporaryScene = default(Scene);
            GameObject clonedCanvas = null;
            GameObject cameraObject = null;
            MetaGameSession temporarySession = null;
            try
            {
                if (openedSource)
                    sourceScene = EditorSceneManager.OpenScene(ScenePath,
                        replaceUntitledBatchScene ? OpenSceneMode.Single : OpenSceneMode.Additive);
                var sourceCanvas = sourceScene.GetRootGameObjects().SingleOrDefault(root => root.name == "Lobby Canvas");
                if (sourceCanvas == null) throw new InvalidOperationException("Lobby Canvas is missing from the production Lobby scene.");

                SceneManager.SetActiveScene(sourceScene);
                temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(temporaryScene);
                clonedCanvas = UnityEngine.Object.Instantiate(sourceCanvas);
                clonedCanvas.name = "Lobby Canvas Capture Clone";
                SceneManager.MoveGameObjectToScene(clonedCanvas, temporaryScene);
                var captureCanvasRect = clonedCanvas.GetComponent<RectTransform>();
                captureCanvasRect.localPosition = Vector3.zero;
                captureCanvasRect.localRotation = Quaternion.identity;
                captureCanvasRect.localScale = Vector3.one;
                cameraObject = new GameObject("Lobby Capture Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .025f, .018f, 1f);
                camera.orthographic = true;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                temporarySession = CreateCaptureSession();
                var bootstrap = clonedCanvas.GetComponent<LobbyBootstrap>();
                var root = clonedCanvas.GetComponent<LobbyRootView>();
                if (bootstrap == null || root == null || !root.HasRequiredBindings)
                    throw new InvalidOperationException("Cloned Lobby Canvas has incomplete authored bindings.");
                bootstrap.enabled = false;
                bootstrap.BindAuthoredView(temporarySession);

                var outputDirectory = Path.Combine(ProjectRoot(), CaptureDirectory);
                Directory.CreateDirectory(outputDirectory);
                foreach (var request in CapturePlan)
                {
                    ShowCapturePage(root, request.Page);
                    CaptureCamera(camera, request.Resolution, Path.Combine(outputDirectory, request.FileName));
                }
                Debug.Log($"Lobby capture completed {CapturePlan.Count} authored-scene PNGs in {outputDirectory}.");
            }
            finally
            {
                if (temporarySession != null) UnityEngine.Object.DestroyImmediate(temporarySession.gameObject);
                SetCurrentSession(previousSession);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (clonedCanvas != null) UnityEngine.Object.DestroyImmediate(clonedCanvas);
                if (temporaryScene.IsValid() && temporaryScene.isLoaded) EditorSceneManager.CloseScene(temporaryScene, true);
                if (openedSource && !replaceUntitledBatchScene && sourceScene.IsValid() && sourceScene.isLoaded)
                    EditorSceneManager.CloseScene(sourceScene, true);
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
            }

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
                CapturePreview();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static MetaGameSession CreateCaptureSession()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(28);
            data.Coins = 50000;
            data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()] = 6;
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 6;
            var styles = WeaponMasteryCatalog.StylesFor(WeaponId.GakgungShot);
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = styles[1].RequiredMastery;

            var sessionObject = new GameObject("Lobby Capture Meta Session");
            var session = sessionObject.AddComponent<MetaGameSession>();
            var router = typeof(MetaGameSession).GetProperty(nameof(MetaGameSession.Router))?.GetSetMethod(true);
            if (router == null) throw new InvalidOperationException("MetaGameSession router setter is unavailable for capture.");
            router.Invoke(session, new object[] { new GameSceneRouter() });
            SetCurrentSession(session);
            if (MetaGameSession.Current != session)
                throw new InvalidOperationException("Lobby capture could not establish its temporary MetaGameSession.");
            return MetaGameSession.EnsureExists(new InMemoryCaptureSaveRepository(data));
        }

        private static void SetCurrentSession(MetaGameSession session)
        {
            var setter = typeof(MetaGameSession).GetProperty(nameof(MetaGameSession.Current))?.GetSetMethod(true);
            if (setter == null) throw new InvalidOperationException("MetaGameSession Current setter is unavailable for capture.");
            setter.Invoke(null, new object[] { session });
        }

        private static void ShowCapturePage(LobbyRootView root, string page)
        {
            switch (page)
            {
                case "Home": root.Navigation.Show(LobbyPageId.Home); break;
                case "Training": root.Navigation.Show(LobbyPageId.Training); break;
                case "Patrol": root.Navigation.Show(LobbyPageId.Patrol); break;
                case "Research-ready":
                    root.Navigation.Show(LobbyPageId.Research);
                    var index = WeaponRoster.All.ToList().FindIndex(id => id.Equals(WeaponId.GakgungShot));
                    if (index < 0) throw new InvalidOperationException("Gakgung is missing from the weapon roster.");
                    root.ResearchPresenter.SelectWeaponForTests(index);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(page), page, "Unknown Lobby capture page.");
            }
        }

        private static void ForceCaptureLayout(GameObject clonedCanvas)
        {
            foreach (var text in clonedCanvas.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.ForceMeshUpdate(true, true);
            foreach (var rect in clonedCanvas.GetComponentsInChildren<RectTransform>(true))
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
        }

        private static void CaptureCamera(Camera camera, Vector2Int resolution, string outputPath)
        {
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = camera.aspect;
            var previousOrthographicSize = camera.orthographicSize;
            var canvases = camera.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).ToArray();
            var modes = canvases.Select(canvas => canvas.renderMode).ToArray();
            var cameras = canvases.Select(canvas => canvas.worldCamera).ToArray();
            var distances = canvases.Select(canvas => canvas.planeDistance).ToArray();
            var canvasRects = canvases.Select(canvas => canvas.GetComponent<RectTransform>()).ToArray();
            var positions = canvasRects.Select(rect => rect.localPosition).ToArray();
            var rotations = canvasRects.Select(rect => rect.localRotation).ToArray();
            var scales = canvasRects.Select(rect => rect.localScale).ToArray();
            var sizes = canvasRects.Select(rect => rect.sizeDelta).ToArray();
            var scalers = canvases.Select(canvas => canvas.GetComponent<CanvasScaler>()).ToArray();
            var scalerStates = scalers.Select(scaler => scaler != null && scaler.enabled).ToArray();
            try
            {
                renderTexture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.aspect = (float)resolution.x / resolution.y;
                var logicalSize = LogicalCaptureSize(resolution);
                camera.orthographicSize = logicalSize.y * .5f;
                for (var index = 0; index < canvases.Length; index++)
                {
                    var canvas = canvases[index];
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = camera;
                    var rect = canvasRects[index];
                    rect.localPosition = Vector3.zero;
                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;
                    rect.sizeDelta = logicalSize;
                    if (scalers[index] != null) scalers[index].enabled = false;
                }
                if (canvases.Length == 0) throw new InvalidOperationException("Lobby capture clone contains no Canvas.");
                ForceCaptureLayout(canvases[0].gameObject);
                texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false);
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                if (new FileInfo(outputPath).Length == 0) throw new InvalidOperationException("Lobby capture PNG was empty.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                camera.orthographicSize = previousOrthographicSize;
                RenderTexture.active = previousActive;
                for (var index = 0; index < canvases.Length; index++)
                {
                    if (canvases[index] == null) continue;
                    canvases[index].renderMode = modes[index];
                    canvases[index].worldCamera = cameras[index];
                    canvases[index].planeDistance = distances[index];
                    canvasRects[index].localPosition = positions[index];
                    canvasRects[index].localRotation = rotations[index];
                    canvasRects[index].localScale = scales[index];
                    canvasRects[index].sizeDelta = sizes[index];
                    if (scalers[index] != null) scalers[index].enabled = scalerStates[index];
                }
                Canvas.ForceUpdateCanvases();
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                if (renderTexture != null) UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Vector2 LogicalCaptureSize(Vector2Int resolution)
        {
            if (resolution.x <= 0 || resolution.y <= 0)
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Capture resolution must be positive.");
            const float logicalWidth = 720f;
            return new Vector2(logicalWidth, logicalWidth * resolution.y / resolution.x);
        }

        private static string ProjectRoot() => Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Unable to resolve the project root.");

        private sealed class InMemoryCaptureSaveRepository : ISaveRepository
        {
            private readonly SaveDataV1 data;
            public InMemoryCaptureSaveRepository(SaveDataV1 source) => data = source.Copy();
            public LoadResult Load() => new LoadResult(data.Copy(), LoadSource.Defaults, SaveError.None);
            public SaveResult Save(SaveDataV1 value)
            {
                data.CopyFrom(value);
                return new SaveResult(true, SaveError.None);
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
