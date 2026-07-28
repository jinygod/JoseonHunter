using System.Collections;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_creates_portrait_hud_and_weapon_rack()
        {
            var root = new GameObject("UI Test");
            root.AddComponent<FirstPlayableUiBootstrap>();
            yield return null;

            var canvas = root.GetComponentInChildren<Canvas>(true);
            var scaler = root.GetComponentInChildren<CanvasScaler>(true);

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(root.GetComponentInChildren<CombatHudPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<WeaponRackPresenter>(true), Is.Not.Null);
            Object.Destroy(root);
        }
    }
}
