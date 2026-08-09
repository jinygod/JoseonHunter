using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyProgressBarView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text valueText;

        public bool HasRequiredBindings => fill != null && valueText != null;

        public void Configure(Image progressFill, TMP_Text value)
        {
            fill = progressFill;
            valueText = value;
        }

        public void Render(float normalized, string label)
        {
            normalized = Mathf.Clamp01(normalized);
            fill.fillAmount = normalized;
            valueText.text = label ?? string.Empty;
        }
    }
}
