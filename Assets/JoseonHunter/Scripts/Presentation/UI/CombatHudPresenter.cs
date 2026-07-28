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
        private TextMeshProUGUI bossText;
        private Image healthFill;
        private Image experienceFill;
        private Image bossFill;
        private GameObject bossRoot;

        public void Build()
        {
            if (levelText != null) return;

            var topLeft = RuntimeUiFactory.Image("Vitals", transform, JoseonUiPalette.Ink).rectTransform;
            topLeft.anchorMin = topLeft.anchorMax = new Vector2(0f, 1f);
            topLeft.pivot = new Vector2(0f, 1f);
            topLeft.anchoredPosition = new Vector2(42f, -42f);
            topLeft.sizeDelta = new Vector2(420f, 174f);
            levelText = Label("Level", topLeft, new Vector2(18f, -14f), new Vector2(150f, 42f), 30f, TextAlignmentOptions.Left);
            healthText = Label("Health", topLeft, new Vector2(18f, -58f), new Vector2(384f, 34f), 23f, TextAlignmentOptions.Left);
            healthFill = Bar("Health Fill", topLeft, new Vector2(18f, -96f), new Vector2(384f, 16f), JoseonUiPalette.Crimson);
            experienceText = Label("Experience", topLeft, new Vector2(18f, -119f), new Vector2(384f, 28f), 19f, TextAlignmentOptions.Left);
            experienceFill = Bar("Experience Fill", topLeft, new Vector2(18f, -150f), new Vector2(384f, 12f), JoseonUiPalette.Jade);

            var topRight = RuntimeUiFactory.Image("Run Stats", transform, JoseonUiPalette.Ink).rectTransform;
            topRight.anchorMin = topRight.anchorMax = new Vector2(1f, 1f);
            topRight.pivot = new Vector2(1f, 1f);
            topRight.anchoredPosition = new Vector2(-42f, -42f);
            topRight.sizeDelta = new Vector2(250f, 112f);
            timerText = Label("Timer", topRight, new Vector2(-14f, -14f), new Vector2(222f, 45f), 34f, TextAlignmentOptions.Right);
            killsText = Label("Kills", topRight, new Vector2(-14f, -62f), new Vector2(222f, 32f), 22f, TextAlignmentOptions.Right);

            bossWarningText = Label("Boss Warning", transform, new Vector2(0f, -260f), new Vector2(640f, 64f), 28f, TextAlignmentOptions.Center);
            var warningRect = bossWarningText.rectTransform;
            warningRect.anchorMin = warningRect.anchorMax = new Vector2(.5f, 1f);
            warningRect.pivot = new Vector2(.5f, 1f);
            warningRect.gameObject.AddComponent<Outline>().effectColor = JoseonUiPalette.Ink;

            bossRoot = RuntimeUiFactory.Image("Boss Health", transform, JoseonUiPalette.Ink).gameObject;
            var bossRect = bossRoot.GetComponent<RectTransform>();
            bossRect.anchorMin = bossRect.anchorMax = new Vector2(.5f, 1f);
            bossRect.pivot = new Vector2(.5f, 1f);
            bossRect.anchoredPosition = new Vector2(0f, -340f);
            bossRect.sizeDelta = new Vector2(620f, 82f);
            bossText = Label("Boss Label", bossRect, new Vector2(0f, -8f), new Vector2(590f, 26f), 21f, TextAlignmentOptions.Center);
            bossFill = Bar("Boss Fill", bossRect, new Vector2(15f, -49f), new Vector2(590f, 18f), JoseonUiPalette.Crimson);
            bossRoot.SetActive(false);
        }

        public void Render(FirstPlayableUiState state)
        {
            Build();
            levelText.text = $"LEVEL {state.Level}";
            healthText.text = $"HP {Mathf.CeilToInt(state.Health)} / {Mathf.CeilToInt(state.MaximumHealth)}";
            experienceText.text = $"XP {state.Experience} / {state.ExperienceToNext}    COIN {state.Coins}";
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, state.Duration - state.Elapsed)).ToString("00");
            killsText.text = $"KILLS {state.Kills}";
            SetFill(healthFill, state.Health, state.MaximumHealth);
            SetFill(experienceFill, state.Experience, state.ExperienceToNext);
            bossWarningText.gameObject.SetActive(state.BossWarning);
            bossWarningText.text = "A DREADFUL PRESENCE APPROACHES";
            bossRoot.SetActive(state.BossAlive);
            if (state.BossAlive)
            {
                bossText.text = $"BOSS  {Mathf.CeilToInt(state.BossHealth)} / {Mathf.CeilToInt(state.BossMaximumHealth)}";
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
