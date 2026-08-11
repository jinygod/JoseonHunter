using JoseonHunter.Domain.Progression;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Audio;
using JoseonHunter.Runtime.Meta;
using UnityEngine;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyBootstrap : MonoBehaviour
    {
        [SerializeField] private LobbyRootView rootView;
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        private Rect lastSafeArea;

        private void Awake()
        {
            if (rootView == null || !rootView.HasRequiredBindings)
            {
                Debug.LogError("Lobby authored view is incomplete. Runtime UI construction was skipped.");
                enabled = false;
                return;
            }

            GameMusicDirector.EnsureExists();
            GameMusicDirector.Instance?.Request(GameMusicRole.Lobby, .8f);
            rootView.Header.gameObject.SetActive(true);
            BindAuthoredView(MetaGameSession.EnsureExists());
            ApplySafeArea();
        }

        private void Update()
        {
            if (rootView != null && Screen.safeArea != lastSafeArea) ApplySafeArea();
        }

        public void BindAuthoredView(MetaGameSession session)
        {
            if (session == null) return;
            rootView.PatrolPresenter.ConfigureView(rootView.PatrolView);
            rootView.TrainingPresenter.ConfigureView(rootView.TrainingView);
            rootView.ResearchPresenter.ConfigureView(rootView.ResearchView);
            rootView.HomePresenter.ConfigureView(rootView.Home);
            rootView.HomePresenter.Initialize(session, weaponCatalog);
            rootView.PatrolPresenter.InitializeAuthored(session, RefreshHeader);
            rootView.TrainingPresenter.InitializeAuthored(session, RefreshHeader);
            rootView.ResearchPresenter.InitializeAuthored(session, RefreshHeader);
            rootView.AudioSettings.InitializeAuthored(rootView.SettingsOverlay.GetComponent<LobbyAudioSettingsView>(), session);
            rootView.SettingsButton.onClick.RemoveListener(OpenSettings);
            rootView.SettingsButton.onClick.AddListener(OpenSettings);
            rootView.AudioSettings.CloseRequested -= CloseSettings;
            rootView.AudioSettings.CloseRequested += CloseSettings;
            rootView.SettingsOverlay.SetActive(false);
            GameAudioButtonFeedback.AttachAll(transform);
            RefreshHeader();
            rootView.Navigation.ShowHome();
        }

        private void OpenSettings()
        {
            rootView.SettingsOverlay.transform.SetAsLastSibling();
            rootView.SettingsOverlay.SetActive(true);
        }

        private void CloseSettings() => rootView.SettingsOverlay.SetActive(false);

        private void RefreshHeader()
        {
            var session = MetaGameSession.Current;
            if (session == null) return;
            var account = AccountProgression.StateFor(session.Data.AccountExperience);
            rootView.Header.Render(account, session.Data.Coins);
        }

        private void ApplySafeArea()
        {
            var safeArea = rootView.SafeArea;
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }
    }
}
