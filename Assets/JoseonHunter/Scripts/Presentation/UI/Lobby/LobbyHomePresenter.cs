using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using UnityEngine;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyHomePresenter : MonoBehaviour
    {
        [SerializeField] private LobbyHomeView view;

        private MetaGameSession session;
        private WeaponCatalogAsset weaponCatalog;

        private void Awake()
        {
            if (view == null) ConfigureView(GetComponent<LobbyHomeView>());
        }

        private void OnEnable()
        {
            if (session != null) Refresh();
        }

        public void Initialize(MetaGameSession metaGameSession, WeaponCatalogAsset catalog)
        {
            session = metaGameSession;
            weaponCatalog = catalog;
            Refresh();
        }

        public void ConfigureView(LobbyHomeView authoredView)
        {
            view = authoredView;
        }

        public void Refresh()
        {
            if (view == null || !view.HasRequiredBindings || session == null) return;

            var selection = session.ActiveStageSelection;
            var loadout = session.ActiveLoadout;
            view.StageText.text = StageCatalog.TryGet(selection.StageId, out var stage)
                ? stage.DisplayName
                : "알 수 없는 지역";
            view.DifficultyText.text = LobbyViewModels.DifficultyName(selection.Difficulty);
            view.StartingWeaponText.text = LobbyViewModels.WeaponName(loadout.StartingWeapon);
            var icon = weaponCatalog != null && weaponCatalog.TryGet(loadout.StartingWeapon, out var weapon)
                ? weapon.UiIcon != null ? weapon.UiIcon : weapon.PresentationSprites.FirstOrDefault()
                : null;
            view.StartingWeaponIcon.sprite = icon;
            view.StartingWeaponIcon.enabled = icon != null;
        }
    }
}
