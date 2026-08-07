using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class WeaponResearchPresenter : MonoBehaviour
    {
        [SerializeField] private Button[] weaponButtons;
        [SerializeField] private Button[] styleButtons;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text masteryText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private Image selectedWeaponIcon;
        [SerializeField] private RectTransform masteryProgressFill;
        private MetaGameSession session;
        private Action refreshHeader;
        private int selectedWeaponIndex;

        public int WeaponCountForTests => WeaponRoster.All.Count;
        public int StyleCountForTests => styleButtons?.Length ?? 0;

        public void Build()
        {
            if (transform.Find("Research Title") != null) return;
            titleText = LobbyUiFactory.Text("Research Title", transform, "무기 연구", 34f,
                TextAlignmentOptions.Left, true);
            titleText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(titleText.rectTransform, new Vector2(.22f, .90f), new Vector2(.96f, .985f),
                Vector2.zero, Vector2.zero);

            selectedWeaponIcon = LobbyUiFactory.Image("Selected Weapon Icon", transform, Color.white);
            selectedWeaponIcon.preserveAspect = true;
            LobbyUiFactory.Anchor(selectedWeaponIcon.rectTransform, new Vector2(.055f, .82f), new Vector2(.195f, .965f),
                Vector2.zero, Vector2.zero);

            var weaponGrid = LobbyUiFactory.Rect("Weapon Grid", transform);
            LobbyUiFactory.Anchor(weaponGrid, new Vector2(.04f, .58f), new Vector2(.96f, .75f),
                Vector2.zero, Vector2.zero);
            var grid = weaponGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(145f, 66f);
            grid.spacing = new Vector2(10f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            weaponButtons = new Button[WeaponRoster.All.Count];
            for (var index = 0; index < weaponButtons.Length; index++)
                weaponButtons[index] = LobbyUiFactory.Button("Weapon " + index, weaponGrid,
                    LobbyViewModels.WeaponName(WeaponRoster.All[index]), 19f,
                    LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);

            masteryText = LobbyUiFactory.Text("Mastery Summary", transform, string.Empty, 21f);
            masteryText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(masteryText.rectTransform, new Vector2(.22f, .825f), new Vector2(.94f, .895f),
                Vector2.zero, Vector2.zero);

            var progress = LobbyUiFactory.Image("Mastery Progress", transform, new Color(.04f, .055f, .05f, 1f));
            LobbyUiFactory.Anchor(progress.rectTransform, new Vector2(.22f, .775f), new Vector2(.94f, .815f),
                Vector2.zero, Vector2.zero);
            var fill = LobbyUiFactory.Image("Mastery Progress Fill", progress.transform,
                new Color(.18f, .76f, .39f, 1f));
            masteryProgressFill = fill.rectTransform;
            LobbyUiFactory.Anchor(masteryProgressFill, Vector2.zero, new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero);

            var styles = LobbyUiFactory.Rect("Style Cards", transform);
            LobbyUiFactory.Anchor(styles, new Vector2(.05f, .10f), new Vector2(.95f, .555f),
                Vector2.zero, Vector2.zero);
            var layout = styles.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            styleButtons = new Button[3];
            for (var index = 0; index < styleButtons.Length; index++)
                styleButtons[index] = LobbyUiFactory.Button("Style " + index, styles, string.Empty, 20f,
                    LobbyUiFactory.Crimson, LobbyUiFactory.HanjiLight);

            feedbackText = LobbyUiFactory.Text("Research Feedback", transform, string.Empty, 19f);
            feedbackText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.05f, .015f), new Vector2(.95f, .085f),
                Vector2.zero, Vector2.zero);
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            for (var index = 0; index < weaponButtons.Length; index++)
            {
                var captured = index;
                weaponButtons[index].onClick.RemoveAllListeners();
                weaponButtons[index].onClick.AddListener(() => SelectWeapon(captured));
            }
            for (var index = 0; index < styleButtons.Length; index++)
            {
                var captured = index;
                styleButtons[index].onClick.RemoveAllListeners();
                styleButtons[index].onClick.AddListener(() => ActivateStyle(captured));
            }
            Refresh();
        }

        public void ConfigureCatalog(WeaponCatalogAsset value) => weaponCatalog = value;

        public string SelectedStyleStateForTests(int index) => StateFor(index);
        public void ActivateStyleForTests(int index) => ActivateStyle(index);
        public void SelectWeaponForTests(int index) => SelectWeapon(index);

        private void SelectWeapon(int index)
        {
            selectedWeaponIndex = Mathf.Clamp(index, 0, WeaponRoster.All.Count - 1);
            feedbackText.text = string.Empty;
            Refresh();
        }

        private void ActivateStyle(int styleIndex)
        {
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            if (styleIndex < 0 || styleIndex >= styles.Count) return;
            var style = styles[styleIndex];
            if (styleIndex == 2 && !session.Data.UnlockedWeaponStyles.Contains(styles[1].LegacyPathId.Value))
            {
                feedbackText.text = "2단계 연구 완료 시 해금";
                Refresh();
                return;
            }
            if (!style.IsBase && !session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value))
            {
                var purchase = session.PurchaseStyle(weaponId, style.LegacyPathId);
                if (!purchase.Success)
                {
                    feedbackText.text = purchase.Error == ProgressionError.InsufficientMastery
                        ? "숙련도가 부족합니다. 이 무기로 막타를 더 기록하세요."
                        : purchase.Error == ProgressionError.InsufficientCoins
                            ? "엽전이 부족합니다."
                            : "아직 해금할 수 없습니다.";
                    Refresh();
                    return;
                }
            }

            var loadout = session.ActiveLoadout;
            var equipped = new Dictionary<WeaponId, WeaponLegacyPathId>(loadout.Styles);
            if (style.IsBase) equipped.Remove(weaponId);
            else equipped[weaponId] = style.LegacyPathId;
            var updated = new PatrolLoadout(loadout.Name, loadout.StartingWeapon, equipped, loadout.DifficultyId);
            var saved = session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex, updated);
            feedbackText.text = saved.Success ? "운용법을 장착했습니다." : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private string StateFor(int styleIndex)
        {
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var style = WeaponMasteryCatalog.StylesFor(weaponId)[styleIndex];
            var equipped = session.ActiveLoadout.StyleFor(weaponId);
            if (style.IsBase) return string.IsNullOrEmpty(equipped.Value) ? "장착 중" : "기본식";
            if (equipped.Equals(style.LegacyPathId)) return "장착 중";
            if (style.IsStarterUnlocked &&
                session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value))
                return "처음부터 해금";
            if (session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value)) return "해금 완료";
            if (styleIndex == 2 &&
                !session.Data.UnlockedWeaponStyles.Contains(
                    WeaponMasteryCatalog.StylesFor(weaponId)[1].LegacyPathId.Value))
                return "2단계 연구 완료 시 해금";
            var mastery = session.Data.WeaponMasteryPoints.TryGetValue(weaponId.Value, out var points) ? points : 0;
            if (mastery < style.RequiredMastery) return "연구 중";
            return session.Data.Coins >= style.CoinCost ? "해금 가능" : "엽전 부족";
        }

        private void Refresh()
        {
            if (session == null) return;
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var mastery = session.Data.WeaponMasteryPoints.TryGetValue(weaponId.Value, out var value) ? value : 0;
            titleText.text = $"{LobbyViewModels.WeaponName(weaponId)} 연구";
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            selectedWeaponIcon.sprite = weaponCatalog != null && weaponCatalog.TryGet(weaponId, out var definition)
                ? definition.UiIcon != null ? definition.UiIcon : definition.PresentationSprites.FirstOrDefault()
                : null;
            selectedWeaponIcon.enabled = selectedWeaponIcon.sprite != null;
            var firstUnlocked = session.Data.UnlockedWeaponStyles.Contains(styles[1].LegacyPathId.Value);
            var nextRequired = firstUnlocked ? styles[2].RequiredMastery : styles[1].RequiredMastery;
            var starterPaths = styles.Skip(1).All(style => style.IsStarterUnlocked);
            masteryText.text = starterPaths
                ? "독니와 월식 · 처음부터 해금"
                : $"숙련도 {mastery:N0} / {nextRequired:N0}";
            masteryProgressFill.anchorMax = new Vector2(
                starterPaths ? 1f : Mathf.Clamp01(nextRequired <= 0 ? 1f : mastery / (float)nextRequired), 1f);
            masteryProgressFill.offsetMin = Vector2.zero;
            masteryProgressFill.offsetMax = Vector2.zero;
            for (var index = 0; index < styleButtons.Length; index++)
            {
                var style = styles[index];
                string line;
                if (style.IsBase)
                    line = $"{style.DisplayName} · {StateFor(index)}\n처음부터 사용 가능";
                else if (style.IsStarterUnlocked)
                    line = $"{style.DisplayName} · {StateFor(index)}\n처음부터 해금 · 눌러서 장착";
                else if (index == 2 && !firstUnlocked)
                    line = $"{style.DisplayName} · 잠김\n2단계 연구 완료 시 해금";
                else
                    line = $"{style.DisplayName} · {StateFor(index)} ({mastery:N0}/{style.RequiredMastery:N0})\n" +
                           $"{style.Benefit} / {style.Tradeoff}";
                styleButtons[index].GetComponentInChildren<TMP_Text>().text = line;
            }
        }
    }
}
