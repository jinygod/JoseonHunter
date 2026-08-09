using JoseonHunter.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyDifficultyCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;

        public bool HasRequiredBindings => button != null && labelText != null;

        public void Configure(Button cardButton, TMP_Text label)
        {
            button = cardButton;
            labelText = label;
        }

        public void Render(string label, bool selected, bool locked)
        {
            labelText.text = label ?? string.Empty;
            button.gameObject.SetActive(true);
            button.interactable = !locked;
            PremiumPixelUiSkin.ApplyDifficulty(button, selected, locked);
        }
    }
}
