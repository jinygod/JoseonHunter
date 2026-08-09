using JoseonHunter.Presentation.UI.Lobby.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    [DisallowMultipleComponent]
    public sealed class ResearchPageView : MonoBehaviour
    {
        [SerializeField] private LobbyWeaponSelectorCardView[] weaponSelectors;
        [SerializeField] private Image selectedWeaponIcon;
        [SerializeField] private TMP_Text selectedWeaponName;
        [SerializeField] private LobbyProgressBarView masteryProgress;
        [SerializeField] private LobbyResearchRowView[] rows;
        [SerializeField] private TMP_Text feedbackText;

        public LobbyWeaponSelectorCardView[] WeaponSelectors => weaponSelectors;
        public Image SelectedWeaponIcon => selectedWeaponIcon;
        public TMP_Text SelectedWeaponName => selectedWeaponName;
        public LobbyProgressBarView MasteryProgress => masteryProgress;
        public LobbyResearchRowView[] Rows => rows;
        public TMP_Text FeedbackText => feedbackText;
        public bool HasRequiredBindings =>
            weaponSelectors != null && weaponSelectors.Length == 8 && selectedWeaponIcon != null &&
            selectedWeaponName != null && masteryProgress != null && masteryProgress.HasRequiredBindings &&
            rows != null && rows.Length == 3 && feedbackText != null && HasUniqueBindings();

        public void Configure(LobbyWeaponSelectorCardView[] selectors, Image icon, TMP_Text name,
            LobbyProgressBarView progress, LobbyResearchRowView[] researchRows, TMP_Text feedback)
        {
            weaponSelectors = selectors;
            selectedWeaponIcon = icon;
            selectedWeaponName = name;
            masteryProgress = progress;
            rows = researchRows;
            feedbackText = feedback;
        }

        private bool HasUniqueBindings()
        {
            for (var index = 0; index < weaponSelectors.Length; index++)
            {
                if (weaponSelectors[index] == null || !weaponSelectors[index].HasRequiredBindings) return false;
                for (var other = index + 1; other < weaponSelectors.Length; other++)
                    if (weaponSelectors[index] == weaponSelectors[other] ||
                        weaponSelectors[index].Button == weaponSelectors[other].Button) return false;
            }

            for (var index = 0; index < rows.Length; index++)
            {
                if (rows[index] == null || !rows[index].HasRequiredBindings) return false;
                for (var other = index + 1; other < rows.Length; other++)
                    if (rows[index] == rows[other] || rows[index].ActionButton == rows[other].ActionButton) return false;
                for (var selector = 0; selector < weaponSelectors.Length; selector++)
                    if (rows[index].ActionButton == weaponSelectors[selector].Button) return false;
            }

            return true;
        }
    }
}
