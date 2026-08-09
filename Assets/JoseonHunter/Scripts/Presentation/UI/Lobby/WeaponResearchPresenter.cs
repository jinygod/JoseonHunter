using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class WeaponResearchPresenter : MonoBehaviour
    {
        [SerializeField] private ResearchPageView view;
        [SerializeField] private Button[] weaponButtons;
        [SerializeField] private Button[] styleButtons;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text masteryText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private Image selectedWeaponIcon;
        [SerializeField] private RectTransform masteryProgressFill;

        private readonly Dictionary<Button, UnityAction> ownedActions = new();
        private MetaGameSession session;
        private Action refreshHeader;
        private int selectedWeaponIndex;

        public int WeaponCountForTests => WeaponRoster.All.Count;
        public int StyleCountForTests => view != null ? view.Rows.Length : styleButtons?.Length ?? 0;

        public void ConfigureView(ResearchPageView authoredView) => view = authoredView;

        public void InitializeAuthored(MetaGameSession value, Action onChanged)
        {
            if (view == null || !view.HasRequiredBindings)
                throw new InvalidOperationException("ResearchPageView is incomplete.");
            UnbindOwnedListeners();
            BindAuthoredView();
            InitializeBoundView(value, onChanged);
        }

        // Transitional adapter for the pre-Task-7 runtime-built Lobby shell.
        // Task 7 removes this path when Lobby.unity owns a complete ResearchPageView hierarchy.
        public void Initialize(MetaGameSession value, Action onChanged) => InitializeLegacyRuntimeBuiltView(value, onChanged);

        public void InitializeLegacyRuntimeBuiltView(MetaGameSession value, Action onChanged)
        {
            UnbindOwnedListeners();
            Build();
            BindLegacyView();
            InitializeBoundView(value, onChanged);
        }

        public void Build()
        {
            if (transform.Find("Research Progress Backplate") != null) return;
            ArchiveLegacyLayoutIfPresent("Research Title", "Legacy Research Layout");
            titleText = LobbyUiFactory.Text("Research Title", transform, "무기 연구", 34f,
                TextAlignmentOptions.Left, true);
            titleText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(titleText.rectTransform, new Vector2(.22f, .86f), new Vector2(.96f, .96f),
                Vector2.zero, Vector2.zero);

            selectedWeaponIcon = LobbyUiFactory.Image("Selected Weapon Icon", transform, Color.white);
            selectedWeaponIcon.preserveAspect = true;
            LobbyUiFactory.Anchor(selectedWeaponIcon.rectTransform, new Vector2(.055f, .86f), new Vector2(.195f, .96f),
                Vector2.zero, Vector2.zero);
            var progressBackplate = LobbyUiFactory.Image("Research Progress Backplate", transform, Color.white);
            PremiumPixelUiSkin.ApplyFrame(progressBackplate, PremiumFrame.ContentBackplate);
            LobbyUiFactory.Anchor(progressBackplate.rectTransform, new Vector2(.05f, .77f), new Vector2(.95f, .855f),
                Vector2.zero, Vector2.zero);
            var weaponGrid = LobbyUiFactory.Rect("Weapon Grid", transform);
            LobbyUiFactory.Anchor(weaponGrid, new Vector2(.04f, .57f), new Vector2(.96f, .755f), Vector2.zero, Vector2.zero);
            var grid = weaponGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(145f, 66f);
            grid.spacing = new Vector2(10f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            weaponButtons = new Button[WeaponRoster.All.Count];
            for (var index = 0; index < weaponButtons.Length; index++)
            {
                weaponButtons[index] = LobbyUiFactory.Button("Weapon " + index, weaponGrid,
                    LobbyViewModels.WeaponName(WeaponRoster.All[index]), 19f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
                PremiumPixelUiSkin.ApplyFrame(weaponButtons[index].GetComponent<Image>(), PremiumFrame.SmallItem);
            }

            masteryText = LobbyUiFactory.Text("Mastery Summary", progressBackplate.transform, string.Empty, 21f);
            masteryText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(masteryText.rectTransform, new Vector2(.06f, .50f), new Vector2(.94f, .94f), Vector2.zero, Vector2.zero);
            var progress = LobbyUiFactory.Image("Mastery Progress", progressBackplate.transform, new Color(.04f, .055f, .05f, 1f));
            LobbyUiFactory.Anchor(progress.rectTransform, new Vector2(.06f, .15f), new Vector2(.94f, .42f), Vector2.zero, Vector2.zero);
            masteryProgressFill = LobbyUiFactory.Image("Mastery Progress Fill", progress.transform, new Color(.18f, .76f, .39f, 1f)).rectTransform;
            LobbyUiFactory.Anchor(masteryProgressFill, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            styleButtons = new Button[3];
            for (var index = 0; index < styleButtons.Length; index++)
            {
                styleButtons[index] = LobbyUiFactory.Button("Style Card " + index, transform, string.Empty, 20f, LobbyUiFactory.Crimson, LobbyUiFactory.HanjiLight);
                PremiumPixelUiSkin.ApplyFrame(styleButtons[index].GetComponent<Image>(), PremiumFrame.ContentBackplate);
                var label = styleButtons[index].GetComponentInChildren<TMP_Text>();
                label.fontSize = 18f;
                label.lineSpacing = -12f;
                LobbyUiFactory.Stretch(label.rectTransform, 12f, 6f, 12f, 6f);
                var maxY = .555f - index * .145f;
                LobbyUiFactory.Anchor(styleButtons[index].GetComponent<RectTransform>(), new Vector2(.05f, maxY - .135f), new Vector2(.95f, maxY), Vector2.zero, Vector2.zero);
            }
            feedbackText = LobbyUiFactory.Text("Research Feedback", transform, string.Empty, 19f);
            feedbackText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.05f, .03f), new Vector2(.95f, .11f), Vector2.zero, Vector2.zero);
        }

        private void ArchiveLegacyLayoutIfPresent(string marker, string archiveName)
        {
            if (transform.Find(marker) == null) return;
            var archive = LobbyUiFactory.Rect(archiveName, transform);
            var legacyChildren = new List<Transform>();
            foreach (Transform child in transform) if (child != archive) legacyChildren.Add(child);
            foreach (var child in legacyChildren) child.SetParent(archive, false);
            archive.gameObject.SetActive(false);
        }

        public void ConfigureCatalog(WeaponCatalogAsset value) => weaponCatalog = value;
        public string SelectedStyleStateForTests(int index) => StateFor(index);
        public void ActivateStyleForTests(int index) => ActivateStyle(index);
        public void SelectWeaponForTests(int index) => SelectWeapon(index);

        private void BindAuthoredView()
        {
            for (var index = 0; index < view.WeaponSelectors.Length; index++)
            {
                var captured = index;
                AddOwnedListener(view.WeaponSelectors[index].Button, () => SelectWeapon(captured));
            }
            for (var index = 0; index < view.Rows.Length; index++)
            {
                var captured = index;
                AddOwnedListener(view.Rows[index].ActionButton, () => ActivateStyle(captured));
            }
        }

        private void BindLegacyView()
        {
            for (var index = 0; index < weaponButtons.Length; index++)
            {
                var captured = index;
                AddOwnedListener(weaponButtons[index], () => SelectWeapon(captured));
            }
            for (var index = 0; index < styleButtons.Length; index++)
            {
                var captured = index;
                AddOwnedListener(styleButtons[index], () => ActivateStyle(captured));
            }
        }

        private void AddOwnedListener(Button button, UnityAction action)
        {
            button.onClick.AddListener(action);
            ownedActions.Add(button, action);
        }

        private void UnbindOwnedListeners()
        {
            foreach (var pair in ownedActions) pair.Key.onClick.RemoveListener(pair.Value);
            ownedActions.Clear();
        }

        private void InitializeBoundView(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            Refresh();
        }

        private void SelectWeapon(int index)
        {
            selectedWeaponIndex = Mathf.Clamp(index, 0, WeaponRoster.All.Count - 1);
            FeedbackText.text = string.Empty;
            Refresh();
        }

        private void ActivateStyle(int styleIndex)
        {
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            if (session == null || styleIndex < 0 || styleIndex >= styles.Count) return;
            var style = styles[styleIndex];
            if (IsEquipped(style)) return;
            if (styleIndex == 2 && !session.Data.UnlockedWeaponStyles.Contains(styles[1].LegacyPathId.Value))
            {
                FeedbackText.text = "2단계 연구 완료 시 해금";
                Refresh();
                return;
            }
            if (!style.IsBase && !session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value))
            {
                var purchase = session.PurchaseStyle(weaponId, style.LegacyPathId);
                if (!purchase.Success)
                {
                    FeedbackText.text = purchase.Error == ProgressionError.InsufficientMastery
                        ? "숙련도가 부족합니다. 해당 무기로 망령을 처치해 기록하세요."
                        : purchase.Error == ProgressionError.InsufficientCoins ? "전수가 부족합니다." : "아직 해금할 수 없습니다.";
                    Refresh();
                    return;
                }
            }

            var loadout = session.ActiveLoadout;
            var equipped = new Dictionary<WeaponId, WeaponLegacyPathId>(loadout.Styles);
            if (style.IsBase) equipped.Remove(weaponId); else equipped[weaponId] = style.LegacyPathId;
            var updated = new PatrolLoadout(loadout.Name, loadout.StartingWeapon, equipped, loadout.DifficultyId);
            var saved = session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex, updated);
            FeedbackText.text = saved.Success ? "운용법을 장착했습니다." : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private string StateFor(int styleIndex)
        {
            if (session == null) return string.Empty;
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            if (styleIndex < 0 || styleIndex >= styles.Count) return string.Empty;
            var style = styles[styleIndex];
            if (style.IsBase) return IsEquipped(style) ? "장착 중 : 기본형" : "기본형";
            if (IsEquipped(style)) return "장착 중";
            if (style.IsStarterUnlocked && session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value)) return "처음부터 해금";
            if (session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value)) return "해금 완료";
            if (styleIndex == 2 && !session.Data.UnlockedWeaponStyles.Contains(styles[1].LegacyPathId.Value)) return "2단계 연구 완료 시 해금";
            var mastery = MasteryFor(weaponId);
            if (mastery < style.RequiredMastery) return "연구 중";
            return session.Data.Coins >= style.CoinCost ? "해금 가능" : "전수 부족";
        }

        private void Refresh()
        {
            if (session == null) return;
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var mastery = MasteryFor(weaponId);
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            var firstUnlocked = session.Data.UnlockedWeaponStyles.Contains(styles[1].LegacyPathId.Value);
            var nextRequired = firstUnlocked ? styles[2].RequiredMastery : styles[1].RequiredMastery;
            var starterPaths = styles.Skip(1).All(style => style.IsStarterUnlocked);
            if (view != null)
            {
                RenderAuthored(weaponId, mastery, styles, firstUnlocked, nextRequired, starterPaths);
                return;
            }
            RenderLegacy(weaponId, mastery, styles, firstUnlocked, nextRequired, starterPaths);
        }

        private void RenderAuthored(WeaponId weaponId, int mastery, IReadOnlyList<WeaponMasteryStyleDefinition> styles,
            bool firstUnlocked, int nextRequired, bool starterPaths)
        {
            var icon = IconFor(weaponId);
            view.SelectedWeaponIcon.sprite = icon;
            view.SelectedWeaponIcon.enabled = icon != null;
            view.SelectedWeaponName.text = LobbyViewModels.WeaponName(weaponId);
            view.MasteryProgress.Render(starterPaths ? 1f : Mathf.Clamp01(nextRequired <= 0 ? 1f : mastery / (float)nextRequired),
                starterPaths ? "처음부터 해금" : $"숙련도 {mastery:N0} / {nextRequired:N0}");
            for (var index = 0; index < view.WeaponSelectors.Length; index++)
            {
                var selector = view.WeaponSelectors[index];
                var selectorId = WeaponRoster.All[index];
                selector.Caption.text = "무기 연구";
                selector.WeaponName.text = LobbyViewModels.WeaponName(selectorId);
                selector.Icon.sprite = IconFor(selectorId);
                selector.Icon.enabled = selector.Icon.sprite != null;
                LobbySelectionChrome.Apply(selector.Button, index == selectedWeaponIndex);
            }
            for (var index = 0; index < view.Rows.Length; index++) RenderRow(view.Rows[index], styles[index], index, mastery, firstUnlocked);
        }

        private void RenderRow(LobbyResearchRowView row, WeaponMasteryStyleDefinition style, int index, int mastery, bool firstUnlocked)
        {
            var owned = style.IsBase || session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value);
            var sequentiallyLocked = index == 2 && !firstUnlocked;
            var eligible = owned || (mastery >= style.RequiredMastery && session.Data.Coins >= style.CoinCost);
            var canAct = !sequentiallyLocked && eligible && !IsEquipped(style);
            var requirement = style.IsBase ? "처음부터 사용 가능" : style.IsStarterUnlocked ? "처음부터 해금" :
                $"숙련도 {mastery:N0}/{style.RequiredMastery:N0} · 전수 {style.CoinCost:N0}";
            row.Render(style.DisplayName, StateFor(index), $"{style.Benefit} / {style.Tradeoff}", requirement,
                owned ? "장착" : "해금", sequentiallyLocked || !eligible, canAct);
        }

        private void RenderLegacy(WeaponId weaponId, int mastery, IReadOnlyList<WeaponMasteryStyleDefinition> styles,
            bool firstUnlocked, int nextRequired, bool starterPaths)
        {
            titleText.text = $"{LobbyViewModels.WeaponName(weaponId)} 연구";
            selectedWeaponIcon.sprite = IconFor(weaponId);
            selectedWeaponIcon.enabled = selectedWeaponIcon.sprite != null;
            masteryText.text = starterPaths ? "처음부터 해금" : $"숙련도 {mastery:N0} / {nextRequired:N0}";
            masteryProgressFill.anchorMax = new Vector2(starterPaths ? 1f : Mathf.Clamp01(nextRequired <= 0 ? 1f : mastery / (float)nextRequired), 1f);
            masteryProgressFill.offsetMin = Vector2.zero;
            masteryProgressFill.offsetMax = Vector2.zero;
            for (var index = 0; index < styleButtons.Length; index++)
            {
                var style = styles[index];
                var requirement = style.IsBase ? "처음부터 사용 가능" : style.IsStarterUnlocked ? "처음부터 해금" :
                    $"숙련도 {mastery:N0}/{style.RequiredMastery:N0} · 전수 {style.CoinCost:N0}";
                styleButtons[index].GetComponentInChildren<TMP_Text>().text =
                    $"{style.DisplayName} · {StateFor(index)}\n{style.Benefit} / {style.Tradeoff}\n{requirement}";
            }
        }

        private int MasteryFor(WeaponId weaponId) =>
            session.Data.WeaponMasteryPoints.TryGetValue(weaponId.Value, out var points) ? points : 0;

        private bool IsEquipped(WeaponMasteryStyleDefinition style)
        {
            var equipped = session.ActiveLoadout.StyleFor(WeaponRoster.All[selectedWeaponIndex]);
            return style.IsBase ? string.IsNullOrEmpty(equipped.Value) : equipped.Equals(style.LegacyPathId);
        }

        private Sprite IconFor(WeaponId weaponId) => weaponCatalog != null && weaponCatalog.TryGet(weaponId, out var definition)
            ? definition.UiIcon != null ? definition.UiIcon : definition.PresentationSprites.FirstOrDefault() : null;

        private TMP_Text FeedbackText => view != null ? view.FeedbackText : feedbackText;
    }
}
