using JoseonHunter.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class FirstPlayableUiBootstrap : MonoBehaviour
    {
        private FirstPlayableController boundController;
        private CombatHudPresenter combatHud;
        private WeaponRackPresenter weaponRack;

        public FirstPlayableController BoundController => boundController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindObjectOfType<FirstPlayableUiBootstrap>() != null) return;
            var root = new GameObject("First Playable UI");
            root.AddComponent<FirstPlayableUiBootstrap>();
        }

        private void Awake()
        {
            BuildCanvas();
            var hudRoot = RuntimeUiFactory.Rect("Combat HUD", transform);
            RuntimeUiFactory.Stretch(hudRoot, 0f, 0f, 0f, 0f);
            combatHud = hudRoot.gameObject.AddComponent<CombatHudPresenter>();
            combatHud.Build();
            var rackRoot = RuntimeUiFactory.Rect("Weapon Rack", transform);
            RuntimeUiFactory.Stretch(rackRoot, 0f, 0f, 0f, 0f);
            weaponRack = rackRoot.gameObject.AddComponent<WeaponRackPresenter>();
        }

        private void Update()
        {
            if (boundController == null) boundController = FindObjectOfType<FirstPlayableController>();
            if (boundController == null) return;
            var state = boundController.UiState;
            combatHud.Render(state);
            weaponRack.Render(state.Weapons);
        }

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
