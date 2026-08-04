using System;
using System.Collections;
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
    public sealed class PatrolPresenter : MonoBehaviour
    {
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text weaponText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button previousWeaponButton;
        [SerializeField] private Button nextWeaponButton;
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

            weaponIcon = LobbyUiFactory.Image("Current Weapon Icon", transform, Color.white);
            weaponIcon.preserveAspect = true;
            LobbyUiFactory.Anchor(weaponIcon.rectTransform, new Vector2(.34f, .49f), new Vector2(.66f, .78f),
                Vector2.zero, Vector2.zero);

            weaponText = LobbyUiFactory.Text("Starting Weapon", transform, string.Empty, 27f,
                TextAlignmentOptions.Center, true);
            weaponText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(weaponText.rectTransform, new Vector2(.19f, .39f), new Vector2(.81f, .49f),
                Vector2.zero, Vector2.zero);

            previousWeaponButton = SmallButton("Previous Weapon", "◀", .08f, .25f, .49f, .69f, 30f);
            nextWeaponButton = SmallButton("Next Weapon", "▶", .75f, .92f, .49f, .69f, 30f);
            patrolButton = LobbyUiFactory.Button("Start Patrol", transform, "출전", 31f,
                LobbyUiFactory.Gold, LobbyUiFactory.Ink);
            LobbyUiFactory.Anchor(patrolButton.GetComponent<RectTransform>(), new Vector2(.22f, .10f),
                new Vector2(.78f, .29f), Vector2.zero, Vector2.zero);
            feedbackText = LobbyUiFactory.Text("Patrol Feedback", transform, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.04f, .03f), new Vector2(.96f, .09f),
                Vector2.zero, Vector2.zero);
        }

        private Button SmallButton(string name, string label, float minX, float maxX, float minY, float maxY,
            float size = 20f)
        {
            var button = LobbyUiFactory.Button(name, transform, label, size);
            LobbyUiFactory.Anchor(button.GetComponent<RectTransform>(), new Vector2(minX, minY),
                new Vector2(maxX, maxY), Vector2.zero, Vector2.zero);
            return button;
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            LoadCurrentWeapon();
            previousWeaponButton.onClick.RemoveAllListeners();
            nextWeaponButton.onClick.RemoveAllListeners();
            patrolButton.onClick.RemoveAllListeners();
            previousWeaponButton.onClick.AddListener(() => CycleWeapon(-1));
            nextWeaponButton.onClick.AddListener(() => CycleWeapon(1));
            patrolButton.onClick.AddListener(StartPatrol);
            Refresh();
        }

        public void ConfigureCatalog(WeaponCatalogAsset value) => weaponCatalog = value;
        public void SelectStartingWeaponForTests(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            SaveCurrentWeapon();
            Refresh();
        }

        private void LoadCurrentWeapon()
        {
            var id = session.ActiveLoadout.StartingWeapon.Value;
            selectedWeapon = WeaponRoster.All.FirstOrDefault(weapon => weapon.Value == id);
            if (string.IsNullOrEmpty(selectedWeapon.Value)) selectedWeapon = WeaponId.HwandoFlyingBlade;
        }

        private void CycleWeapon(int direction)
        {
            var index = WeaponRoster.All.ToList().FindIndex(id => id.Equals(selectedWeapon));
            index = (index + direction + WeaponRoster.All.Count) % WeaponRoster.All.Count;
            selectedWeapon = WeaponRoster.All[index];
            SaveCurrentWeapon();
            Refresh();
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

        private void Refresh()
        {
            if (session == null) return;
            weaponText.text = LobbyViewModels.WeaponName(selectedWeapon);
            weaponIcon.sprite = weaponCatalog != null && weaponCatalog.TryGet(selectedWeapon, out var definition)
                ? definition.UiIcon != null ? definition.UiIcon : definition.PresentationSprites.FirstOrDefault()
                : null;
            weaponIcon.enabled = weaponIcon.sprite != null;
        }
    }
}
