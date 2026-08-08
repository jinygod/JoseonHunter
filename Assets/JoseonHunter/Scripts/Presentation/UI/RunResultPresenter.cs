using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Progression;
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
        private TextMeshProUGUI lobbyReturnLabel;
        private Button lobbyReturnButton;

        public event Action LobbyReturnRequested;

        public void Render(FirstPlayableUiState state)
        {
            Build();
            resultRoot.SetActive(state.RunEnded);
            if (!state.RunEnded) return;

            title.text = state.Victory ? "승전" : "전투 종료";
            var lines = new List<string>
            {
                $"{state.StageDisplayName} · {state.DifficultyDisplayName}",
                $"생존 시간  {state.Elapsed:0.0}초",
                $"처치  {state.Kills}",
                $"도달 레벨  {state.Level}",
                $"획득 엽전  {state.SettlementCoinsEarned}",
                $"획득 숙련도  {state.SettlementMasteryEarned}"
            };
            if (state.SettlementFailed)
            {
                lines.Add("전투 기록을 저장하지 못했습니다. 다시 시도해 주세요.");
            }
            else
            {
                lines.Add($"계정 경험치 +{state.AccountExperienceEarned:N0}");
                if (state.AccountLevelAfter >= AccountProgression.MaximumLevel)
                    lines.Add($"계정 레벨 {AccountProgression.MaximumLevel} · 최대");
                else if (state.AccountLevelAfter != state.AccountLevelBefore)
                    lines.Add($"계정 레벨 {state.AccountLevelBefore} → {state.AccountLevelAfter}");
                foreach (var unlocked in state.NewlyUnlockedNodes) lines.Add(unlocked);
            }
            summary.text = string.Join("\n", lines);
            lobbyReturnLabel.text = state.SettlementFailed ? "다시 저장" : "로비로 돌아가기";
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

            lobbyReturnButton = RuntimeUiFactory.Button("Lobby Return Button", panel.transform,
                JoseonUiPalette.AppraisalResult);
            SetRect(lobbyReturnButton.GetComponent<RectTransform>(), new Vector2(0f, -252f),
                new Vector2(370f, 86f));
            lobbyReturnButton.onClick.AddListener(() => LobbyReturnRequested?.Invoke());
            lobbyReturnLabel = RuntimeUiFactory.Text("Lobby Return Label", lobbyReturnButton.transform,
                "로비로 돌아가기", 30f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            RuntimeUiFactory.Stretch(lobbyReturnLabel.rectTransform, 12f, 8f, 12f, 8f);
            lobbyReturnLabel.fontStyle = FontStyles.Bold;
            lobbyReturnLabel.color = JoseonUiPalette.DarkPanelText;
            JoseonButtonSkin.Apply(lobbyReturnButton, JoseonButtonStyle.Secondary, JoseonButtonIcon.Lobby);

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
