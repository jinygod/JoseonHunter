using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyPageHeaderView : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image icon;

        public Button BackButton => backButton;
        public TMP_Text Title => title;
        public Image Icon => icon;

        public void Configure(Button back, TMP_Text pageTitle, Image pageIcon)
        {
            backButton = back;
            title = pageTitle;
            icon = pageIcon;
        }
    }
}
