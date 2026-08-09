using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    [DisallowMultipleComponent]
    public sealed class LobbyResearchRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private GameObject lockOverlay;

        public TMP_Text StageNameText => stageNameText;
        public TMP_Text StatusText => statusText;
        public TMP_Text EffectText => effectText;
        public TMP_Text RequirementText => requirementText;
        public Button ActionButton => actionButton;
        public TMP_Text ActionText => actionText;
        public GameObject LockOverlay => lockOverlay;
        public bool HasRequiredBindings =>
            stageNameText != null && statusText != null && effectText != null && requirementText != null &&
            actionButton != null && actionText != null && lockOverlay != null;

        public void Configure(TMP_Text stageName, TMP_Text status, TMP_Text effect, TMP_Text requirement,
            Button action, TMP_Text actionLabel, GameObject overlay)
        {
            stageNameText = stageName;
            statusText = status;
            effectText = effect;
            requirementText = requirement;
            actionButton = action;
            actionText = actionLabel;
            lockOverlay = overlay;
        }

        public void Render(string stageName, string status, string effect, string requirement, string action,
            bool locked, bool canAct)
        {
            stageNameText.text = stageName;
            statusText.text = status;
            effectText.text = effect;
            requirementText.text = requirement;
            actionText.text = action;
            actionButton.interactable = canAct;
            lockOverlay.SetActive(locked);
        }
    }
}
