using JoseonHunter.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyDifficultyCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image lockSlash;
        [SerializeField] private Image lockIcon;
        [SerializeField] private LockSlashConstraint lockSlashConstraint;
        private bool hasCapturedLabelLayout;
        private Vector2 defaultLabelAnchorMin;
        private Vector2 defaultLabelAnchorMax;
        private Vector2 defaultLabelOffsetMin;
        private Vector2 defaultLabelOffsetMax;

        public Button Button => button;
        public Image Background => button == null ? null : button.targetGraphic as Image ?? button.GetComponent<Image>();
        public TMP_Text Label => labelText;
        public Image LockSlash => lockSlash;
        public Image LockIcon => lockIcon;
        public LockSlashConstraint LockSlashConstraint => lockSlashConstraint;
        public bool HasRequiredBindings =>
            button != null && Background != null && labelText != null &&
            lockSlash != null && lockSlash.name == "Lock Slash" && lockSlash.transform.parent == button.transform &&
            lockIcon != null && lockIcon.name == "Lock Icon" && lockIcon.transform.parent == button.transform &&
            lockSlashConstraint != null && lockSlashConstraint.transform == lockSlash.transform;

        public void Configure(
            Button cardButton,
            TMP_Text label,
            Image authoredLockSlash,
            Image authoredLockIcon,
            LockSlashConstraint authoredLockSlashConstraint)
        {
            button = cardButton;
            labelText = label;
            lockSlash = authoredLockSlash;
            lockIcon = authoredLockIcon;
            lockSlashConstraint = authoredLockSlashConstraint;
        }

        public void Render(string label, bool selected, bool locked)
        {
            labelText.text = label ?? string.Empty;
            button.gameObject.SetActive(true);
            button.interactable = true;
            PremiumPixelUiSkin.ApplyDifficulty(button, selected, locked);
            ApplyLabelLayout(locked);
        }

        private void ApplyLabelLayout(bool locked)
        {
            var rect = labelText.rectTransform;
            if (!hasCapturedLabelLayout)
            {
                hasCapturedLabelLayout = true;
                defaultLabelAnchorMin = rect.anchorMin;
                defaultLabelAnchorMax = rect.anchorMax;
                defaultLabelOffsetMin = rect.offsetMin;
                defaultLabelOffsetMax = rect.offsetMax;
            }

            if (!locked)
            {
                rect.anchorMin = defaultLabelAnchorMin;
                rect.anchorMax = defaultLabelAnchorMax;
                rect.offsetMin = defaultLabelOffsetMin;
                rect.offsetMax = defaultLabelOffsetMax;
                return;
            }

            rect.anchorMin = new Vector2(.07f, .06f);
            rect.anchorMax = new Vector2(.93f, .47f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
