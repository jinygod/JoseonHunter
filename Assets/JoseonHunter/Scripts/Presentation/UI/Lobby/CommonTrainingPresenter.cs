using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class CommonTrainingPresenter : MonoBehaviour
    {
        [SerializeField] private TrainingPageView view;

        private readonly Dictionary<Button, UnityAction> rowActions = new();
        private MetaGameSession session;
        private Action refreshHeader;
        private CommonTrainingId selected;
        private UnityAction purchaseAction;
        private UnityAction resetAction;

        public string CurrentTextForTests => view.CurrentEffectText.text;
        public string NextTextForTests => view.NextEffectText.text;
        public string CostTextForTests => view.CostText.text;
        public string CapacityTextForTests => view.CapacityText.text;
        public string FeedbackTextForTests => view.FeedbackText.text;
        public bool PurchaseInteractableForTests => view.PurchaseButton.interactable;
        public string ButtonTextForTests(CommonTrainingId id) => view.Row(id).RankText.text;

        public void ConfigureView(TrainingPageView authoredView) => view = authoredView;

        public void InitializeAuthored(MetaGameSession value, Action onChanged)
        {
            if (view == null || !view.HasRequiredBindings)
                throw new InvalidOperationException("TrainingPageView is incomplete.");

            UnbindListeners();
            session = value ?? throw new ArgumentNullException(nameof(value));
            refreshHeader = onChanged;
            BindListeners();
            Refresh();
        }

        private void BindListeners()
        {
            foreach (var row in view.Rows)
            {
                var id = row.TrainingId;
                UnityAction action = () => Select(id);
                row.Button.onClick.AddListener(action);
                rowActions.Add(row.Button, action);
            }
            purchaseAction = Purchase;
            resetAction = ResetAll;
            view.PurchaseButton.onClick.AddListener(purchaseAction);
            view.ResetButton.onClick.AddListener(resetAction);
        }

        private void UnbindListeners()
        {
            foreach (var binding in rowActions) RemoveOwnedListener(binding.Key, binding.Value);
            rowActions.Clear();
            if (view != null)
            {
                RemoveOwnedListener(view.PurchaseButton, purchaseAction);
                RemoveOwnedListener(view.ResetButton, resetAction);
            }
            purchaseAction = null;
            resetAction = null;
        }

        public void SelectForTests(CommonTrainingId id) => Select(id);
        public void PurchaseForTests() => Purchase();
        public void ResetForTests() => ResetAll();

        private void Select(CommonTrainingId id)
        {
            selected = id;
            view.FeedbackText.text = string.Empty;
            Refresh();
        }

        private void Purchase()
        {
            var result = session.PurchaseTraining(selected);
            view.FeedbackText.text = result.Success ? "수련 성과가 모든 출전에 적용됩니다."
                : result.Error == ProgressionError.InsufficientCoins ? "엽전이 부족합니다."
                : result.Error == ProgressionError.AccountLevelRequired ? CapacityFeedback(new CommonTrainingProgression(session.Data))
                : result.Error == ProgressionError.MaximumReached ? "이미 최대 단계입니다."
                : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private void ResetAll()
        {
            var refund = 0;
            foreach (var spent in session.Data.CommonTrainingSpentCoins.Values) refund += spent;
            var result = session.ResetTraining();
            view.FeedbackText.text = result.Success
                ? $"수련을 초기화하고 엽전 {refund:N0}을 돌려받았습니다."
                : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private void Refresh()
        {
            if (session == null || view == null) return;
            var progression = new CommonTrainingProgression(session.Data);
            var rank = progression.Rank(selected);
            var effect = LobbyViewModels.TrainingEffect(selected);
            var trackMaximum = rank >= CommonTrainingProgression.MaximumRankPerTrack;
            var capacityReached = progression.TotalRanks >= progression.Capacity;
            view.CapacityText.text = $"총 수련 {progression.TotalRanks}/{progression.Capacity} · 계정 {AccountProgression.StateFor(session.Data.AccountExperience).Level}레벨 한도";
            view.CurrentEffectText.text = $"현재 {effect} +{FormatPercent(CommonTrainingProgression.BonusForRank(rank))}%";
            view.NextEffectText.text = trackMaximum ? "최대 단계에 도달했습니다" : $"강화 후 {effect} +{FormatPercent(CommonTrainingProgression.BonusForRank(rank + 1))}%";
            var cost = trackMaximum ? 0 : CommonTrainingProgression.CostForRank(rank + 1);
            view.CostText.text = trackMaximum ? "추가 엽전 필요 없음" : $"필요 엽전 {cost:N0} · 강화 후 {Math.Max(0, session.Data.Coins - cost):N0}";
            view.PurchaseButton.interactable = !trackMaximum && !capacityReached;
            if (!trackMaximum && capacityReached) view.FeedbackText.text = CapacityFeedback(progression);
            foreach (var row in view.Rows)
            {
                var id = row.TrainingId;
                row.Render(LobbyViewModels.TrainingName(id), view.Icon(id), progression.Rank(id),
                    CommonTrainingProgression.MaximumRankPerTrack, selected == id);
            }
        }

        private static void RemoveOwnedListener(Button button, UnityAction action)
        {
            if (button != null && action != null) button.onClick.RemoveListener(action);
        }

        private static string FormatPercent(float bonus) => (bonus * 100f).ToString("0.#");

        private static string CapacityFeedback(CommonTrainingProgression progression) =>
            progression.Capacity >= CommonTrainingProgression.MaximumTotalRanks
                ? "총 수련 최대치에 도달했습니다."
                : $"계정 레벨 {progression.NextCapacityLevel}에서 추가 수련이 열립니다.";

        private void OnDestroy() => UnbindListeners();
    }
}
