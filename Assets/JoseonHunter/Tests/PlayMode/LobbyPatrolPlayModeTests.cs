using System;
using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Save;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class LobbyPatrolPlayModeTests
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
            foreach (var harness in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                         .Where(item => item.name == "Authored Patrol Harness").ToArray())
                Object.DestroyImmediate(harness.gameObject);
        }

        [UnityTest]
        public IEnumerator CyclingCurrentWeaponImmediatelySavesActiveLoadout()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<PatrolPresenter>();

            presenter.SelectStartingWeaponForTests(WeaponId.GakgungShot);

            var active = MetaGameSession.Current.Data.ActivePatrolLoadoutIndex;
            Assert.That(MetaGameSession.Current.Data.PatrolLoadouts[active].StartingWeaponId,
                Is.EqualTo(WeaponId.GakgungShot.Value));
            Assert.That(GameObject.Find("Previous Preset"), Is.Null);
            Assert.That(GameObject.Find("Next Preset"), Is.Null);
            Assert.That(GameObject.Find("Save Preset"), Is.Null);
        }

        [UnityTest]
        public IEnumerator PatrolHomePresentsStageAndLargePrimaryAction()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var stage = GameObject.Find("Stage Name");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.GetComponent<TMPro.TMP_Text>().text, Does.Contain("귀곡 들판"));

            var start = GameObject.Find("Start Patrol");
            Assert.That(start, Is.Not.Null);
            Assert.That(start.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("출전 시작"));
            Assert.That(start.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(76f));
        }

        [Test]
        public void AuthoredPatrolPageViewTypeExists()
        {
            var viewType = typeof(PatrolPresenter).Assembly.GetType(
                "JoseonHunter.Presentation.UI.Lobby.Views.PatrolPageView");

            Assert.That(viewType, Is.Not.Null,
                "Patrol must expose a strict authored view instead of discovering runtime-built controls.");
        }

        [Test]
        public void AuthoredPatrolViewSurfaceDeclaresTypedBindings()
        {
            var assembly = typeof(PatrolPresenter).Assembly;
            var pageType = assembly.GetType("JoseonHunter.Presentation.UI.Lobby.Views.PatrolPageView");
            var selectorType = assembly.GetType(
                "JoseonHunter.Presentation.UI.Lobby.Views.LobbyWeaponSelectorCardView");

            Assert.That(selectorType, Is.Not.Null, "Weapon selector module must expose typed authored bindings.");
            foreach (var propertyName in new[]
                     {
                         "PageHeader", "StageName", "StageStatus", "PreviousStageButton", "NextStageButton",
                         "HeroImage", "NormalDifficulty", "OmenDifficulty", "GreatOmenDifficulty",
                         "WeaponSelector", "Feedback", "WeaponSelectionOverlay", "CloseWeaponSelectionButton",
                         "StartButton", "HasRequiredBindings"
                     })
                Assert.That(pageType.GetProperty(propertyName), Is.Not.Null, propertyName);

            foreach (var propertyName in new[] { "Button", "Background", "Icon", "Caption", "WeaponName", "Chevron" })
                Assert.That(selectorType.GetProperty(propertyName), Is.Not.Null, propertyName);

            var difficultyType = typeof(LobbyDifficultyCardView);
            foreach (var propertyName in new[]
                     {
                         "Button", "Background", "Label", "LockSlash", "LockIcon", "LockSlashConstraint"
                     })
                Assert.That(difficultyType.GetProperty(propertyName), Is.Not.Null, propertyName);
        }

        [UnityTest]
        public IEnumerator AuthoredInitializeIsIdempotentAndPreservesOwnedListenerBoundaries()
        {
            var session = MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            var harness = CreateAuthoredHarness();
            var presenterType = typeof(PatrolPresenter);
            var configure = presenterType.GetMethod("ConfigureView");
            var initialize = presenterType.GetMethod("InitializeAuthored");
            Assert.That(configure, Is.Not.Null, "PatrolPresenter must expose the authored-view configuration seam.");
            Assert.That(initialize, Is.Not.Null, "PatrolPresenter must expose strict authored initialization.");

            var initialIds = harness.AllTransformIds();
            var externalNextClicks = 0;
            var externalWeaponClicks = 0;
            harness.View.NextStageButton.onClick.AddListener(() => externalNextClicks++);
            harness.GakgungOption.onClick.AddListener(() => externalWeaponClicks++);

            configure.Invoke(harness.Presenter, new object[] { harness.View });
            initialize.Invoke(harness.Presenter, new object[] { session, null });
            initialize.Invoke(harness.Presenter, new object[] { session, null });
            yield return null;

            Assert.That(harness.AllTransformIds(), Is.EqualTo(initialIds),
                "Strict authored initialization must not create or replace controls.");
            foreach (var controlName in AuthoredPatrolHarness.UniqueControlNames)
                Assert.That(harness.Root.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name == controlName), Is.EqualTo(1), controlName);

            harness.View.NextStageButton.onClick.Invoke();
            Assert.That(externalNextClicks, Is.EqualTo(1), "Presenter must preserve unowned listeners.");
            Assert.That(harness.View.StageName.text, Does.StartWith("2장 ·"),
                "Exactly one presenter listener must advance exactly one stage.");

            harness.View.PreviousStageButton.onClick.Invoke();
            harness.View.OmenDifficulty.Button.onClick.Invoke();
            Assert.That(harness.View.OmenDifficulty.Button.interactable, Is.True,
                "Locked difficulty cards must remain selectable so their unlock feedback can be shown.");
            Assert.That(harness.View.Feedback.text, Is.EqualTo("이 장 보통 승리 시 해금"));
            Assert.That(session.ActiveStageSelection,
                Is.EqualTo(new StageSelection(StageId.GwigokField, StageDifficulty.Normal)));

            harness.GakgungOption.onClick.Invoke();
            Assert.That(externalWeaponClicks, Is.EqualTo(1));
            Assert.That(session.ActiveLoadout.StartingWeapon, Is.EqualTo(WeaponId.GakgungShot));
            Assert.That(harness.View.WeaponSelector.WeaponName.text, Is.EqualTo("각궁"));

            Assert.That(harness.ActionLabel.text, Is.EqualTo("출전 시작"));
            Assert.That(harness.View.PageHeader.Title.text, Is.EqualTo("출전"));
            Assert.That(harness.View.WeaponSelector.Caption.text, Is.EqualTo("시작 무기"));
            Assert.That(harness.View.NormalDifficulty.Label.text, Is.EqualTo("보통"));
            Assert.That(harness.View.OmenDifficulty.Label.text, Is.EqualTo("흉조"));
            Assert.That(harness.View.GreatOmenDifficulty.Label.text, Is.EqualTo("대흉"));
            Assert.That(harness.View.StartButton.targetGraphic.GetComponent<Image>().sprite.name,
                Is.EqualTo("primary_red_button"));
            Assert.That(harness.View.NormalDifficulty.Background.sprite.name, Is.EqualTo("difficulty_selected"));
            Assert.That(harness.View.OmenDifficulty.Background.sprite.name, Is.EqualTo("difficulty_locked"));
            Assert.That(harness.View.GreatOmenDifficulty.Background.sprite.name, Is.EqualTo("difficulty_locked"));
            AssertDifficultyCardsHaveEqualSizeAndInternalLocks(harness.View);
        }

        [Test]
        public void AuthoredInitializeRejectsMissingLockBindingBeforeMutatingHierarchyOrListeners()
        {
            var session = MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            var harness = CreateAuthoredHarness();
            var externalNextClicks = 0;
            harness.View.NextStageButton.onClick.AddListener(() => externalNextClicks++);
            harness.Presenter.ConfigureView(harness.View);
            harness.Presenter.InitializeAuthored(session, null);

            var initialIds = harness.AllTransformIds();
            var greatOmen = harness.View.GreatOmenDifficulty;
            var lockSlash = greatOmen.Button.transform.Find("Lock Slash").GetComponent<Image>();
            var constraint = lockSlash.GetComponent<LockSlashConstraint>();
            ConfigureDifficultyBindings(greatOmen, greatOmen.Button, greatOmen.Label, lockSlash, null, constraint);

            Assert.Throws<InvalidOperationException>(() => harness.Presenter.InitializeAuthored(session, null),
                "Strict authored initialization must reject an incomplete lock decoration before rendering it.");
            Assert.That(harness.AllTransformIds(), Is.EqualTo(initialIds),
                "Failed strict initialization must not repair or replace authored hierarchy objects.");

            harness.View.NextStageButton.onClick.Invoke();
            Assert.That(externalNextClicks, Is.EqualTo(1), "Unowned listeners must remain attached.");
            Assert.That(harness.View.StageName.text, Does.StartWith("2장 ·"),
                "The previously owned presenter listener must remain attached after rejected reconfiguration.");
        }

        [UnityTest]
        public IEnumerator AuthoredPatrolPreservesDifficultySaveAndGameplayRouting()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var harness = CreateAuthoredHarness();
            var configure = typeof(PatrolPresenter).GetMethod("ConfigureView");
            var initialize = typeof(PatrolPresenter).GetMethod("InitializeAuthored");
            Assert.That(configure, Is.Not.Null);
            Assert.That(initialize, Is.Not.Null);
            configure.Invoke(harness.Presenter, new object[] { harness.View });
            initialize.Invoke(harness.Presenter, new object[] { session, null });

            harness.View.OmenDifficulty.Button.onClick.Invoke();
            Assert.That(session.ActiveStageSelection,
                Is.EqualTo(new StageSelection(StageId.GwigokField, StageDifficulty.Omen)));
            Assert.That(harness.View.OmenDifficulty.Background.sprite.name, Is.EqualTo("difficulty_selected"));
            Assert.That(harness.View.NormalDifficulty.Background.sprite.name, Is.EqualTo("difficulty_idle"));

            string firstTransitionTarget = null;
            void CaptureFirstTransition(Scene scene, LoadSceneMode _) => firstTransitionTarget ??= scene.name;
            SceneManager.sceneLoaded += CaptureFirstTransition;
            harness.GakgungOption.onClick.Invoke();
            harness.View.StartButton.onClick.Invoke();
            var pendingDestination = session.ConsumePendingDestination("Fallback");
            session.SetPendingDestination("Lobby");
            yield return null;
            var bootstrapIntentStarted = session.Router.IsRouting || firstTransitionTarget == "Bootstrap";
            if (harness.Root != null) Object.DestroyImmediate(harness.Root);
            while (session.Router.IsRouting) yield return null;
            yield return null;
            while (SceneManager.GetActiveScene().name != "Lobby") yield return null;
            var loadingPresenter = Object.FindAnyObjectByType<BootstrapLoadingPresenter>();
            if (loadingPresenter != null) Object.DestroyImmediate(loadingPresenter.gameObject);
            SceneManager.sceneLoaded -= CaptureFirstTransition;

            Assert.That(session.ActiveLoadout.StartingWeapon, Is.EqualTo(WeaponId.GakgungShot));
            Assert.That(pendingDestination, Is.EqualTo("Gameplay"),
                "Patrol must preserve Gameplay as the exact post-loading destination.");
            Assert.That(bootstrapIntentStarted, Is.True,
                "Patrol must start the existing Bootstrap router transition before yielding control.");
            Assert.That(firstTransitionTarget, Is.EqualTo("Bootstrap"),
                "The first transition must target Bootstrap; Bootstrap-to-Gameplay arrival is covered by final integration.");
        }

        [UnityTest]
        public IEnumerator PatrolHomeCentersTransparentPixelHeroAndCompactWeaponSelector()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var hero = GameObject.Find("Patrol Hero")?.GetComponent<Image>();
            var shadow = GameObject.Find("Patrol Hero Shadow")?.GetComponent<PixelOvalGraphic>();
            var selector = GameObject.Find("Starting Weapon Selector")?.GetComponent<Button>();

            Assert.That(hero, Is.Not.Null);
            Assert.That(hero.sprite, Is.Not.Null);
            Assert.That(hero.preserveAspect, Is.True);
            Assert.That(hero.transform.parent.name, Is.EqualTo("Patrol Panel"));
            var contentPanel = GameObject.Find("Patrol Panel").GetComponent<Image>();
            Assert.That(contentPanel.sprite, Is.Not.Null,
                "The patrol content panel must retain the approved thin content border.");
            Assert.That(contentPanel.sprite.name,
                Is.EqualTo("thin_outer_frame"),
                "The patrol content panel must retain the approved thin content border.");
            Assert.That(shadow, Is.Not.Null);
            Assert.That(shadow.color.a, Is.InRange(.08f, .28f));
            Assert.That(shadow.transform.GetSiblingIndex(), Is.LessThan(hero.transform.GetSiblingIndex()));
            Assert.That(selector, Is.Not.Null);
            Assert.That(GameObject.Find("Previous Weapon"), Is.Null);
            Assert.That(GameObject.Find("Next Weapon"), Is.Null);
            Assert.That(GameObject.Find("Current Weapon Icon"), Is.Null);
        }

        [UnityTest]
        public IEnumerator PatrolUsesStageArrowsPremiumCardsAndHeroFrame()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            Assert.That(GameObject.Find("Stage Plaque").GetComponent<Image>().sprite.name,
                Is.EqualTo("stage_title_plate"));
            Assert.That(GameObject.Find("Patrol Hero Frame").GetComponent<Image>().sprite.name,
                Is.EqualTo("hero_oval_frame"));
            Assert.That(GameObject.Find("Previous Stage").transform.Find("Premium Icon")
                .GetComponent<Image>().sprite.name, Is.EqualTo("icon_previous"));
            Assert.That(GameObject.Find("Next Stage").transform.Find("Premium Icon")
                .GetComponent<Image>().sprite.name, Is.EqualTo("icon_next"));
            Assert.That(((Image)GameObject.Find("Difficulty Normal").GetComponent<Button>().targetGraphic)
                .sprite.name, Is.EqualTo("difficulty_selected"));
            Assert.That(GameObject.Find("Starting Weapon Selector").GetComponent<Image>().sprite.name,
                Is.EqualTo("weapon_selector_frame"));
        }

        [UnityTest]
        public IEnumerator PatrolUsesApprovedMockupAnchorsAndSemanticSprites()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            AssertAnchor("Stage Plaque", new Vector2(.18f, .875f), new Vector2(.82f, .95f));
            AssertAnchor("Previous Stage", new Vector2(.04f, .875f), new Vector2(.16f, .95f));
            AssertAnchor("Next Stage", new Vector2(.84f, .875f), new Vector2(.96f, .95f));
            AssertAnchor("Patrol Hero Frame", new Vector2(.30f, .55f), new Vector2(.70f, .84f));
            AssertAnchor("Difficulty Normal", new Vector2(.055f, .43f), new Vector2(.35f, .535f));
            AssertAnchor("Difficulty Omen", new Vector2(.352f, .43f), new Vector2(.648f, .535f));
            AssertAnchor("Difficulty Great Omen", new Vector2(.65f, .43f), new Vector2(.945f, .535f));
            AssertAnchor("Starting Weapon Selector", new Vector2(.12f, .285f), new Vector2(.88f, .405f));
            AssertAnchor("Start Patrol", new Vector2(.20f, .09f), new Vector2(.80f, .235f));

            Assert.That(GameObject.Find("Stage Plaque").GetComponent<Image>().sprite.name,
                Is.EqualTo("stage_title_plate"));
            Assert.That(((Image)GameObject.Find("Difficulty Normal").GetComponent<Button>().targetGraphic)
                .sprite.name, Is.EqualTo("difficulty_selected"));
            Assert.That(((Image)GameObject.Find("Difficulty Omen").GetComponent<Button>().targetGraphic)
                .sprite.name, Is.EqualTo("difficulty_locked"));
            Assert.That(((Image)FindIncludingInactive("Difficulty Great Omen").GetComponent<Button>().targetGraphic)
                .sprite.name, Is.EqualTo("difficulty_locked"));
            Assert.That(GameObject.Find("Starting Weapon Selector").GetComponent<Image>().sprite.name,
                Is.EqualTo("weapon_selector_frame"));
            Assert.That(((Image)GameObject.Find("Start Patrol").GetComponent<Button>().targetGraphic).sprite.name,
                Is.EqualTo("primary_red_button"));
            AssertDifficultyPresentation("Difficulty Normal");
            AssertDifficultyPresentation("Difficulty Omen");
            AssertDifficultyPresentation("Difficulty Great Omen");
        }

        [UnityTest]
        public IEnumerator WeaponSelectorOpensGridAndImmediatelySavesChosenWeapon()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var selector = GameObject.Find("Starting Weapon Selector").GetComponent<Button>();
            var overlay = FindIncludingInactive("Weapon Selection Overlay");
            Assert.That(overlay.activeSelf, Is.False);

            selector.onClick.Invoke();
            yield return null;
            Assert.That(overlay.activeSelf, Is.True);

            var gakgung = overlay.transform.Find("Weapon Selection Panel/Weapon Grid/Weapon Option gakgung_shot")
                ?.GetComponent<Button>();
            Assert.That(gakgung, Is.Not.Null);
            gakgung.onClick.Invoke();
            yield return null;

            var active = MetaGameSession.Current.Data.ActivePatrolLoadoutIndex;
            Assert.That(MetaGameSession.Current.Data.PatrolLoadouts[active].StartingWeaponId,
                Is.EqualTo(WeaponId.GakgungShot.Value));
            Assert.That(overlay.activeSelf, Is.False);
            Assert.That(GameObject.Find("Starting Weapon Name").GetComponent<TMPro.TMP_Text>().text,
                Is.EqualTo("각궁"));
        }

        [UnityTest]
        public IEnumerator NewAccountShowsStageOneWithKoreanDifficultyLocks()
        {
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            Screen.SetResolution(1080, 2340, false);
            try
            {
                MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
                SceneManager.LoadScene("Lobby");
                yield return null;

            Assert.That(GameObject.Find("Stage Name").GetComponent<TMPro.TMP_Text>().text,
                Does.Contain("귀곡 들판"));
            Assert.That(GameObject.Find("Difficulty Normal").GetComponentInChildren<TMPro.TMP_Text>().text,
                Does.Contain("보통"));
            Assert.That(GameObject.Find("Difficulty Omen").GetComponentInChildren<TMPro.TMP_Text>().text,
                Does.Contain("흉조"));
            Assert.That(FindIncludingInactive("Stage Status").activeSelf, Is.False);
            Assert.That(((Image)FindIncludingInactive("Difficulty Normal").GetComponent<Button>().targetGraphic)
                .sprite.name, Is.EqualTo("difficulty_selected"));
            Assert.That(((Image)FindIncludingInactive("Difficulty Omen").GetComponent<Button>().targetGraphic)
                .sprite.name, Is.EqualTo("difficulty_locked"));
            var greatOmen = FindIncludingInactive("Difficulty Great Omen");
            Assert.That(greatOmen.GetComponentInChildren<TMPro.TMP_Text>(true).text, Is.EqualTo("대흉"));
            Assert.That(greatOmen.transform.Find("Lock Slash").gameObject.activeSelf, Is.True);
            var lockSlash = greatOmen.transform.Find("Lock Slash").GetComponent<RectTransform>();
            Assert.That(lockSlash.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)),
                "production lock slash must be centered rather than stretched across transparent card margins");
            Assert.That(lockSlash.anchorMax, Is.EqualTo(new Vector2(.5f, .5f)));
            AssertVisualRectInside(lockSlash, greatOmen.GetComponent<RectTransform>());
            Assert.That(greatOmen.transform.Find("Lock Icon").GetComponent<Image>().sprite.name,
                Is.EqualTo("icon_lock"));
            var startImage = GameObject.Find("Start Patrol").GetComponent<Button>().targetGraphic as Image;
            Assert.That(startImage.sprite.name, Is.EqualTo("primary_red_button"));
            Assert.That(startImage.type, Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));

            GameObject.Find("Difficulty Omen").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("Patrol Feedback").GetComponent<TMPro.TMP_Text>().text,
                Is.EqualTo("이 장 보통 승리 시 해금"));
                Assert.That(MetaGameSession.Current.ActiveStageSelection,
                    Is.EqualTo(new StageSelection(StageId.GwigokField, StageDifficulty.Normal)));
            }
            finally
            {
                Screen.SetResolution(originalWidth, originalHeight, false);
            }
        }

        [UnityTest]
        public IEnumerator UnlockedDifficultyMovesBrightSelectionBorderToChosenButton()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var normal = FindIncludingInactive("Difficulty Normal").transform;
            var omen = FindIncludingInactive("Difficulty Omen").transform;
            Assert.That(((Image)normal.GetComponent<Button>().targetGraphic).sprite.name,
                Is.EqualTo("difficulty_selected"));
            Assert.That(((Image)omen.GetComponent<Button>().targetGraphic).sprite.name,
                Is.EqualTo("difficulty_idle"));

            omen.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(((Image)normal.GetComponent<Button>().targetGraphic).sprite.name,
                Is.EqualTo("difficulty_idle"));
            Assert.That(((Image)omen.GetComponent<Button>().targetGraphic).sprite.name,
                Is.EqualTo("difficulty_selected"));
            AssertDifficultyPresentation("Difficulty Normal");
            AssertDifficultyPresentation("Difficulty Omen");
        }

        [UnityTest]
        public IEnumerator StageOneNormalClearOpensOmenAndPlayableStageTwo()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            GameObject.Find("Difficulty Omen").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(MetaGameSession.Current.ActiveStageSelection.Difficulty,
                Is.EqualTo(StageDifficulty.Omen));

            GameObject.Find("Next Stage").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("Stage Name").GetComponent<TMPro.TMP_Text>().text,
                Does.Contain("도깨비 고갯길"));
            Assert.That(GameObject.Find("Patrol Feedback").GetComponent<TMPro.TMP_Text>().text,
                Is.Empty);
            Assert.That(GameObject.Find("Start Patrol").GetComponent<Button>().interactable, Is.True);
        }

        private static GameObject FindIncludingInactive(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Single(transform => transform.name == name).gameObject;

        private static void AssertAnchor(string name, Vector2 minimum, Vector2 maximum)
        {
            var rect = FindIncludingInactive(name).GetComponent<RectTransform>();
            Assert.That(rect.anchorMin, Is.EqualTo(minimum), name + " anchor minimum");
            Assert.That(rect.anchorMax, Is.EqualTo(maximum), name + " anchor maximum");
        }

        private static void AssertDifficultyPresentation(string name)
        {
            var button = FindIncludingInactive(name).GetComponent<Button>();
            var image = button.targetGraphic as Image;
            var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            Assert.That(image.color, Is.EqualTo(Color.white), name + " image tint");
            Assert.That(label.color, Is.EqualTo(new Color(.96f, .89f, .71f, 1f)), name + " label color");
        }

        private static void AssertVisualRectInside(RectTransform child, RectTransform parent)
        {
            var childCorners = new Vector3[4];
            var parentCorners = new Vector3[4];
            child.GetWorldCorners(childCorners);
            parent.GetWorldCorners(parentCorners);
            foreach (var corner in childCorners)
            {
                Assert.That(corner.x, Is.InRange(parentCorners[0].x, parentCorners[2].x),
                    "lock slash must not extend beyond production card width");
                Assert.That(corner.y, Is.InRange(parentCorners[0].y, parentCorners[2].y),
                    "lock slash must not extend beyond production card height");
            }
        }

        private static void AssertDifficultyCardsHaveEqualSizeAndInternalLocks(PatrolPageView view)
        {
            var cards = new[] { view.NormalDifficulty, view.OmenDifficulty, view.GreatOmenDifficulty };
            var expected = cards[0].GetComponent<RectTransform>().rect.size;
            foreach (var card in cards)
                Assert.That(card.GetComponent<RectTransform>().rect.size, Is.EqualTo(expected));

            foreach (var card in cards.Skip(1))
            {
                var slash = card.Button.transform.Find("Lock Slash")?.GetComponent<RectTransform>();
                Assert.That(slash, Is.Not.Null);
                Assert.That(slash.IsChildOf(card.transform), Is.True);
                AssertVisualRectInside(slash, card.GetComponent<RectTransform>());
            }
        }

        private static AuthoredPatrolHarness CreateAuthoredHarness()
        {
            var root = new GameObject("Authored Patrol Harness", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 1100f);
            var presenter = root.AddComponent<PatrolPresenter>();
            var view = root.AddComponent<PatrolPageView>();

            var pageHeaderRoot = Rect("Page Header", root.transform, new Vector2(640f, 100f));
            var pageHeader = pageHeaderRoot.gameObject.AddComponent<LobbyPageHeaderView>();
            var back = Button("Back Button", pageHeaderRoot, out _);
            var pageTitle = Text("Title", pageHeaderRoot, "임시 제목");
            var pageIcon = Image("Icon", pageHeaderRoot);
            pageHeader.Configure(back, pageTitle, pageIcon);

            var stageName = Text("Stage Name", root.transform, "임시 지역");
            var stageStatus = Text("Stage Status", root.transform, "임시 상태");
            var previous = Button("Previous Stage", root.transform, out _);
            var next = Button("Next Stage", root.transform, out _);
            var hero = Image("Patrol Hero", root.transform);
            var normal = Difficulty("Difficulty Normal", root.transform, "임시 보통");
            var omen = Difficulty("Difficulty Omen", root.transform, "임시 흉조");
            var greatOmen = Difficulty("Difficulty Great Omen", root.transform, "임시 대흉");
            var weaponSelector = WeaponSelector(root.transform);
            var feedback = Text("Patrol Feedback", root.transform, string.Empty);

            var overlay = Rect("Weapon Selection Overlay", root.transform, new Vector2(620f, 900f)).gameObject;
            var panel = Rect("Weapon Selection Panel", overlay.transform, new Vector2(580f, 800f));
            var close = Button("Close Weapon Selection", panel, out _);
            var grid = Rect("Weapon Grid", panel, new Vector2(540f, 650f));
            Button gakgung = null;
            foreach (var weaponId in WeaponRoster.All)
            {
                var option = Button($"Weapon Option {weaponId.Value}", grid, out _);
                Image("Weapon Option Icon", option.transform);
                if (weaponId.Equals(WeaponId.GakgungShot)) gakgung = option;
            }
            overlay.SetActive(false);

            var start = Button("Start Patrol", root.transform, out var actionLabel);
            start.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 92f);
            view.Configure(pageHeader, stageName, stageStatus, previous, next, hero, normal, omen, greatOmen,
                weaponSelector, feedback, overlay, close, start);

            return new AuthoredPatrolHarness(root, presenter, view, gakgung, actionLabel);
        }

        private static LobbyDifficultyCardView Difficulty(string name, Transform parent, string labelValue)
        {
            var root = Rect(name, parent, new Vector2(280f, 100f));
            var button = Button("Button", root, out _);
            Stretch(button.GetComponent<RectTransform>());
            var label = Text("Label", root, labelValue);
            Stretch(label.rectTransform);
            var lockSlash = Image("Lock Slash", button.transform);
            var constraint = lockSlash.gameObject.AddComponent<LockSlashConstraint>();
            constraint.Configure();
            var lockIcon = Image("Lock Icon", button.transform);
            var view = root.gameObject.AddComponent<LobbyDifficultyCardView>();
            ConfigureDifficultyBindings(view, button, label, lockSlash, lockIcon, constraint);
            return view;
        }

        private static void ConfigureDifficultyBindings(
            LobbyDifficultyCardView view,
            Button button,
            TMP_Text label,
            Image lockSlash,
            Image lockIcon,
            LockSlashConstraint constraint)
        {
            var authoredConfigure = typeof(LobbyDifficultyCardView).GetMethod(
                "Configure",
                new[] { typeof(Button), typeof(TMP_Text), typeof(Image), typeof(Image), typeof(LockSlashConstraint) });
            if (authoredConfigure != null)
            {
                authoredConfigure.Invoke(view, new object[] { button, label, lockSlash, lockIcon, constraint });
                return;
            }

            var legacyConfigure = typeof(LobbyDifficultyCardView).GetMethod(
                "Configure",
                new[] { typeof(Button), typeof(TMP_Text) });
            Assert.That(legacyConfigure, Is.Not.Null, "Difficulty card must expose a configuration seam.");
            legacyConfigure.Invoke(view, new object[] { button, label });
        }

        private static LobbyWeaponSelectorCardView WeaponSelector(Transform parent)
        {
            var root = Rect("Starting Weapon Selector", parent, new Vector2(560f, 116f));
            var button = Button("Button", root, out _);
            Stretch(button.GetComponent<RectTransform>());
            var icon = Image("Icon", root);
            var caption = Text("Caption", root, "임시 무기");
            var weaponName = Text("Weapon Name", root, "임시 이름");
            var chevron = Text("Chevron", root, "〉");
            var view = root.gameObject.AddComponent<LobbyWeaponSelectorCardView>();
            view.Configure(button, icon, caption, weaponName, chevron);
            PremiumPixelUiSkin.ApplyFrame(view.Background, PremiumFrame.WeaponSelector);
            return view;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private static Button Button(string name, Transform parent, out TMP_Text label)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = Text("Label", button.transform, "임시");
            Stretch(label.rectTransform);
            return button;
        }

        private static Image Image(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, string value)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class AuthoredPatrolHarness
        {
            public static readonly string[] UniqueControlNames =
            {
                "Stage Name", "Stage Status", "Previous Stage", "Next Stage", "Patrol Hero",
                "Difficulty Normal", "Difficulty Omen", "Difficulty Great Omen",
                "Starting Weapon Selector", "Weapon Selection Overlay", "Close Weapon Selection",
                "Start Patrol", "Patrol Feedback"
            };

            public AuthoredPatrolHarness(GameObject root, PatrolPresenter presenter, PatrolPageView view,
                Button gakgungOption, TMP_Text actionLabel)
            {
                Root = root;
                Presenter = presenter;
                View = view;
                GakgungOption = gakgungOption;
                ActionLabel = actionLabel;
            }

            public GameObject Root { get; }
            public PatrolPresenter Presenter { get; }
            public PatrolPageView View { get; }
            public Button GakgungOption { get; }
            public TMP_Text ActionLabel { get; }

            public string[] AllTransformIds() => Root.GetComponentsInChildren<Transform>(true)
                .Select(item => item.GetEntityId().ToString()).ToArray();
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
