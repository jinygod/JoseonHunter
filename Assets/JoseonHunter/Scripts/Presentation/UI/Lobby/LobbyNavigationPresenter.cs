using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyNavigationPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject researchPanel;
        [SerializeField] private GameObject patrolPanel;
        [SerializeField] private GameObject trainingPanel;
        [SerializeField] private Button researchButton;
        [SerializeField] private Button patrolButton;
        [SerializeField] private Button trainingButton;

        private void Awake()
        {
            if (researchPanel != null && patrolPanel != null && trainingPanel != null) Bind();
        }

        public void Initialize(GameObject research, GameObject patrol, GameObject training,
            Button researchButton, Button patrolButton, Button trainingButton)
        {
            researchPanel = research;
            patrolPanel = patrol;
            trainingPanel = training;
            this.researchButton = researchButton;
            this.patrolButton = patrolButton;
            this.trainingButton = trainingButton;
            Bind();
        }

        private void Bind()
        {
            researchButton.onClick.RemoveAllListeners();
            patrolButton.onClick.RemoveAllListeners();
            trainingButton.onClick.RemoveAllListeners();
            researchButton.onClick.AddListener(() => Show(researchPanel));
            patrolButton.onClick.AddListener(() => Show(patrolPanel));
            trainingButton.onClick.AddListener(() => Show(trainingPanel));
            Show(patrolPanel);
        }

        private void Show(GameObject selected)
        {
            researchPanel.SetActive(selected == researchPanel);
            patrolPanel.SetActive(selected == patrolPanel);
            trainingPanel.SetActive(selected == trainingPanel);
            ApplySelection(researchButton, selected == researchPanel, PremiumIcon.Research);
            ApplySelection(patrolButton, selected == patrolPanel, PremiumIcon.Patrol);
            ApplySelection(trainingButton, selected == trainingPanel, PremiumIcon.Training);
        }

        private static void ApplySelection(Button button, bool selected, PremiumIcon icon)
        {
            LobbySelectionChrome.ApplyNavigation(button, icon, selected);
        }
    }
}
