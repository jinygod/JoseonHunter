using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyMenuCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image icon;
        [SerializeField] private Image inputSurface;

        public Button Button => button;
        public TMP_Text Title => title;
        public TMP_Text Description => description;
        public Image Icon => icon;
        public Image InputSurface => inputSurface;
        public bool HasRequiredBindings =>
            button != null && title != null && icon != null && inputSurface != null &&
            inputSurface.enabled && inputSurface.raycastTarget &&
            inputSurface.transform.IsChildOf(button.transform) && button.targetGraphic == inputSurface;

        public void Configure(Button cardButton, TMP_Text cardTitle, TMP_Text cardDescription, Image cardIcon,
            Image cardInputSurface)
        {
            button = cardButton;
            title = cardTitle;
            description = cardDescription;
            icon = cardIcon;
            inputSurface = cardInputSurface;
        }
    }
}
