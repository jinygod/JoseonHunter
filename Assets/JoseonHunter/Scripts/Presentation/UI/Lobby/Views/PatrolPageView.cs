using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class PatrolPageView : MonoBehaviour
    {
        [SerializeField] private LobbyPageHeaderView pageHeader;
        [SerializeField] private TMP_Text stageName;
        [SerializeField] private TMP_Text stageStatus;
        [SerializeField] private Button previousStageButton;
        [SerializeField] private Button nextStageButton;
        [SerializeField] private Image heroImage;
        [SerializeField] private LobbyDifficultyCardView normalDifficulty;
        [SerializeField] private LobbyDifficultyCardView omenDifficulty;
        [SerializeField] private LobbyDifficultyCardView greatOmenDifficulty;
        [SerializeField] private LobbyWeaponSelectorCardView weaponSelector;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private GameObject weaponSelectionOverlay;
        [SerializeField] private Button closeWeaponSelectionButton;
        [SerializeField] private Button startButton;

        public LobbyPageHeaderView PageHeader => pageHeader;
        public TMP_Text StageName => stageName;
        public TMP_Text StageStatus => stageStatus;
        public Button PreviousStageButton => previousStageButton;
        public Button NextStageButton => nextStageButton;
        public Image HeroImage => heroImage;
        public LobbyDifficultyCardView NormalDifficulty => normalDifficulty;
        public LobbyDifficultyCardView OmenDifficulty => omenDifficulty;
        public LobbyDifficultyCardView GreatOmenDifficulty => greatOmenDifficulty;
        public LobbyWeaponSelectorCardView WeaponSelector => weaponSelector;
        public TMP_Text Feedback => feedback;
        public GameObject WeaponSelectionOverlay => weaponSelectionOverlay;
        public Button CloseWeaponSelectionButton => closeWeaponSelectionButton;
        public Button StartButton => startButton;

        public bool HasRequiredBindings =>
            pageHeader != null && pageHeader.BackButton != null && pageHeader.Title != null && pageHeader.Icon != null &&
            stageName != null && stageStatus != null && previousStageButton != null && nextStageButton != null &&
            heroImage != null && normalDifficulty != null && normalDifficulty.HasRequiredBindings &&
            omenDifficulty != null && omenDifficulty.HasRequiredBindings &&
            greatOmenDifficulty != null && greatOmenDifficulty.HasRequiredBindings &&
            weaponSelector != null && weaponSelector.HasRequiredBindings && feedback != null &&
            weaponSelectionOverlay != null && closeWeaponSelectionButton != null && startButton != null;

        public void Configure(
            LobbyPageHeaderView header,
            TMP_Text stageTitle,
            TMP_Text status,
            Button previousStage,
            Button nextStage,
            Image hero,
            LobbyDifficultyCardView normal,
            LobbyDifficultyCardView omen,
            LobbyDifficultyCardView greatOmen,
            LobbyWeaponSelectorCardView startingWeapon,
            TMP_Text feedbackText,
            GameObject selectionOverlay,
            Button closeSelection,
            Button action)
        {
            pageHeader = header;
            stageName = stageTitle;
            stageStatus = status;
            previousStageButton = previousStage;
            nextStageButton = nextStage;
            heroImage = hero;
            normalDifficulty = normal;
            omenDifficulty = omen;
            greatOmenDifficulty = greatOmen;
            weaponSelector = startingWeapon;
            feedback = feedbackText;
            weaponSelectionOverlay = selectionOverlay;
            closeWeaponSelectionButton = closeSelection;
            startButton = action;
        }
    }
}
