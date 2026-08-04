using System;
using System.Collections;
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
    public sealed class PatrolPresenter : MonoBehaviour
    {
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private Image heroImage;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text weaponText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button weaponSelectorButton;
        [SerializeField] private GameObject weaponSelectionOverlay;
        [SerializeField] private Button closeWeaponSelectionButton;
        [SerializeField] private Button patrolButton;

        private MetaGameSession session;
        private Action refreshHeader;
        private WeaponId selectedWeapon = WeaponId.HwandoFlyingBlade;

        public void Build()
        {
            if (transform.Find("Stage Name") != null) return;

            var title = LobbyUiFactory.Text("Stage Name", transform, "출전 준비", 34f,
                TextAlignmentOptions.Center, true);
            title.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Anchor(title.rectTransform, new Vector2(.04f, .84f), new Vector2(.96f, .96f),
                Vector2.zero, Vector2.zero);

            var shadowRect = new GameObject("Patrol Hero Shadow", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(PixelOvalGraphic)).GetComponent<RectTransform>();
            shadowRect.SetParent(transform, false);
            shadowRect.GetComponent<PixelOvalGraphic>().color = new Color(0f, 0f, 0f, .17f);
            shadowRect.GetComponent<PixelOvalGraphic>().raycastTarget = false;
            LobbyUiFactory.Anchor(shadowRect, new Vector2(.34f, .50f), new Vector2(.66f, .55f),
                Vector2.zero, Vector2.zero);

            heroImage = LobbyUiFactory.Image("Patrol Hero", transform, Color.white);
            heroImage.preserveAspect = true;
            LobbyUiFactory.Anchor(heroImage.rectTransform, new Vector2(.31f, .52f), new Vector2(.69f, .82f),
                Vector2.zero, Vector2.zero);

            weaponSelectorButton = LobbyUiFactory.Button("Starting Weapon Selector", transform, string.Empty, 23f);
            LobbyUiFactory.Anchor(weaponSelectorButton.GetComponent<RectTransform>(), new Vector2(.15f, .34f),
                new Vector2(.85f, .48f), Vector2.zero, Vector2.zero);
            var selectorLabel = weaponSelectorButton.GetComponentInChildren<TMP_Text>(true);
            selectorLabel.name = "Starting Weapon Name";
            selectorLabel.alignment = TextAlignmentOptions.MidlineLeft;
            LobbyUiFactory.Anchor(selectorLabel.rectTransform, new Vector2(.35f, .12f), new Vector2(.86f, .88f),
                Vector2.zero, Vector2.zero);
            var caption = LobbyUiFactory.Text("Starting Weapon Caption", weaponSelectorButton.transform,
                "시작 무기", 15f, TextAlignmentOptions.MidlineLeft);
            caption.color = LobbyUiFactory.Hanji;
            LobbyUiFactory.Anchor(caption.rectTransform, new Vector2(.18f, .58f), new Vector2(.35f, .91f),
                Vector2.zero, Vector2.zero);
            var chevron = LobbyUiFactory.Text("Starting Weapon Chevron", weaponSelectorButton.transform,
                "〉", 28f, TextAlignmentOptions.Center, true);
            chevron.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Anchor(chevron.rectTransform, new Vector2(.86f, .1f), new Vector2(.98f, .9f),
                Vector2.zero, Vector2.zero);
            weaponIcon = LobbyUiFactory.Image("Starting Weapon Icon", weaponSelectorButton.transform, Color.white);
            weaponIcon.preserveAspect = true;
            LobbyUiFactory.Anchor(weaponIcon.rectTransform, new Vector2(.03f, .12f), new Vector2(.17f, .88f),
                Vector2.zero, Vector2.zero);
            weaponText = selectorLabel;

            patrolButton = LobbyUiFactory.Button("Start Patrol", transform, "출전", 31f,
                LobbyUiFactory.Gold, LobbyUiFactory.Ink);
            LobbyUiFactory.Anchor(patrolButton.GetComponent<RectTransform>(), new Vector2(.22f, .10f),
                new Vector2(.78f, .29f), Vector2.zero, Vector2.zero);
            feedbackText = LobbyUiFactory.Text("Patrol Feedback", transform, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.04f, .03f), new Vector2(.96f, .09f),
                Vector2.zero, Vector2.zero);

            BuildWeaponSelectionOverlay();
        }

        private void BuildWeaponSelectionOverlay()
        {
            var overlay = LobbyUiFactory.Image("Weapon Selection Overlay", transform,
                new Color(.02f, .025f, .04f, .96f), true);
            LobbyUiFactory.Stretch(overlay.rectTransform, 8f, 8f, 8f, 8f);
            weaponSelectionOverlay = overlay.gameObject;

            var panel = LobbyUiFactory.Image("Weapon Selection Panel", overlay.transform,
                new Color(.08f, .065f, .08f, 1f));
            LobbyUiFactory.Anchor(panel.rectTransform, new Vector2(.06f, .12f), new Vector2(.94f, .88f),
                Vector2.zero, Vector2.zero);
            var heading = LobbyUiFactory.Text("Weapon Selection Title", panel.transform, "시작 무기 선택", 28f,
                TextAlignmentOptions.Center, true);
            heading.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Anchor(heading.rectTransform, new Vector2(.08f, .84f), new Vector2(.92f, .97f),
                Vector2.zero, Vector2.zero);

            closeWeaponSelectionButton = LobbyUiFactory.Button("Close Weapon Selection", panel.transform, "닫기", 20f);
            LobbyUiFactory.Anchor(closeWeaponSelectionButton.GetComponent<RectTransform>(), new Vector2(.34f, .04f),
                new Vector2(.66f, .14f), Vector2.zero, Vector2.zero);

            var gridRect = LobbyUiFactory.Rect("Weapon Grid", panel.transform);
            LobbyUiFactory.Anchor(gridRect, new Vector2(.07f, .17f), new Vector2(.93f, .82f),
                Vector2.zero, Vector2.zero);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(246f, 110f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var weaponId in WeaponRoster.All)
            {
                var option = LobbyUiFactory.Button($"Weapon Option {weaponId.Value}", gridRect,
                    LobbyViewModels.WeaponName(weaponId), 18f);
                var label = option.GetComponentInChildren<TMP_Text>(true);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                LobbyUiFactory.Anchor(label.rectTransform, new Vector2(.32f, .08f), new Vector2(.96f, .92f),
                    Vector2.zero, Vector2.zero);
                var icon = LobbyUiFactory.Image("Weapon Option Icon", option.transform, Color.white);
                icon.preserveAspect = true;
                LobbyUiFactory.Anchor(icon.rectTransform, new Vector2(.04f, .12f), new Vector2(.28f, .88f),
                    Vector2.zero, Vector2.zero);
            }

            weaponSelectionOverlay.SetActive(false);
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            LoadCurrentWeapon();

            weaponSelectorButton.onClick.RemoveAllListeners();
            closeWeaponSelectionButton.onClick.RemoveAllListeners();
            patrolButton.onClick.RemoveAllListeners();
            weaponSelectorButton.onClick.AddListener(OpenWeaponSelection);
            closeWeaponSelectionButton.onClick.AddListener(CloseWeaponSelection);
            patrolButton.onClick.AddListener(StartPatrol);
            BindWeaponOptions();
            Refresh();
        }

        public void ConfigureCatalog(WeaponCatalogAsset value) => weaponCatalog = value;

        public void SelectStartingWeaponForTests(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            SaveCurrentWeapon();
            Refresh();
        }

        private void BindWeaponOptions()
        {
            foreach (var weaponId in WeaponRoster.All)
            {
                var id = weaponId;
                var optionTransform = weaponSelectionOverlay.transform.Find(
                    $"Weapon Selection Panel/Weapon Grid/Weapon Option {id.Value}");
                if (optionTransform == null) continue;
                var option = optionTransform.GetComponent<Button>();
                option.onClick.RemoveAllListeners();
                option.onClick.AddListener(() => SelectWeapon(id));
                var icon = optionTransform.Find("Weapon Option Icon")?.GetComponent<Image>();
                if (icon == null) continue;
                icon.sprite = ResolveWeaponSprite(id);
                icon.enabled = icon.sprite != null;
            }
        }

        private void LoadCurrentWeapon()
        {
            var id = session.ActiveLoadout.StartingWeapon.Value;
            selectedWeapon = WeaponRoster.All.FirstOrDefault(weapon => weapon.Value == id);
            if (string.IsNullOrEmpty(selectedWeapon.Value)) selectedWeapon = WeaponId.HwandoFlyingBlade;
        }

        private void OpenWeaponSelection()
        {
            weaponSelectionOverlay.transform.SetAsLastSibling();
            weaponSelectionOverlay.SetActive(true);
        }

        private void CloseWeaponSelection() => weaponSelectionOverlay.SetActive(false);

        private void SelectWeapon(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            if (!SaveCurrentWeapon()) return;
            Refresh();
            CloseWeaponSelection();
        }

        private bool SaveCurrentWeapon()
        {
            var current = session.ActiveLoadout;
            var loadout = new PatrolLoadout(current.Name, selectedWeapon, current.Styles, current.DifficultyId);
            var result = session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex, loadout);
            feedbackText.text = result.Success ? string.Empty : "무기를 저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            return result.Success;
        }

        private void StartPatrol()
        {
            if (session.Router.IsRouting || !SaveCurrentWeapon()) return;
            patrolButton.interactable = false;
            session.SetPendingDestination("Gameplay");
            StartCoroutine(LoadBootstrap());
        }

        private IEnumerator LoadBootstrap()
        {
            yield return session.Router.LoadBootstrap();
            if (patrolButton != null) patrolButton.interactable = true;
        }

        private Sprite ResolveWeaponSprite(WeaponId id)
        {
            if (weaponCatalog == null || !weaponCatalog.TryGet(id, out var definition)) return null;
            return definition.UiIcon != null ? definition.UiIcon : definition.PresentationSprites.FirstOrDefault();
        }

        private void Refresh()
        {
            if (session == null) return;
            weaponText.text = LobbyViewModels.WeaponName(selectedWeapon);
            weaponIcon.sprite = ResolveWeaponSprite(selectedWeapon);
            weaponIcon.enabled = weaponIcon.sprite != null;
        }

        private void OnDisable()
        {
            if (weaponSelectionOverlay != null) weaponSelectionOverlay.SetActive(false);
        }
    }
}
