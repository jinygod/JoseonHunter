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
            if (view == null) view = GetComponent<LobbyHomeView>();
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

        public void Refresh()
        {
            if (view == null || session == null) return;

            var selection = session.ActiveStageSelection;
            var loadout = session.ActiveLoadout;
            view.StageText.text = StageCatalog.TryGet(selection.StageId, out var stage)
                ? stage.DisplayName
                : "알 수 없는 지역";
            view.DifficultyText.text = LobbyViewModels.DifficultyName(selection.Difficulty);
            view.StartingWeaponText.text = LobbyViewModels.WeaponName(loadout.StartingWeapon);
            view.StartingWeaponIcon.sprite = weaponCatalog != null &&
                                           weaponCatalog.TryGet(loadout.StartingWeapon, out var weapon)
                ? weapon.UiIcon != null ? weapon.UiIcon : weapon.PresentationSprites.FirstOrDefault()
                : null;
        }
    }
}
