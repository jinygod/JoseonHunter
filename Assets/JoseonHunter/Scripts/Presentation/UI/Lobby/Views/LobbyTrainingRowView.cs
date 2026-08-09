using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    [DisallowMultipleComponent]
    public sealed class LobbyTrainingRowView : MonoBehaviour
    {
        [SerializeField] private CommonTrainingId trainingId;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private LobbyProgressBarView progress;

        public CommonTrainingId TrainingId => trainingId;
        public Button Button => button;
        public TMP_Text NameText => nameText;
        public Image IconImage => iconImage;
        public TMP_Text RankText => rankText;
        public LobbyProgressBarView Progress => progress;
        public bool HasRequiredBindings =>
            button != null && nameText != null && iconImage != null && rankText != null &&
            progress != null && progress.HasRequiredBindings;

        public void Configure(CommonTrainingId id, Button rowButton, TMP_Text label, Image icon,
            TMP_Text rank, LobbyProgressBarView progressBar)
        {
            trainingId = id;
            button = rowButton;
            nameText = label;
            iconImage = icon;
            rankText = rank;
            progress = progressBar;
        }

        public void Render(string label, Sprite icon, int rank, int maximum, bool selected)
        {
            nameText.text = label;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            rankText.text = $"{rank} / {maximum}";
            progress.Render(maximum <= 0 ? 0f : (float)rank / maximum, string.Empty);
            LobbySelectionChrome.ApplyRow(button, selected);
        }
    }
}
