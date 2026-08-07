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
            Assert.That(shadow, Is.Not.Null);
            Assert.That(shadow.color.a, Is.InRange(.08f, .28f));
            Assert.That(shadow.transform.GetSiblingIndex(), Is.LessThan(hero.transform.GetSiblingIndex()));
            Assert.That(selector, Is.Not.Null);
            Assert.That(GameObject.Find("Previous Weapon"), Is.Null);
            Assert.That(GameObject.Find("Next Weapon"), Is.Null);
            Assert.That(GameObject.Find("Current Weapon Icon"), Is.Null);
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

            GameObject.Find("Difficulty Omen").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("Patrol Feedback").GetComponent<TMPro.TMP_Text>().text,
                Is.EqualTo("이 장 보통 승리 시 해금"));
            Assert.That(MetaGameSession.Current.ActiveStageSelection,
                Is.EqualTo(new StageSelection(StageId.GwigokField, StageDifficulty.Normal)));
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

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
