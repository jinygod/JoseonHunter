using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Save;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            Assert.That(start.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("출전"));
            Assert.That(start.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(76f));
        }

        [UnityTest]
        public IEnumerator PatrolHomeCentersTransparentPixelHeroAndCompactWeaponSelector()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;

            var hero = GameObject.Find("Patrol Hero")?.GetComponent<Image>();
            var shadow = GameObject.Find("Patrol Hero Shadow")?.GetComponent<PixelOvalGraphic>();
            var selector = GameObject.Find("Starting Weapon Selector")?.GetComponent<Button>();

            Assert.That(hero, Is.Not.Null);
            Assert.That(hero.sprite, Is.Not.Null);
            Assert.That(hero.preserveAspect, Is.True);
            Assert.That(hero.transform.parent.name, Is.EqualTo("Patrol Panel"));
            Assert.That(GameObject.Find("Patrol Panel").GetComponent<Image>().sprite, Is.Null,
                "The patrol content panel must not stretch an outer architectural frame.");
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
            Assert.That(greatOmen.transform.Find("Lock Icon").GetComponent<Image>().sprite.name,
                Is.EqualTo("icon_lock"));
            var startImage = GameObject.Find("Start Patrol").GetComponent<Button>().targetGraphic as Image;
            Assert.That(startImage.sprite.name, Is.EqualTo("primary_red_button"));
            Assert.That(startImage.type, Is.EqualTo(Image.Type.Sliced));

            GameObject.Find("Difficulty Omen").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("Patrol Feedback").GetComponent<TMPro.TMP_Text>().text,
                Is.EqualTo("이 장 보통 승리 시 해금"));
            Assert.That(MetaGameSession.Current.ActiveStageSelection,
                Is.EqualTo(new StageSelection(StageId.GwigokField, StageDifficulty.Normal)));
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

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
