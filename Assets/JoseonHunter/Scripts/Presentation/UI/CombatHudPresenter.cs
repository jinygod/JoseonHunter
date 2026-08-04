using System;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class CombatHudPresenter : MonoBehaviour
    {
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI healthText;
        private TextMeshProUGUI experienceText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI killsText;
        private TextMeshProUGUI bossWarningText;
        private TextMeshProUGUI waveAnnouncementText;
        private TextMeshProUGUI bossText;
        private Image healthFill;
        private Image experienceFill;
        private Image bossFill;
        private GameObject bossRoot;
        private RectTransform vitalsRect;

        public event Action ReturnRequested;

        public void Build()
        {
            if (levelText != null) return;

            var topLeft = RuntimeUiFactory.Image("Vitals", transform, JoseonUiPalette.Ink).rectTransform;
            vitalsRect = topLeft;
            topLeft.anchorMin = topLeft.anchorMax = new Vector2(.5f, 1f);
            topLeft.pivot = new Vector2(.5f, 1f);
            topLeft.anchoredPosition = new Vector2(0f, -PortraitUiMetrics.TopMargin);
            topLeft.sizeDelta = new Vector2(984f, 176f);
            levelText = Label("Level", topLeft, new Vector2(18f, -14f), new Vector2(190f, 42f), 30f, TextAlignmentOptions.Left);
            healthText = Label("Health", topLeft, new Vector2(18f, -58f), new Vector2(360f, 34f), 23f, TextAlignmentOptions.Left);
            healthFill = Bar("Health Fill", topLeft, new Vector2(18f, -96f), new Vector2(360f, 16f), JoseonUiPalette.Crimson);
            experienceText = Label("Experience", topLeft, new Vector2(18f, -119f), new Vector2(360f, 28f), 19f, TextAlignmentOptions.Left);
            experienceFill = Bar("Experience Fill", topLeft, new Vector2(18f, -150f), new Vector2(360f, 12f), JoseonUiPalette.Jade);

            var topRight = RuntimeUiFactory.Rect("Run Stats", topLeft);
            topRight.anchorMin = topRight.anchorMax = new Vector2(.5f, .5f);
            topRight.pivot = new Vector2(.5f, .5f);
            topRight.anchoredPosition = Vector2.zero;
            topRight.sizeDelta = new Vector2(250f, 112f);
            timerText = Label("Timer", topRight, new Vector2(0f, -14f), new Vector2(250f, 45f), 34f, TextAlignmentOptions.Center);
            killsText = Label("Kills", topRight, new Vector2(0f, -62f), new Vector2(250f, 32f), 22f, TextAlignmentOptions.Center);

            bossWarningText = Label("Boss Warning", transform, new Vector2(0f, -260f), new Vector2(640f, 64f), 28f, TextAlignmentOptions.Center);
            var warningRect = bossWarningText.rectTransform;
            warningRect.anchorMin = warningRect.anchorMax = new Vector2(.5f, 1f);
            warningRect.pivot = new Vector2(.5f, 1f);
            warningRect.gameObject.AddComponent<Outline>().effectColor = JoseonUiPalette.Ink;

            waveAnnouncementText = Label("Wave Announcement", transform, new Vector2(0f, -184f),
                new Vector2(760f, 76f), 32f, TextAlignmentOptions.Center);
            var waveRect = waveAnnouncementText.rectTransform;
            waveRect.anchorMin = waveRect.anchorMax = new Vector2(.5f, 1f);
            waveRect.pivot = new Vector2(.5f, 1f);
            waveAnnouncementText.fontStyle = FontStyles.Bold;
            waveAnnouncementText.gameObject.AddComponent<Outline>().effectColor =
                new Color(.025f, .02f, .03f, .96f);
            waveAnnouncementText.gameObject.SetActive(false);

            bossRoot = RuntimeUiFactory.Image("Boss Health", transform, JoseonUiPalette.Ink).gameObject;
            var bossRect = bossRoot.GetComponent<RectTransform>();
            bossRect.anchorMin = bossRect.anchorMax = new Vector2(.5f, 1f);
            bossRect.pivot = new Vector2(.5f, 1f);
            bossRect.anchoredPosition = new Vector2(0f, -224f);
            bossRect.sizeDelta = new Vector2(936f, 82f);
            bossText = Label("Boss Label", bossRect, new Vector2(0f, -8f), new Vector2(590f, 26f), 21f, TextAlignmentOptions.Center);
            bossFill = Bar("Boss Fill", bossRect, new Vector2(15f, -49f), new Vector2(590f, 18f), JoseonUiPalette.Crimson);
            bossRoot.SetActive(false);

            var returnButton = RuntimeUiFactory.Button("Pause Button", topLeft, new Color(.12f, .10f, .09f, 1f));
            var returnRect = returnButton.GetComponent<RectTransform>();
            returnRect.anchorMin = returnRect.anchorMax = new Vector2(1f, 1f);
            returnRect.pivot = new Vector2(1f, 1f);
            returnRect.anchoredPosition = new Vector2(-14f, -14f);
            returnRect.sizeDelta = new Vector2(64f, 64f);
            returnButton.onClick.AddListener(() => ReturnRequested?.Invoke());
            PauseBar("Pause Bar Left", returnButton.transform, -9f);
            PauseBar("Pause Bar Right", returnButton.transform, 9f);
            ApplyPortraitLayout();
        }

        private static void PauseBar(string name, Transform parent, float x)
        {
            var bar = RuntimeUiFactory.Image(name, parent, JoseonUiPalette.Hanji);
            var rect = bar.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(9f, 30f);
            bar.raycastTarget = false;
        }

        public void ApplyPortraitLayout()
        {
            if (vitalsRect == null) return;
            vitalsRect.sizeDelta = new Vector2(PortraitUiMetrics.ContainedWidth(transform as RectTransform, 984f), 176f);
            if (bossRoot != null)
            {
                var bossRect = bossRoot.GetComponent<RectTransform>();
                bossRect.sizeDelta = new Vector2(PortraitUiMetrics.ContainedWidth(transform as RectTransform, 936f), 82f);
            }
        }

        public void Render(FirstPlayableUiState state)
        {
            Build();
            levelText.text = $"레벨 {state.Level}";
            healthText.text = $"체력 {Mathf.CeilToInt(state.Health)} / {Mathf.CeilToInt(state.MaximumHealth)}";
            experienceText.text = $"경험치 {state.Experience} / {state.ExperienceToNext}    엽전 {state.Coins}";
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, state.Duration - state.Elapsed)).ToString("00");
            killsText.text = $"처치 {state.Kills}";
            SetFill(healthFill, state.Health, state.MaximumHealth);
            SetFill(experienceFill, state.Experience, state.ExperienceToNext);
            bossWarningText.gameObject.SetActive(state.BossWarning);
            bossWarningText.text = "강한 기운이 다가옵니다";
            var showWave = state.WaveAnnouncementRemaining > 0f &&
                           !string.IsNullOrWhiteSpace(state.WaveAnnouncement);
            waveAnnouncementText.gameObject.SetActive(showWave);
            if (showWave)
            {
                waveAnnouncementText.text = state.WaveAnnouncement;
                waveAnnouncementText.color = state.WaveAnnouncementIntensity >= 3
                    ? new Color(1f, .35f, .18f)
                    : state.WaveAnnouncementIntensity >= 2
                        ? new Color(1f, .78f, .28f)
                        : new Color(.62f, 1f, .94f);
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 18f) *
                    (state.WaveAnnouncementIntensity >= 3 ? .055f : .035f);
                waveAnnouncementText.rectTransform.localScale = Vector3.one * pulse;
            }
            bossRoot.SetActive(state.BossAlive);
            if (state.BossAlive)
            {
                bossText.text = $"우두머리  {Mathf.CeilToInt(state.BossHealth)} / {Mathf.CeilToInt(state.BossMaximumHealth)}";
                SetFill(bossFill, state.BossHealth, state.BossMaximumHealth);
            }
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment)
        {
            var label = RuntimeUiFactory.Text(name, parent, string.Empty, fontSize, alignment);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }

        private static Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var background = RuntimeUiFactory.Image(name + " Background", parent, new Color(0f, 0f, 0f, .45f));
            var rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var fill = RuntimeUiFactory.Image(name, background.transform, color);
            RuntimeUiFactory.Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            return fill;
        }

        private static void SetFill(Image fill, float current, float maximum)
        {
            fill.fillAmount = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
        }
    }
}
