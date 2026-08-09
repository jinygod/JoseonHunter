using System;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Meta;
using JoseonHunter.Presentation.UI.Lobby.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace JoseonHunter.Presentation.UI
{
    public enum AudioSettingsTheme
    {
        Hanji,
        Night
    }

    [DisallowMultipleComponent]
    public sealed class AudioSettingsPresenter : MonoBehaviour
    {
        private const float SaveDelaySeconds = .2f;

        private MetaGameSession session;
        private Slider musicSlider;
        private Slider soundEffectSlider;
        private TextMeshProUGUI musicValue;
        private TextMeshProUGUI soundEffectValue;
        private bool built;
        private bool suppressChanges;
        private bool savePending;
        private float saveAt;
        private UnityAction<float> musicAction;
        private UnityAction<float> soundAction;
        private UnityAction closeAction;
        private Slider boundMusicSlider;
        private Slider boundSoundEffectSlider;
        private Button boundCloseButton;

        public event Action CloseRequested;

        public void InitializeAuthored(LobbyAudioSettingsView view, MetaGameSession value)
        {
            if (view == null || !view.HasRequiredBindings) throw new ArgumentException("Authored audio settings view is incomplete.", nameof(view));
            session = value ?? throw new ArgumentNullException(nameof(value));
            musicSlider = view.MusicSlider;
            soundEffectSlider = view.SoundEffectSlider;
            musicValue = view.MusicValue as TextMeshProUGUI;
            soundEffectValue = view.SoundEffectValue as TextMeshProUGUI;
            if (musicValue == null || soundEffectValue == null) throw new ArgumentException("Authored audio value labels are missing.", nameof(view));
            if (musicAction != null && boundMusicSlider != null) boundMusicSlider.onValueChanged.RemoveListener(musicAction);
            if (soundAction != null && boundSoundEffectSlider != null) boundSoundEffectSlider.onValueChanged.RemoveListener(soundAction);
            if (closeAction != null && boundCloseButton != null) boundCloseButton.onClick.RemoveListener(closeAction);
            musicAction = _ => OnValueChanged(); soundAction = _ => OnValueChanged(); closeAction = RequestClose;
            musicSlider.onValueChanged.AddListener(musicAction);
            soundEffectSlider.onValueChanged.AddListener(soundAction);
            view.CloseButton.onClick.AddListener(closeAction);
            boundMusicSlider = musicSlider; boundSoundEffectSlider = soundEffectSlider; boundCloseButton = view.CloseButton;
            suppressChanges = true;
            musicSlider.SetValueWithoutNotify(Mathf.Clamp01(session.Data.MusicVolume));
            soundEffectSlider.SetValueWithoutNotify(Mathf.Clamp01(session.Data.SoundEffectVolume));
            suppressChanges = false;
            RefreshLabels(); ApplyRuntimeVolumes(musicSlider.value, soundEffectSlider.value);
        }

        private void RequestClose() { CommitPending(); CloseRequested?.Invoke(); }

        public void Initialize(MetaGameSession value, bool showCloseButton,
            AudioSettingsTheme theme = AudioSettingsTheme.Hanji)
        {
            session = value ?? throw new ArgumentNullException(nameof(value));
            Build(showCloseButton, theme);
            suppressChanges = true;
            musicSlider.SetValueWithoutNotify(Mathf.Clamp01(session.Data.MusicVolume));
            soundEffectSlider.SetValueWithoutNotify(Mathf.Clamp01(session.Data.SoundEffectVolume));
            suppressChanges = false;
            RefreshLabels();
            ApplyRuntimeVolumes(musicSlider.value, soundEffectSlider.value);
        }

        public void CommitPending()
        {
            if (!savePending || session == null) return;
            savePending = false;
            session.SaveAudioSettings(musicSlider.value, soundEffectSlider.value);
        }

        public static void ApplySavedVolumes(MetaGameSession value)
        {
            if (value == null) return;
            ApplyRuntimeVolumes(value.Data.MusicVolume, value.Data.SoundEffectVolume);
        }

        private void Update()
        {
            if (savePending && Time.unscaledTime >= saveAt)
                CommitPending();
        }

        private void OnDisable() => CommitPending();

        private void Build(bool showCloseButton, AudioSettingsTheme theme)
        {
            if (built) return;
            built = true;

            var title = RuntimeUiFactory.Text("Audio Settings Title", transform, "소리 설정", 29f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            title.color = TextColor(theme);
            SetRect(title.rectTransform, new Vector2(0f, 70f), new Vector2(560f, 42f));

            musicSlider = BuildSlider("Music Volume Slider", "배경 음악", 18f, theme, out musicValue);
            soundEffectSlider = BuildSlider("Sound Effect Volume Slider", "효과음", -55f,
                theme, out soundEffectValue);
            musicSlider.onValueChanged.AddListener(_ => OnValueChanged());
            soundEffectSlider.onValueChanged.AddListener(_ => OnValueChanged());

            if (!showCloseButton) return;
            var close = RuntimeUiFactory.Button("Close Audio Settings", transform, JoseonUiPalette.AppraisalResult);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0f, -135f), new Vector2(220f, 58f));
            close.onClick.AddListener(() =>
            {
                CommitPending();
                CloseRequested?.Invoke();
            });
            var closeLabel = RuntimeUiFactory.Text("Label", close.transform, "닫기", 23f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            closeLabel.color = JoseonUiPalette.Hanji;
            RuntimeUiFactory.Stretch(closeLabel.rectTransform, 8f, 4f, 8f, 4f);
            JoseonButtonSkin.Apply(close, JoseonButtonStyle.Secondary);
        }

        private Slider BuildSlider(string name, string labelText, float y, AudioSettingsTheme theme,
            out TextMeshProUGUI valueLabel)
        {
            var label = RuntimeUiFactory.Text(name + " Label", transform, labelText, 22f,
                TextAlignmentOptions.Left, RuntimeFontRole.BodyEmphasis);
            label.color = TextColor(theme);
            SetRect(label.rectTransform, new Vector2(-178f, y + 20f), new Vector2(210f, 34f));

            valueLabel = RuntimeUiFactory.Text(name + " Value", transform, "100%", 21f,
                TextAlignmentOptions.Right, RuntimeFontRole.BodyEmphasis);
            valueLabel.color = TextColor(theme);
            SetRect(valueLabel.rectTransform, new Vector2(218f, y + 20f), new Vector2(100f, 34f));

            var track = RuntimeUiFactory.Image(name, transform, new Color(.12f, .09f, .07f, 1f));
            SetRect(track.rectTransform, new Vector2(40f, y - 18f), new Vector2(500f, 22f));
            var slider = track.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            var fillArea = RuntimeUiFactory.Rect("Fill Area", track.transform);
            RuntimeUiFactory.Stretch(fillArea, 4f, 4f, 4f, 4f);
            var fill = RuntimeUiFactory.Image("Fill", fillArea, JoseonUiPalette.Jade);
            RuntimeUiFactory.Stretch(fill.rectTransform, 0f, 0f, 0f, 0f);
            slider.fillRect = fill.rectTransform;

            var handleArea = RuntimeUiFactory.Rect("Handle Slide Area", track.transform);
            RuntimeUiFactory.Stretch(handleArea, 15f, -2f, 15f, -2f);
            var handle = RuntimeUiFactory.Image("Handle", handleArea, JoseonUiPalette.Gold);
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(.5f, .5f);
            handle.rectTransform.sizeDelta = new Vector2(20f, 26f);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        private static Color TextColor(AudioSettingsTheme theme) =>
            theme == AudioSettingsTheme.Night ? JoseonUiPalette.Hanji : JoseonUiPalette.HanjiInk;

        private void OnValueChanged()
        {
            if (suppressChanges) return;
            RefreshLabels();
            ApplyRuntimeVolumes(musicSlider.value, soundEffectSlider.value);
            savePending = true;
            saveAt = Time.unscaledTime + SaveDelaySeconds;
        }

        private void RefreshLabels()
        {
            musicValue.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
            soundEffectValue.text = Mathf.RoundToInt(soundEffectSlider.value * 100f) + "%";
        }

        private static void ApplyRuntimeVolumes(float music, float effects)
        {
            GameMusicDirector.EnsureExists();
            GameAudioDirector.EnsureExists();
            GameMusicDirector.Instance?.SetMasterVolume(music);
            GameAudioDirector.Instance?.SetMasterVolume(effects);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
