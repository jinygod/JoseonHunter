using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    public static class LobbySelectionChrome
    {
        public static void Apply(Button button, bool selected, bool locked = false)
        {
            PremiumPixelUiSkin.ApplyDifficulty(button, selected, locked);
        }

        public static void ApplyNavigation(Button button, PremiumIcon icon, bool selected)
        {
            PremiumPixelUiSkin.ApplyNavigation(button, icon, selected);
        }
    }
}
