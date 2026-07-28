using System.Collections;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_creates_one_portrait_hud_with_a_safe_area_container()
        {
            yield return DestroyBootstraps();
            var root = new GameObject("UI Test");
            var bootstrap = root.AddComponent<FirstPlayableUiBootstrap>();
            yield return null;

            var canvas = root.GetComponentInChildren<Canvas>(true);
            var scaler = root.GetComponentInChildren<CanvasScaler>(true);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(root.GetComponentInChildren<CombatHudPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<WeaponRackPresenter>(true), Is.Not.Null);

            bootstrap.ApplySafeArea(new Rect(0f, 120f, 1000f, 1760f), new Vector2(1000f, 2000f));
            Assert.That(bootstrap.SafeAreaContainer.anchorMin, Is.EqualTo(new Vector2(0f, .06f)));
            Assert.That(bootstrap.SafeAreaContainer.anchorMax, Is.EqualTo(new Vector2(1f, .94f)));

            new GameObject("Duplicate UI Test").AddComponent<FirstPlayableUiBootstrap>();
            yield return null;
            Assert.That(Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(1));
            Assert.That(bootstrap.BoundController, Is.Null);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Weapon_rack_hides_a_null_icon_without_creating_extra_slots()
        {
            var root = new GameObject("Rack Test");
            var rack = root.AddComponent<WeaponRackPresenter>();
            rack.Render(new[] { new WeaponSlotView("hwando_flying_blade", "Hwando", 1, null) });
            yield return null;

            var icon = System.Array.Find(root.GetComponentsInChildren<Image>(true), image => image.name == "Icon");
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.enabled, Is.False);
            rack.Render(new[] { new WeaponSlotView("hwando_flying_blade", "Hwando", 1, null) });
            Assert.That(root.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(3));
            Object.Destroy(root);
        }

        private static IEnumerator DestroyBootstraps()
        {
            foreach (var bootstrap in Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(bootstrap.gameObject);
            yield return null;
        }
    }
}
