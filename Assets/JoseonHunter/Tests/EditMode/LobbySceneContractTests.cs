using System.Linq;
using JoseonHunter.Presentation.UI.Lobby;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
                             "Stage Content", "Coin Icon", "Patrol Hero", "Patrol Hero Shadow",
                             "Starting Weapon Selector", "Weapon Selection Overlay"
                         })
                    Assert.That(transforms.Any(item => item.name == required), Is.True, required);

                var background = transforms.Single(item => item.name == "Lobby Background").GetComponent<Image>();
                Assert.That(background.sprite, Is.Not.Null);
                foreach (var removed in new[]
                         {
                             "Hero Viewport", "Hero Art", "Hero Shade", "Hero Name", "Hero Subtitle",
                             "Previous Preset", "Next Preset", "Save Preset", "Preset", "Difficulty", "Record",
                             "Previous Weapon", "Next Weapon", "Current Weapon Icon"
                         })
                    Assert.That(transforms.Any(item => item.name == removed), Is.False, removed);

                var navigation = transforms.Single(item => item.name == "Bottom Navigation");
                var navigationLabels = navigation.GetComponentsInChildren<Button>(true)
                    .Select(button => button.GetComponentInChildren<TMP_Text>(true).text).ToArray();
                Assert.That(navigationLabels, Is.EquivalentTo(new[] { "무기 연구", "출전", "수련" }));

                var hero = transforms.Single(item => item.name == "Patrol Hero").GetComponent<Image>();
                Assert.That(hero.sprite, Is.Not.Null);
                Assert.That(hero.preserveAspect, Is.True);
                Assert.That(hero.transform.parent.name, Is.EqualTo("Patrol Panel"));
                foreach (var panelName in new[] { "Weapon Research Panel", "Patrol Panel", "Common Training Panel" })
                {
                    var panel = transforms.Single(item => item.name == panelName).GetComponent<Image>();
                    Assert.That(panel.color.a, Is.EqualTo(1f));
                }

                foreach (var button in canvas.GetComponentsInChildren<Button>(true))
                {
                    var image = button.GetComponent<Image>();
                    Assert.That(image.sprite, Is.Not.Null, button.name);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), button.name);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}
