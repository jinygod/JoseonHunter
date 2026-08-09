using System;
using JoseonHunter.Domain.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    [DisallowMultipleComponent]
    public sealed class TrainingPageView : MonoBehaviour
    {
        [SerializeField] private LobbyTrainingRowView[] rows;
        [SerializeField] private LobbyTrainingIconSet trainingIconSet;
        [SerializeField] private TMP_Text currentEffectText;
        [SerializeField] private TMP_Text nextEffectText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text feedbackText;

        public LobbyTrainingRowView[] Rows => rows;
        public LobbyTrainingIconSet TrainingIconSet => trainingIconSet;
        public TMP_Text CurrentEffectText => currentEffectText;
        public TMP_Text NextEffectText => nextEffectText;
        public TMP_Text CostText => costText;
        public TMP_Text CapacityText => capacityText;
        public Button PurchaseButton => purchaseButton;
        public Button ResetButton => resetButton;
        public TMP_Text FeedbackText => feedbackText;
        public bool HasRequiredBindings =>
            rows != null && rows.Length == 6 && trainingIconSet != null && trainingIconSet.HasExactBindings &&
            currentEffectText != null && nextEffectText != null && costText != null && capacityText != null &&
            purchaseButton != null && resetButton != null && feedbackText != null &&
            RowsMatchTrainingIds() && HasUniqueReferences();

        public void Configure(LobbyTrainingRowView[] trainingRows, LobbyTrainingIconSet iconSet, TMP_Text current,
            TMP_Text next, TMP_Text cost, TMP_Text capacity, Button purchase, Button reset, TMP_Text feedback)
        {
            rows = trainingRows;
            trainingIconSet = iconSet;
            currentEffectText = current;
            nextEffectText = next;
            costText = cost;
            capacityText = capacity;
            purchaseButton = purchase;
            resetButton = reset;
            feedbackText = feedback;
        }

        public LobbyTrainingRowView Row(CommonTrainingId id) =>
            rows != null && (int)id >= 0 && (int)id < rows.Length ? rows[(int)id] : null;

        public Sprite Icon(CommonTrainingId id) =>
            trainingIconSet == null ? null : trainingIconSet.Icon(id);

        private bool RowsMatchTrainingIds()
        {
            for (var index = 0; index < rows.Length; index++)
                if (rows[index] == null || !rows[index].HasRequiredBindings || rows[index].TrainingId != (CommonTrainingId)index)
                    return false;
            return true;
        }

        private bool HasUniqueReferences()
        {
            for (var index = 0; index < rows.Length; index++)
            {
                for (var other = index + 1; other < rows.Length; other++)
                    if (rows[index] == rows[other] || rows[index].Button == rows[other].Button) return false;
            }
            return true;
        }
    }
}
