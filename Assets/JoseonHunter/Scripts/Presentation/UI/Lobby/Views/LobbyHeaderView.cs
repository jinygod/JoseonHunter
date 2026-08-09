using JoseonHunter.Domain.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text accountLevelText;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text coinsText;

        public bool HasRequiredBindings => accountLevelText != null && progressFill != null && coinsText != null;

        public void Configure(TMP_Text accountLevel, Image fill, TMP_Text coins)
        {
            accountLevelText = accountLevel;
            progressFill = fill;
            coinsText = coins;
        }

        public void Render(AccountLevelState account, int coins)
        {
            accountLevelText.text = account.IsMaximumLevel ? "최고 레벨" : $"레벨 {account.Level}";
            progressFill.fillAmount = account.IsMaximumLevel || account.NextLevelRequirement <= 0
                ? 1f
                : Mathf.Clamp01((float)account.CurrentLevelExperience / account.NextLevelRequirement);
            coinsText.text = Mathf.Max(0, coins).ToString("N0");
        }
    }
}
