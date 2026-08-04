using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            ApplySelection(researchButton, selected == researchPanel);
            ApplySelection(patrolButton, selected == patrolPanel);
            ApplySelection(trainingButton, selected == trainingPanel);
        }

        private static void ApplySelection(Button button, bool selected)
        {
            var background = selected ? LobbyUiFactory.Crimson : LobbyUiFactory.NightInk;
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, .14f);
            colors.pressedColor = Color.Lerp(background, Color.black, .24f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(background.r, background.g, background.b, .45f);
            button.colors = colors;
            if (button.targetGraphic != null) button.targetGraphic.color = background;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = selected ? LobbyUiFactory.Gold : LobbyUiFactory.HanjiLight;
        }
    }
}
