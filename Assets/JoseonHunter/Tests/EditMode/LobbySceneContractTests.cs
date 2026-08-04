using System.Linq;
using JoseonHunter.Presentation.UI.Lobby;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class LobbySceneContractTests
    {
        private const string ScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";

        [Test]
        public void LobbySceneContainsAuthoredOpaqueThreeMenuShell()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.Any(root => root.name == "Lobby Camera"), Is.True);
                Assert.That(roots.Any(root => root.name == "Lobby Canvas"), Is.True);
                Assert.That(roots.Any(root => root.name == "EventSystem"), Is.True);

                var canvas = roots.Single(root => root.name == "Lobby Canvas");
                Assert.That(canvas.GetComponent<LobbyBootstrap>(), Is.Not.Null);
                Assert.That(canvas.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(roots.Single(root => root.name == "EventSystem").GetComponent<EventSystem>(), Is.Not.Null);

                var transforms = canvas.GetComponentsInChildren<Transform>(true);
                foreach (var required in new[]
                         {
                             "Lobby Background", "Safe Area", "Header", "Weapon Research Panel",
                             "Patrol Panel", "Common Training Panel", "Bottom Navigation",
                             "Hero Art", "Hero Shade", "Stage Content"
                         })
                    Assert.That(transforms.Any(item => item.name == required), Is.True, required);

                var background = transforms.Single(item => item.name == "Lobby Background").GetComponent<Image>();
                Assert.That(background.sprite, Is.Not.Null);
                var hero = transforms.Single(item => item.name == "Hero Art");
                Assert.That(hero.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(hero.GetComponent("LobbyHeroMotion"), Is.Not.Null);
                foreach (var panelName in new[] { "Weapon Research Panel", "Patrol Panel", "Common Training Panel" })
                {
                    var panel = transforms.Single(item => item.name == panelName).GetComponent<Image>();
                    Assert.That(panel.color.a, Is.EqualTo(1f));
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}
