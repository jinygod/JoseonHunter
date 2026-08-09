using System.Collections;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class LobbyHomePlayModeTests
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
        public IEnumerator HomeRendersThreeMenuButtonsAndActivePatrolSummary()
        {
            var data = SaveDataV1.CreateDefaults();
            data.SelectedStageId = StageId.GwigokField.Value;
            data.SelectedStageDifficulty = "normal";
            data.PatrolLoadouts[0].StartingWeaponId = "gakgung_shot";
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var home = CreateHome();
            var presenter = home.GetComponent<LobbyHomePresenter>();

            presenter.Initialize(session, null);
            yield return null;

            var buttons = home.GetComponentsInChildren<Button>(false);
            Assert.That(buttons, Has.Length.EqualTo(3));
            Assert.That(buttons.Select(button => button.GetComponentInChildren<TMP_Text>().text),
                Is.EquivalentTo(new[] { "훈련", "출전", "연구" }));
            Assert.That(home.GetComponentsInChildren<TMP_Text>(false).Select(text => text.text),
                Does.Not.Contain("환도 비검 연구"));
            var view = home.GetComponent<LobbyHomeView>();
            Assert.That(view.StageText.text, Is.EqualTo("귀곡 들판"));
            Assert.That(view.DifficultyText.text, Is.EqualTo("보통"));
            Assert.That(view.StartingWeaponText.text, Is.EqualTo("각궁"));
        }

        [UnityTest]
        public IEnumerator ReturningToAuthoredHomeRefreshesTheSavedPatrolSummaryWithoutRecreatingControls()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 400, 35)));
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), 900f, 400, 35)));
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var home = CreateHome();
            var presenter = home.GetComponent<LobbyHomePresenter>();
            var view = home.GetComponent<LobbyHomeView>();
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(
                "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            Assert.That(catalog, Is.Not.Null);

            presenter.Initialize(session, catalog);
            yield return null;

            var authoredButtons = home.GetComponentsInChildren<Button>(true);
            var authoredIcon = view.StartingWeaponIcon;
            var clicks = 0;
            authoredButtons[0].onClick.AddListener(() => clicks++);

            home.SetActive(false);
            Assert.That(session.SaveStageSelection(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Omen)).Success, Is.True);
            var currentLoadout = session.ActiveLoadout;
            Assert.That(session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex,
                new PatrolLoadout(currentLoadout.Name, WeaponId.GakgungShot, currentLoadout.Styles,
                    currentLoadout.DifficultyId)).Success, Is.True);

            home.SetActive(true);
            yield return null;

            Assert.That(view.StageText.text, Is.EqualTo("도깨비 고갯길"));
            Assert.That(view.DifficultyText.text, Is.EqualTo("흉조"));
            Assert.That(view.StartingWeaponText.text, Is.EqualTo("각궁"));
            Assert.That(view.StartingWeaponIcon.sprite, Is.Not.Null);
            Assert.That(view.StartingWeaponIcon, Is.SameAs(authoredIcon));
            CollectionAssert.AreEqual(authoredButtons, home.GetComponentsInChildren<Button>(true));
            authoredButtons[0].onClick.Invoke();
            Assert.That(clicks, Is.EqualTo(1));
        }

        private static GameObject CreateHome()
        {
            var home = new GameObject("Home", typeof(RectTransform));
            var view = home.AddComponent<LobbyHomeView>();
            var stage = CreateText("Stage", home.transform);
            var difficulty = CreateText("Difficulty", home.transform);
            var weapon = CreateText("Starting Weapon", home.transform);
            var icon = new GameObject("Starting Weapon Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            icon.transform.SetParent(home.transform, false);
            var training = CreateCard("훈련", home.transform);
            var patrol = CreateCard("출전", home.transform);
            var research = CreateCard("연구", home.transform);
            view.Configure(stage, difficulty, weapon, icon, training, patrol, research);
            home.AddComponent<LobbyHomePresenter>();
            return home;
        }

        private static LobbyMenuCardView CreateCard(string label, Transform parent)
        {
            var card = new GameObject(label + " Card", typeof(RectTransform), typeof(LobbyMenuCardView));
            card.transform.SetParent(parent, false);
            var button = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button))
                .GetComponent<Button>();
            button.transform.SetParent(card.transform, false);
            var title = CreateText("Title", button.transform);
            title.text = label;
            var description = CreateText("Description", card.transform);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(card.transform, false);
            var view = card.GetComponent<LobbyMenuCardView>();
            view.Configure(button, title, description, icon);
            return view;
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TMP_Text>();
            text.transform.SetParent(parent, false);
            return text;
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
