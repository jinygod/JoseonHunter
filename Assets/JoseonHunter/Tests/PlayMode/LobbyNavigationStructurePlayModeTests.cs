using System;
using JoseonHunter.Presentation.UI.Lobby;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class LobbyNavigationStructurePlayModeTests
    {
        [Test]
        public void NamedActionsUseTheSameFourPageStateMachine()
        {
            var fixture = CreateFixture();
            Assert.That(fixture.Presenter.HasRequiredBindings, Is.True);

            fixture.Presenter.OpenTrainingPage();
            AssertPage(fixture, LobbyPageId.Training);
            fixture.Presenter.ReturnToHomePage();
            AssertPage(fixture, LobbyPageId.Home);
            fixture.Presenter.OpenPatrolPage();
            AssertPage(fixture, LobbyPageId.Patrol);
            fixture.Presenter.ReturnToHomePage();
            fixture.Presenter.OpenResearchPage();
            AssertPage(fixture, LobbyPageId.Research);
            fixture.Destroy();
        }

        [Test]
        public void DuplicateReferenceIsRejectedBeforePageOrExternalListenerMutation()
        {
            var presenter = new GameObject("Duplicate Navigation").AddComponent<LobbyNavigationPresenter>();
            var home = new GameObject("Home");
            var training = new GameObject("Training");
            var research = new GameObject("Research");
            var menu = Button("Menu");
            var patrolMenu = Button("Patrol Menu");
            var researchMenu = Button("Research Menu");
            var trainingBack = Button("Training Back");
            var patrolBack = Button("Patrol Back");
            var researchBack = Button("Research Back");
            home.SetActive(false);
            training.SetActive(true);
            research.SetActive(false);
            var externalClicks = 0;
            menu.onClick.AddListener(() => externalClicks++);

            var error = Assert.Throws<InvalidOperationException>(() => presenter.Initialize(
                home, training, training, research,
                menu, patrolMenu, researchMenu, trainingBack, patrolBack, researchBack));

            Assert.That(error.Message, Does.Contain("unique page and button references"));
            Assert.That(presenter.HasRequiredBindings, Is.False);
            Assert.That(home.activeSelf, Is.False);
            Assert.That(training.activeSelf, Is.True);
            Assert.That(research.activeSelf, Is.False);
            menu.onClick.Invoke();
            Assert.That(externalClicks, Is.EqualTo(1));
            foreach (var item in new Object[] { presenter.gameObject, home, training, research,
                         menu.gameObject, patrolMenu.gameObject, researchMenu.gameObject,
                         trainingBack.gameObject, patrolBack.gameObject, researchBack.gameObject })
                Object.DestroyImmediate(item);
        }

        [Test]
        public void DestroyRemovesOnlyOwnedNavigationListeners()
        {
            var fixture = CreateFixture();
            var externalClicks = 0;
            fixture.TrainingMenu.onClick.AddListener(() => externalClicks++);

            Object.DestroyImmediate(fixture.Presenter.gameObject);
            fixture.TrainingMenu.onClick.Invoke();

            Assert.That(externalClicks, Is.EqualTo(1));
            Assert.That(fixture.Home.activeSelf, Is.True);
            Assert.That(fixture.Training.activeSelf, Is.False);
            fixture.Destroy();
        }

        private static Fixture CreateFixture()
        {
            var presenter = new GameObject("Navigation").AddComponent<LobbyNavigationPresenter>();
            var fixture = new Fixture
            {
                Presenter = presenter,
                Home = new GameObject("Home"),
                Training = new GameObject("Training"),
                Patrol = new GameObject("Patrol"),
                Research = new GameObject("Research"),
                TrainingMenu = Button("Training Menu"),
                PatrolMenu = Button("Patrol Menu"),
                ResearchMenu = Button("Research Menu"),
                TrainingBack = Button("Training Back"),
                PatrolBack = Button("Patrol Back"),
                ResearchBack = Button("Research Back")
            };
            presenter.Initialize(fixture.Home, fixture.Training, fixture.Patrol, fixture.Research,
                fixture.TrainingMenu, fixture.PatrolMenu, fixture.ResearchMenu,
                fixture.TrainingBack, fixture.PatrolBack, fixture.ResearchBack);
            return fixture;
        }

        private static Button Button(string name) =>
            new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button))
                .GetComponent<Button>();

        private static void AssertPage(Fixture fixture, LobbyPageId expected)
        {
            Assert.That(fixture.Presenter.CurrentPage, Is.EqualTo(expected));
            var pages = new[] { fixture.Home, fixture.Training, fixture.Patrol, fixture.Research };
            Assert.That(pages[(int)expected].activeSelf, Is.True);
            Assert.That(Array.FindAll(pages, page => page.activeSelf).Length, Is.EqualTo(1));
        }

        private sealed class Fixture
        {
            public LobbyNavigationPresenter Presenter;
            public GameObject Home;
            public GameObject Training;
            public GameObject Patrol;
            public GameObject Research;
            public Button TrainingMenu;
            public Button PatrolMenu;
            public Button ResearchMenu;
            public Button TrainingBack;
            public Button PatrolBack;
            public Button ResearchBack;

            public void Destroy()
            {
                foreach (var item in new Object[]
                         {
                             Presenter != null ? Presenter.gameObject : null, Home, Training, Patrol, Research,
                             TrainingMenu.gameObject, PatrolMenu.gameObject, ResearchMenu.gameObject,
                             TrainingBack.gameObject, PatrolBack.gameObject, ResearchBack.gameObject
                         })
                    if (item != null) Object.DestroyImmediate(item);
            }
        }
    }
}
