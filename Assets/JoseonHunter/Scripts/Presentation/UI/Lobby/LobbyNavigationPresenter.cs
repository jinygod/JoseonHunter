using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyNavigationPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject trainingPage;
        [SerializeField] private GameObject patrolPage;
        [SerializeField] private GameObject researchPage;
        [SerializeField] private Button trainingMenuButton;
        [SerializeField] private Button patrolMenuButton;
        [SerializeField] private Button researchMenuButton;
        [SerializeField] private Button trainingBackButton;
        [SerializeField] private Button patrolBackButton;
        [SerializeField] private Button researchBackButton;

        private LobbyPageId currentPage;
        private UnityAction trainingMenuAction;
        private UnityAction patrolMenuAction;
        private UnityAction researchMenuAction;
        private UnityAction trainingBackAction;
        private UnityAction patrolBackAction;
        private UnityAction researchBackAction;

        public LobbyPageId CurrentPage => currentPage;

        private void Awake()
        {
            if (homePage != null || trainingPage != null || patrolPage != null || researchPage != null)
                Bind();
        }

        public void Initialize(GameObject home, GameObject training, GameObject patrol, GameObject research,
            Button trainingMenu, Button patrolMenu, Button researchMenu,
            Button trainingBack, Button patrolBack, Button researchBack)
        {
            ValidateRequiredReferences(home, training, patrol, research,
                trainingMenu, patrolMenu, researchMenu, trainingBack, patrolBack, researchBack);
            Unbind();
            homePage = home;
            trainingPage = training;
            patrolPage = patrol;
            researchPage = research;
            trainingMenuButton = trainingMenu;
            patrolMenuButton = patrolMenu;
            researchMenuButton = researchMenu;
            trainingBackButton = trainingBack;
            patrolBackButton = patrolBack;
            researchBackButton = researchBack;
            Bind();
        }

        public void Show(LobbyPageId page)
        {
            ValidatePageReferences();
            currentPage = page;
            homePage.SetActive(page == LobbyPageId.Home);
            trainingPage.SetActive(page == LobbyPageId.Training);
            patrolPage.SetActive(page == LobbyPageId.Patrol);
            researchPage.SetActive(page == LobbyPageId.Research);
        }

        public void ShowHome() => Show(LobbyPageId.Home);

        private void Bind()
        {
            ValidateBindingReferences();
            Unbind();
            trainingMenuAction = () => Show(LobbyPageId.Training);
            patrolMenuAction = () => Show(LobbyPageId.Patrol);
            researchMenuAction = () => Show(LobbyPageId.Research);
            trainingBackAction = ShowHome;
            patrolBackAction = ShowHome;
            researchBackAction = ShowHome;
            trainingMenuButton.onClick.AddListener(trainingMenuAction);
            patrolMenuButton.onClick.AddListener(patrolMenuAction);
            researchMenuButton.onClick.AddListener(researchMenuAction);
            if (trainingBackButton != null) trainingBackButton.onClick.AddListener(trainingBackAction);
            if (patrolBackButton != null) patrolBackButton.onClick.AddListener(patrolBackAction);
            if (researchBackButton != null) researchBackButton.onClick.AddListener(researchBackAction);
            ShowHome();
        }

        private void Unbind()
        {
            RemoveOwnedListener(trainingMenuButton, trainingMenuAction);
            RemoveOwnedListener(patrolMenuButton, patrolMenuAction);
            RemoveOwnedListener(researchMenuButton, researchMenuAction);
            RemoveOwnedListener(trainingBackButton, trainingBackAction);
            RemoveOwnedListener(patrolBackButton, patrolBackAction);
            RemoveOwnedListener(researchBackButton, researchBackAction);
            trainingMenuAction = null;
            patrolMenuAction = null;
            researchMenuAction = null;
            trainingBackAction = null;
            patrolBackAction = null;
            researchBackAction = null;
        }

        private void ValidateBindingReferences()
        {
            ValidatePageReferences();
            Require(trainingMenuButton, nameof(trainingMenuButton));
            Require(patrolMenuButton, nameof(patrolMenuButton));
            Require(researchMenuButton, nameof(researchMenuButton));
        }

        private void ValidatePageReferences()
        {
            Require(homePage, nameof(homePage));
            Require(trainingPage, nameof(trainingPage));
            Require(patrolPage, nameof(patrolPage));
            Require(researchPage, nameof(researchPage));
        }

        private static void ValidateRequiredReferences(GameObject home, GameObject training,
            GameObject patrol, GameObject research, Button trainingMenu, Button patrolMenu, Button researchMenu,
            Button trainingBack, Button patrolBack, Button researchBack)
        {
            Require(home, nameof(homePage));
            Require(training, nameof(trainingPage));
            Require(patrol, nameof(patrolPage));
            Require(research, nameof(researchPage));
            Require(trainingMenu, nameof(trainingMenuButton));
            Require(patrolMenu, nameof(patrolMenuButton));
            Require(researchMenu, nameof(researchMenuButton));
            Require(trainingBack, nameof(trainingBackButton));
            Require(patrolBack, nameof(patrolBackButton));
            Require(researchBack, nameof(researchBackButton));
        }

        private static void Require(UnityEngine.Object reference, string name)
        {
            if (reference == null)
                throw new InvalidOperationException($"Lobby navigation requires '{name}' before binding or showing.");
        }

        private static void RemoveOwnedListener(Button button, UnityAction action)
        {
            if (button != null && action != null) button.onClick.RemoveListener(action);
        }
    }
}
