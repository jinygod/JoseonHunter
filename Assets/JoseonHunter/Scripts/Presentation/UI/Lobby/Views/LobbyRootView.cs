using System;
using JoseonHunter.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    [DisallowMultipleComponent]
    public sealed class LobbyRootView : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private LobbyHeaderView header;
        [SerializeField] private LobbyHomeView home;
        [SerializeField] private LobbyHomePresenter homePresenter;
        [SerializeField] private LobbyNavigationPresenter navigation;
        [SerializeField] private PatrolPageView patrolView;
        [SerializeField] private PatrolPresenter patrolPresenter;
        [SerializeField] private TrainingPageView trainingView;
        [SerializeField] private CommonTrainingPresenter trainingPresenter;
        [SerializeField] private ResearchPageView researchView;
        [SerializeField] private WeaponResearchPresenter researchPresenter;
        [SerializeField] private GameObject settingsOverlay;
        [SerializeField] private Button settingsButton;
        [SerializeField] private AudioSettingsPresenter audioSettings;

        public RectTransform SafeArea => safeArea;
        public LobbyHeaderView Header => header;
        public LobbyHomeView Home => home;
        public LobbyHomePresenter HomePresenter => homePresenter;
        public LobbyNavigationPresenter Navigation => navigation;
        public PatrolPageView PatrolView => patrolView;
        public PatrolPresenter PatrolPresenter => patrolPresenter;
        public TrainingPageView TrainingView => trainingView;
        public CommonTrainingPresenter TrainingPresenter => trainingPresenter;
        public ResearchPageView ResearchView => researchView;
        public WeaponResearchPresenter ResearchPresenter => researchPresenter;
        public GameObject SettingsOverlay => settingsOverlay;
        public Button SettingsButton => settingsButton;
        public AudioSettingsPresenter AudioSettings => audioSettings;

        public bool HasRequiredBindings =>
            safeArea != null && header != null && header.HasRequiredBindings &&
            home != null && homePresenter != null && navigation != null &&
            patrolView != null && patrolView.HasRequiredBindings && patrolPresenter != null &&
            trainingView != null && trainingView.HasRequiredBindings && trainingPresenter != null &&
            researchView != null && researchView.HasRequiredBindings && researchPresenter != null &&
            settingsOverlay != null && settingsButton != null && audioSettings != null &&
            header.SettingsButton == settingsButton &&
            settingsOverlay.TryGetComponent<LobbyAudioSettingsView>(out var settingsView) && settingsView.HasRequiredBindings &&
            HasUniqueReferences();

        private bool HasUniqueReferences()
        {
            var values = new UnityEngine.Object[]
            {
                safeArea, header, home, homePresenter, navigation, patrolView, patrolPresenter,
                trainingView, trainingPresenter, researchView, researchPresenter, settingsOverlay,
                settingsButton, audioSettings
            };
            for (var index = 0; index < values.Length; index++)
                for (var other = index + 1; other < values.Length; other++)
                    if (values[index] == values[other]) return false;
            return true;
        }
    }
}
