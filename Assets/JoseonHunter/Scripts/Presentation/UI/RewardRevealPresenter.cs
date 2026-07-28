using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class RewardRevealPresenter : MonoBehaviour
    {
        private const float NewWeaponDuration = .6f;

        private GameObject root;
        private Image overlay;
        private Image icon;
        private TextMeshProUGUI glyph;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private Coroutine revealRoutine;

        public static int IntensityFor(ProgressionRewardKind kind) => kind switch
        {
            ProgressionRewardKind.Evolution => 100,
            ProgressionRewardKind.NewWeapon => 90,
            ProgressionRewardKind.WeaponLevel => 80,
            _ => 70
        };

        public void Play(ProgressionRewardEvent reward)
        {
            Build();
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            title.text = reward.DisplayName;
            detail.text = reward.ChangeSummary;
            icon.sprite = reward.Icon;
            icon.enabled = reward.Icon != null;
            glyph.gameObject.SetActive(reward.Icon == null);
            glyph.text = GlyphFor(reward.Kind);
            root.SetActive(true);
            revealRoutine = StartCoroutine(PlayRoutine(reward.Kind));
        }

        public void HideImmediately()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            if (root != null) root.SetActive(false);
        }

        private void OnDisable() => HideImmediately();

        private IEnumerator PlayRoutine(ProgressionRewardKind kind)
        {
            var duration = kind == ProgressionRewardKind.NewWeapon ? NewWeaponDuration : .45f;
            var intensity = IntensityFor(kind) / 100f;
            overlay.color = new Color(.025f, .03f, .045f, kind == ProgressionRewardKind.Evolution ? .72f : 0f);
            var canvasGroup = root.GetComponent<CanvasGroup>();
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Sin(progress * Mathf.PI) * intensity;
                yield return null;
            }

            HideImmediately();
        }

        private void Build()
        {
            if (root != null) return;

            root = RuntimeUiFactory.Rect("Reward Reveal", transform).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            root.AddComponent<CanvasGroup>();
            overlay = RuntimeUiFactory.Image("Evolution Overlay", root.transform, Color.clear);
            RuntimeUiFactory.Stretch(overlay.rectTransform, 0f, 0f, 0f, 0f);
            var panel = RuntimeUiFactory.Image("Reward Panel", root.transform, JoseonUiPalette.Ink);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(720f, 220f);
            icon = RuntimeUiFactory.Image("Icon", panel.transform, Color.white);
            Position(icon.rectTransform, new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(128f, 128f), new Vector2(0f, .5f));
            icon.preserveAspect = true;
            glyph = RuntimeUiFactory.Text("Glyph", panel.transform, string.Empty, 78f, TextAlignmentOptions.Center);
            Position(glyph.rectTransform, new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(128f, 128f), new Vector2(0f, .5f));
            title = RuntimeUiFactory.Text("Title", panel.transform, string.Empty, 36f, TextAlignmentOptions.Left);
            Position(title.rectTransform, new Vector2(0f, .5f), new Vector2(206f, 34f), new Vector2(470f, 54f), new Vector2(0f, .5f));
            detail = RuntimeUiFactory.Text("Detail", panel.transform, string.Empty, 24f, TextAlignmentOptions.Left);
            Position(detail.rectTransform, new Vector2(0f, .5f), new Vector2(206f, -34f), new Vector2(470f, 42f), new Vector2(0f, .5f));
            root.SetActive(false);
        }

        private static string GlyphFor(ProgressionRewardKind kind) => kind == ProgressionRewardKind.Evolution ? "進" :
            kind == ProgressionRewardKind.NewWeapon ? "武" : kind == ProgressionRewardKind.WeaponLevel ? "力" : "福";

        private static void Position(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
