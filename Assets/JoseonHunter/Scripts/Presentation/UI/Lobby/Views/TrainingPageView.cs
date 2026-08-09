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
        [SerializeField] private Sprite[] trainingIcons;
        [SerializeField] private TMP_Text currentEffectText;
        [SerializeField] private TMP_Text nextEffectText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text feedbackText;

        public LobbyTrainingRowView[] Rows => rows;
        public Sprite[] TrainingIcons => trainingIcons;
        public TMP_Text CurrentEffectText => currentEffectText;
        public TMP_Text NextEffectText => nextEffectText;
        public TMP_Text CostText => costText;
        public TMP_Text CapacityText => capacityText;
        public Button PurchaseButton => purchaseButton;
        public Button ResetButton => resetButton;
        public TMP_Text FeedbackText => feedbackText;
        public bool HasRequiredBindings =>
            rows != null && rows.Length == 6 && trainingIcons != null && trainingIcons.Length == 6 &&
            currentEffectText != null && nextEffectText != null && costText != null && capacityText != null &&
            purchaseButton != null && resetButton != null && feedbackText != null &&
            RowsMatchTrainingIds() && IconsAreBound();

        public void Configure(LobbyTrainingRowView[] trainingRows, Sprite[] icons, TMP_Text current,
            TMP_Text next, TMP_Text cost, TMP_Text capacity, Button purchase, Button reset, TMP_Text feedback)
        {
            rows = trainingRows;
            trainingIcons = icons;
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
            trainingIcons != null && (int)id >= 0 && (int)id < trainingIcons.Length ? trainingIcons[(int)id] : null;

        private bool RowsMatchTrainingIds()
        {
            for (var index = 0; index < rows.Length; index++)
                if (rows[index] == null || !rows[index].HasRequiredBindings || rows[index].TrainingId != (CommonTrainingId)index)
                    return false;
            return true;
        }

        private bool IconsAreBound()
        {
            for (var index = 0; index < trainingIcons.Length; index++)
                if (trainingIcons[index] == null) return false;
            return true;
        }
    }
}
