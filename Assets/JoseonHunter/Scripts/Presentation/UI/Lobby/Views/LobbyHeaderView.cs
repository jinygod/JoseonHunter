using JoseonHunter.Domain.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text accountLevelText;
        [SerializeField] private TMP_Text accountNameText;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text accountExperienceText;
        [SerializeField] private Image coinIcon;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button settingsButton;

        public TMP_Text AccountLevelText => accountLevelText;
        public TMP_Text AccountNameText => accountNameText;
        public Image ProgressFill => progressFill;
        public TMP_Text AccountExperienceText => accountExperienceText;
        public Image CoinIcon => coinIcon;
        public TMP_Text CoinsText => coinsText;
        public Button SettingsButton => settingsButton;
        public bool HasRequiredBindings =>
            accountLevelText != null && accountNameText != null && progressFill != null &&
            accountExperienceText != null && coinIcon != null && coinIcon.sprite != null &&
            coinsText != null && settingsButton != null;

        public void Configure(
            TMP_Text accountLevel,
            TMP_Text accountName,
            Image fill,
            TMP_Text experience,
            Image currencyIcon,
            TMP_Text coins,
            Button settings)
        {
            accountLevelText = accountLevel;
            accountNameText = accountName;
            progressFill = fill;
            accountExperienceText = experience;
            coinIcon = currencyIcon;
            coinsText = coins;
            settingsButton = settings;
        }

        public void Render(AccountLevelState account, int coins)
        {
            accountLevelText.text = account.IsMaximumLevel ? "최고 레벨" : account.Level.ToString();
            var progress = account.IsMaximumLevel || account.NextLevelRequirement <= 0
                ? 1f
                : Mathf.Clamp01((float)account.CurrentLevelExperience / account.NextLevelRequirement);
            progressFill.fillAmount = progress;
            accountExperienceText.text = account.IsMaximumLevel
                ? "최고 단계"
                : $"{account.CurrentLevelExperience:N0} / {account.NextLevelRequirement:N0}";
            coinsText.text = Mathf.Max(0, coins).ToString("N0");
        }
    }
}
