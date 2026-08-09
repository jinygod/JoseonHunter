using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyWeaponSelectorCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private TMP_Text weaponName;
        [SerializeField] private TMP_Text chevron;

        public Button Button => button;
        public Image Background => button == null ? null : button.targetGraphic as Image ?? button.GetComponent<Image>();
        public Image Icon => icon;
        public TMP_Text Caption => caption;
        public TMP_Text WeaponName => weaponName;
        public TMP_Text Chevron => chevron;
        public bool HasRequiredBindings =>
            button != null && Background != null && icon != null && caption != null && weaponName != null && chevron != null;

        public void Configure(Button cardButton, Image selectorIcon, TMP_Text selectorCaption,
            TMP_Text selectorWeaponName, TMP_Text selectorChevron)
        {
            button = cardButton;
            icon = selectorIcon;
            caption = selectorCaption;
            weaponName = selectorWeaponName;
            chevron = selectorChevron;
        }
    }
}
