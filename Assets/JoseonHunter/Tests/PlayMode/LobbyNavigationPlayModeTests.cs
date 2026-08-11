using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Save;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class LobbyNavigationPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator NavigationStartsAtHomeAndBackButtonsReturnToHome()
        {
            var presenter = new GameObject("Navigation").AddComponent<LobbyNavigationPresenter>();
            var homePage = new GameObject("Home");
            var trainingPage = new GameObject("Training");
            var patrolPage = new GameObject("Patrol");
            var researchPage = new GameObject("Research");
            var trainingMenuButton = CreateButton("Training Menu");
            var patrolMenuButton = CreateButton("Patrol Menu");
            var researchMenuButton = CreateButton("Research Menu");
            var trainingBackButton = CreateButton("Training Back");
            var patrolBackButton = CreateButton("Patrol Back");
            var researchBackButton = CreateButton("Research Back");

            presenter.Initialize(
                homePage, trainingPage, patrolPage, researchPage,
                trainingMenuButton, patrolMenuButton, researchMenuButton,
                trainingBackButton, patrolBackButton, researchBackButton);
            yield return null;

            AssertPage(presenter, LobbyPageId.Home, homePage, trainingPage, patrolPage, researchPage);

            trainingMenuButton.onClick.Invoke();
            AssertPage(presenter, LobbyPageId.Training, homePage, trainingPage, patrolPage, researchPage);
            trainingBackButton.onClick.Invoke();
            AssertPage(presenter, LobbyPageId.Home, homePage, trainingPage, patrolPage, researchPage);

            patrolMenuButton.onClick.Invoke();
            AssertPage(presenter, LobbyPageId.Patrol, homePage, trainingPage, patrolPage, researchPage);
            patrolBackButton.onClick.Invoke();
            AssertPage(presenter, LobbyPageId.Home, homePage, trainingPage, patrolPage, researchPage);

            researchMenuButton.onClick.Invoke();
            AssertPage(presenter, LobbyPageId.Research, homePage, trainingPage, patrolPage, researchPage);
            researchBackButton.onClick.Invoke();
            AssertPage(presenter, LobbyPageId.Home, homePage, trainingPage, patrolPage, researchPage);
        }

        [UnityTest]
        public IEnumerator AuthoredHomeCardsAndBackButtonsSurviveTenPointerRoundTrips()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var root = Object.FindObjectsByType<LobbyRootView>(FindObjectsInactive.Include)
                .Single();
            var eventSystem = EventSystem.current;
            Assert.That(root.HasRequiredBindings, Is.True);
            var raycaster = root.GetComponent<GraphicRaycaster>();
            Assert.That(raycaster, Is.Not.Null);
            Assert.That(eventSystem, Is.Not.Null);

            var trainingBack = root.TrainingView.GetComponentInChildren<LobbyPageHeaderView>(true).BackButton;
            var patrolBack = root.PatrolView.PageHeader.BackButton;
            var researchBack = root.ResearchView.GetComponentInChildren<LobbyPageHeaderView>(true).BackButton;
            var pages = new[]
            {
                root.Home.gameObject, root.TrainingView.gameObject,
                root.PatrolView.gameObject, root.ResearchView.gameObject
            };
            foreach (var card in new[] { root.Home.TrainingCard, root.Home.PatrolCard, root.Home.ResearchCard })
            {
                Assert.That(card.HasRequiredBindings, Is.True, card.name);
                Assert.That(card.InputSurface.gameObject.activeInHierarchy, Is.True, card.name);
                Assert.That(card.InputSurface.canvas, Is.SameAs(root.GetComponent<Canvas>()), card.name);
                Assert.That(card.InputSurface.rectTransform.rect.width, Is.GreaterThan(0f), card.name);
                Assert.That(card.InputSurface.rectTransform.rect.height, Is.GreaterThan(0f), card.name);
                Assert.That(card.InputSurface.canvasRenderer.cull, Is.False, card.name);
            }

            AssertPage(root.Navigation, LobbyPageId.Home, pages);
            for (var round = 0; round < 10; round++)
            {
                ClickThroughAuthoredSurface(root.Home.TrainingCard.Button, root.Home.TrainingCard.InputSurface, eventSystem, raycaster);
                AssertPage(root.Navigation, LobbyPageId.Training, pages);
                ClickThroughAuthoredSurface(trainingBack, trainingBack.targetGraphic, eventSystem, raycaster);
                AssertPage(root.Navigation, LobbyPageId.Home, pages);

                ClickThroughAuthoredSurface(root.Home.PatrolCard.Button, root.Home.PatrolCard.InputSurface, eventSystem, raycaster);
                AssertPage(root.Navigation, LobbyPageId.Patrol, pages);
                ClickThroughAuthoredSurface(patrolBack, patrolBack.targetGraphic, eventSystem, raycaster);
                AssertPage(root.Navigation, LobbyPageId.Home, pages);

                ClickThroughAuthoredSurface(root.Home.ResearchCard.Button, root.Home.ResearchCard.InputSurface, eventSystem, raycaster);
                AssertPage(root.Navigation, LobbyPageId.Research, pages);
                ClickThroughAuthoredSurface(researchBack, researchBack.targetGraphic, eventSystem, raycaster);
                AssertPage(root.Navigation, LobbyPageId.Home, pages);
            }
        }

        [UnityTest]
        public IEnumerator ReinitializingNavigationPreservesUnownedListenersAndAddsOneTransition()
        {
            var data = SaveDataV1.CreateDefaults();
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var expectedSelection = session.ActiveStageSelection;
            var expectedLoadout = session.ActiveLoadout;
            var presenter = new GameObject("Navigation").AddComponent<LobbyNavigationPresenter>();
            var homePage = new GameObject("Home");
            var trainingPage = new GameObject("Training");
            var patrolPage = new GameObject("Patrol");
            var researchPage = new GameObject("Research");
            var trainingMenuButton = CreateButton("Training Menu");
            var patrolMenuButton = CreateButton("Patrol Menu");
            var researchMenuButton = CreateButton("Research Menu");
            var trainingBackButton = CreateButton("Training Back");
            var patrolBackButton = CreateButton("Patrol Back");
            var researchBackButton = CreateButton("Research Back");
            var unrelatedInvocations = 0;
            trainingMenuButton.onClick.AddListener(() => unrelatedInvocations++);

            presenter.Initialize(homePage, trainingPage, patrolPage, researchPage,
                trainingMenuButton, patrolMenuButton, researchMenuButton,
                trainingBackButton, patrolBackButton, researchBackButton);
            presenter.Initialize(homePage, trainingPage, patrolPage, researchPage,
                trainingMenuButton, patrolMenuButton, researchMenuButton,
                trainingBackButton, patrolBackButton, researchBackButton);
            yield return null;

            var activations = trainingPage.AddComponent<DeactivateWhenEnabled>();
            activations.Record = true;
            trainingMenuButton.onClick.Invoke();

            Assert.That(unrelatedInvocations, Is.EqualTo(1));
            Assert.That(activations.Count, Is.EqualTo(1),
                "One click after repeated initialization must dispatch one owned transition.");
            Assert.That(presenter.CurrentPage, Is.EqualTo(LobbyPageId.Training));
            Assert.That(session.ActiveStageSelection, Is.EqualTo(expectedSelection));
            Assert.That(session.ActiveLoadout.StartingWeapon, Is.EqualTo(expectedLoadout.StartingWeapon));
        }

        [Test]
        public void NavigationRejectsIncompleteWiringBeforeBindingOrShowing()
        {
            var presenter = new GameObject("Navigation").AddComponent<LobbyNavigationPresenter>();
            var page = new GameObject("Page");
            var button = CreateButton("Button");

            var initializeError = Assert.Throws<InvalidOperationException>(() => presenter.Initialize(
                null, page, page, page, button, button, button, button, button, button));
            Assert.That(initializeError.Message, Does.Contain("homePage"));

            var showError = Assert.Throws<InvalidOperationException>(() => presenter.Show(LobbyPageId.Home));
            Assert.That(showError.Message, Does.Contain("homePage"));
        }

        [Test]
        public void AuthoredLobbyPresentersExposeNoRuntimeBuiltCompatibilityApis()
        {
            Assert.That(typeof(LobbyNavigationPresenter).GetMethod("Initialize", new[]
                {
                    typeof(GameObject), typeof(GameObject), typeof(GameObject), typeof(Button), typeof(Button),
                    typeof(Button)
                }), Is.Null);

            foreach (var presenterType in new[]
                     {
                         typeof(PatrolPresenter), typeof(CommonTrainingPresenter), typeof(WeaponResearchPresenter)
                     })
            {
                Assert.That(presenterType.GetMethod("Initialize", new[] { typeof(MetaGameSession), typeof(System.Action) }),
                    Is.Null, presenterType.Name);
                Assert.That(presenterType.GetMethod("InitializeLegacyRuntimeBuiltView"), Is.Null, presenterType.Name);
                Assert.That(presenterType.GetMethod("Build"), Is.Null, presenterType.Name);
            }
        }

        [UnityTest]
        public IEnumerator HeaderShowsCoinSpriteBesideBareNumber()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 155;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var commonHeader = GameObject.Find("Common Header")?.transform;
            var coinIcon = commonHeader?.Find("Currency Capsule/Coin Icon")?.GetComponent<Image>();
            var coinText = commonHeader?.Find("Currency Capsule/Coin Text")?.GetComponent<TMPro.TMP_Text>();

            Assert.That(coinIcon, Is.Not.Null);
            Assert.That(coinIcon.sprite, Is.Not.Null);
            Assert.That(coinIcon.preserveAspect, Is.True);
            Assert.That(coinText, Is.Not.Null);
            Assert.That(coinText.text, Is.EqualTo("155"));
            Assert.That(coinText.text, Does.Not.Contain("엽전"));
            Assert.That(coinText.text, Does.Not.Contain("냥"));
        }

        [UnityTest]
        public IEnumerator HeaderShowsAccountLevelNameAndCurrentExperienceProgress()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = 250;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var commonHeader = GameObject.Find("Common Header")?.transform;
            var profile = commonHeader?.Find("Account Profile");
            var level = profile?.Find("Account Level")?.GetComponent<TMPro.TMP_Text>();
            var accountName = profile?.Find("Account Name")?.GetComponent<TMPro.TMP_Text>();
            var experienceText = profile?.Find("Account Experience/Account Experience Text")
                ?.GetComponent<TMPro.TMP_Text>();
            var experienceFill = profile?.Find("Account Experience/Account Experience Fill")?.GetComponent<Image>();

            Assert.That(level, Is.Not.Null);
            Assert.That(level.text, Is.EqualTo("3"));
            Assert.That(accountName.text, Is.EqualTo("요괴 사냥꾼"));
            Assert.That(experienceText.text, Is.EqualTo("8 / 188"));
            Assert.That(experienceFill.fillAmount, Is.EqualTo(8f / 188f).Within(.001f));
            Assert.That(GameObject.Find("Lobby Title"), Is.Null);
        }

        [UnityTest]
        public IEnumerator HeaderSeparatesAccountProfileCurrencyAndSettingsControls()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 155;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var header = GameObject.Find("Common Header")?.transform;
            Assert.That(header, Is.Not.Null);
            var profile = header.Find("Account Profile")?.GetComponent<RectTransform>();
            var currency = header.Find("Currency Capsule")?.GetComponent<RectTransform>();
            var settings = header.Find("Settings Button")?.GetComponent<RectTransform>();

            Assert.That(profile, Is.Not.Null);
            Assert.That(currency, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
            Assert.That(profile.anchorMax.x, Is.LessThanOrEqualTo(currency.anchorMin.x));
            Assert.That(currency.anchorMax.x, Is.LessThanOrEqualTo(settings.anchorMin.x));
            Assert.That(header.Find("Currency Capsule/Coin Icon"), Is.Not.Null);
            Assert.That(header.Find("Currency Capsule/Coin Text"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator LobbySettingsUsesAReadableSpriteInsteadOfComposedGearBlocks()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settings = GameObject.Find("Settings Button")?.transform;
            Assert.That(settings, Is.Not.Null);
            var icon = settings.Find("Settings Icon")?.GetComponent<Image>();
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.sprite, Is.Not.Null);
            Assert.That(icon.sprite.name, Is.EqualTo("icon_settings"));
            Assert.That(settings.Find("Gear Tooth 0"), Is.Null);
            Assert.That(settings.Find("Gear Hub"), Is.Null);
        }

        [UnityTest]
        public IEnumerator LobbyGearOpensPersistentAudioControls()
        {
            var data = SaveDataV1.CreateDefaults();
            data.MusicVolume = .4f;
            data.SoundEffectVolume = .7f;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settingsButton = GameObject.Find("Settings Button")?.GetComponent<Button>();
            Assert.That(settingsButton, Is.Not.Null);
            settingsButton.onClick.Invoke();
            yield return null;

            var sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);
            var music = sliders.Single(slider => slider.name == "Music Volume Slider");
            var effects = sliders.Single(slider => slider.name == "Sound Effect Volume Slider");
            Assert.That(music.gameObject.activeInHierarchy, Is.True);
            Assert.That(music.value, Is.EqualTo(.4f).Within(.001f));
            Assert.That(effects.value, Is.EqualTo(.7f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator LobbyAudioSettingsCloseButtonHidesSerializedOverlay()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settingsButton = GameObject.Find("Settings Button")?.GetComponent<Button>();
            Assert.That(settingsButton, Is.Not.Null);
            settingsButton.onClick.Invoke();
            yield return null;

            var overlay = GameObject.Find("Settings Overlay");
            var closeButton = GameObject.Find("Close Audio Settings")?.GetComponent<Button>();
            Assert.That(overlay, Is.Not.Null);
            Assert.That(closeButton, Is.Not.Null);
            Assert.That(overlay.activeInHierarchy, Is.True);

            closeButton.onClick.Invoke();
            yield return null;

            Assert.That(overlay.activeSelf, Is.False,
                "The serialized lobby audio overlay must subscribe its close request to LobbyBootstrap.");
        }

        [UnityTest]
        public IEnumerator LobbyAudioSliderHandlesStayCompactAndInsideTheirTracks()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settingsButton = GameObject.Find("Settings Button")?.GetComponent<Button>();
            Assert.That(settingsButton, Is.Not.Null);
            settingsButton.onClick.Invoke();
            yield return null;

            foreach (var slider in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include))
            {
                var track = slider.GetComponent<RectTransform>();
                var handle = slider.handleRect;
                Assert.That(handle.sizeDelta.x, Is.LessThanOrEqualTo(24f), slider.name);
                Assert.That(handle.sizeDelta.y, Is.LessThanOrEqualTo(28f), slider.name);
                Assert.That(handle.parent.GetComponent<RectTransform>().offsetMin.x,
                    Is.GreaterThanOrEqualTo(12f), slider.name);
                Assert.That(handle.parent.GetComponent<RectTransform>().offsetMax.x,
                    Is.LessThanOrEqualTo(-12f), slider.name);
                Assert.That(track.rect.width, Is.GreaterThan(handle.rect.width), slider.name);
            }
        }

        private static Button CreateButton(string name) =>
            new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button))
                .GetComponent<Button>();

        private static void ClickThroughAuthoredSurface(Button button, Graphic inputSurface, EventSystem eventSystem,
            GraphicRaycaster raycaster)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(inputSurface, Is.Not.Null, button.name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, button.name);
            Assert.That(inputSurface.enabled, Is.True, button.name);
            Assert.That(inputSurface.raycastTarget, Is.True, button.name);
            Assert.That(inputSurface.transform.IsChildOf(button.transform) || inputSurface.gameObject == button.gameObject,
                Is.True, button.name);
            var canvasTransform = raycaster.GetComponent<RectTransform>();
            var authoredCanvasScale = canvasTransform.localScale;
            var canvas = raycaster.GetComponent<Canvas>();
            var authoredRenderMode = canvas.renderMode;
            var authoredWorldCamera = canvas.worldCamera;
            var authoredPlaneDistance = canvas.planeDistance;
            var camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            var authoredTargetTexture = camera != null ? camera.targetTexture : null;
            RenderTexture renderTexture = null;
            try
            {
                // A batch Test Runner does not perform the GameView render pass that normalizes a root Overlay Canvas.
                // Render only this runtime instance once while asking the production GraphicRaycaster for its hit.
                Assert.That(camera, Is.Not.Null, "The authored lobby needs its Main Camera for the batch raycast check.");
                if (authoredCanvasScale == Vector3.zero) canvasTransform.localScale = Vector3.one;
                renderTexture = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                camera.targetTexture = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                Canvas.ForceUpdateCanvases();
                var pointer = new PointerEventData(eventSystem)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera,
                        inputSurface.rectTransform.TransformPoint(inputSurface.rectTransform.rect.center))
                };
                var hits = new List<RaycastResult>();
                raycaster.Raycast(pointer, hits);
                var hit = hits.FirstOrDefault();
                Assert.That(hit.gameObject, Is.SameAs(inputSurface.gameObject),
                    $"'{button.name}' input surface was not the top production GraphicRaycaster hit at its visual center. " +
                    $"Depth: {inputSurface.depth}; Screen: {Screen.width}x{Screen.height}; " +
                    $"Pointer: {pointer.position}; Hits: {string.Join(", ", hits.Select(result => result.gameObject.name))}");
                Assert.That(ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler),
                    Is.Not.Null, $"'{button.name}' did not handle the pointer click.");
            }
            finally
            {
                if (camera != null) camera.targetTexture = authoredTargetTexture;
                canvas.renderMode = authoredRenderMode;
                canvas.worldCamera = authoredWorldCamera;
                canvas.planeDistance = authoredPlaneDistance;
                canvasTransform.localScale = authoredCanvasScale;
                Canvas.ForceUpdateCanvases();
                if (renderTexture != null) RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void AssertPage(LobbyNavigationPresenter presenter, LobbyPageId expected,
            params GameObject[] pages)
        {
            Assert.That(presenter.CurrentPage, Is.EqualTo(expected));
            Assert.That(pages.Count(page => page.activeSelf), Is.EqualTo(1));
            Assert.That(pages[(int)expected].activeSelf, Is.True);
        }

        private sealed class DeactivateWhenEnabled : MonoBehaviour
        {
            public bool Record { get; set; }
            public int Count { get; private set; }

            private void OnEnable()
            {
                if (!Record) return;
                Count++;
                gameObject.SetActive(false);
            }
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
