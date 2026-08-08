using System;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class AbandonRunPresenter : MonoBehaviour
    {
        private GameObject root;
        private RectTransform panelRect;
        private AudioSettingsPresenter audioSettings;

        public event Action Confirmed;
        public event Action Cancelled;
        public bool IsOpen => root != null && root.activeSelf;

        public void Open()
        {
            Build();
            root.SetActive(true);
        }

        public void CloseImmediately()
        {
            if (root != null) root.SetActive(false);
        }

        public void ApplyPortraitLayout()
        {
            if (panelRect != null)
                panelRect.sizeDelta = new Vector2(
                    PortraitUiMetrics.ContainedWidth(transform as RectTransform, 760f), 680f);
        }

        private void Build()
        {
            if (root != null) return;
            var rootRect = RuntimeUiFactory.Rect("Abandon Run Root", transform);
            RuntimeUiFactory.Stretch(rootRect, 0f, 0f, 0f, 0f);
            root = rootRect.gameObject;
            var scrim = RuntimeUiFactory.Image("Abandon Scrim", rootRect, new Color(.025f, .02f, .016f, .9f));
            RuntimeUiFactory.Stretch(scrim.rectTransform, 0f, 0f, 0f, 0f);
            scrim.raycastTarget = true;

            var panel = RuntimeUiFactory.Image("Abandon Panel", rootRect, JoseonUiPalette.Hanji);
            panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, 680f);
            panel.raycastTarget = true;
            PremiumPixelUiSkin.ApplyFrame(panel, PremiumFrame.Panel);

            var title = RuntimeUiFactory.Text("Abandon Title", panel.transform, "일시정지", 43f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            SetRect(title.rectTransform, new Vector2(0f, 258f), new Vector2(650f, 70f));
            title.color = JoseonUiPalette.Hanji;

            var message = RuntimeUiFactory.Text("Abandon Message", panel.transform,
                "전투를 계속하거나 현재 성과를 저장하고 로비로 돌아갈 수 있습니다.", 27f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            SetRect(message.rectTransform, new Vector2(0f, 176f), new Vector2(640f, 80f));
            message.color = JoseonUiPalette.Hanji;

            var divider = RuntimeUiFactory.Image("Pause Divider", panel.transform, Color.white);
            divider.sprite = Resources.Load<Sprite>("UI/PremiumJoseon/divider_gold");
            divider.preserveAspect = true;
            divider.raycastTarget = false;
            SetRect(divider.rectTransform, new Vector2(0f, 118f), new Vector2(430f, 28f));

            var audioRoot = RuntimeUiFactory.Rect("Pause Audio Settings", panel.transform);
            SetRect(audioRoot, new Vector2(0f, -8f), new Vector2(640f, 250f));
            audioSettings = audioRoot.gameObject.AddComponent<AudioSettingsPresenter>();
            audioSettings.Initialize(MetaGameSession.EnsureExists(), false, AudioSettingsTheme.Night);

            var cancel = RuntimeUiFactory.Button("Continue Combat Button", panel.transform, JoseonUiPalette.Ink);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0f, -205f), new Vector2(430f, 72f));
            cancel.onClick.AddListener(() =>
            {
                audioSettings.CommitPending();
                Cancelled?.Invoke();
            });
            ButtonLabel(cancel, "계속하기");
            JoseonButtonSkin.Apply(cancel, JoseonButtonStyle.Primary, JoseonButtonIcon.Continue);

            var confirm = RuntimeUiFactory.Button("Confirm Return Button", panel.transform,
                JoseonUiPalette.AppraisalResult);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0f, -292f), new Vector2(430f, 66f));
            confirm.onClick.AddListener(() =>
            {
                audioSettings.CommitPending();
                Confirmed?.Invoke();
            });
            ButtonLabel(confirm, "로비로 돌아가기");
            JoseonButtonSkin.Apply(confirm, JoseonButtonStyle.Secondary, JoseonButtonIcon.Lobby);
            ApplyPortraitLayout();
            root.SetActive(false);
        }

        private static void ButtonLabel(Button button, string value)
        {
            var label = RuntimeUiFactory.Text("Label", button.transform, value, 27f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            label.color = JoseonUiPalette.Hanji;
            RuntimeUiFactory.Stretch(label.rectTransform, 10f, 6f, 10f, 6f);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
