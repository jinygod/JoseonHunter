using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    [DisallowMultipleComponent]
    public sealed class LobbyAudioSettingsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider soundEffectSlider;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text soundEffectValue;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image dim;
        [SerializeField] private RectTransform dialog;

        public bool HasRequiredBindings => title != null && musicSlider != null && soundEffectSlider != null && musicValue != null && soundEffectValue != null &&
                                           closeButton != null && dim != null && dialog != null;
        public Slider MusicSlider => musicSlider;
        public Slider SoundEffectSlider => soundEffectSlider;
        public Button CloseButton => closeButton;
        public TMP_Text MusicValue => musicValue;
        public TMP_Text SoundEffectValue => soundEffectValue;

        public void Configure(TMP_Text valueTitle, Slider music, Slider effects, TMP_Text musicLabel, TMP_Text effectsLabel, Button close, Image overlayDim,
            RectTransform overlayDialog)
        {
            title = valueTitle; musicSlider = music; soundEffectSlider = effects; musicValue = musicLabel; soundEffectValue = effectsLabel; closeButton = close;
            dim = overlayDim; dialog = overlayDialog;
        }
    }
}
