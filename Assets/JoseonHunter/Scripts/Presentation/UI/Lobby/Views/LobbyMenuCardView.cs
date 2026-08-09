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

        public Button Button => button;
        public TMP_Text Title => title;
        public TMP_Text Description => description;
        public Image Icon => icon;

        public void Configure(Button cardButton, TMP_Text cardTitle, TMP_Text cardDescription, Image cardIcon)
        {
            button = cardButton;
            title = cardTitle;
            description = cardDescription;
            icon = cardIcon;
        }
    }
}
