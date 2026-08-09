using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Audio;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class PatrolPresenter : MonoBehaviour
    {
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private PatrolPageView view;

        private readonly Dictionary<Button, UnityAction> weaponOptionActions = new();
        private MetaGameSession session;
        private Action refreshHeader;
        private WeaponId selectedWeapon = WeaponId.HwandoFlyingBlade;
        private int viewedStageIndex;
        private StageDifficulty viewedDifficulty = StageDifficulty.Normal;
        private string stageFeedback = string.Empty;
        private UnityAction weaponSelectorAction;
        private UnityAction closeWeaponSelectionAction;
        private UnityAction patrolAction;
        private UnityAction previousStageAction;
        private UnityAction nextStageAction;
        private UnityAction normalDifficultyAction;
        private UnityAction omenDifficultyAction;
        private UnityAction greatOmenDifficultyAction;

        public void ConfigureView(PatrolPageView authoredView) => view = authoredView;

        public void InitializeAuthored(MetaGameSession value, Action onChanged)
        {
            if (view == null || !view.HasRequiredBindings)
                throw new InvalidOperationException("PatrolPageView is incomplete.");

            UnbindListeners();
            session = value ?? throw new ArgumentNullException(nameof(value));
            refreshHeader = onChanged;
            ApplyAuthoredCopyAndStyle();
            GameAudioButtonFeedback.Attach(view.StartButton, GameAudioCueId.UiConfirm);
            LoadCurrentWeapon();
            LoadCurrentStage();
            BindListeners();
            BindWeaponOptions();
            view.WeaponSelectionOverlay.SetActive(false);
            Refresh();
        }

        public void ConfigureCatalog(WeaponCatalogAsset value) => weaponCatalog = value;

        public void SelectStartingWeaponForTests(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            SaveCurrentWeapon();
            Refresh();
        }

        private void ApplyAuthoredCopyAndStyle()
        {
            var actionLabel = view.StartButton.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            if (actionLabel != null) actionLabel.text = "출전 시작";
            view.PageHeader.Title.text = "출전";
            view.WeaponSelector.Caption.text = "시작 무기";
            PremiumPixelUiSkin.ApplyAction(view.StartButton, PremiumActionStyle.Primary);
            PremiumPixelUiSkin.ApplyAction(view.CloseWeaponSelectionButton, PremiumActionStyle.Secondary);
        }

        private void BindListeners()
        {
            weaponSelectorAction = OpenWeaponSelection;
            closeWeaponSelectionAction = CloseWeaponSelection;
            patrolAction = StartPatrol;
            previousStageAction = () => BrowseStage(-1);
            nextStageAction = () => BrowseStage(1);
            normalDifficultyAction = () => SelectDifficulty(StageDifficulty.Normal);
            omenDifficultyAction = () => SelectDifficulty(StageDifficulty.Omen);
            greatOmenDifficultyAction = () => SelectDifficulty(StageDifficulty.GreatOmen);

            view.WeaponSelector.Button.onClick.AddListener(weaponSelectorAction);
            view.CloseWeaponSelectionButton.onClick.AddListener(closeWeaponSelectionAction);
            view.StartButton.onClick.AddListener(patrolAction);
            view.PreviousStageButton.onClick.AddListener(previousStageAction);
            view.NextStageButton.onClick.AddListener(nextStageAction);
            view.NormalDifficulty.Button.onClick.AddListener(normalDifficultyAction);
            view.OmenDifficulty.Button.onClick.AddListener(omenDifficultyAction);
            view.GreatOmenDifficulty.Button.onClick.AddListener(greatOmenDifficultyAction);
        }

        private void BindWeaponOptions()
        {
            foreach (var weaponId in WeaponRoster.All)
            {
                var optionTransform = view.WeaponSelectionOverlay.transform.Find(
                    $"Weapon Selection Panel/Weapon Grid/Weapon Option {weaponId.Value}");
                if (optionTransform == null) continue;
                var option = optionTransform.GetComponent<Button>();
                if (option == null) continue;
                UnityAction action = () => SelectWeapon(weaponId);
                option.onClick.AddListener(action);
                weaponOptionActions[option] = action;
                var icon = optionTransform.Find("Weapon Option Icon")?.GetComponent<Image>();
                if (icon == null) continue;
                icon.sprite = ResolveWeaponSprite(weaponId);
                icon.enabled = icon.sprite != null;
            }
        }

        private void UnbindListeners()
        {
            if (view != null)
            {
                RemoveOwnedListener(view.WeaponSelector?.Button, weaponSelectorAction);
                RemoveOwnedListener(view.CloseWeaponSelectionButton, closeWeaponSelectionAction);
                RemoveOwnedListener(view.StartButton, patrolAction);
                RemoveOwnedListener(view.PreviousStageButton, previousStageAction);
                RemoveOwnedListener(view.NextStageButton, nextStageAction);
                RemoveOwnedListener(view.NormalDifficulty?.Button, normalDifficultyAction);
                RemoveOwnedListener(view.OmenDifficulty?.Button, omenDifficultyAction);
                RemoveOwnedListener(view.GreatOmenDifficulty?.Button, greatOmenDifficultyAction);
            }
            foreach (var binding in weaponOptionActions) RemoveOwnedListener(binding.Key, binding.Value);
            weaponOptionActions.Clear();
            weaponSelectorAction = null;
            closeWeaponSelectionAction = null;
            patrolAction = null;
            previousStageAction = null;
            nextStageAction = null;
            normalDifficultyAction = null;
            omenDifficultyAction = null;
            greatOmenDifficultyAction = null;
        }

        private static void RemoveOwnedListener(Button button, UnityAction action)
        {
            if (button != null && action != null) button.onClick.RemoveListener(action);
        }

        private void LoadCurrentWeapon()
        {
            var id = session.ActiveLoadout.StartingWeapon.Value;
            selectedWeapon = WeaponRoster.All.FirstOrDefault(weapon => weapon.Value == id);
            if (string.IsNullOrEmpty(selectedWeapon.Value)) selectedWeapon = WeaponId.HwandoFlyingBlade;
        }

        private void LoadCurrentStage()
        {
            var selection = session.ActiveStageSelection;
            viewedStageIndex = Mathf.Max(0, StageCatalog.IndexOf(selection.StageId));
            viewedDifficulty = selection.Difficulty;
            stageFeedback = string.Empty;
        }

        private void BrowseStage(int direction)
        {
            viewedStageIndex = Mathf.Clamp(viewedStageIndex + direction, 0, StageCatalog.All.Count - 1);
            viewedDifficulty = StageDifficulty.Normal;
            stageFeedback = string.Empty;
            Refresh();
        }

        private void SelectDifficulty(StageDifficulty difficulty)
        {
            var selection = new StageSelection(StageCatalog.All[viewedStageIndex].Id, difficulty);
            var records = StageClearRecordData.DomainRecords(session.Data.StageClearRecords);
            if (!StageUnlockRules.IsUnlocked(selection, records))
            {
                stageFeedback = StageUnlockRules.LockReason(selection, records);
                Refresh();
                return;
            }
            if (!session.SaveStageSelection(selection).Success)
            {
                stageFeedback = "출전 정보를 저장하지 못했습니다";
                Refresh();
                return;
            }
            viewedDifficulty = difficulty;
            stageFeedback = string.Empty;
            refreshHeader?.Invoke();
            Refresh();
        }

        private void OpenWeaponSelection()
        {
            view.WeaponSelectionOverlay.transform.SetAsLastSibling();
            view.WeaponSelectionOverlay.SetActive(true);
        }

        private void CloseWeaponSelection() => view.WeaponSelectionOverlay.SetActive(false);

        private void SelectWeapon(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            if (!SaveCurrentWeapon()) return;
            Refresh();
            CloseWeaponSelection();
        }

        private bool SaveCurrentWeapon()
        {
            var current = session.ActiveLoadout;
            var loadout = new PatrolLoadout(current.Name, selectedWeapon, current.Styles, current.DifficultyId);
            var result = session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex, loadout);
            view.Feedback.text = result.Success ? string.Empty : "무기를 저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            return result.Success;
        }

        private void StartPatrol()
        {
            if (session.Router.IsRouting) return;
            var definition = StageCatalog.All[viewedStageIndex];
            var selection = new StageSelection(definition.Id, viewedDifficulty);
            var records = StageClearRecordData.DomainRecords(session.Data.StageClearRecords);
            if (!StageUnlockRules.IsUnlocked(selection, records))
            {
                stageFeedback = StageUnlockRules.LockReason(selection, records);
                Refresh();
                return;
            }
            if (!definition.HasPlayableContent)
            {
                stageFeedback = "아직 준비 중인 지역입니다";
                Refresh();
                return;
            }
            if (!session.SaveStageSelection(selection).Success || !SaveCurrentWeapon()) return;
            view.StartButton.interactable = false;
            session.SetPendingDestination("Gameplay");
            StartCoroutine(LoadBootstrap());
        }

        private IEnumerator LoadBootstrap()
        {
            yield return session.Router.LoadBootstrap();
            if (view != null && view.StartButton != null) view.StartButton.interactable = true;
        }

        private Sprite ResolveWeaponSprite(WeaponId id)
        {
            if (weaponCatalog == null || !weaponCatalog.TryGet(id, out var definition)) return null;
            return definition.UiIcon != null ? definition.UiIcon : definition.PresentationSprites.FirstOrDefault();
        }

        private void Refresh()
        {
            if (session == null || view == null) return;
            view.WeaponSelector.WeaponName.text = LobbyViewModels.WeaponName(selectedWeapon);
            view.WeaponSelector.Icon.sprite = ResolveWeaponSprite(selectedWeapon);
            view.WeaponSelector.Icon.enabled = view.WeaponSelector.Icon.sprite != null;

            var definition = StageCatalog.All[viewedStageIndex];
            var selection = new StageSelection(definition.Id, viewedDifficulty);
            var records = StageClearRecordData.DomainRecords(session.Data.StageClearRecords);
            var unlocked = StageUnlockRules.IsUnlocked(selection, records);
            view.StageName.text = $"{viewedStageIndex + 1}장 · {definition.DisplayName}";
            view.StageStatus.text = string.Empty;
            view.StageStatus.gameObject.SetActive(false);
            view.PreviousStageButton.interactable = viewedStageIndex > 0;
            view.NextStageButton.interactable = viewedStageIndex < StageCatalog.All.Count - 1;
            RefreshDifficultyCard(view.NormalDifficulty, StageDifficulty.Normal, records);
            RefreshDifficultyCard(view.OmenDifficulty, StageDifficulty.Omen, records);
            RefreshDifficultyCard(view.GreatOmenDifficulty, StageDifficulty.GreatOmen, records);
            view.StartButton.interactable = unlocked && definition.HasPlayableContent && !session.Router.IsRouting;

            view.Feedback.text = !string.IsNullOrEmpty(stageFeedback) ? stageFeedback : !unlocked
                ? StageUnlockRules.LockReason(selection, records) : !definition.HasPlayableContent
                    ? "아직 준비 중인 지역입니다" : string.Empty;
        }

        private void RefreshDifficultyCard(LobbyDifficultyCardView card, StageDifficulty difficulty,
            IReadOnlyCollection<StageClearRecord> records)
        {
            var selection = new StageSelection(StageCatalog.All[viewedStageIndex].Id, difficulty);
            card.Render(StageDifficultyNames.DisplayName(difficulty), viewedDifficulty == difficulty,
                !StageUnlockRules.IsUnlocked(selection, records));
        }

        private void OnDisable()
        {
            if (view != null && view.WeaponSelectionOverlay != null) view.WeaponSelectionOverlay.SetActive(false);
        }

        private void OnDestroy() => UnbindListeners();
    }
}
