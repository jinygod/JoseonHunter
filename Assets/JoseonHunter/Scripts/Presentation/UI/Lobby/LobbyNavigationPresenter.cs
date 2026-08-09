using UnityEngine;
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

        public LobbyPageId CurrentPage => currentPage;

        private void Awake()
        {
            if (homePage != null && trainingPage != null && patrolPage != null && researchPage != null)
                Bind();
        }

        public void Initialize(GameObject home, GameObject training, GameObject patrol, GameObject research,
            Button trainingMenu, Button patrolMenu, Button researchMenu,
            Button trainingBack, Button patrolBack, Button researchBack)
        {
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

        public void Initialize(GameObject research, GameObject patrol, GameObject training,
            Button researchButton, Button patrolButton, Button trainingButton)
        {
            homePage = patrol;
            trainingPage = training;
            patrolPage = patrol;
            researchPage = research;
            trainingMenuButton = trainingButton;
            patrolMenuButton = patrolButton;
            researchMenuButton = researchButton;
            trainingBackButton = null;
            patrolBackButton = null;
            researchBackButton = null;
            Bind();
            Show(LobbyPageId.Patrol);
        }

        public void Show(LobbyPageId page)
        {
            currentPage = page;
            homePage.SetActive(page == LobbyPageId.Home);
            trainingPage.SetActive(page == LobbyPageId.Training);
            patrolPage.SetActive(page == LobbyPageId.Patrol);
            researchPage.SetActive(page == LobbyPageId.Research);
        }

        public void ShowHome() => Show(LobbyPageId.Home);

        private void Bind()
        {
            RemoveListeners(trainingMenuButton, patrolMenuButton, researchMenuButton,
                trainingBackButton, patrolBackButton, researchBackButton);
            trainingMenuButton.onClick.AddListener(() => Show(LobbyPageId.Training));
            patrolMenuButton.onClick.AddListener(() => Show(LobbyPageId.Patrol));
            researchMenuButton.onClick.AddListener(() => Show(LobbyPageId.Research));
            if (trainingBackButton != null) trainingBackButton.onClick.AddListener(ShowHome);
            if (patrolBackButton != null) patrolBackButton.onClick.AddListener(ShowHome);
            if (researchBackButton != null) researchBackButton.onClick.AddListener(ShowHome);
            ShowHome();
        }

        private static void RemoveListeners(params Button[] buttons)
        {
            foreach (var button in buttons)
                if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
