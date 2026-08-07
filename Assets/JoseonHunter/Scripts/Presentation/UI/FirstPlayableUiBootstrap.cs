using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class FirstPlayableUiBootstrap : MonoBehaviour
    {
        private const float RenderInterval = .1f;

        private static FirstPlayableUiBootstrap instance;
        private FirstPlayableController boundController;
        private CombatHudPresenter combatHud;
        private WeaponRackPresenter weaponRack;
        private RewardRevealPresenter rewardReveal;
        private WeaponAffixRevealPresenter affixReveal;
        private UpgradeChoicePresenter upgradeChoice;
        private WeaponReplacementPresenter weaponReplacement;
        private WeaponLegacyChoicePresenter weaponLegacyChoice;
        private RunResultPresenter runResult;
        private AbandonRunPresenter abandonRun;
        private CanvasGroup combatHudGroup;
        private CanvasGroup weaponRackGroup;
        private RectTransform safeAreaContainer;
        private RectTransform modalSafeAreaContainer;
        private GameObject modalScrim;
        private Rect lastSafeArea;
        private Vector2 lastScreenSize;
        private float nextRenderTime;
        private int weaponSignature = int.MinValue;
        private bool waitingForRewardReveal;
        private bool waitingForChoiceClose;
        private ProgressionRewardEvent pendingReward;
        private bool hasPendingReward;
        private bool resultWasOpen;
        private readonly GameplayMusicState gameplayMusic = new GameplayMusicState();

        public FirstPlayableController BoundController => boundController;
        public RectTransform SafeAreaContainer => safeAreaContainer;
        public RectTransform ModalSafeAreaContainer => modalSafeAreaContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include).Length != 0)
                return;

            new GameObject("First Playable UI").AddComponent<FirstPlayableUiBootstrap>();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Gameplay") EnsureBootstrap();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildCanvas();
            EnsureEventSystem();
            safeAreaContainer = RuntimeUiFactory.Rect("Safe Area", transform);
            var modalLayer = RuntimeUiFactory.Rect("Modal Layer", transform);
            RuntimeUiFactory.Stretch(modalLayer, 0f, 0f, 0f, 0f);
            modalScrim = RuntimeUiFactory.Image("Modal Scrim", modalLayer, new Color(.008f, .012f, .022f, .78f)).gameObject;
            RuntimeUiFactory.Stretch(modalScrim.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            modalScrim.GetComponent<Image>().raycastTarget = false;
            modalScrim.SetActive(false);
            modalSafeAreaContainer = RuntimeUiFactory.Rect("Modal Safe Area", modalLayer);
            ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));

            var hudRoot = RuntimeUiFactory.Rect("Combat HUD", safeAreaContainer);
            RuntimeUiFactory.Stretch(hudRoot, 0f, 0f, 0f, 0f);
            combatHud = hudRoot.gameObject.AddComponent<CombatHudPresenter>();
            combatHudGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();
            combatHud.Build();
            combatHud.ReturnRequested += OpenAbandonConfirmation;
            var rackRoot = RuntimeUiFactory.Rect("Weapon Rack", safeAreaContainer);
            RuntimeUiFactory.Stretch(rackRoot, 0f, 0f, 0f, 0f);
            weaponRack = rackRoot.gameObject.AddComponent<WeaponRackPresenter>();
            weaponRackGroup = rackRoot.gameObject.AddComponent<CanvasGroup>();
            weaponRack.WeaponSelected += OpenWeaponDetails;
            var rewardRoot = RuntimeUiFactory.Rect("Reward Reveal", modalSafeAreaContainer);
            RuntimeUiFactory.Stretch(rewardRoot, 0f, 0f, 0f, 0f);
            rewardReveal = rewardRoot.gameObject.AddComponent<RewardRevealPresenter>();
            rewardReveal.RevealCompleted += OnRewardRevealCompleted;
            affixReveal = rewardRoot.gameObject.AddComponent<WeaponAffixRevealPresenter>();
            affixReveal.RevealCompleted += OnRewardRevealCompleted;
            affixReveal.DetailClosed += OnWeaponDetailsClosed;
            affixReveal.AppraisalTicked += OnAppraisalTicked;
            affixReveal.AppraisalRevealed += OnAppraisalRevealed;
            var upgradeRoot = RuntimeUiFactory.Rect("Upgrade Choice", modalSafeAreaContainer);
            RuntimeUiFactory.Stretch(upgradeRoot, 0f, 0f, 0f, 0f);
            upgradeChoice = upgradeRoot.gameObject.AddComponent<UpgradeChoicePresenter>();
            upgradeChoice.Build();
            upgradeChoice.PresentationClosed += NotifyUpgradePresentationClosed;
            var replacementRoot = RuntimeUiFactory.Rect("Weapon Replacement", modalSafeAreaContainer);
            RuntimeUiFactory.Stretch(replacementRoot, 0f, 0f, 0f, 0f);
            weaponReplacement = replacementRoot.gameObject.AddComponent<WeaponReplacementPresenter>();
            weaponReplacement.Build();
            weaponReplacement.PresentationClosed += NotifyUpgradePresentationClosed;
            var legacyRoot = RuntimeUiFactory.Rect("Weapon Legacy Choice", modalSafeAreaContainer);
            RuntimeUiFactory.Stretch(legacyRoot, 0f, 0f, 0f, 0f);
            weaponLegacyChoice = legacyRoot.gameObject.AddComponent<WeaponLegacyChoicePresenter>();
            weaponLegacyChoice.Build();
            weaponLegacyChoice.PresentationClosed += NotifyUpgradePresentationClosed;
            var resultRoot = RuntimeUiFactory.Rect("Run Result", modalSafeAreaContainer);
            RuntimeUiFactory.Stretch(resultRoot, 0f, 0f, 0f, 0f);
            runResult = resultRoot.gameObject.AddComponent<RunResultPresenter>();
            runResult.LobbyReturnRequested += OnLobbyReturnRequested;
            var abandonRoot = RuntimeUiFactory.Rect("Abandon Run", modalSafeAreaContainer);
            RuntimeUiFactory.Stretch(abandonRoot, 0f, 0f, 0f, 0f);
            abandonRun = abandonRoot.gameObject.AddComponent<AbandonRunPresenter>();
            abandonRun.Confirmed += ConfirmAbandon;
            abandonRun.Cancelled += CancelAbandon;
        }

        private void OnDestroy()
        {
            CancelUiModalPresentation();
            UnbindController();
            if (upgradeChoice != null) upgradeChoice.PresentationClosed -= NotifyUpgradePresentationClosed;
            if (weaponReplacement != null) weaponReplacement.PresentationClosed -= NotifyUpgradePresentationClosed;
            if (weaponLegacyChoice != null) weaponLegacyChoice.PresentationClosed -= NotifyUpgradePresentationClosed;
            if (rewardReveal != null) rewardReveal.RevealCompleted -= OnRewardRevealCompleted;
            if (affixReveal != null)
            {
                affixReveal.RevealCompleted -= OnRewardRevealCompleted;
                affixReveal.DetailClosed -= OnWeaponDetailsClosed;
                affixReveal.AppraisalTicked -= OnAppraisalTicked;
                affixReveal.AppraisalRevealed -= OnAppraisalRevealed;
            }
            if (weaponRack != null) weaponRack.WeaponSelected -= OpenWeaponDetails;
            if (combatHud != null) combatHud.ReturnRequested -= OpenAbandonConfirmation;
            if (runResult != null) runResult.LobbyReturnRequested -= OnLobbyReturnRequested;
            if (abandonRun != null)
            {
                abandonRun.Confirmed -= ConfirmAbandon;
                abandonRun.Cancelled -= CancelAbandon;
            }
            if (instance == this) instance = null;
        }

        private void OnDisable()
        {
            CloseUpgradeChoice();
            CloseRewardReveal();
            abandonRun?.CloseImmediately();
            CancelUiModalPresentation();
        }

        private void CancelUiModalPresentation()
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                if (boundController == null) return;
                boundController.CancelUiModalPresentation();
                SetBackgroundRaycastsEnabled(true);
            }
        }

        private void Update()
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);
            if (safeArea != lastSafeArea || screenSize != lastScreenSize) ApplySafeArea(safeArea, screenSize);

            if (boundController == null) BindController(FindAnyObjectByType<FirstPlayableController>());
            if (boundController != null && GameAudioDirector.Instance != null)
                GameAudioDirector.Instance.SetCombatEnabled(boundController.Flow != null &&
                                                            boundController.Flow.IsGameplayRunning);
            if (boundController == null || Time.unscaledTime < nextRenderTime) return;

            nextRenderTime = Time.unscaledTime + RenderInterval;
            using (FirstPlayableProfilerMarkers.UiHud.Auto())
            {
                var state = boundController.UiState;
                combatHud.Render(state);
                runResult.Render(state);
                if (state.RunEnded != resultWasOpen)
                {
                    resultWasOpen = state.RunEnded;
                    SetBackgroundRaycastsEnabled(!resultWasOpen);
                    if (resultWasOpen)
                    {
                        SetModalScrimVisible(false);
                        PlayCue(state.Victory ? GameAudioCueId.Victory : GameAudioCueId.Defeat);
                    }
                }
                var signature = WeaponSignature(state);
                if (signature == weaponSignature) return;
                weaponSignature = signature;
                weaponRack.Render(state.Weapons);
            }
        }

        private void BindController(FirstPlayableController controller)
        {
            if (boundController == controller) return;
            UnbindController();
            boundController = controller;
            if (boundController == null) return;
            boundController.UpgradeOpened += OpenUpgradeChoice;
            boundController.WeaponReplacementOpened += OpenWeaponReplacement;
            boundController.WeaponLegacyOpened += OpenWeaponLegacyChoice;
            boundController.UpgradeChosen += OnUpgradeChosen;
            boundController.ExperienceCollected += OnExperienceCollected;
            boundController.YeopjeonCollected += OnYeopjeonCollected;
            boundController.MagnetCollected += OnMagnetCollected;
            boundController.PlayerLevelIncreased += OnPlayerLevelIncreased;
            boundController.BossWarningStarted += OnBossWarningStarted;
            boundController.BossAppeared += OnBossAppeared;
            boundController.BossDefeated += OnBossDefeated;
            boundController.PlayerDamaged += OnPlayerDamaged;
            boundController.PlayerDefeated += OnPlayerDefeated;
            boundController.EliteAppeared += OnEliteAppeared;
            boundController.EliteDefeated += OnEliteDefeated;
            boundController.WaveWarningStarted += OnWaveWarningStarted;
            boundController.BossAttackExecuted += OnBossAttackExecuted;
            boundController.TreasureAppeared += OnTreasureAppeared;
            boundController.TreasureOpened += OnTreasureOpened;
            boundController.CombatMusicPhaseChanged += OnCombatMusicPhaseChanged;
            boundController.MidBossAppeared += OnMidBossAppeared;
            boundController.MidBossDefeated += OnMidBossDefeated;
            boundController.BossAppeared += OnFinalBossMusicStarted;
            boundController.BossDefeated += OnGameplayMusicEnded;
            boundController.PlayerDefeated += OnGameplayMusicEnded;
            boundController.RunReset += CloseUpgradeChoice;
            boundController.RunReset += CloseRewardReveal;
            boundController.RunReset += CloseAbandonWithoutFlowChange;
            boundController.RunReset += ResetGameplayMusic;
            ResetGameplayMusic();
        }

        private void UnbindController()
        {
            if (boundController == null) return;
            boundController.UpgradeOpened -= OpenUpgradeChoice;
            boundController.WeaponReplacementOpened -= OpenWeaponReplacement;
            boundController.WeaponLegacyOpened -= OpenWeaponLegacyChoice;
            boundController.UpgradeChosen -= OnUpgradeChosen;
            boundController.ExperienceCollected -= OnExperienceCollected;
            boundController.YeopjeonCollected -= OnYeopjeonCollected;
            boundController.MagnetCollected -= OnMagnetCollected;
            boundController.PlayerLevelIncreased -= OnPlayerLevelIncreased;
            boundController.BossWarningStarted -= OnBossWarningStarted;
            boundController.BossAppeared -= OnBossAppeared;
            boundController.BossDefeated -= OnBossDefeated;
            boundController.PlayerDamaged -= OnPlayerDamaged;
            boundController.PlayerDefeated -= OnPlayerDefeated;
            boundController.EliteAppeared -= OnEliteAppeared;
            boundController.EliteDefeated -= OnEliteDefeated;
            boundController.WaveWarningStarted -= OnWaveWarningStarted;
            boundController.BossAttackExecuted -= OnBossAttackExecuted;
            boundController.TreasureAppeared -= OnTreasureAppeared;
            boundController.TreasureOpened -= OnTreasureOpened;
            boundController.CombatMusicPhaseChanged -= OnCombatMusicPhaseChanged;
            boundController.MidBossAppeared -= OnMidBossAppeared;
            boundController.MidBossDefeated -= OnMidBossDefeated;
            boundController.BossAppeared -= OnFinalBossMusicStarted;
            boundController.BossDefeated -= OnGameplayMusicEnded;
            boundController.PlayerDefeated -= OnGameplayMusicEnded;
            boundController.RunReset -= CloseUpgradeChoice;
            boundController.RunReset -= CloseRewardReveal;
            boundController.RunReset -= CloseAbandonWithoutFlowChange;
            boundController.RunReset -= ResetGameplayMusic;
            boundController = null;
        }

        private void OpenUpgradeChoice(UpgradeChoiceState state)
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                SetBackgroundRaycastsEnabled(false);
                SetModalScrimVisible(true);
                weaponReplacement?.CloseImmediately();
                weaponLegacyChoice?.CloseImmediately();
                upgradeChoice?.Open(state, boundController.TryChooseUpgrade);
            }
        }

        private void OpenWeaponReplacement(WeaponReplacementState state)
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                SetBackgroundRaycastsEnabled(false);
                SetModalScrimVisible(true);
                upgradeChoice?.CloseImmediately();
                weaponLegacyChoice?.CloseImmediately();
                weaponReplacement?.Open(state, boundController.TryChooseWeaponReplacement,
                    boundController.CancelWeaponReplacement);
            }
        }

        private void OpenWeaponLegacyChoice(WeaponLegacyChoiceState state)
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                SetBackgroundRaycastsEnabled(false);
                SetModalScrimVisible(true);
                upgradeChoice?.CloseImmediately();
                weaponReplacement?.CloseImmediately();
                weaponLegacyChoice?.Open(state, boundController.TryChooseWeaponLegacy);
            }
        }

        private void CloseUpgradeChoice()
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                upgradeChoice?.CloseImmediately();
                weaponReplacement?.CloseImmediately();
                weaponLegacyChoice?.CloseImmediately();
            }
        }

        private void OpenWeaponDetails(WeaponSlotView weapon)
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                if (affixReveal == null || affixReveal.IsRevealing || upgradeChoice == null || upgradeChoice.IsOpen ||
                    (weaponReplacement != null && weaponReplacement.IsOpen) ||
                    (weaponLegacyChoice != null && weaponLegacyChoice.IsOpen))
                    return;
                if (boundController == null || !boundController.Flow.TryTransition(GameFlowState.Paused)) return;
                SetBackgroundRaycastsEnabled(false);
                SetModalScrimVisible(true);
                affixReveal.ShowDetails(weapon);
            }
        }

        private void OnWeaponDetailsClosed()
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                boundController?.Flow.TryTransition(GameFlowState.Playing);
                SetBackgroundRaycastsEnabled(true);
                SetModalScrimVisible(false);
            }
        }

        private void OnUpgradeChosen(ProgressionRewardEvent reward)
        {
            PlayCue(GameAudioCueId.UpgradeSelected);
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                var requiresRewardPresentation = reward.Kind != ProgressionRewardKind.Support;
                waitingForRewardReveal = requiresRewardPresentation;
                waitingForChoiceClose = true;
                pendingReward = reward;
                hasPendingReward = requiresRewardPresentation;
                upgradeChoice?.CloseAfterExternalSelection();
                if (reward.Kind != ProgressionRewardKind.Support)
                {
                    var state = boundController.UiState;
                    weaponSignature = WeaponSignature(state);
                    weaponRack.Render(state.Weapons);
                    weaponRack.Pulse(reward.WeaponId, reward.NewLevel, reward.AffixResult?.NewPotentials.Count ?? 0);
                }
            }

        }

        private static void OnExperienceCollected() => PlayCue(GameAudioCueId.ExperiencePickup);
        private static void OnYeopjeonCollected() => PlayCue(GameAudioCueId.YeopjeonPickup);
        private static void OnMagnetCollected() => PlayCue(GameAudioCueId.MagnetPickup);
        private static void OnPlayerLevelIncreased() => PlayCue(GameAudioCueId.LevelUp);
        private static void OnBossWarningStarted() => PlayCue(GameAudioCueId.BossWarning);
        private static void OnBossAppeared() => PlayCue(GameAudioCueId.BossAppear);
        private static void OnBossDefeated() => PlayCue(GameAudioCueId.BossDefeat);
        private static void OnPlayerDamaged() => PlayCue(GameAudioCueId.PlayerHurt);
        private static void OnPlayerDefeated() => PlayCue(GameAudioCueId.PlayerDefeat);
        private static void OnEliteAppeared() => PlayCue(GameAudioCueId.EliteAppear);
        private static void OnEliteDefeated() => PlayCue(GameAudioCueId.EliteDefeat);
        private static void OnWaveWarningStarted() => PlayCue(GameAudioCueId.WaveWarning);
        private static void OnTreasureAppeared() => PlayCue(GameAudioCueId.TreasureAppear);
        private static void OnTreasureOpened() => PlayCue(GameAudioCueId.TreasureOpen);
        private static void OnAppraisalTicked() => PlayCue(GameAudioCueId.AppraisalTick);
        private static void OnAppraisalRevealed() => PlayCue(GameAudioCueId.AppraisalReveal);

        private void ResetGameplayMusic()
        {
            gameplayMusic.Reset();
            RequestGameplayMusic(.6f);
        }

        private void OnCombatMusicPhaseChanged(CombatMusicPhase phase)
        {
            gameplayMusic.SetPhase(phase);
            RequestGameplayMusic(2f);
        }

        private void OnMidBossAppeared()
        {
            gameplayMusic.EnterMidBoss();
            RequestGameplayMusic(.9f);
        }

        private void OnMidBossDefeated()
        {
            gameplayMusic.ExitMidBoss();
            RequestGameplayMusic(1.4f);
        }

        private void OnFinalBossMusicStarted()
        {
            gameplayMusic.EnterFinalBoss();
            RequestGameplayMusic(.8f);
        }

        private void OnGameplayMusicEnded()
        {
            gameplayMusic.EndRun();
            GameMusicDirector.Instance?.FadeOut(1.2f);
        }

        private void RequestGameplayMusic(float fadeSeconds)
        {
            GameMusicDirector.EnsureExists();
            GameMusicDirector.Instance?.Request(gameplayMusic.CurrentRole, fadeSeconds);
        }

        private static void OnBossAttackExecuted(BossAttackKind attack)
        {
            PlayCue(attack switch
            {
                BossAttackKind.SuppressionSlam => GameAudioCueId.BossSlam,
                BossAttackKind.BloodCharge => GameAudioCueId.BossCharge,
                BossAttackKind.SpiritVolley => GameAudioCueId.BossVolley,
                _ => GameAudioCueId.None
            });
        }

        private static void PlayCue(GameAudioCueId cue)
        {
            GameAudioDirector.EnsureExists();
            GameAudioDirector.Instance?.TryPlay(cue);
        }

        private void CloseRewardReveal()
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                waitingForRewardReveal = false;
                waitingForChoiceClose = false;
                rewardReveal?.HideImmediately();
                affixReveal?.HideImmediately();
                hasPendingReward = false;
                weaponRack?.ResetPulses();
                SetBackgroundRaycastsEnabled(true);
                SetModalScrimVisible(false);
            }
        }

        private void NotifyUpgradePresentationClosed()
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                waitingForChoiceClose = false;
                PlayPendingRewardAfterChoiceClose();
                NotifyUpgradeWhenPresentationComplete();
            }
        }

        private void PlayPendingRewardAfterChoiceClose()
        {
            if (!hasPendingReward) return;
            var reward = pendingReward;
            hasPendingReward = false;
            // Weapon selections use the appraisal sheet. Support/evolution keep their existing reveal.
            if (reward.Kind != ProgressionRewardKind.Support && reward.Kind != ProgressionRewardKind.Evolution && reward.AffixResult != null)
            {
                var state = boundController.UiState;
                var found = false;
                var slot = default(WeaponSlotView);
                for (var index = 0; index < state.Weapons.Count; index++)
                {
                    if (state.Weapons[index].Id != reward.WeaponId) continue;
                    slot = state.Weapons[index];
                    found = true;
                    break;
                }
                if (found)
                    affixReveal.Play(WeaponAppraisalViewModel.From(reward, slot));
                else
                    affixReveal.Play(reward.AffixResult);
            }
            else
                rewardReveal.Play(reward);
        }

        private void OnRewardRevealCompleted()
        {
            using (FirstPlayableProfilerMarkers.UiModal.Auto())
            {
                waitingForRewardReveal = false;
                NotifyUpgradeWhenPresentationComplete();
            }
        }

        private void NotifyUpgradeWhenPresentationComplete()
        {
            if (waitingForChoiceClose || waitingForRewardReveal) return;
            boundController?.NotifyUpgradePresentationClosed();
            SetBackgroundRaycastsEnabled(true);
            SetModalScrimVisible(false);
        }

        private void OnLobbyReturnRequested()
        {
            boundController?.ReturnToLobby();
        }

        private void OpenAbandonConfirmation()
        {
            if (boundController == null || boundController.UiState.RunEnded || abandonRun == null || abandonRun.IsOpen)
                return;
            if (!boundController.Flow.TryTransition(GameFlowState.Paused)) return;
            PlayCue(GameAudioCueId.PauseOpen);
            SetBackgroundRaycastsEnabled(false);
            SetModalScrimVisible(true);
            abandonRun.Open();
        }

        private void CancelAbandon()
        {
            abandonRun?.CloseImmediately();
            boundController?.Flow.TryTransition(GameFlowState.Playing);
            SetBackgroundRaycastsEnabled(true);
            SetModalScrimVisible(false);
        }

        private void ConfirmAbandon()
        {
            abandonRun?.CloseImmediately();
            SetModalScrimVisible(false);
            OnGameplayMusicEnded();
            boundController?.ConfirmAbandonAndReturn();
        }

        private void CloseAbandonWithoutFlowChange()
        {
            abandonRun?.CloseImmediately();
        }

        private void SetBackgroundRaycastsEnabled(bool enabled)
        {
            if (combatHudGroup != null) combatHudGroup.blocksRaycasts = enabled;
            if (weaponRackGroup != null) weaponRackGroup.blocksRaycasts = enabled;
        }

        private void SetModalScrimVisible(bool visible)
        {
            if (modalScrim != null) modalScrim.SetActive(visible);
        }

        public void ApplySafeArea(Rect safeArea, Vector2 screenSize)
        {
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            if (safeAreaContainer == null || modalSafeAreaContainer == null || screenSize.x <= 0f || screenSize.y <= 0f) return;

            var min = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            var max = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            ApplyNormalizedSafeArea(safeAreaContainer, min, max);
            ApplyNormalizedSafeArea(modalSafeAreaContainer, min, max);
            Canvas.ForceUpdateCanvases();
            combatHud?.ApplyPortraitLayout();
            weaponRack?.ApplyPortraitLayout();
            upgradeChoice?.ApplyPortraitLayout();
            weaponReplacement?.ApplyPortraitLayout();
            weaponLegacyChoice?.ApplyPortraitLayout();
            affixReveal?.ApplyPortraitLayout();
            runResult?.ApplyPortraitLayout();
            abandonRun?.ApplyPortraitLayout();
        }

        private static void ApplyNormalizedSafeArea(RectTransform container, Vector2 min, Vector2 max)
        {
            container.anchorMin = min;
            container.anchorMax = max;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
        }

        private static int WeaponSignature(FirstPlayableUiState state)
        {
            unchecked
            {
                var signature = state.Weapons.Count;
                for (var index = 0; index < state.Weapons.Count; index++)
                {
                    var weapon = state.Weapons[index];
                    signature = signature * 31 + StableContentHash(weapon.Id);
                    signature = signature * 31 + weapon.Level;
                    signature = signature * 31 + StableContentHash(weapon.GeneralAffixSummary);
                    signature = signature * 31 + StableContentHash(weapon.LegacyName);
                    signature = signature * 31 + StableContentHash(weapon.LegacyStageName);
                    signature = signature * 31 + StableContentHash(weapon.NextLegacyMilestone);
                    signature = signature * 31 + weapon.PotentialIds.Count;
                    for (var potentialIndex = 0; potentialIndex < weapon.PotentialIds.Count; potentialIndex++)
                        signature = signature * 31 + StableContentHash(weapon.PotentialIds[potentialIndex].Value);
                    signature = signature * 31 + weapon.GeneralAffixTiers.Count;
                    for (var tierIndex = 0; tierIndex < weapon.GeneralAffixTiers.Count; tierIndex++)
                        signature = signature * 31 + (int)weapon.GeneralAffixTiers[tierIndex];
                }

                return signature;
            }
        }

        private static int StableContentHash(string value)
        {
            unchecked
            {
                var hash = 17;
                if (value == null) return hash;
                for (var index = 0; index < value.Length; index++) hash = hash * 31 + value[index];
                return hash;
            }
        }

#if UNITY_INCLUDE_TESTS
        public static int WeaponSignatureForTests(FirstPlayableUiState state) => WeaponSignature(state);
#endif

        private void BuildCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = PortraitUiMetrics.ReferenceResolution;
            scaler.matchWidthOrHeight = .5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            var systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            EventSystem eventSystem = systems.Length > 0 ? systems[0] : null;
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

            for (var index = 1; index < systems.Length; index++)
            {
                if (systems[index] != null && systems[index] != eventSystem)
                    Destroy(systems[index].gameObject);
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
                if (module != inputModule) Destroy(module);
        }
    }
}
