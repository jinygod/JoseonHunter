using System;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class RunResultPresenter : MonoBehaviour
    {
        private GameObject resultRoot;
        private RectTransform panelRect;
        private TextMeshProUGUI title;
        private TextMeshProUGUI summary;
        private TextMeshProUGUI restartLabel;
        private Button restartButton;

        public event Action RestartRequested;

        public void Render(FirstPlayableUiState state)
        {
            Build();
            resultRoot.SetActive(state.RunEnded);
            if (!state.RunEnded) return;

            title.text = state.Victory ? "승전" : "전투 종료";
            summary.text =
                $"생존 시간  {state.Elapsed:0.0}초\n\n" +
                $"처치  {state.Kills}\n\n" +
                $"도달 레벨  {state.Level}\n\n" +
                $"획득 엽전  {state.Coins}";
        }

        public void ApplyPortraitLayout()
        {
            if (panelRect == null) return;
            panelRect.sizeDelta = new Vector2(
                PortraitUiMetrics.ContainedWidth(transform as RectTransform, 780f), 720f);
        }

        private void Build()
        {
            if (resultRoot != null) return;

            var rootRect = RuntimeUiFactory.Rect("Run Result Root", transform);
            RuntimeUiFactory.Stretch(rootRect, 0f, 0f, 0f, 0f);
            resultRoot = rootRect.gameObject;

            var scrim = RuntimeUiFactory.Image("Result Scrim", rootRect,
                new Color(.025f, .02f, .016f, .88f));
            RuntimeUiFactory.Stretch(scrim.rectTransform, 0f, 0f, 0f, 0f);
            scrim.raycastTarget = true;

            var panel = RuntimeUiFactory.Image("Result Panel", rootRect, JoseonUiPalette.Hanji);
            panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(780f, 720f);
            panel.raycastTarget = true;

            Border("Result Border Top", panel.transform, new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 8f));
            Border("Result Border Bottom", panel.transform, new Vector2(0f, 0f),
                new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 8f));
            Border("Result Border Left", panel.transform, new Vector2(0f, 0f),
                new Vector2(0f, 1f), new Vector2(8f, 0f), new Vector2(8f, 0f));
            Border("Result Border Right", panel.transform, new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(-8f, 0f), new Vector2(8f, 0f));

            title = RuntimeUiFactory.Text("Result Title", panel.transform, string.Empty, 54f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            SetRect(title.rectTransform, new Vector2(0f, 238f), new Vector2(660f, 92f));
            title.fontStyle = FontStyles.Bold;
            title.color = JoseonUiPalette.AppraisalBorder;

            summary = RuntimeUiFactory.Text("Result Summary", panel.transform, string.Empty, 31f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            SetRect(summary.rectTransform, new Vector2(0f, 10f), new Vector2(640f, 350f));
            summary.color = JoseonUiPalette.HanjiInk;

            restartButton = RuntimeUiFactory.Button("Restart Button", panel.transform,
                JoseonUiPalette.AppraisalResult);
            SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0f, -252f),
                new Vector2(370f, 86f));
            restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            restartLabel = RuntimeUiFactory.Text("Restart Label", restartButton.transform, "다시 시작", 30f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            RuntimeUiFactory.Stretch(restartLabel.rectTransform, 12f, 8f, 12f, 8f);
            restartLabel.fontStyle = FontStyles.Bold;
            restartLabel.color = JoseonUiPalette.DarkPanelText;

            ApplyPortraitLayout();
            resultRoot.SetActive(false);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Border(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            var image = RuntimeUiFactory.Image(name, parent, JoseonUiPalette.AppraisalBorder);
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.raycastTarget = false;
        }
    }
}
